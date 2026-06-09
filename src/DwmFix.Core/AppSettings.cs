namespace DwmFix.Core;

public sealed class AppSettings
{
    public const int MinRenderFps = 15;
    public const int MaxRenderFps = 240;

    public int Version { get; set; } = 1;

    public bool FixEnabled { get; set; } = true;

    public bool BoostMode { get; set; }

    public bool StartMinimized { get; set; } = true;

    public int RenderFps { get; set; } = 60;

    public List<string> TargetDisplays { get; set; } = [];

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Version = Version,
            FixEnabled = FixEnabled,
            BoostMode = BoostMode,
            StartMinimized = StartMinimized,
            RenderFps = RenderFps,
            TargetDisplays = [.. TargetDisplays],
        };
    }

    public void Normalize()
    {
        Version = Math.Max(1, Version);
        RenderFps = Math.Clamp(RenderFps, MinRenderFps, MaxRenderFps);
        TargetDisplays = TargetDisplays
            .Where(static display => !string.IsNullOrWhiteSpace(display))
            .Select(static display => display.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
