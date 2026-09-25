using GitUI;
using GitUI.Avatars;
using NSubstitute;

namespace GitUITests.Avatars;
public class ChainedAvatarProviderTests
{
    private const int _size = 16;

    private const string _email1 = "a@a.a";
    private const string _email2 = "b@b.b";
    private const string _email3 = "c@c.c";
    private const string _email4 = "d@d.d";

    private const string _name1 = "John Lennon";
    private const string _name2 = "Paul McCartney";
    private const string _name3 = "George Harrison";
    private const string _name4 = "Ringo Starr";

    private readonly byte[] _img1;
    private readonly byte[] _img2;
    private readonly byte[] _img3;
    private readonly byte[] _img4;
    private readonly byte[] _img5;
    private readonly byte[] _img6;

    public ChainedAvatarProviderTests()
    {
        _img1 = PngImages.Fill(Color.FromArgb(1 * 40, 0, 0), _size);
        _img2 = PngImages.Fill(Color.FromArgb(2 * 40, 0, 0), _size);
        _img3 = PngImages.Fill(Color.FromArgb(3 * 40, 0, 0), _size);
        _img4 = PngImages.Fill(Color.FromArgb(4 * 40, 0, 0), _size);
        _img5 = PngImages.Fill(Color.FromArgb(5 * 40, 0, 0), _size);
        _img6 = PngImages.Fill(Color.FromArgb(6 * 40, 0, 0), _size);
    }

    [Test]
    public async Task Construction_without_parameter_is_allowed_and_returns_null()
    {
        ChainedAvatarProvider provider = new();

        byte[]? image = await provider.GetAvatarAsync(_email1, _name1, _size);
        image.Should().BeNull();
    }

    [Test]
    public void Construction_with_null_parameters_is_permitted()
    {
        ((Action)(() => new ChainedAvatarProvider(null!))).Should().Throw<ArgumentNullException>();

        ((Action)(() => new ChainedAvatarProvider(null!, null!))).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Return_first_non_null_result()
    {
        IAvatarProvider provider1 = Substitute.For<IAvatarProvider>();
        IAvatarProvider provider2 = Substitute.For<IAvatarProvider>();
        IAvatarProvider provider3 = Substitute.For<IAvatarProvider>();

        // Register different images (for the same parameters)
        // for each provider. This allows us to detect incorrect results.

        // Case 1: First provider hit
        provider1.GetAvatarAsync(_email1, _name1, _size).Returns(_img1);
        provider2.GetAvatarAsync(_email1, _name1, _size).Returns(_img2);
        provider3.GetAvatarAsync(_email1, _name1, _size).Returns(_img3);

        // Case 2: Second provider hit
        provider1.GetAvatarAsync(_email2, _name2, _size).Returns((byte[]?)null);
        provider2.GetAvatarAsync(_email2, _name2, _size).Returns(_img4);
        provider3.GetAvatarAsync(_email2, _name2, _size).Returns(_img5);

        // Case 3: Third provider hit
        provider1.GetAvatarAsync(_email3, _name3, _size).Returns((byte[]?)null);
        provider2.GetAvatarAsync(_email3, _name3, _size).Returns((byte[]?)null);
        provider3.GetAvatarAsync(_email3, _name3, _size).Returns(_img6);

        // Case 4: No provider hit
        provider1.GetAvatarAsync(_email4, _name4, _size).Returns((byte[]?)null);
        provider2.GetAvatarAsync(_email4, _name4, _size).Returns((byte[]?)null);
        provider3.GetAvatarAsync(_email4, _name4, _size).Returns((byte[]?)null);

        ChainedAvatarProvider chainedProvider = new(provider1, provider2, provider3);

        byte[]? res1 = await chainedProvider.GetAvatarAsync(_email1, _name1, _size);
        byte[]? res2 = await chainedProvider.GetAvatarAsync(_email2, _name2, _size);
        byte[]? res3 = await chainedProvider.GetAvatarAsync(_email3, _name3, _size);
        byte[]? res4 = await chainedProvider.GetAvatarAsync(_email4, _name4, _size);

        res1.Should().BeSameAs(_img1);
        res2.Should().BeSameAs(_img4);
        res3.Should().BeSameAs(_img6);
        res4.Should().BeSameAs(null);
    }
}
