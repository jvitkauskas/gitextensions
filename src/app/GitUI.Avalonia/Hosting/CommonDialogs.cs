using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Presentation.HelperDialogs;
using DrawingColor = System.Drawing.Color;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The common dialogs of Avalonia (files and folders, colors, fonts), shown off Windows instead of the native ones
///  (<see cref="IDialogBoxHost"/>, docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
public static class AvaloniaCommonDialogs
{
    /// <summary>
    ///  Runs <paramref name="action"/> with the window of <paramref name="owner"/> (else the active window) as the top level of
    ///  a picker of the storage provider, or with a transparent window while none is open (e.g. at startup).
    /// </summary>
    public static T WithTopLevel<T>(nint owner, Func<TopLevel, Task<T>> action)
    {
        if ((AvaloniaDialogHost.FindOpenWindow(owner) ?? AvaloniaDialogHost.GetActiveWindow()) is { } window)
        {
            return AvaloniaUi.WaitFor(action(window));
        }

        Window host = new()
        {
            Width = 1,
            Height = 1,
            WindowDecorations = WindowDecorations.None,
            ShowInTaskbar = false,
            Opacity = 0,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        host.Show();
        try
        {
            return AvaloniaUi.WaitFor(action(host));
        }
        finally
        {
            host.Close();
        }
    }

    /// <summary>A file dialog of the storage provider of <paramref name="topLevel"/> for <paramref name="request"/>.</summary>
    public static async Task<FileDialogResult?> ShowFileDialogAsync(TopLevel topLevel, FileDialogRequest request)
    {
        IStorageProvider storage = topLevel.StorageProvider;
        string? initialDirectory = !string.IsNullOrEmpty(request.InitialDirectory) ? request.InitialDirectory
            : !string.IsNullOrEmpty(request.FileName) ? Path.GetDirectoryName(request.FileName)
            : null;
        IStorageFolder? startLocation = await TryGetFolderAsync(storage, initialDirectory);
        List<FilePickerFileType> fileTypes = [.. request.FileTypes.Select(t => new FilePickerFileType(t.Name) { Patterns = [.. t.Patterns] })];
        string? title = string.IsNullOrEmpty(request.Title) ? null : request.Title;

        IReadOnlyList<string> paths;
        switch (request.Kind)
        {
            case FileDialogKind.Folder:
                IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                    SuggestedStartLocation = startLocation,
                });
                paths = [.. folders.Select(f => f.TryGetLocalPath()).OfType<string>()];
                break;
            case FileDialogKind.Save:
                IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = title,
                    SuggestedFileName = string.IsNullOrEmpty(request.FileName) ? null : Path.GetFileName(request.FileName),
                    DefaultExtension = request.DefaultExtension,
                    FileTypeChoices = fileTypes,
                    ShowOverwritePrompt = request.OverwritePrompt,
                    SuggestedStartLocation = startLocation,
                });
                paths = file?.TryGetLocalPath() is { } path ? [path] : [];
                break;
            default:
                IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = request.Multiselect,
                    FileTypeFilter = fileTypes,
                    SuggestedStartLocation = startLocation,
                });
                paths = [.. files.Select(f => f.TryGetLocalPath()).OfType<string>()];
                break;
        }

        return paths.Count == 0 ? null : new FileDialogResult(paths, request.FileTypeIndex);
    }

    private static async Task<IStorageFolder?> TryGetFolderAsync(IStorageProvider storage, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return await storage.TryGetFolderFromPathAsync(Path.GetFullPath(path));
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>The color dialog of Avalonia: a color view with OK and Cancel.</summary>
public sealed class ColorPickerWindow : DialogWindow
{
    public ColorPickerWindow(DrawingColor color, DialogBoxStrings strings)
    {
        Title = strings.ColorTitle.Text;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;

        ColorView = new ColorView
        {
            Color = global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B),
            IsAlphaEnabled = false,
            IsAlphaVisible = false,
            Margin = new Thickness(12),
            Name = "colorView",
        };

        Button ok = DialogBoxParts.CreateButton(strings.Ok.Text, "okButton");
        ok.IsDefault = true;
        ok.Classes.Add("accent");
        ok.Click += (_, _) =>
        {
            global::Avalonia.Media.Color chosen = ColorView.Color;
            SelectedColor = DrawingColor.FromArgb(255, chosen.R, chosen.G, chosen.B);
            Close();
        };
        Button cancel = DialogBoxParts.CreateButton(strings.Cancel.Text, "cancelButton");
        cancel.IsCancel = true;
        cancel.Click += (_, _) => Close();

        StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Children = { ok, cancel } };
        Border footer = new() { Child = buttons };
        footer.Classes.Add("dialogFooter");
        DockPanel content = new();
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);
        content.Children.Add(ColorView);
        Content = content;
    }

    public ColorView ColorView { get; }

    /// <summary>The chosen color, once accepted.</summary>
    public DrawingColor? SelectedColor { get; private set; }
}

