using GitExtUtils.GitUI;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace GitUI.Infrastructure.Telemetry;

internal sealed class MonitorsTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        IDictionary<string, string> properties = telemetry.Context.GlobalProperties;
        IReadOnlyList<(Rectangle Bounds, bool IsPrimary)> screens = Screens.GetAll();
        properties["Monitor count"] = screens.Count.ToString();
        properties["Monitor primary DPI"] = DpiUtil.DpiX.ToString();

        for (int i = 0; i < screens.Count; i++)
        {
            string key = screens[i].IsPrimary ? "primary" : $"secondary{i}";

            Rectangle bounds = screens[i].Bounds;
            properties[$"Monitor {key} resolution"] = $"{bounds.Width}x{bounds.Height}";
        }
    }
}
