using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GitExtensions.Extensibility.Tests")]
[assembly: InternalsVisibleTo("GitCommands")]
[assembly: InternalsVisibleTo("GitUI")] // NumberSetting<T>.TryConvertFromString is used in GitUI's NumberSettingTextBoxBinding.
[assembly: InternalsVisibleTo("GitUI.Presentation")] // and in its port, NumberTextSettingValue.
