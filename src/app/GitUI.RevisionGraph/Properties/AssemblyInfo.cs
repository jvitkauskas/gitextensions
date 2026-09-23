using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GitUI")]
[assembly: InternalsVisibleTo("GitUI.Avalonia")]
[assembly: InternalsVisibleTo("GitUI.Tests")]
[assembly: InternalsVisibleTo("GitUI.Avalonia.Tests")]
[assembly: InternalsVisibleTo("UI.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // required for NSubstitute for mocking internal members
