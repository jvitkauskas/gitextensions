using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using GitCommands;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.ScriptsEngine;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>The scripts of <c>ScriptsSettingsPage</c> (<c>IScriptsManager</c>) and the embedded icons.</summary>
    private sealed class ScriptsSettingsHost(IScriptsManager scriptsManager) : IScriptsSettingsHost
    {
        public int MinimumUserScriptId => ScriptsManager.MinimumUserScriptID;

        public IReadOnlyList<string> EventNames { get; } = Enum.GetNames<ScriptEvent>();

        public IReadOnlyList<ScriptItem> LoadScripts()
            => [.. scriptsManager.GetScripts().Select(script => new ScriptItem
            {
                Enabled = script.Enabled,
                HotkeyCommandIdentifier = script.HotkeyCommandIdentifier,
                Name = script.Name,
                Command = script.Command,
                Arguments = script.Arguments,
                AskConfirmation = script.AskConfirmation,
                IsPowerShell = script.IsPowerShell,
                OnEvent = script.OnEvent.ToString(),
                AddToRevisionGridContextMenu = script.AddToRevisionGridContextMenu,
                RunInBackground = script.RunInBackground,
                Icon = script.Icon,
                IconFilePath = script.IconFilePath,
            })];

        // As PageToSettings.
        public void SaveScripts(IReadOnlyList<ScriptItem> scripts)
        {
            BindingList<ScriptInfo> scriptInfos = scriptsManager.GetScripts();
            scriptInfos.Clear();
            foreach (ScriptItem script in scripts)
            {
                scriptInfos.Add(new ScriptInfo
                {
                    Enabled = script.Enabled,
                    HotkeyCommandIdentifier = script.HotkeyCommandIdentifier,
                    Name = script.Name,
                    Command = script.Command,
                    Arguments = script.Arguments,
                    AskConfirmation = script.AskConfirmation,
                    IsPowerShell = script.IsPowerShell,
                    OnEvent = Enum.TryParse(script.OnEvent, out ScriptEvent onEvent) ? onEvent : ScriptEvent.None,
                    AddToRevisionGridContextMenu = script.AddToRevisionGridContextMenu,
                    RunInBackground = script.RunInBackground,
                    Icon = script.Icon,
                    IconFilePath = script.IconFilePath,
                });
            }

            AppSettings.OwnScripts = scriptsManager.SerializeIntoXml();
        }

        // As OnPageShown: the icons of the resources (GitUI.Properties.Images), by name.
        public IReadOnlyList<ScriptIconChoice> LoadIcons()
            => [.. EmbeddedIcons.Names.Select(name => new ScriptIconChoice(name, EmbeddedIcons.Get(name)))];

        public byte[]? GetFileIcon(string path) => new ScriptInfo { IconFilePath = path }.GetIcon();
    }
}
