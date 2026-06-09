namespace DwmFix.Core;

public static class DisplayTargetSelector
{
    public static IReadOnlyList<string> SelectTargets(AppSettings settings, IReadOnlyList<DisplaySnapshot> displays)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(displays);

        if (displays.Count == 0)
        {
            return [];
        }

        var available = displays
            .Select(static display => display.DeviceName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var configured = settings.TargetDisplays
            .Where(available.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (configured.Length > 0)
        {
            return configured;
        }

        var secondaryDisplays = displays
            .Where(static display => !display.IsPrimary)
            .Select(static display => display.DeviceName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return secondaryDisplays.Length > 0
            ? secondaryDisplays
            : [displays[0].DeviceName];
    }
}
