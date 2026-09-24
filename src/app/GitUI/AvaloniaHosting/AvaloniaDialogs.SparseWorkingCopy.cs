using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the sparse working copy dialog (docs/avalonia-port/PLAN.md, phase 3).</summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowSparseWorkingCopy(IWin32Window? owner, IGitUICommands commands)
    {
        ShowDialog(
            () =>
            {
                SparseWorkingCopyWindow window = new();
                window.DataContext = new SparseWorkingCopyViewModel(
                    ViewStrings.Load<SparseWorkingCopyStrings>(),
                    new SparseWorkingCopyHost(commands, window),
                    new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: "FormSparseWorkingCopy");
        return true;
    }

    /// <summary>As <c>FormSparseWorkingCopyViewModel</c>.</summary>
    private sealed class SparseWorkingCopyHost(IGitUICommands commands, DialogWindow window) : ISparseWorkingCopyHost
    {
        private IGitModule Module => commands.Module;

        private string RulesPath => Path.Join(Module.ResolveGitInternalPath("info"), "sparse-checkout");

        public bool IsSparseCheckoutEnabled
            => StringComparer.OrdinalIgnoreCase.Equals(Module.GetEffectiveSetting(SparseWorkingCopyViewModel.SettingCoreSparseCheckout), bool.TrueString);

        public void SetSparseCheckoutEnabled(bool enabled)
            => Module.SetSetting(SparseWorkingCopyViewModel.SettingCoreSparseCheckout, enabled.ToString().ToLowerInvariant());

        public string? LoadRules() => File.Exists(RulesPath) ? File.ReadAllText(RulesPath, GitModule.SystemEncoding) : null;

        public void SaveRules(string text) => File.WriteAllBytes(RulesPath, GitModule.SystemEncoding.GetBytes(text));

        public void RefreshWorkingCopy()
            => RunRemoteProcess(new NativeWindowOwner(window), commands, SparseWorkingCopyViewModel.RefreshWorkingCopyCommandName);
    }
}
