namespace GitUI.Presentation.Services;

/// <summary>
///  File and folder pickers for view models, replacing direct use of <c>OpenFileDialog</c> / <c>OsShellUtil.PickFolder</c>.
///  The view provides them for its window.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Lets the user pick existing files; returns their full paths, or an empty list if cancelled.</summary>
    Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null);

    /// <summary>Lets the user pick a folder; returns its full path, or <see langword="null"/> if cancelled.</summary>
    Task<string?> PickFolderAsync(string? startDirectory = null);
}
