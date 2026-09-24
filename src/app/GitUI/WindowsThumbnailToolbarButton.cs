namespace GitUI;

public sealed class WindowsThumbnailToolbarButton
{
    public WindowsThumbnailToolbarButton(string text, Image image, EventHandler click)
    {
        Text = text;
        Image = image;
        Click = click;
    }

    public EventHandler Click { get; }
    public Image Image { get; }
    public string Text { get; }
}
