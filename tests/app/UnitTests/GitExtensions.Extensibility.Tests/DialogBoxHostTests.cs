using System.Drawing;
using GitExtensions.Extensibility;

namespace GitExtensions.ExtensibilityTests;

/// <summary>
///  The dialogs of the UI of the application, which the message boxes, task dialogs and common dialogs use off Windows
///  (docs/avalonia-port/CROSS-PLATFORM.md, phase 2); here with <see cref="DialogBoxHost.UseOnWindows"/> on every system.
/// </summary>
[NonParallelizable]
public sealed class DialogBoxHostTests
{
    private FakeDialogBoxHost _host = null!;
    private IDialogBoxHost? _previousHost;
    private bool _previousUseOnWindows;

    [SetUp]
    public void SetUp()
    {
        _previousHost = DialogBoxHost.Current;
        _previousUseOnWindows = DialogBoxHost.UseOnWindows;
        _host = new FakeDialogBoxHost();
        DialogBoxHost.Current = _host;
        DialogBoxHost.UseOnWindows = true;
    }

    [TearDown]
    public void TearDown()
    {
        DialogBoxHost.Current = _previousHost;
        DialogBoxHost.UseOnWindows = _previousUseOnWindows;
    }

    [Test]
    public void Message_boxes_are_shown_by_the_host_with_the_owner()
    {
        _host.MessageBoxResult = DialogResult.No;

        MessageBoxes.Show(new Owner(42), "Text", "Caption", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2).Should().Be(DialogResult.No);

        _host.LastOwner.Should().Be(42);
        _host.LastMessageBox.Should().Be(("Text", "Caption", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2));
    }

    [Test]
    public void Task_dialogs_are_shown_by_the_host()
    {
        TaskDialogPage page = new();
        page.Buttons.Add(TaskDialogButton.Retry);
        _host.TaskDialogResult = TaskDialogButton.Retry;

        (TaskDialog.ShowDialog(page) == TaskDialogButton.Retry).Should().BeTrue();

        _host.LastTaskDialog.Should().BeSameAs(page);
    }

    [Test]
    public void Open_file_dialog_asks_the_host_as_it_is_set_up_and_takes_the_chosen_files()
    {
        _host.FileDialogResult = new FileDialogResult(["/tmp/a.patch", "/tmp/b.patch"], 2);
        using OpenFileDialog dialog = new()
        {
            Title = "Apply patches",
            Filter = "Patch files (*.patch;*.diff)|*.patch; *.diff|All files (*.*)|*.*",
            InitialDirectory = "/tmp",
            Multiselect = true,
        };

        dialog.ShowDialog(new Owner(7)).Should().Be(DialogResult.OK);

        _host.LastOwner.Should().Be(7);
        FileDialogRequest request = _host.LastFileDialog!;
        request.Kind.Should().Be(FileDialogKind.Open);
        request.Title.Should().Be("Apply patches");
        request.FileTypes.Should().HaveCount(2);
        request.FileTypes[0].Name.Should().Be("Patch files (*.patch;*.diff)");
        request.FileTypes[0].Patterns.Should().Equal("*.patch", "*.diff");
        request.FileTypes[1].Patterns.Should().Equal("*.*");
        request.FileTypeIndex.Should().Be(1);
        request.InitialDirectory.Should().Be("/tmp");
        request.Multiselect.Should().BeTrue();
        dialog.FileNames.Should().Equal("/tmp/a.patch", "/tmp/b.patch");
        dialog.FileName.Should().Be("/tmp/a.patch");
        dialog.FilterIndex.Should().Be(2);
    }

    [Test]
    public void Save_file_dialog_asks_for_the_default_extension_and_overwrite_prompt()
    {
        _host.FileDialogResult = new FileDialogResult(["/tmp/out.zip"], 1);
        using SaveFileDialog dialog = new() { FileName = "/tmp/archive.zip", DefaultExt = ".zip", Filter = "Zip (*.zip)|*.zip" };

        dialog.ShowDialog().Should().Be(DialogResult.OK);

        FileDialogRequest request = _host.LastFileDialog!;
        request.Kind.Should().Be(FileDialogKind.Save);
        request.FileName.Should().Be("/tmp/archive.zip");
        request.DefaultExtension.Should().Be("zip");
        request.OverwritePrompt.Should().BeTrue();
        dialog.FileName.Should().Be("/tmp/out.zip");
    }

    [TestCase(".txt", "txt")]
    [TestCase("txt", "txt")]
    [TestCase(null, null)]
    public void The_default_extension_is_kept_without_its_dot(string? extension, string? expected)
    {
        // As WinForms: the file history builds its filter as "*." + DefaultExt, from Path.GetExtension.
        using SaveFileDialog dialog = new() { DefaultExt = extension };

        dialog.DefaultExt.Should().Be(expected);
    }

