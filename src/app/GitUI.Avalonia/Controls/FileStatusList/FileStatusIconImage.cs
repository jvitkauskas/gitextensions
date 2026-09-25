using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Avalonia.Controls.FileStatusList;

/// <summary>
///  The icon of a node of the file status list: the image of its key, and for a plain file the icon of its type in the shell
///  (as <c>FileStatusList.LoadFileIcons</c>: read in the background, once for each extension).
/// </summary>
public sealed class FileStatusIconImage : Image
{
    public static readonly StyledProperty<string?> IconKeyProperty = AvaloniaProperty.Register<FileStatusIconImage, string?>(nameof(IconKey));

    public static readonly StyledProperty<string?> FileNameProperty = AvaloniaProperty.Register<FileStatusIconImage, string?>(nameof(FileName));

    private static readonly Dictionary<string, Bitmap?> _fileTypeIcons = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<WeakReference<FileStatusIconImage>>> _waiting = new(StringComparer.OrdinalIgnoreCase);

    static FileStatusIconImage()
    {
        IconKeyProperty.Changed.AddClassHandler<FileStatusIconImage>((image, _) => image.Update());
        FileNameProperty.Changed.AddClassHandler<FileStatusIconImage>((image, _) => image.Update());
    }

    public FileStatusIconImage()
    {
        Width = 16;
        Height = 16;
    }

    /// <summary>
    ///  Reads the icon of the type of a file (a relative path) in the shell as PNG, <see langword="null"/> for none (as
    ///  <c>FileAssociatedIconProvider</c>); set by the application, none without.
    /// </summary>
    public static Func<string, byte[]?>? LoadFileTypeIcon { get; set; }

    /// <summary>The image key of the node (<see cref="FileStatusNode.IconKey"/>).</summary>
    public string? IconKey
    {
        get => GetValue(IconKeyProperty);
        set => SetValue(IconKeyProperty, value);
    }

    /// <summary>The file whose type the icon of a plain file shows (<see cref="FileStatusNode.IconFileName"/>).</summary>
    public string? FileName
    {
        get => GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    private void Update()
    {
        Source = IconKey is { } key ? FileStatusIconConverter.GetIcon(key) : null;
        if (IconKey != FileStatusIcons.DefaultFileImage || LoadFileTypeIcon is not { } load || Path.GetExtension(FileName) is not { Length: > 0 } extension)
        {
            return;
        }

        if (_fileTypeIcons.TryGetValue(extension, out Bitmap? icon))
        {
            Source = icon ?? Source;
            return;
        }

        if (_waiting.TryGetValue(extension, out List<WeakReference<FileStatusIconImage>>? waiting))
        {
            waiting.Add(new WeakReference<FileStatusIconImage>(this));
            return;
        }

        _waiting[extension] = [new WeakReference<FileStatusIconImage>(this)];
        string fileName = FileName!;
        _ = Task.Run(() =>
        {
            byte[]? png = null;
            try
            {
                png = load(fileName);
            }
            catch (Exception)
            {
                // No icon: the default one stays.
            }

            Dispatcher.UIThread.Post(() => OnLoaded(extension, png));
        });
    }

    private static void OnLoaded(string extension, byte[]? png)
    {
        Bitmap? icon = null;
        if (png is not null)
        {
            try
            {
                using MemoryStream stream = new(png);
                icon = new Bitmap(stream);
            }
            catch (Exception)
            {
                // Not an image Avalonia can decode.
            }
        }

        _fileTypeIcons[extension] = icon;
        if (_waiting.Remove(extension, out List<WeakReference<FileStatusIconImage>>? waiting))
        {
            foreach (WeakReference<FileStatusIconImage> reference in waiting)
            {
                if (reference.TryGetTarget(out FileStatusIconImage? image))
                {
                    image.Update();
                }
            }
        }
    }
}
