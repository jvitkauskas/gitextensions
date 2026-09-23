using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The Avalonia file viewer (the viewing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md, phase 3):
///  the text editor for texts and diffs, or an image.
/// </summary>
public partial class FileViewerView : UserControl
{
    private FileViewerViewModel? _viewModel;

    public FileViewerView()
    {
        InitializeComponent();

        // As internalFileViewer.MouseMove and MouseLeave: the toolbar shows while the mouse is over the text.
        textView.PointerMoved += (_, _) => toolbar.IsVisible = true;
        PointerExited += (_, _) => toolbar.IsVisible = false;
        nextChangeButton.Click += (_, _) => GoToChange(backwards: false);
        previousChangeButton.Click += (_, _) => GoToChange(backwards: true);
    }

    /// <summary>The text editor, e.g. for tests.</summary>
    public TextEditorView TextView => textView;

    /// <summary>The image shown instead of the text, e.g. for tests.</summary>
    public Image ImageView => image;

    /// <summary>The toolbar of the options, e.g. for tests.</summary>
    public Border Toolbar => toolbar;

    /// <summary>
    ///  As <c>NextChangeButtonClick</c> and <c>PreviousChangeButtonClick</c>: the caret goes to the next (or previous) change,
    ///  shown below its lines of context.
    /// </summary>
    public void GoToChange(bool backwards)
    {
        AvaloniaEdit.TextEditor editor = textView.Editor;
        if (_viewModel?.GetChangeLine(editor.TextArea.Caret.Line, backwards) is not int line)
        {
            return;
        }

        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        int firstVisibleLine = Math.Max(1, line - _viewModel.Settings.NumberOfContextLines - 1);
        editor.ScrollToVerticalOffset(editor.TextArea.TextView.GetVisualTopByDocumentLine(firstVisibleLine));
        editor.TextArea.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as FileViewerViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        ShowImage();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileViewerViewModel.Image))
        {
            ShowImage();
        }
    }

    private void ShowImage()
    {
        (image.Source as IDisposable)?.Dispose();
        image.Source = null;
        if (_viewModel?.Image is { } bytes)
        {
            try
            {
                using MemoryStream stream = new(bytes);
                image.Source = new Bitmap(stream);
            }
            catch (Exception)
            {
                // Not an image Avalonia can decode (as FileViewer.CreateImage, which shows the text then).
            }
        }

        imageViewer.IsVisible = image.Source is not null;
        textView.IsVisible = !imageViewer.IsVisible;
    }
}
