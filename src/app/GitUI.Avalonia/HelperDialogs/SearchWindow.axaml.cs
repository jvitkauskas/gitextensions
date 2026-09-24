using Avalonia.Input;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>
///  Avalonia port of <c>SearchWindow</c>: finds an item (e.g. a file) by a name typed, from the candidates matching it
///  (read in the background, as <c>SearchControl</c>); Enter picks the chosen (or the first) match, Escape cancels.
/// </summary>
public partial class SearchWindow : DialogWindow
{
    private readonly Func<string, IEnumerable<object>> _getCandidates;

    public SearchWindow()
        : this(_ => [], "")
    {
    }

    public SearchWindow(Func<string, IEnumerable<object>> getCandidates, string label)
    {
        _getCandidates = getCandidates;
        InitializeComponent();
        this.label.Text = label;
        searchBox.AsyncPopulator = PopulateAsync;
        searchBox.AddHandler(KeyDownEvent, OnSearchBoxKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Opened += (_, _) => searchBox.Focus();
    }

    /// <summary>The chosen item, or <see langword="null"/> if cancelled.</summary>
    public object? SelectedItem { get; private set; }

    /// <summary>The matches of the last text searched.</summary>
    public IReadOnlyList<object> Matches { get; private set; } = [];

    private async Task<IEnumerable<object>> PopulateAsync(string? text, CancellationToken cancellationToken)
    {
        string searchText = text ?? "";
        List<object> matches = await Task.Run(() => _getCandidates(searchText).Take(1000).ToList(), cancellationToken);
        Matches = matches;
        return matches;
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                SelectedItem = searchBox.SelectedItem ?? (Matches.Count > 0 ? Matches[0] : null);
                e.Handled = true;
                Close();
                break;
            case Key.Escape when !searchBox.IsDropDownOpen:
                SelectedItem = null;
                e.Handled = true;
                Close();
                break;
        }
    }
}