    [Test]
    public void Save_file_dialog_without_AddExtension_asks_for_no_default_extension()
    {
        using SaveFileDialog dialog = new() { DefaultExt = "zip", AddExtension = false };

        dialog.ShowDialog().Should().Be(DialogResult.Cancel);

        _host.LastFileDialog!.DefaultExtension.Should().BeNull();
    }

    [Test]
    public void Folder_dialog_asks_for_a_folder_from_the_selected_path()
    {
        _host.FileDialogResult = new FileDialogResult(["/home/user/repo"], 1);
        using FolderBrowserDialog dialog = new() { Description = "Clone to", SelectedPath = "/home/user" };

        dialog.ShowDialog().Should().Be(DialogResult.OK);

        _host.LastFileDialog!.Kind.Should().Be(FileDialogKind.Folder);
        _host.LastFileDialog.Title.Should().Be("Clone to");
        _host.LastFileDialog.InitialDirectory.Should().Be("/home/user");
        dialog.SelectedPath.Should().Be("/home/user/repo");
    }

    [Test]
    public void Cancelled_file_dialog_keeps_the_file_name()
    {
        using OpenFileDialog dialog = new() { FileName = "keep.txt" };

        dialog.ShowDialog().Should().Be(DialogResult.Cancel);

        dialog.FileName.Should().Be("keep.txt");
        dialog.FileNames.Should().BeEmpty();
    }

    [Test]
    public void Color_dialog_takes_the_chosen_color()
    {
        _host.ColorResult = Color.FromArgb(255, 10, 20, 30);
        using ColorDialog dialog = new() { Color = Color.Red };

        dialog.ShowDialog().Should().Be(DialogResult.OK);

        _host.LastColor.Should().Be(Color.Red);
        dialog.Color.Should().Be(Color.FromArgb(255, 10, 20, 30));
    }

    [Test]
    public void Cancelled_color_dialog_keeps_the_color()
    {
        using ColorDialog dialog = new() { Color = Color.Red };

        dialog.ShowDialog().Should().Be(DialogResult.Cancel);

        dialog.Color.Should().Be(Color.Red);
    }

    [Test]
    public void Font_picker_asks_the_host_for_a_font_of_fixed_pitch()
    {
        FontDescriptor font = new("DejaVu Sans Mono", 10);
        _host.FontResult = new FontDescriptor("Fira Code", 11, IsBold: true);

        FontPicker.Show(null, font, fixedPitchOnly: true).Should().Be(new FontDescriptor("Fira Code", 11, IsBold: true));

        _host.LastFont.Should().Be((font, true));
    }

    private sealed record Owner(nint Handle) : IWin32Window;

    private sealed class FakeDialogBoxHost : IDialogBoxHost
    {
        public nint LastOwner { get; private set; }

        public (string, string, MessageBoxButtons, MessageBoxIcon, MessageBoxDefaultButton)? LastMessageBox { get; private set; }

        public DialogResult MessageBoxResult { get; set; }

        public TaskDialogPage? LastTaskDialog { get; private set; }

        public TaskDialogButton TaskDialogResult { get; set; } = TaskDialogButton.OK;

        public FileDialogRequest? LastFileDialog { get; private set; }

        public FileDialogResult? FileDialogResult { get; set; }

        public Color? LastColor { get; private set; }

        public Color? ColorResult { get; set; }

        public (FontDescriptor?, bool)? LastFont { get; private set; }

        public FontDescriptor? FontResult { get; set; }

        public DialogResult ShowMessageBox(nint owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            LastOwner = owner;
            LastMessageBox = (text, caption, buttons, icon, defaultButton);
            return MessageBoxResult;
        }

        public TaskDialogButton ShowTaskDialog(nint owner, TaskDialogPage page)
        {
            LastOwner = owner;
            LastTaskDialog = page;
            return TaskDialogResult;
        }

        public FileDialogResult? ShowFileDialog(nint owner, FileDialogRequest request)
        {
            LastOwner = owner;
            LastFileDialog = request;
            return FileDialogResult;
        }

        public Color? ShowColorDialog(nint owner, Color color)
        {
            LastOwner = owner;
            LastColor = color;
            return ColorResult;
        }

        public FontDescriptor? ShowFontDialog(nint owner, FontDescriptor? font, bool fixedPitchOnly)
        {
            LastOwner = owner;
            LastFont = (font, fixedPitchOnly);
            return FontResult;
        }
    }
}
