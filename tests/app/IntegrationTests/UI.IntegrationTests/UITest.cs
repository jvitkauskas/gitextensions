using CommonTestUtils;

namespace GitExtensions.UITests;

public static class UITest
{
    public static void ProcessUntil(string processName, Func<bool> condition, int maxMilliseconds = 1500)
        => MessagePumpTestHelper.ProcessUntil(processName, condition, maxMilliseconds);

    public static void ProcessEventsFor(int milliseconds)
        => MessagePumpTestHelper.ProcessEventsFor(milliseconds);
}
