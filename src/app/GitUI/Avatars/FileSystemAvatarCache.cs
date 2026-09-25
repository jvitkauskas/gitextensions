using System.Diagnostics;
using System.IO.Abstractions;
using GitCommands;

namespace GitUI.Avatars;

/// <summary>
/// Decorates an avatar provider, adding persistent caching to the file system.
/// </summary>
public sealed class FileSystemAvatarCache : IAvatarProvider, IAvatarCacheCleaner
{
    private readonly IAvatarProvider _inner;
    private readonly IFileSystem _fileSystem;
    private readonly string _cacheDir;
    private readonly int _cacheDays;

    public FileSystemAvatarCache(IAvatarProvider inner, IFileSystem? fileSystem = null)
    {
        _inner = inner;
        _fileSystem = fileSystem ?? new FileSystem();

        _cacheDays = AppSettings.AvatarImageCacheDays;
        if (_cacheDays < 1)
        {
            const int DefaultCacheDays = 30;
            _cacheDays = DefaultCacheDays;
        }

        _cacheDir = AppSettings.AvatarImageCachePath;
        if (!_fileSystem.Directory.Exists(_cacheDir))
        {
            _fileSystem.Directory.CreateDirectory(_cacheDir);
        }
    }

    /// <inheritdoc />
    public event EventHandler? CacheCleared;

    public bool PerformsIo => true;

    /// <inheritdoc />
    public async Task<byte[]?> GetAvatarAsync(string email, string? name, int imageSize)
    {
        if (!_inner.PerformsIo)
        {
            return await _inner.GetAvatarAsync(email, name, imageSize);
        }

        string path = Path.Join(_cacheDir, $"{email}.{imageSize}px.png");

        byte[]? image = ReadImage();

        if (image is not null)
        {
            return image;
        }

        image = await _inner.GetAvatarAsync(email, name, imageSize);

        if (image is not null)
        {
            WriteImage();
        }

        return image;

        void WriteImage()
        {
            try
            {
                _fileSystem.File.WriteAllBytes(path, image);
            }
            catch
            {
            }
        }

        byte[]? ReadImage()
        {
            if (!HasExpired())
            {
                try
                {
                    byte[] data = _fileSystem.File.ReadAllBytes(path);
                    return PngImages.GetWidth(data) is null ? null : data;
                }
                catch
                {
                    // ignore
                }
            }

            return null;

            bool HasExpired()
            {
                IFileInfo info = _fileSystem.FileInfo.New(path);

                if (!info.Exists)
                {
                    return true;
                }

                if (AppSettings.AvatarProvider == AvatarProvider.None)
                {
                    // No need to refresh because the image returned is always the same.
                    return false;
                }

                if (info.LastWriteTime < DateTime.Now.AddDays(-_cacheDays))
                {
                    TryDelete();
                    return true;
                }

                return false;
            }

            void TryDelete()
            {
                try
                {
                    _fileSystem.File.Delete(path);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task ClearCacheAsync()
    {
        string cachePath = AppSettings.AvatarImageCachePath;

        if (_fileSystem.Directory.Exists(cachePath))
        {
            try
            {
                foreach (string file in _fileSystem.Directory.GetFiles(cachePath))
                {
                    try
                    {
                        _fileSystem.File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        // do nothing
                        Trace.WriteLine($"Failed to delete file '{file}'. Error: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                // do nothing
                Trace.WriteLine($"Failed to enumerate files. Error: {ex}");
            }
        }

        if (CacheCleared is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            CacheCleared.Invoke(this, EventArgs.Empty);
        }
    }
}
