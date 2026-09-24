using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Presentation;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.ProxySwitcher;

/// <summary>Strings of the Avalonia port of <see cref="ProxySwitcherForm"/>; ids match the form.</summary>
public sealed class ProxySwitcherStrings : ViewStrings
{
    public ProxySwitcherStrings()
        : base(nameof(ProxySwitcherForm))
    {
        Title = Add("_pluginDescription", "Text", "Proxy Switcher");
        LocalHttpProxy = Add("LocalHttpProxy_Label", "Text", "Local http.proxy:");
        GlobalHttpProxy = Add("GlobalHttpProxy_Label", "Text", "Global http.proxy:");
        ApplyGlobally = Add("ApplyGlobally_CheckBox", "Text", "Apply globally");
        SetProxy = Add("SetProxy_Button", "Text", "Set proxy");
        UnsetProxy = Add("UnsetProxy_Button", "Text", "Unset proxy");
        PleaseSetProxy = Add("_pleaseSetProxy", "Text", "There is no proxy configured. Please set the proxy host in the plugin settings.");
    }

    public TranslatedText Title { get; }

    public TranslatedText LocalHttpProxy { get; }

    public TranslatedText GlobalHttpProxy { get; }

    public TranslatedText ApplyGlobally { get; }

    public TranslatedText SetProxy { get; }

    public TranslatedText UnsetProxy { get; }

    public TranslatedText PleaseSetProxy { get; }
}

/// <summary>The settings of the plugin that the proxy is built from.</summary>
public sealed record ProxySettings(string Username, string Password, string HttpProxy, string HttpProxyPort);

/// <summary>The git configuration that the Avalonia port of <see cref="ProxySwitcherForm"/> reads and writes.</summary>
public interface IProxySwitcherGit
{
    /// <summary>The effective <c>http.proxy</c> of the repository (<c>GetEffectiveSetting</c>).</summary>
    string GetEffectiveProxy();

    /// <summary>The <c>http.proxy</c> of the global git config.</summary>
    string GetGlobalProxy();

    /// <summary>Runs git with <paramref name="arguments"/>.</summary>
    void Run(ArgumentString arguments);
}

/// <summary>View model of the Avalonia port of <see cref="ProxySwitcherForm"/>.</summary>
public sealed partial class ProxySwitcherViewModel : DialogViewModel
{
    private readonly ProxySettings _settings;
    private readonly IProxySwitcherGit _git;

    public ProxySwitcherViewModel(ProxySwitcherStrings strings, ProxySettings settings, IProxySwitcherGit git)
    {
        Strings = strings;
        _settings = settings;
        _git = git;
        RefreshProxy();
    }

    public ProxySwitcherStrings Strings { get; }

    [ObservableProperty]
    public partial string LocalHttpProxy { get; private set; } = "";

    [ObservableProperty]
    public partial string GlobalHttpProxy { get; private set; } = "";

    [ObservableProperty]
    public partial bool ApplyGlobally { get; set; }

    [GeneratedRegex(@":(.*)@", RegexOptions.ExplicitCapture)]
    private static partial Regex PasswordRegex { get; }

    /// <summary>As <c>ProxySwitcherForm_Load</c>: whether a proxy host is configured (otherwise the dialog is not shown).</summary>
    public static bool IsConfigured(ProxySettings settings) => !string.IsNullOrEmpty(settings.HttpProxy);

    /// <summary>As <c>SetProxy_Button_Click</c>.</summary>
    [RelayCommand]
    private void SetProxy()
    {
        GitArgumentBuilder args = new("config")
        {
            { ApplyGlobally, "--global" },
            "http.proxy",
            BuildHttpProxy()
        };
        _git.Run(args);

        RefreshProxy();
    }

    /// <summary>As <c>UnsetProxy_Button_Click</c>.</summary>
    [RelayCommand]
    private void UnsetProxy()
    {
        _git.Run(ApplyGlobally ? "config --global --unset http.proxy" : "config --unset http.proxy");

        RefreshProxy();
    }

    /// <summary>As <c>RefreshProxy</c>.</summary>
    private void RefreshProxy()
    {
        LocalHttpProxy = HidePassword(_git.GetEffectiveProxy());
        GlobalHttpProxy = HidePassword(_git.GetGlobalProxy());
        ApplyGlobally = string.Equals(LocalHttpProxy, GlobalHttpProxy);
    }

    private static string HidePassword(string httpProxy) => PasswordRegex.Replace(httpProxy, ":****@");

    /// <summary>As <c>BuildHttpProxy</c>.</summary>
    private string BuildHttpProxy()
    {
        StringBuilder sb = new();
        sb.Append('"');
        if (!string.IsNullOrEmpty(_settings.Username))
        {
            sb.Append(_settings.Username);
            if (!string.IsNullOrEmpty(_settings.Password))
            {
                sb.Append(':');
                sb.Append(_settings.Password);
            }

            sb.Append('@');
        }

        sb.Append(_settings.HttpProxy);
        if (!string.IsNullOrEmpty(_settings.HttpProxyPort))
        {
            sb.Append(':');
            sb.Append(_settings.HttpProxyPort);
        }

        sb.Append('"');
        return sb.ToString();
    }
}
