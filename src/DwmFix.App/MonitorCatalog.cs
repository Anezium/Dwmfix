using DwmFix.Core;

namespace DwmFix.App;

internal static class MonitorCatalog
{
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Screen.AllScreens
            .Select(static (screen, index) => new MonitorInfo(
                DeviceName: screen.DeviceName,
                FriendlyName: $"Display {index + 1}",
                Bounds: screen.Bounds,
                IsPrimary: screen.Primary))
            .ToArray();
    }

    public static IReadOnlyList<DisplaySnapshot> GetSnapshots()
    {
        return GetMonitors()
            .Select(static monitor => new DisplaySnapshot(monitor.DeviceName, monitor.IsPrimary))
            .ToArray();
    }
}

internal sealed record MonitorInfo(string DeviceName, string FriendlyName, Rectangle Bounds, bool IsPrimary)
{
    public string MenuText
    {
        get
        {
            var primarySuffix = IsPrimary ? " (primary)" : string.Empty;
            return $"{FriendlyName} {Bounds.Width}x{Bounds.Height}{primarySuffix}";
        }
    }
}
