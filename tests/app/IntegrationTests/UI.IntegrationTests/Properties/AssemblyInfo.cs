using CommonTestUtils;

[assembly: Epilogue]
[assembly: ConfigureJoinableTaskFactory]
[assembly: TestAppSettings]

// They drive native windows (GetWindowRect, PrintWindow, the native dialogs of Windows): Windows only for now; NUnit
// skips the assembly elsewhere (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]

// Don't allow tests to run in parallel
[assembly: NonParallelizable]
[assembly: Category("IntegrationTests")]