/// <summary>The font dialog of Avalonia: the families of the system (of fixed pitch only, for code), the size and style, a sample.</summary>
public sealed class FontPickerWindow : DialogWindow
{
    private readonly bool _fixedPitchOnly;
    private readonly List<string> _families;
    private readonly TextBlock _sample;

    public FontPickerWindow(FontDescriptor? font, bool fixedPitchOnly, DialogBoxStrings strings)
    {
        _fixedPitchOnly = fixedPitchOnly;
        Title = strings.FontTitle.Text;
        Width = 460;
        Height = 420;
        MinWidth = 360;
        MinHeight = 300;

        _families = [.. FontManager.Current.SystemFonts.Select(f => f.Name).Where(name => !fixedPitchOnly || IsFixedPitch(name)).Distinct().Order(StringComparer.CurrentCultureIgnoreCase)];

        FamilyFilter = new TextBox { Name = "familyFilter" };
        Families = new ListBox { ItemsSource = _families, Name = "families" };
        FamilyFilter.TextChanged += (_, _) =>
        {
            string filter = FamilyFilter.Text ?? "";
            Families.ItemsSource = _families.Where(f => f.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        };

        FontSizeBox = new NumericUpDown { Minimum = 4, Maximum = 96, Increment = 1, FormatString = "0.#", Value = (decimal)(font?.SizeInPoints ?? 10), Width = 110, Name = "size" };
        Bold = new CheckBox { Content = strings.Bold.AccessKeyText, IsChecked = font?.IsBold == true, Name = "bold" };
        Italic = new CheckBox { Content = strings.Italic.AccessKeyText, IsChecked = font?.IsItalic == true, Name = "italic" };
        _sample = new TextBlock { Text = "AaBbYyZz 0123 {}[]", TextWrapping = TextWrapping.Wrap, Name = "sample" };

        string? current = font is null ? null : _families.FirstOrDefault(f => string.Equals(f, font.FamilyName, StringComparison.OrdinalIgnoreCase));
        Families.SelectedItem = current ?? _families.FirstOrDefault();
        Families.SelectionChanged += (_, _) => UpdateSample();
        FontSizeBox.ValueChanged += (_, _) => UpdateSample();
        Bold.IsCheckedChanged += (_, _) => UpdateSample();
        Italic.IsCheckedChanged += (_, _) => UpdateSample();

        Button ok = DialogBoxParts.CreateButton(strings.Ok.Text, "okButton");
        ok.IsDefault = true;
        ok.Classes.Add("accent");
        ok.Click += (_, _) =>
        {
            SelectedFont = CurrentFont;
            Close();
        };
        Button cancel = DialogBoxParts.CreateButton(strings.Cancel.Text, "cancelButton");
        cancel.IsCancel = true;
        cancel.Click += (_, _) => Close();

        StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Children = { ok, cancel } };
        Border footer = new() { Child = buttons };
        footer.Classes.Add("dialogFooter");

        Grid body = new() { Margin = new Thickness(9), RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), ColumnDefinitions = new ColumnDefinitions("*,Auto"), RowSpacing = 6, ColumnSpacing = 12 };
        Label familyLabel = new() { Content = strings.FontFamily.AccessKeyText, Target = FamilyFilter, Padding = default };
        Label sizeLabel = new() { Content = strings.FontSize.AccessKeyText, Target = FontSizeBox, Padding = default };
        Grid.SetColumn(sizeLabel, 1);
        Grid.SetRow(FamilyFilter, 1);
        Grid.SetRow(Families, 2);
        StackPanel style = new() { Spacing = 6, Children = { FontSizeBox, Bold, Italic } };
        Grid.SetRow(style, 1);
        Grid.SetRowSpan(style, 2);
        Grid.SetColumn(style, 1);
        HeaderedContentControl sampleBox = new() { Header = strings.Sample.Text, Content = new Border { Padding = new Thickness(6), MinHeight = 60, Child = _sample } };
        Grid.SetRow(sampleBox, 3);
        Grid.SetColumnSpan(sampleBox, 2);
        body.Children.Add(familyLabel);
        body.Children.Add(sizeLabel);
        body.Children.Add(FamilyFilter);
        body.Children.Add(Families);
        body.Children.Add(style);
        body.Children.Add(sampleBox);

