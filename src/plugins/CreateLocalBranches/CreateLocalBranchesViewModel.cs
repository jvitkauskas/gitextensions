using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.CreateLocalBranches;

/// <summary>Strings of the Avalonia port of <see cref="CreateLocalBranchesForm"/>; ids match the form.</summary>
public sealed class CreateLocalBranchesStrings : ViewStrings
{
    public CreateLocalBranchesStrings()
        : base(nameof(CreateLocalBranchesForm))
    {
        Title = Add("$this", "Text", "Create local tracking branches");
        Remote = Add("label1", "Text", "Remote to create tracking branches for");
        Create = Add("button1", "Text", "Create local tracking branches");
    }

    public TranslatedText Title { get; }

    public TranslatedText Remote { get; }

    public TranslatedText Create { get; }
}

/// <summary>View model of the Avalonia port of <see cref="CreateLocalBranchesForm"/>.</summary>
public sealed partial class CreateLocalBranchesViewModel : DialogViewModel
{
    // Not translated, as in CreateLocalBranchesForm.
    public const string NoRemoteBranchesFound = "No remote branches found.";
    public const string BranchesCreatedFormat = "{0} local tracking branches have been created/updated.";
    public const string InformationCaption = "Information";
    public const string ErrorCaption = "Error";

    private readonly IExecutable _gitExecutable;
    private readonly IMessageBoxService _messageBoxes;

    public CreateLocalBranchesViewModel(CreateLocalBranchesStrings strings, IExecutable gitExecutable, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _gitExecutable = gitExecutable;
        _messageBoxes = messageBoxes;
    }

    public CreateLocalBranchesStrings Strings { get; }

    [ObservableProperty]
    public partial string Remote { get; set; } = "origin";

    /// <summary>As <c>CreateLocalBranchesForm.button1_Click</c>: tracks every branch of the remote.</summary>
    [RelayCommand]
    private void Create()
    {
        GitArgumentBuilder args = new("branch") { "-a" };
        string[] references = _gitExecutable.GetOutput(args).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (references.Length == 0)
        {
            _messageBoxes.ShowError(NoRemoteBranchesFound, ErrorCaption);
            Close(accepted: false);
            return;
        }

        string remotePrefix = $"remotes/{Remote}/";
        foreach (string reference in references)
        {
            try
            {
                string branchName = reference.Trim(Delimiters.GitOutput);
                if (branchName.StartsWith(remotePrefix))
                {
                    args = new GitArgumentBuilder("branch")
                    {
                        "--track",
                        branchName.Replace(remotePrefix, ""),
                        branchName
                    };
                    _gitExecutable.GetOutput(args);
                }
            }
            catch
            {
                // As the WinForms form: a branch that cannot be tracked is skipped.
            }
        }

        _messageBoxes.ShowInformation(string.Format(BranchesCreatedFormat, references.Length), InformationCaption);
        Close(accepted: true);
    }
}
