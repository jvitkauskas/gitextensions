namespace GitUI.Presentation.Services;

/// <summary>
///  File and folder pickers for view models, replacing direct use of <c>OpenFileDialog</c> / <c>OsShellUtil.PickFolder</c>.
///  The view provides them for its window.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Lets the user pick existing files; returns their full paths, or an empty list if cancelled.</summary>
    Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null);

    /// <summary>
    ///  Lets the user pick an existing file of one type (as an <c>OpenFileDialog</c> with a title and a filter); returns its full
    ///  path, or <see langword="null"/> if cancelled.
    /// </summary>
    /// <param name="filterName">The name of the file type, e.g. "Patch file (*.Patch)".</param>
    /// <param name="pattern">The pattern of the file type, e.g. "*.patch".</param>
    Task<string?> PickFileAsync(string title, string filterName, string pattern, string? startDirectory = null);

    /// <summary>Lets the user pick a folder; returns its full path, or <see langword="null"/> if cancelled.</summary>
    Task<string?> PickFolderAsync(string? startDirectory = null);

    /// <summary>
    ///  Lets the user choose a file to save to; returns its full path, or <see langword="null"/> if cancelled.
    /// </summary>
    /// <param name="filterName">The name of the only file type offered, e.g. "Zip file (*.zip)".</param>
    /// <param name="extension">The extension of that file type, without the dot.</param>
    Task<string?> PickSaveFileAsync(string title, string filterName, string extension, string? suggestedFileName = null, string? startDirectory = null);
}
