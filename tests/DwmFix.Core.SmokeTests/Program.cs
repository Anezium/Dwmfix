using DwmFix.Core;

Run("settings are normalized and persisted", SettingsAreNormalizedAndPersisted);
Run("auto targets prefer secondary displays", AutoTargetsPreferSecondaryDisplays);
Run("configured targets ignore missing displays", ConfiguredTargetsIgnoreMissingDisplays);

Console.WriteLine("DwmFix.Core smoke tests passed.");

static void SettingsAreNormalizedAndPersisted()
{
    var directory = Path.Combine(Path.GetTempPath(), "DwmFix.SmokeTests", Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "settings.json");
    var store = new SettingsStore(path);

    var settings = new AppSettings
    {
        FixEnabled = false,
        BoostMode = true,
        StartMinimized = false,
        RenderFps = 999,
        TargetDisplays = [@"\\.\DISPLAY2", "", @"\\.\DISPLAY2"],
    };

    store.Save(settings);
    var loaded = store.Load();

    Assert(!loaded.FixEnabled, "FixEnabled should round-trip.");
    Assert(loaded.BoostMode, "BoostMode should round-trip.");
    Assert(!loaded.StartMinimized, "StartMinimized should round-trip.");
    Assert(loaded.RenderFps == AppSettings.MaxRenderFps, "RenderFps should be clamped.");
    Assert(loaded.TargetDisplays.SequenceEqual([@"\\.\DISPLAY2"]), "TargetDisplays should be cleaned.");

    Directory.Delete(directory, recursive: true);
}

static void AutoTargetsPreferSecondaryDisplays()
{
    var settings = new AppSettings();
    var displays = new[]
    {
        new DisplaySnapshot(@"\\.\DISPLAY1", IsPrimary: true),
        new DisplaySnapshot(@"\\.\DISPLAY2", IsPrimary: false),
        new DisplaySnapshot(@"\\.\DISPLAY3", IsPrimary: false),
    };

    var selected = DisplayTargetSelector.SelectTargets(settings, displays);
    Assert(selected.SequenceEqual([@"\\.\DISPLAY2", @"\\.\DISPLAY3"]), "Secondary displays should be selected by default.");
}

static void ConfiguredTargetsIgnoreMissingDisplays()
{
    var settings = new AppSettings
    {
        TargetDisplays = [@"\\.\MISSING", @"\\.\DISPLAY1"],
    };
    var displays = new[]
    {
        new DisplaySnapshot(@"\\.\DISPLAY1", IsPrimary: true),
    };

    var selected = DisplayTargetSelector.SelectTargets(settings, displays);
    Assert(selected.SequenceEqual([@"\\.\DISPLAY1"]), "Only connected configured displays should be selected.");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine("[pass] " + name);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine("[fail] " + name);
        Console.Error.WriteLine(exception);
        Environment.ExitCode = 1;
        throw;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
