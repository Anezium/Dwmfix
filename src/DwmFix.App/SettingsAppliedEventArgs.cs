using DwmFix.Core;

namespace DwmFix.App;

internal sealed class SettingsAppliedEventArgs : EventArgs
{
    public SettingsAppliedEventArgs(AppSettings settings, bool startWithWindows)
    {
        Settings = settings;
        StartWithWindows = startWithWindows;
    }

    public AppSettings Settings { get; }

    public bool StartWithWindows { get; }
}
