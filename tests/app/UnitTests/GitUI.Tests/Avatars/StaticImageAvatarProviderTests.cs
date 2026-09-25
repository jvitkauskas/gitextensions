using GitUI;
using GitUI.Avatars;

namespace GitUITests.Avatars;
public class StaticImageAvatarProviderTests
{
    private const int _size = 64;

    private const string _email = "a@a.a";
    private const string _name = "John Lennon";

    private readonly byte[] _img;

    public StaticImageAvatarProviderTests()
    {
        _img = PngImages.Fill(Color.Red, _size);
    }

    [Test]
    public async Task Original_image_is_returned_if_size_matches()
    {
        StaticImageAvatarProvider provider = new(_img);

        byte[]? result = await provider.GetAvatarAsync(_email, _name, _size);

        result.Should().BeSameAs(_img);
    }

    [Test]
    public async Task Resized_images_are_cached_and_same_instance_is_returned_on_second_call()
    {
        StaticImageAvatarProvider provider = new(_img);
        int otherSize = 32;

        byte[]? result1 = await provider.GetAvatarAsync(_email, _name, otherSize);
        byte[]? result2 = await provider.GetAvatarAsync(_email, _name, otherSize);

        result2.Should().BeSameAs(result1);
    }
}
