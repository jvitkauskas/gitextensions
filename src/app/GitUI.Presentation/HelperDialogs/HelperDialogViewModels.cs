using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>View model of the SSH authentication error dialog (port of <c>FormPuttyError</c>).</summary>
public sealed partial class PuttyErrorViewModel : DialogViewModel
{
    private readonly Func<string?> _browseAndLoadKey;

    /// <param name="browseAndLoadKey">
    ///  Lets the user pick a private key and loads it into the SSH agent; returns its path, or <see langword="null"/> if cancelled.
    /// </param>
    public PuttyErrorViewModel(PuttyErrorStrings strings, Func<string?> browseAndLoadKey)
    {
        Strings = strings;
        _browseAndLoadKey = browseAndLoadKey;
    }

    public PuttyErrorStrings Strings { get; }

    /// <summary>The loaded key, if the user loaded one.</summary>
    public string? KeyPath { get; private set; }

    /// <summary>Whether the command should be retried (the dialog result <c>Retry</c> of the WinForms form).</summary>
    public bool ShouldRetry { get; private set; }

    [RelayCommand]
    private void LoadSshKey()
    {
        string? keyPath = _browseAndLoadKey();
        if (!string.IsNullOrEmpty(keyPath))
        {
            KeyPath = keyPath;
            Retry();
        }
    }

    [RelayCommand]
    private void Retry()
    {
        ShouldRetry = true;
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

/// <summary>The authentication methods of a build server (mirrors <c>BuildServerCredentialsType</c>).</summary>
public enum BuildServerAuthentication
{
    Guest,
    UsernameAndPassword,
    BearerToken,
}

/// <summary>
///  View model of the build server credentials dialog (port of <c>FormBuildServerCredentials</c>, which is not translated).
/// </summary>
public sealed partial class BuildServerCredentialsViewModel : DialogViewModel
{
    public BuildServerCredentialsViewModel(string buildServerUniqueKey)
    {
        Header = $"Please enter the credentials for the build server at {buildServerUniqueKey}.";
    }

    public string Title => "Enter credentials";

    public string Header { get; }

    public string GuestAccessText => "Guest access";

    public string AuthenticatedUserText => "Authenticated user";

    public string BearerTokenText => "Bearer token";

    public string UsernameText => "Username:";

    public string PasswordText => "Password:";

    public string BearerTokenLabelText => "Bearer token:";

    public string OkText => "OK";

    public string CancelText => "Cancel";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGuest), nameof(IsUsernameAndPassword), nameof(IsBearerToken))]
    public partial BuildServerAuthentication Authentication { get; set; }

    public bool IsGuest
    {
        get => Authentication == BuildServerAuthentication.Guest;
        set => SetAuthentication(value, BuildServerAuthentication.Guest);
    }

    public bool IsUsernameAndPassword
    {
        get => Authentication == BuildServerAuthentication.UsernameAndPassword;
        set => SetAuthentication(value, BuildServerAuthentication.UsernameAndPassword);
    }

    public bool IsBearerToken
    {
        get => Authentication == BuildServerAuthentication.BearerToken;
        set => SetAuthentication(value, BuildServerAuthentication.BearerToken);
    }

    [ObservableProperty]
    public partial string Username { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial string BearerToken { get; set; } = "";

    private void SetAuthentication(bool isChecked, BuildServerAuthentication authentication)
    {
        if (isChecked)
        {
            Authentication = authentication;
        }
    }

    [RelayCommand]
    private void Ok() => Close(accepted: true);

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

/// <summary>An item of a check list; <see cref="Value"/> is the underlying object.</summary>
public sealed partial class CheckableItem(object value, string text) : ObservableObject
{
    public object Value { get; } = value;

    public string Text { get; } = text;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}

/// <summary>View model of the branch multi-selection dialog (port of <c>FormSelectMultipleBranches</c>).</summary>
public sealed partial class SelectMultipleBranchesViewModel : DialogViewModel
{
    /// <param name="branches">The branches (any object) with their display names.</param>
    /// <param name="selectedNames">The names of the initially selected branches.</param>
    public SelectMultipleBranchesViewModel(SelectMultipleBranchesStrings strings, IEnumerable<(object Branch, string Name)> branches, IEnumerable<string> selectedNames)
    {
        Strings = strings;
        HashSet<string> selected = [.. selectedNames];
        Branches = [.. branches.Select(b => new CheckableItem(b.Branch, b.Name) { IsChecked = selected.Contains(b.Name) })];
    }

    public SelectMultipleBranchesStrings Strings { get; }

    public IReadOnlyList<CheckableItem> Branches { get; }

    /// <summary>The checked branches, in list order.</summary>
    public IReadOnlyList<object> SelectedBranches => [.. Branches.Where(b => b.IsChecked).Select(b => b.Value)];

    [RelayCommand]
    private void Ok() => Close(accepted: true);
}
