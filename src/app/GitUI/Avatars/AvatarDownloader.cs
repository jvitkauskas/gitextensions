using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.VisualStudio.Threading;

namespace GitUI.Avatars;

/// <summary>
/// Helps downloading avatar images and implements concurrent requests and automatic retries.
/// </summary>
public sealed class AvatarDownloader : IAvatarDownloader
{
    private const int _maxConcurrentDownloads = 10;
    private const int _requestCacheDurationInSeconds = 30;

    private static TimeSpan _requestCacheTimeSpan = TimeSpan.FromSeconds(_requestCacheDurationInSeconds);

    private static readonly SemaphoreSlim _downloadSemaphore = new(initialCount: _maxConcurrentDownloads);
    private static readonly ConcurrentDictionary<Uri, (DateTime, Task<byte[]?>)> _downloads = new();
    private static readonly HttpClient _client = new(new HttpClientHandler() { UseProxy = true, DefaultProxyCredentials = CredentialCache.DefaultCredentials });

    public async Task<byte[]?> DownloadImageAsync(Uri? imageUrl)
    {
        if (imageUrl is null)
        {
            return null;
        }

        // check network connectivity (Windows tells it; elsewhere the download fails without a network)
        if (OperatingSystem.IsWindows() && !NativeMethods.InternetGetConnectedState(out _, 0))
        {
            return null;
        }

        ClearOldCacheEntries();

        int errorCount = 0;

        while (true)
        {
            (DateTime _, Task<byte[]?> task) = _downloads.GetOrAdd(imageUrl, _ => (DateTime.UtcNow, DownloadAsync(imageUrl)));

            // If we discover a faulted task, remove it and try again
            if (task.IsFaulted || task.IsCanceled)
            {
                _downloads.TryRemove(imageUrl, out _);

                if (++errorCount > 3)
                {
                    await task;
                }

                continue;
            }

            return await task;
        }
    }

    private static async Task<byte[]?> DownloadAsync(Uri imageUrl)
    {
        // Get onto background thread
        await TaskScheduler.Default;

        // Limit the number of concurrent download requests
        await _downloadSemaphore.WaitAsync();

        try
        {
            using HttpResponseMessage response = await _client.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // As PNG data (the cache files are PNG files); null if the response is not an image.
            return PngImages.ToPng(await response.Content.ReadAsByteArrayAsync());
        }
        catch (Exception ex)
        {
            // catch IO errors
            Trace.WriteLine(ex.Message);
        }
        finally
        {
            _downloadSemaphore.Release();
        }

        return null;
    }

    private void ClearOldCacheEntries()
    {
        DateTime now = DateTime.UtcNow;

        foreach ((Uri key, (DateTime time, Task<byte[]?> _)) in _downloads)
        {
            if (now - time > _requestCacheTimeSpan)
            {
                _downloads.TryRemove(key, out _);
            }
        }
    }
}