        DockPanel content = new();
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);
        content.Children.Add(body);
        Content = content;

        UpdateSample();
        Opened += (_, _) => Families.ScrollIntoView(Families.SelectedItem ?? 0);
    }

    public TextBox FamilyFilter { get; }

    public ListBox Families { get; }

    public NumericUpDown FontSizeBox { get; }

    public CheckBox Bold { get; }

    public CheckBox Italic { get; }

    /// <summary>The families offered (of fixed pitch only, if asked).</summary>
    public IReadOnlyList<string> AllFamilies => _families;

    /// <summary>The font as chosen so far.</summary>
    public FontDescriptor? CurrentFont
        => Families.SelectedItem is string family
            ? new FontDescriptor(family, (float)(FontSizeBox.Value ?? 10), Bold.IsChecked == true, Italic.IsChecked == true)
            : null;

    /// <summary>The chosen font, once accepted.</summary>
    public FontDescriptor? SelectedFont { get; private set; }

    public bool FixedPitchOnly => _fixedPitchOnly;

    /// <summary>Whether the glyphs of a family have one width (e.g. for code), measured: Avalonia does not tell the pitch.</summary>
    public static bool IsFixedPitch(string family)
    {
        Typeface typeface = new(family);
        double MeasureWidth(string text)
            => new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 20, Brushes.Black).WidthIncludingTrailingWhitespace;
        double narrow = MeasureWidth("iiiiiiiiii");
        return narrow > 0 && Math.Abs(narrow - MeasureWidth("WWWWWWWWWW")) < 0.5;
    }

    private void UpdateSample()
    {
        if (CurrentFont is not { } font)
        {
            return;
        }

        _sample.FontFamily = new FontFamily(font.FamilyName);
        _sample.FontSize = font.SizeInPixels;
        _sample.FontWeight = font.IsBold ? FontWeight.Bold : FontWeight.Normal;
        _sample.FontStyle = font.IsItalic ? FontStyle.Italic : FontStyle.Normal;
    }
}

/// <summary>The clipboard of Avalonia for <see cref="ClipboardUtil"/> off Windows (the clipboard of the active window).</summary>
public sealed class AvaloniaClipboardBackend : IClipboardBackend
{
    public bool TrySetText(string text)
        => AvaloniaDialogHost.GetActiveWindow()?.Clipboard is { } clipboard && Run(clipboard.SetTextAsync(text));

    /// <summary>Only the text: the clipboard of Avalonia has no HTML format on every system.</summary>
    public bool TrySetHtml(string html, string text) => TrySetText(text);

    public bool TryGetText([NotNullWhen(returnValue: true)] out string? text)
    {
        text = AvaloniaDialogHost.GetActiveWindow()?.Clipboard is { } clipboard ? AvaloniaUi.WaitFor(clipboard.TryGetTextAsync()) : null;
        return text is not null;
    }

    private static bool Run(Task task)
    {
        try
        {
            return AvaloniaUi.WaitFor(CompleteAsync(task));
        }
        catch (Exception)
        {
            return false;
        }

#pragma warning disable VSTHRD003 // The task of the clipboard, awaited on the UI thread that started it.
        static async Task<bool> CompleteAsync(Task task)
        {
            await task;
            return true;
        }
#pragma warning restore VSTHRD003
    }
}
