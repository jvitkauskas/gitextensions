using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using GitUI.Avalonia.Controls.FileStatusList;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The icons of the types of the files in the shell (<c>FileStatusList.LoadFileIcons</c>).</summary>
[TestFixture]
public sealed class FileStatusIconImageTests : HeadlessTest
{
    [Test]
    public Task A_plain_file_gets_the_icon_of_its_type_once_read_for_its_extension() => OnUiThreadAsync(() =>
    {
        List<string> reads = [];
        FileStatusIconImage.LoadFileTypeIcon = fileName =>
        {
            lock (reads)
            {
                reads.Add(fileName);
            }

            using Stream asset = AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/Settings.png"));
            using MemoryStream stream = new();
            asset.CopyTo(stream);
            return stream.ToArray();
        };
        try
        {
            FileStatusIconImage first = new() { IconKey = FileStatusIcons.DefaultFileImage, FileName = "src/First.xyz1" };
            FileStatusIconImage second = new() { IconKey = FileStatusIcons.DefaultFileImage, FileName = "Second.xyz1" };
            FileStatusIconImage status = new() { IconKey = "FileStatusModified", FileName = "Third.xyz1" };
            IImage? defaultIcon = FileStatusIconConverter.GetIcon(FileStatusIcons.DefaultFileImage);
            first.Source.Should().BeSameAs(defaultIcon, "the default icon until the icon of the type is read");

            // The icon is read in the background, then set on the UI thread.
            for (int i = 0; i < 200 && ReferenceEquals(first.Source, defaultIcon); i++)
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.RunJobs();
            }

            first.Source.Should().NotBeSameAs(defaultIcon);
            second.Source.Should().BeSameAs(first.Source, "the icons are read once for each extension");
            status.Source.Should().BeSameAs(FileStatusIconConverter.GetIcon("FileStatusModified"), "the icon of a status is kept");
            reads.Should().Equal("src/First.xyz1");
        }
        finally
        {
            FileStatusIconImage.LoadFileTypeIcon = null;
        }
    });
}
