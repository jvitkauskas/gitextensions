using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  File and folder pickers of an Avalonia window (native dialogs on Windows, portable to other platforms).
/// </summary>
public sealed class AvaloniaFileDialogService(TopLevel topLevel) : IFileDialogService
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null)
    {
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = allowMultiple,
            SuggestedStartLocation = await GetFolderAsync(startDirectory),
        });

        return [.. files.Select(file => file.TryGetLocalPath()).OfType<string>()];
    }

    public async Task<string?> PickFileAsync(string title, string filterName, string pattern, string? startDirectory = null)
    {
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = [pattern] }],
            SuggestedStartLocation = await GetFolderAsync(startDirectory),
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickFileAsync(string title, IReadOnlyList<(string Name, string Pattern)> fileTypes, string? startDirectory = null)
    {
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = string.IsNullOrEmpty(title) ? null : title,
            AllowMultiple = false,
            FileTypeFilter = [.. fileTypes.Select(fileType => new FilePickerFileType(fileType.Name) { Patterns = fileType.Pattern.Split(';') })],
            SuggestedStartLocation = await GetFolderAsync(startDirectory),
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickFolderAsync(string? startDirectory = null)
    {
        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = await GetFolderAsync(startDirectory),
        });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    public async Task<string?> PickSaveFileAsync(string title, string filterName, string extension, string? suggestedFileName = null, string? startDirectory = null)
    {
        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(filterName) { Patterns = [$"*.{extension}"] }],
            SuggestedStartLocation = await GetFolderAsync(startDirectory),
        });

        return file?.TryGetLocalPath();
    }

    private async Task<IStorageFolder?> GetFolderAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return await topLevel.StorageProvider.TryGetFolderFromPathAsync(Path.GetFullPath(path));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
