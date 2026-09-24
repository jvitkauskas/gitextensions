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

        // As OnPageShown: the images of GitUI.Properties.Images, by name.
        public IReadOnlyList<ScriptIconChoice> LoadIcons()
        {
            System.Resources.ResourceManager resources = new("GitUI.Properties.Images", Assembly.GetExecutingAssembly());

            // A dummy request: the resource sets are not loaded before the first request.
            resources.GetObject("dummy");
            using System.Resources.ResourceSet? resourceSet = resources.GetResourceSet(CultureInfo.CurrentUICulture, createIfNotExists: true, tryParents: true);
            if (resourceSet is null)
            {
                return [];
            }

            List<ScriptIconChoice> icons = [];
            foreach (DictionaryEntry icon in resourceSet.Cast<DictionaryEntry>().OrderBy(icon => icon.Key))
            {
                if (icon.Value is Bitmap bitmap && ToPng(bitmap) is byte[] png)
                {
                    icons.Add(new ScriptIconChoice(icon.Key.ToString()!, png));
                }
            }

            resources.ReleaseAllResources();
            return icons;
        }

        public byte[]? GetFileIcon(string path)
        {
            using Bitmap? icon = new ScriptInfo { IconFilePath = path }.GetIcon();
            return ToPng(icon);
        }
    }
}
