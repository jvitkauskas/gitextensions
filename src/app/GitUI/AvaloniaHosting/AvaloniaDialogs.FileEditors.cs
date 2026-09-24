using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Editor;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the file editors, the first users of the Avalonia text editor (docs/avalonia-port/PLAN.md, phase 3).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowEditGitAttributes(IWin32Window? owner, IGitUICommands commands)
        => TryShowRepoFileEditor<GitAttributesEditorStrings>(owner, commands, "FormGitAttributes", ".gitattributes", notifyRepoChanged: false);

    public static bool TryShowMailMap(IWin32Window? owner, IGitUICommands commands)
        => TryShowRepoFileEditor<MailMapEditorStrings>(owner, commands, "FormMailMap", ".mailmap", notifyRepoChanged: true);

    /// <summary>As <c>FormGitAttributes</c> and <c>FormMailMap</c>, which differ in the file and their strings.</summary>
    private static bool TryShowRepoFileEditor<TStrings>(IWin32Window? owner, IGitUICommands commands, string formName, string fileName, bool notifyRepoChanged)
        where TStrings : RepoFileEditorStrings, new()
    {
        TStrings strings = ViewStrings.Load<TStrings>();
        IGitModule module = commands.Module;
        if (module.IsBareRepository())
        {
            MessageBoxes.Show(owner, strings.NoWorkingDir.Text, strings.NoWorkingDirCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return true;
        }

        FullPathResolver fullPathResolver = new(() => module.WorkingDir);
        ShowDialog(
            () =>
            {
                RepoFileEditorWindow window = new();
                window.DataContext = new RepoFileEditorViewModel(
                    strings,
                    fileName,
                    new RepoFileEditorHost(commands, fullPathResolver.Resolve(fileName), notifyRepoChanged),
                    new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: formName);
        return true;
    }

    /// <summary>
    ///  Shows the Avalonia port of <c>FormEditor</c>; returns <see langword="false"/> if it is disabled.
    /// </summary>
    /// <param name="accepted">Whether the file was saved or left unchanged (<c>DialogResult</c> not <c>Cancel</c>).</param>
    public static bool TryShowFileEditor(IGitUICommands commands, string? fileName, bool showWarning, int? lineNumber, out bool accepted)
    {
        accepted = false;
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        IGitModule module = commands.Module;
        string text;
        byte[] preamble;
        try
        {
            // As FileViewer.ViewFileAsync: the encoding is detected, the repository's by default.
            string fullPath = new FullPathResolver(() => module.WorkingDir).Resolve(fileName) ?? fileName;
            using FileStream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = FileReader.OpenStream(stream, module.FilesEncoding);
            text = reader.ReadToEnd();
            preamble = reader.CurrentEncoding.GetPreamble();
        }
        catch (Exception ex)
        {
            FileEditorStrings strings = ViewStrings.Load<FileEditorStrings>();
            MessageBoxes.Show(null, strings.CannotOpenFile.Text + Environment.NewLine + ex.Message, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return true;
        }

        FileEditorViewModel? viewModel = null;
        ShowDialog(
            () =>
            {
                FileEditorWindow window = new();
                viewModel = new FileEditorViewModel(
                    ViewStrings.Load<FileEditorStrings>(),
                    fileName,
                    text,
                    showWarning,
                    readOnly: false,
                    lineNumber,
                    TranslatedStrings.Error,
                    new FileEditorHost(module, preamble),
                    new MessageBoxService(window));
                viewModel.Editor.ShowWhitespace = AppSettings.ShowNonPrintingChars.Value;
                window.DataContext = viewModel;
                return window;
            },
            owner: null,
            positionName: "FormEditor");
        accepted = viewModel?.Accepted ?? false;
        return true;
    }

    private sealed class RepoFileEditorHost(IGitUICommands commands, string? path, bool notifyRepoChanged) : IRepoFileEditorHost
    {
        public string Load()
        {
            try
            {
                if (File.Exists(path))
                {
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using StreamReader reader = FileReader.OpenStream(stream, commands.Module.FilesEncoding);
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                // As FormGitAttributes.LoadFile.
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }

            return "";
        }

        public void Save(string text)
        {
            // As FormGitAttributes.SaveFile and FormMailMap.SaveFile.
            FileInfoExtensions.MakeFileTemporaryWritable(path!, x => File.WriteAllBytes(x, GitModule.SystemEncoding.GetBytes(text)));
            if (notifyRepoChanged)
            {
                commands.RepoChangedNotifier.Notify();
            }
        }
    }

    private sealed class FileEditorHost(IGitModule module, byte[] preamble) : IFileEditorHost
    {
        public void Save(string fileName, string text)
        {
            // As FormEditor.SaveChanges: the preamble of the file is kept unless it is the one of the repository's encoding.
            byte[] filePreamble = module.FilesEncoding.GetPreamble().SequenceEqual(preamble) ? [] : preamble;
            FileUtility.SafeWriteAllText(fileName, text, module.FilesEncoding, filePreamble);
        }
    }
}
