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

    /// <summary>
    ///  Lets the user pick an existing file of one of several types (as an <c>OpenFileDialog</c> whose filter has several
    ///  types, the first one being selected); returns its full path, or <see langword="null"/> if cancelled.
    /// </summary>
    /// <param name="fileTypes">The name and the patterns (separated by <c>;</c>) of each type, see <see cref="FileDialogFilter.Parse"/>.</param>
    Task<string?> PickFileAsync(string title, IReadOnlyList<(string Name, string Pattern)> fileTypes, string? startDirectory = null)
        => PickFileAsync(title, fileTypes[0].Name, fileTypes[0].Pattern, startDirectory);

    /// <summary>Lets the user pick a folder; returns its full path, or <see langword="null"/> if cancelled.</summary>
    Task<string?> PickFolderAsync(string? startDirectory = null);

    /// <summary>
    ///  Lets the user choose a file to save to; returns its full path, or <see langword="null"/> if cancelled.
    /// </summary>
    /// <param name="filterName">The name of the only file type offered, e.g. "Zip file (*.zip)".</param>
    /// <param name="extension">The extension of that file type, without the dot.</param>
    Task<string?> PickSaveFileAsync(string title, string filterName, string extension, string? suggestedFileName = null, string? startDirectory = null);
}

/// <summary>The file types of a WinForms <c>FileDialog.Filter</c>.</summary>
public static class FileDialogFilter
{
    /// <summary>
    ///  Splits a WinForms filter (<c>"Name1|pattern1|Name2|pattern2;pattern3"</c>) into its file types, for the file picker
    ///  of several types of <see cref="IFileDialogService"/>.
    /// </summary>
    public static IReadOnlyList<(string Name, string Pattern)> Parse(string filter)
    {
        string[] parts = filter.Split('|');
        List<(string Name, string Pattern)> fileTypes = [];
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            fileTypes.Add((parts[i], parts[i + 1]));
        }

        return fileTypes;
    }
}
