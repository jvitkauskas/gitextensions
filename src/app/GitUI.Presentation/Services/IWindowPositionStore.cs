namespace GitUI.Presentation.Services;

/// <summary>
///  A saved window position and size, in physical pixels at <paramref name="Dpi"/>.
/// </summary>
public sealed record WindowPlacement(int X, int Y, int Width, int Height, int Dpi, bool IsMaximized);

/// <summary>
///  Persists window placements by name. The host stores them in the same file and under the same names
///  (the WinForms form names) as <c>WindowPositionManager</c>, so positions carry over between the two UIs.
/// </summary>
public interface IWindowPositionStore
{
    WindowPlacement? Load(string name);

    void Save(string name, WindowPlacement placement);
}
