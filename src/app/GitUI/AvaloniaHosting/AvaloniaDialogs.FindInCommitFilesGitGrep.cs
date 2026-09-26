using System.Runtime.CompilerServices;
using GitCommands;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.AvaloniaHosting;

/// <summary>The git grep prompt of the file status list (port of <c>FormFindInCommitFilesGitGrep</c>).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The open git grep prompt of each file status list, which is reused as <c>FileStatusList</c> does.</summary>
    private static readonly ConditionalWeakTable<FileStatusListViewModel, FindInCommitFilesGitGrepWindow> _gitGrepWindows = [];

    /// <summary>
    ///  Shows the git grep prompt of <paramref name="files"/> in <paramref name="window"/> when the list asks for it, and closes
    ///  it when the list asks (as <c>FileStatusList.ShowFindInCommitFileGitGrepDialog</c>).
    /// </summary>
    internal static void UseFindInCommitFilesGitGrep(FileStatusListViewModel files, DialogWindow window)
    {
        files.GitGrepDialogRequested += (_, text) => ShowFindInCommitFilesGitGrep(files, window, text);
        files.GitGrepDialogCloseRequested += (_, _) =>
        {
            if (_gitGrepWindows.TryGetValue(files, out FindInCommitFilesGitGrepWindow? prompt))
            {
                prompt.Close();
            }
        };
    }

    /// <summary>Shows the prompt, or updates and activates the one already open, with <paramref name="text"/> or the searched expression.</summary>
    private static void ShowFindInCommitFilesGitGrep(FileStatusListViewModel files, DialogWindow owner, string text)
    {
        string? expression = !string.IsNullOrEmpty(text) ? text : files.IsGitGrepActive ? files.GitGrepText : null;
        if (_gitGrepWindows.TryGetValue(files, out FindInCommitFilesGitGrepWindow? window) && window.IsVisible)
        {
            ((FindInCommitFilesGitGrepViewModel)window.DataContext!).SetState(expression, files.GitGrepHistory, files.IsGitGrepBoxVisible);
            window.Activate();
            return;
        }

        FindInCommitFilesGitGrepViewModel viewModel = new(ViewStrings.Load<FindInCommitFilesGitGrepStrings>(), new FindInCommitFilesGitGrepHost(files));
        window = new FindInCommitFilesGitGrepWindow { DataContext = viewModel };
        viewModel.SetState(expression, files.GitGrepHistory, files.IsGitGrepBoxVisible);
        _gitGrepWindows.AddOrUpdate(files, window);

        // Offset a few pixels compared to the search of the editors, as FileStatusList places it.
        window.StartupScreenPosition = new global::Avalonia.PixelPoint(owner.Position.X + 90, owner.Position.Y + 110);
        files.IsGitGrepDialogOpen = true;
        window.Closed += (_, _) => files.IsGitGrepDialogOpen = false;
        AvaloniaDialogHost.Show(window, owner.OwnerHandle);
    }

    /// <summary>The file status list and the git grep settings (<c>AppSettings</c>).</summary>
    private sealed class FindInCommitFilesGitGrepHost(FileStatusListViewModel files) : IFindInCommitFilesGitGrepHost
    {
        public void Search(string expression) => files.SearchGitGrep(expression);

        public void SetSearchBoxVisible(bool visible) => files.SetGitGrepBoxVisible(visible);

        public string UserArguments
        {
            get => AppSettings.GitGrepUserArguments.Value;
            set => AppSettings.GitGrepUserArguments.Value = value;
        }

        public bool IgnoreCase
        {
            get => AppSettings.GitGrepIgnoreCase.Value;
            set => AppSettings.GitGrepIgnoreCase.Value = value;
        }

        public bool MatchWholeWord
        {
            get => AppSettings.GitGrepMatchWholeWord.Value;
            set => AppSettings.GitGrepMatchWholeWord.Value = value;
        }

        public bool ShowSearchBox
        {
            get => AppSettings.ShowFindInCommitFilesGitGrep.Value;
            set => AppSettings.ShowFindInCommitFilesGitGrep.Value = value;
        }
    }
}
