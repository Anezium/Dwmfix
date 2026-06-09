using DwmFix.Core;
using Microsoft.Win32;

namespace DwmFix.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly StartupService _startupService;
    private readonly DwmStimulusManager _stimulusManager;
    private readonly NotifyIcon _trayIcon;
    private readonly SingleInstancePipeServer _pipeServer;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext(bool startedFromStartup)
    {
        _settingsStore = new SettingsStore();
        var firstRun = !_settingsStore.Exists;
        _settings = _settingsStore.Load();

        _startupService = new StartupService(Application.ExecutablePath);
        _stimulusManager = new DwmStimulusManager();
        _trayIcon = CreateTrayIcon();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        var context = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _pipeServer = new SingleInstancePipeServer(context, HandleInstanceCommand);
        _pipeServer.Start();

        SaveAndApply(refreshMenu: true);

        if (firstRun || (!startedFromStartup && !_settings.StartMinimized))
        {
            ShowSettings();
        }
        else if (_settings.FixEnabled)
        {
            _trayIcon.ShowBalloonTip(1400, "DwmFix", "Active in the tray.", ToolTipIcon.Info);
        }
    }

    protected override void ExitThreadCore()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _settingsForm?.Close();
        _pipeServer.Dispose();
        _stimulusManager.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        base.ExitThreadCore();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DwmFix",
            Visible = true,
        };
        trayIcon.DoubleClick += (_, _) => ShowSettings();
        return trayIcon;
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem(_settings.FixEnabled ? "Status: active" : "Status: stopped") { Enabled = false });

        var enabledItem = new ToolStripMenuItem("Enable fix")
        {
            Checked = _settings.FixEnabled,
            CheckOnClick = false,
        };
        enabledItem.Click += (_, _) => UpdateSettings(settings => settings.FixEnabled = !settings.FixEnabled);
        menu.Items.Add(enabledItem);

        var boostItem = new ToolStripMenuItem("Boost mode")
        {
            Checked = _settings.BoostMode,
            CheckOnClick = false,
        };
        boostItem.Click += (_, _) => UpdateSettings(settings => settings.BoostMode = !settings.BoostMode);
        menu.Items.Add(boostItem);

        var targetMenu = new ToolStripMenuItem("Target monitors");
        var autoItem = new ToolStripMenuItem("Auto-select secondary displays")
        {
            Checked = _settings.TargetDisplays.Count == 0,
            CheckOnClick = false,
        };
        autoItem.Click += (_, _) => UpdateSettings(settings => settings.TargetDisplays = []);
        targetMenu.DropDownItems.Add(autoItem);
        targetMenu.DropDownItems.Add(new ToolStripSeparator());

        var selectedTargets = GetSelectedTargets().ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var monitor in MonitorCatalog.GetMonitors())
        {
            var item = new ToolStripMenuItem(monitor.MenuText)
            {
                Checked = selectedTargets.Contains(monitor.DeviceName),
                CheckOnClick = false,
            };
            item.Click += (_, _) => ToggleMonitor(monitor.DeviceName);
            targetMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(targetMenu);

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = _startupService.IsEnabled(),
            CheckOnClick = false,
        };
        startupItem.Click += (_, _) =>
        {
            _startupService.SetEnabled(!_startupService.IsEnabled());
            RefreshTrayMenu();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Show();
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_settings.Clone(), MonitorCatalog.GetMonitors(), _startupService.IsEnabled());
        _settingsForm.SettingsApplied += (_, args) =>
        {
            _settings = args.Settings;
            _startupService.SetEnabled(args.StartWithWindows);
            SaveAndApply(refreshMenu: true);
        };
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void UpdateSettings(Action<AppSettings> mutate)
    {
        mutate(_settings);
        SaveAndApply(refreshMenu: true);
    }

    private void SaveAndApply(bool refreshMenu)
    {
        _settings.Normalize();
        _settingsStore.Save(_settings);
        _stimulusManager.Apply(_settings);

        if (refreshMenu)
        {
            RefreshTrayMenu();
        }
    }

    private void RefreshTrayMenu()
    {
        var oldMenu = _trayIcon.ContextMenuStrip;
        _trayIcon.ContextMenuStrip = BuildTrayMenu();
        oldMenu?.Dispose();
    }

    private IReadOnlyList<string> GetSelectedTargets()
    {
        return DisplayTargetSelector.SelectTargets(_settings, MonitorCatalog.GetSnapshots());
    }

    private void ToggleMonitor(string deviceName)
    {
        var targets = _settings.TargetDisplays.Count == 0
            ? GetSelectedTargets().ToList()
            : [.. _settings.TargetDisplays];

        var existing = targets.FindIndex(target => string.Equals(target, deviceName, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            targets.RemoveAt(existing);
        }
        else
        {
            targets.Add(deviceName);
        }

        if (targets.Count == 0)
        {
            targets.Add(deviceName);
        }

        _settings.TargetDisplays = targets;
        SaveAndApply(refreshMenu: true);
    }

    private void HandleInstanceCommand(string command)
    {
        if (string.Equals(command, "show", StringComparison.OrdinalIgnoreCase))
        {
            ShowSettings();
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _stimulusManager.Apply(_settings);
        RefreshTrayMenu();
    }
}
