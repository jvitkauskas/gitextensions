using GitUI;
using GitUI.Avatars;
using NSubstitute;

namespace GitUITests.Avatars;

public abstract class AvatarTestBase
{
    protected const int _size = 16;

    protected const string _email1 = "a@a.a";
    protected const string _email2 = "b@b.b";
    protected const string _email3 = "c@c.c";
    protected const string _email4 = "d@d.d";
    protected const string _emailMissing = "missing@avatar.com";

    protected const string _name1 = "John Lennon";
    protected const string _name2 = "Paul McCartney";
    protected const string _name3 = "George Harrison";
    protected const string _name4 = "Ringo Starr";
    protected const string _nameMissing = "Fifth Beatle";

    protected byte[] _img1 = null!;
    protected byte[] _img2 = null!;
    protected byte[] _img3 = null!;
    protected byte[] _img4 = null!;
    protected byte[] _imgGenerated = null!;

    protected IAvatarProvider _inner = null!;
    protected IAvatarProvider _cache = null!;
    protected IAvatarCacheCleaner _cacheCleaner => (_cache as IAvatarCacheCleaner)!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _img1 = PngImages.Fill(Color.Red, _size);
        _img2 = PngImages.Fill(Color.Green, _size);
        _img3 = PngImages.Fill(Color.Blue, _size);
        _img4 = PngImages.Fill(Color.Yellow, _size);
        _imgGenerated = PngImages.Fill(Color.Gray, _size);
    }

    [SetUp]
    public virtual void SetUp()
    {
        _inner = Substitute.For<IAvatarProvider>();

        _inner.PerformsIo.Returns(true);
        _inner.GetAvatarAsync(_email1, _name1, _size).Returns(Task.FromResult<byte[]?>(_img1));
        _inner.GetAvatarAsync(_email2, _name2, _size).Returns(Task.FromResult<byte[]?>(_img2));
        _inner.GetAvatarAsync(_email3, _name3, _size).Returns(Task.FromResult<byte[]?>(_img3));
        _inner.GetAvatarAsync(_email4, _name4, _size).Returns(Task.FromResult<byte[]?>(_img4));
        _inner.GetAvatarAsync(_emailMissing, _nameMissing, _size).Returns(Task.FromResult((byte[]?)null));
    }

    protected async Task MissAsync(string email, string name, byte[] expected = null!)
    {
        _inner.ClearReceivedCalls();

        byte[]? actual = await _cache.GetAvatarAsync(email, name, _size);

        _ = _inner.Received(1).GetAvatarAsync(email, name, _size);

        if (expected is not null)
        {
            actual.Should().BeSameAs(expected);
        }
    }

    protected async Task HitAsync(string email, string name, byte[] expected = null!)
    {
        _inner.ClearReceivedCalls();

        byte[]? actual = await _cache.GetAvatarAsync(email, name, _size);

        _ = _inner.Received(0).GetAvatarAsync(email, name, _size);

        if (expected is not null)
        {
            actual.Should().BeSameAs(expected);
        }
    }
}
