using System.Runtime.CompilerServices;
using GitCommands;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the git grep prompt of the file status list (docs/avalonia-port/PLAN.md).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The open git grep prompt of each file status list, which is reused as <c>FileStatusList</c> does.</summary>
    private static readonly ConditionalWeakTable<Control, FindInCommitFilesGitGrepWindow> _gitGrepWindows = [];

    /// <summary>
    ///  Shows the Avalonia port of <c>FormFindInCommitFilesGitGrep</c> modelessly over the form of <paramref name="fileList"/>,
    ///  or updates and activates the one already open (as <c>FileStatusList.ShowFindInCommitFileGitGrepDialog</c>).
    /// </summary>
    /// <param name="fileList">The file status list the prompt searches.</param>
    /// <param name="expression">The expression to show, or <see langword="null"/> to keep the current one.</param>
    /// <param name="searchItems">The previous expressions of the search box of the list.</param>
    /// <param name="showSearchBox">Whether the list shows its git grep search box.</param>
    /// <param name="search">Searches the list with an expression (<c>FilesGitGrepLocator</c>).</param>
    /// <param name="setSearchBoxVisible">Shows or hides the search box of the list (<c>FindInCommitFilesGitGrepToggle</c>).</param>
    public static bool TryShowFindInCommitFilesGitGrep(
        Control fileList,
        string? expression,
        IEnumerable<string> searchItems,
        bool showSearchBox,
        Action<string> search,
        Action<bool> setSearchBoxVisible)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormFindInCommitFilesGitGrep)))
        {
            return false;
        }

        if (!_gitGrepWindows.TryGetValue(fileList, out FindInCommitFilesGitGrepWindow? window) || !window.IsVisible)
        {
            AvaloniaUi.EnsureInitialized(GetOptions);
            window = new FindInCommitFilesGitGrepWindow
            {
                DataContext = new FindInCommitFilesGitGrepViewModel(
                    ViewStrings.Load<FindInCommitFilesGitGrepStrings>(),
                    new FindInCommitFilesGitGrepHost(fileList, search, setSearchBoxVisible)),
            };
            _gitGrepWindows.AddOrUpdate(fileList, window);
            ((FindInCommitFilesGitGrepViewModel)window.DataContext).SetState(expression, searchItems, showSearchBox);

            Form? form = fileList.FindForm();
            if (form is not null)
            {
                // Offset a few pixels compared to FindAndReplaceForm, as FileStatusList places it.
                window.StartupScreenPosition = new global::Avalonia.PixelPoint(form.Location.X + 90, form.Location.Y + 110);

                void CloseWithForm(object? sender, FormClosedEventArgs e) => window.Close();
                form.FormClosed += CloseWithForm;
                window.Closed += (_, _) => form.FormClosed -= CloseWithForm;
            }

            AvaloniaDialogHost.Show(window, fileList.Handle);
            return true;
        }

        ((FindInCommitFilesGitGrepViewModel)window.DataContext!).SetState(expression, searchItems, showSearchBox);
        window.Activate();
        return true;
    }

    /// <summary>The file status list and the git grep settings (<c>AppSettings</c>).</summary>
    private sealed class FindInCommitFilesGitGrepHost(Control fileList, Action<string> search, Action<bool> setSearchBoxVisible) : IFindInCommitFilesGitGrepHost
    {
        public void Search(string expression)
        {
            // The prompt also closes with the form of the list, which may be disposed by then.
            if (!fileList.IsDisposed)
            {
                AvaloniaUi.RunInHostContext(() => search(expression));
            }
        }

        public void SetSearchBoxVisible(bool visible)
        {
            if (!fileList.IsDisposed)
            {
                AvaloniaUi.RunInHostContext(() => setSearchBoxVisible(visible));
            }
        }

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
