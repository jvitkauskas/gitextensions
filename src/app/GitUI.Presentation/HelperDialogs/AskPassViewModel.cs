using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>
///  The prompt of ssh and git off Windows (<c>GitExtensions askpass &lt;prompt&gt;</c>, set as <c>SSH_ASKPASS</c>;
///  docs/avalonia-port/CROSS-PLATFORM.md, phase 4): a passphrase, a password, a user name or the confirmation of a host key.
/// </summary>
public sealed partial class AskPassViewModel(string prompt, DialogBoxStrings strings) : DialogViewModel
{
    public DialogBoxStrings Strings { get; } = strings;

    /// <summary>The prompt of ssh or git, as it is.</summary>
    public string Prompt { get; } = prompt.Trim();

    /// <summary>Whether the answer is hidden (a password, a passphrase or a PIN, not a user name or a yes/no question).</summary>
    public bool IsSecret { get; } = IsSecretPrompt(prompt);

    [ObservableProperty]
    public partial string Input { get; set; } = "";

    /// <summary>The accepted answer, or <see langword="null"/> if cancelled.</summary>
    public string? Answer { get; private set; }

    /// <summary>Whether the answer to <paramref name="prompt"/> is a secret (else shown, as for "Username for ..." or "(yes/no)").</summary>
    public static bool IsSecretPrompt(string prompt)
        => prompt.Contains("password", StringComparison.OrdinalIgnoreCase)
            || prompt.Contains("passphrase", StringComparison.OrdinalIgnoreCase)
            || prompt.Contains("PIN", StringComparison.Ordinal)
            || !(prompt.Contains("yes/no", StringComparison.OrdinalIgnoreCase) || prompt.Contains("username", StringComparison.OrdinalIgnoreCase));

    [RelayCommand]
    private void Ok()
    {
        Answer = Input;
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}
