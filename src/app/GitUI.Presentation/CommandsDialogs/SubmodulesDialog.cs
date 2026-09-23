using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the submodule list; ids match <c>FormSubmodules</c>.</summary>
public sealed class SubmodulesStrings : ViewStrings
{
    public SubmodulesStrings()
        : base("FormSubmodules")
    {
        Title = Add("$this", "Text", "Submodules");
        AddSubmodule = Add("AddSubmodule", "Text", "Add submodule");
        Remove = Add("RemoveSubmodule", "Text", "Remove");
        Update = Add("UpdateSubmodule", "Text", "Update");
        Synchronize = Add("SynchronizeSubmodule", "Text", "Synchronize");
        NameColumn = Add("nameDataGridViewTextBoxColumn", "HeaderText", "Name");
        StatusColumn = Add("Status", "HeaderText", "Status");
        Details = Add("groupBox1", "Text", "Details");
        Name = Add("label1", "Text", "Name");
        RemotePath = Add("label2", "Text", "Remote path");
        LocalPath = Add("label3", "Text", "Local path");
        Commit = Add("label4", "Text", "Commit");
        Branch = Add("label5", "Text", "Branch");
        Status = Add("label6", "Text", "Status");
        RemoveSelectedSubmodule = Add("_removeSelectedSubmodule", "Text", "Are you sure you want remove the selected submodule?");
        RemoveSelectedSubmoduleCaption = Add("_removeSelectedSubmoduleCaption", "Text", "Remove");
    }

    public TranslatedText Title { get; }

    public TranslatedText AddSubmodule { get; }

    public TranslatedText Remove { get; }

    public TranslatedText Update { get; }

    public TranslatedText Synchronize { get; }

    public TranslatedText NameColumn { get; }

    public TranslatedText StatusColumn { get; }

    public TranslatedText Details { get; }

    public TranslatedText Name { get; }

    public TranslatedText RemotePath { get; }

    public TranslatedText LocalPath { get; }

    public TranslatedText Commit { get; }

    public TranslatedText Branch { get; }

    public TranslatedText Status { get; }

    public TranslatedText RemoveSelectedSubmodule { get; }

    public TranslatedText RemoveSelectedSubmoduleCaption { get; }
}

/// <summary>A submodule as listed (the fields of <c>GitSubmoduleInfo</c> the dialog shows).</summary>
public sealed record SubmoduleItem(string Name, string Status, string RemotePath, string LocalPath, string CurrentCommit, string Branch);

/// <summary>Operations of the submodule list that need the host (git and the other dialogs).</summary>
public interface ISubmodulesHost
{
    /// <summary>Loads the submodules in the background, reporting each on the UI thread; a later call cancels an earlier one.</summary>
    void LoadSubmodules(Action<SubmoduleItem> report, Action completed);

    /// <summary>Shows the add submodule dialog.</summary>
    void Add();

    /// <summary>Runs <c>git submodule sync</c>; all submodules if <paramref name="localPath"/> is empty.</summary>
    void Synchronize(string localPath);

    /// <summary>Runs <c>git submodule update</c>; all submodules if <paramref name="localPath"/> is empty.</summary>
    void Update(string localPath);

    /// <summary>Removes the submodule from the index and the configuration (as <c>FormSubmodules.RemoveSubmoduleClick</c>).</summary>
    void Remove(string name, string localPath);

    /// <summary>Shows the pull dialog of the submodule.</summary>
    void Pull(string localPath);
}

/// <summary>View model of the submodule list (port of <c>FormSubmodules</c>).</summary>
public sealed partial class SubmodulesViewModel : DialogViewModel
{
    private readonly ISubmodulesHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private string? _reselectName;

    public SubmodulesViewModel(SubmodulesStrings strings, ISubmodulesHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
    }

    public SubmodulesStrings Strings { get; }

    public ObservableCollection<SubmoduleItem> Submodules { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    public partial SubmoduleItem? SelectedSubmodule { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>Reloads the list, keeping the selected submodule selected (as <c>FormSubmodules.Initialize</c>).</summary>
    public void Reload()
    {
        _reselectName = SelectedSubmodule?.Name;
        Submodules.Clear();
        IsLoading = true;
        _host.LoadSubmodules(
            submodule =>
            {
                Submodules.Add(submodule);
                if (submodule.Name == _reselectName)
                {
                    SelectedSubmodule = submodule;
                }
            },
            () => IsLoading = false);
    }

    private string SelectedLocalPath => SelectedSubmodule?.LocalPath ?? "";

    [RelayCommand]
    private void Add()
    {
        _host.Add();
        Reload();
    }

    [RelayCommand]
    private void Synchronize()
    {
        _host.Synchronize(SelectedLocalPath);
        Reload();
    }

    [RelayCommand]
    private void Update()
    {
        _host.Update(SelectedLocalPath);
        Reload();
    }

    [RelayCommand]
    private void Pull()
    {
        _host.Pull(SelectedLocalPath);
        Reload();
    }

    private bool CanRemove() => SelectedSubmodule is not null;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedSubmodule is not { } submodule
            || !_messageBoxes.Confirm(Strings.RemoveSelectedSubmodule.Text, Strings.RemoveSelectedSubmoduleCaption.Text))
        {
            return;
        }

        _host.Remove(submodule.Name, submodule.LocalPath);
        Reload();
    }
}
