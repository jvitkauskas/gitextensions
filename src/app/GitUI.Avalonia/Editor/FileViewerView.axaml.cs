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
    }

    /// <summary>The text editor, e.g. for tests.</summary>
    public TextEditorView TextView => textView;

    /// <summary>The image shown instead of the text, e.g. for tests.</summary>
    public Image ImageView => image;

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
