using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Presentation.UserControls;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  Avalonia port of <c>HelpImageDisplayUserControl</c>: shows <see cref="Image1"/>, or <see cref="Image2"/> while hovered
///  if <see cref="HelpImageViewModel.IsOnHoverShowImage2"/>.
/// </summary>
public partial class HelpImageView : UserControl
{
    public static readonly StyledProperty<IImage?> Image1Property = AvaloniaProperty.Register<HelpImageView, IImage?>(nameof(Image1));

    public static readonly StyledProperty<IImage?> Image2Property = AvaloniaProperty.Register<HelpImageView, IImage?>(nameof(Image2));

    private HelpImageViewModel? _viewModel;

    public HelpImageView()
    {
        InitializeComponent();
    }

    public IImage? Image1
    {
        get => GetValue(Image1Property);
        set => SetValue(Image1Property, value);
    }

    public IImage? Image2
    {
        get => GetValue(Image2Property);
        set => SetValue(Image2Property, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Image1Property || change.Property == Image2Property || change.Property == IsPointerOverProperty)
        {
            UpdateImage();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as HelpImageViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateImage();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HelpImageViewModel.IsOnHoverShowImage2))
        {
            UpdateImage();
        }
    }

    private void UpdateImage()
        => helpImage.Source = _viewModel?.IsOnHoverShowImage2 == true && IsPointerOver && Image2 is not null ? Image2 : Image1;
}
