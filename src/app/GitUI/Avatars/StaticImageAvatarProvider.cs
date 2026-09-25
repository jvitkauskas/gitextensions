namespace GitUI.Avatars;

public sealed class StaticImageAvatarProvider : IAvatarProvider
{
    private readonly byte[] _image;
    private readonly Lock _sizeCacheLock = new();
    private readonly Dictionary<int, byte[]> _sizeCache = [];

    /// <param name="image">A square image (encoded, as PNG).</param>
    public StaticImageAvatarProvider(byte[] image)
    {
        _image = image;
        if (PngImages.GetWidth(image) is int size)
        {
            _sizeCache.Add(size, image);
        }
    }

    public bool PerformsIo => false;

    /// <inheritdoc />
    public Task<byte[]?> GetAvatarAsync(string email, string? name, int imageSize)
    {
        return Task.FromResult<byte[]?>(GetCachedResizedImage(imageSize));
    }

    private byte[] GetCachedResizedImage(int imageSize)
    {
        lock (_sizeCacheLock)
        {
            if (_sizeCache.TryGetValue(imageSize, out byte[]? image))
            {
                return image;
            }

            byte[] resizedImage = PngImages.Resize(_image, imageSize);
            _sizeCache.Add(imageSize, resizedImage);

            return resizedImage;
        }
    }
}
