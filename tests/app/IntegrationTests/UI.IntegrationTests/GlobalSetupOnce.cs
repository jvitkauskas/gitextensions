[SetUpFixture]
// No namespace, so it runs for all tests.
#pragma warning disable CA1050 // Declare types in namespaces
public class GlobalSetupOnce
#pragma warning restore CA1050 // Declare types in namespaces
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Existing tests exercise the WinForms forms; AvaloniaHostingTests opt in to the Avalonia ports.
        Environment.SetEnvironmentVariable(GitUI.Avalonia.Hosting.AvaloniaUi.EnvironmentVariable, "none");
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
    }
}
