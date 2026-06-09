using DwmFix.Core;

namespace DwmFix.App;

internal sealed class DwmStimulusManager : IDisposable
{
    private readonly Dictionary<string, StimulusWindow> _windows = new(StringComparer.OrdinalIgnoreCase);

    public void Apply(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.FixEnabled)
        {
            StopAll();
            return;
        }

        settings.Normalize();

        var screens = Screen.AllScreens;
        var snapshots = screens
            .Select(static screen => new DisplaySnapshot(screen.DeviceName, screen.Primary))
            .ToArray();
        var targetDevices = DisplayTargetSelector.SelectTargets(settings, snapshots)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var existingDevice in _windows.Keys.Where(device => !targetDevices.Contains(device)).ToArray())
        {
            DestroyWindow(existingDevice);
        }

        foreach (var screen in screens.Where(screen => targetDevices.Contains(screen.DeviceName)))
        {
            if (_windows.TryGetValue(screen.DeviceName, out var window))
            {
                window.UpdateFor(screen, settings);
                continue;
            }

            window = new StimulusWindow(screen, settings);
            _windows.Add(screen.DeviceName, window);
            window.Show();
        }
    }

    public void Dispose()
    {
        StopAll();
    }

    private void StopAll()
    {
        foreach (var deviceName in _windows.Keys.ToArray())
        {
            DestroyWindow(deviceName);
        }
    }

    private void DestroyWindow(string deviceName)
    {
        if (!_windows.Remove(deviceName, out var window))
        {
            return;
        }

        window.Close();
        window.Dispose();
    }
}
