using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Editor;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the .gitignore / .git/info/exclude editor (docs/avalonia-port/PLAN.md, phase 3).</summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowGitIgnore(IWin32Window? owner, IGitUICommands commands, bool localExclude)
    {
        GitIgnoreStrings strings = ViewStrings.Load<GitIgnoreStrings>();
        GitIgnoreFileStrings fileStrings = localExclude ? ViewStrings.Load<GitLocalExcludeModelStrings>() : ViewStrings.Load<GitIgnoreModelStrings>();
        IGitModule module = commands.Module;
        if (module.IsBareRepository())
        {
            // As FormGitIgnoreLoad.
            MessageBoxes.Show(owner, fileStrings.OnlyInWorkingDirSupported.Text, strings.NoWorkingDirCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return true;
        }

        // As GitIgnoreModel and GitLocalExcludeModel.
        string? path = localExclude
            ? Path.Join(module.ResolveGitInternalPath("info"), "exclude")
            : new FullPathResolver(() => module.WorkingDir).Resolve(".gitignore");
        ShowDialog(
            () =>
            {
                GitIgnoreEditorWindow window = new();
                GitIgnoreEditorViewModel viewModel = new(strings, fileStrings, new GitIgnoreEditorHost(commands, path, localExclude, window), new MessageBoxService(window));
                viewModel.Options = new TextEditorOptionsViewModel(viewModel.Editor, new TextEditorOptionsHost(commands));
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormGitIgnore");
        return true;
    }

    private sealed class GitIgnoreEditorHost(IGitUICommands commands, string? path, bool localExclude, DialogWindow window) : IGitIgnoreEditorHost
    {
        private static readonly string DefaultIgnorePatternsFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitExtensions/DefaultIgnorePatterns.txt");

        public string? Load()
        {
            try
            {
                if (File.Exists(path))
                {
                    // As FileViewer.ViewFileAsync.
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using StreamReader reader = FileReader.OpenStream(stream, commands.Module.FilesEncoding);
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                // As FormGitIgnore.LoadGitIgnore.
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }

            return null;
        }

        public void Save(string text)
        {
            if (path is null)
            {
                return;
            }

            // As FormGitIgnore.SaveGitIgnore.
            FileInfoExtensions.MakeFileTemporaryWritable(path, x =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(x)!);
                File.WriteAllBytes(x, GitModule.SystemEncoding.GetBytes(text));
            });
        }

        public IReadOnlyList<string>? LoadDefaultIgnorePatterns()
            => File.Exists(DefaultIgnorePatternsFile) ? File.ReadAllLines(DefaultIgnorePatternsFile) : null;

        public void AddPattern()
            => AvaloniaUi.RunInHostContext(() => commands.StartAddToGitIgnoreDialog(new NativeWindowOwner(window), localExclude, "*.dll"));

        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));
    }
}
