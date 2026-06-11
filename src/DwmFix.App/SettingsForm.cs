using DwmFix.Core;

namespace DwmFix.App;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _enableFixCheckBox;
    private readonly CheckBox _boostModeCheckBox;
    private readonly CheckBox _startWithWindowsCheckBox;
    private readonly CheckBox _startMinimizedCheckBox;
    private readonly CheckBox _autoTargetsCheckBox;
    private readonly CheckedListBox _displayList;
    private readonly NumericUpDown _fpsInput;

    public SettingsForm(AppSettings settings, IReadOnlyList<MonitorInfo> monitors, bool startWithWindows, Icon appIcon)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(monitors);

        Text = "DwmFix Settings";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(460, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = appIcon;
        ShowIcon = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 9,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _enableFixCheckBox = new CheckBox { Text = "Enable DWM fix", AutoSize = true, Checked = settings.FixEnabled };
        _boostModeCheckBox = new CheckBox { Text = "Boost mode", AutoSize = true, Checked = settings.BoostMode };
        _startWithWindowsCheckBox = new CheckBox { Text = "Start with Windows", AutoSize = true, Checked = startWithWindows };
        _startMinimizedCheckBox = new CheckBox { Text = "Start minimized to tray", AutoSize = true, Checked = settings.StartMinimized };
        _autoTargetsCheckBox = new CheckBox { Text = "Auto-select secondary displays", AutoSize = true, Checked = settings.TargetDisplays.Count == 0 };

        var fpsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        fpsPanel.Controls.Add(new Label { Text = "Render FPS", AutoSize = true, Margin = new Padding(0, 6, 12, 0) });
        _fpsInput = new NumericUpDown
        {
            Minimum = AppSettings.MinRenderFps,
            Maximum = AppSettings.MaxRenderFps,
            Value = Math.Clamp(settings.RenderFps, AppSettings.MinRenderFps, AppSettings.MaxRenderFps),
            Width = 80,
        };
        fpsPanel.Controls.Add(_fpsInput);

        _displayList = new CheckedListBox
        {
            CheckOnClick = true,
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Enabled = !_autoTargetsCheckBox.Checked,
            DisplayMember = nameof(MonitorInfo.MenuText),
        };
        _autoTargetsCheckBox.CheckedChanged += (_, _) => _displayList.Enabled = !_autoTargetsCheckBox.Checked;

        var explicitTargets = settings.TargetDisplays.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var monitor in monitors)
        {
            var index = _displayList.Items.Add(monitor);
            _displayList.SetItemChecked(index, explicitTargets.Contains(monitor.DeviceName));
        }

        var displayLabel = new Label
        {
            Text = "Target displays",
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 4),
        };

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0),
        };
        var saveButton = new Button { Text = "Save", AutoSize = true, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        saveButton.Click += (_, _) => Save(settings);
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        root.Controls.Add(_enableFixCheckBox);
        root.Controls.Add(_boostModeCheckBox);
        root.Controls.Add(_startWithWindowsCheckBox);
        root.Controls.Add(_startMinimizedCheckBox);
        root.Controls.Add(fpsPanel);
        root.Controls.Add(_autoTargetsCheckBox);
        root.Controls.Add(displayLabel);
        root.Controls.Add(_displayList);
        root.Controls.Add(buttons);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    public event EventHandler<SettingsAppliedEventArgs>? SettingsApplied;

    private void Save(AppSettings previousSettings)
    {
        var nextSettings = previousSettings.Clone();
        nextSettings.FixEnabled = _enableFixCheckBox.Checked;
        nextSettings.BoostMode = _boostModeCheckBox.Checked;
        nextSettings.StartMinimized = _startMinimizedCheckBox.Checked;
        nextSettings.RenderFps = (int)_fpsInput.Value;
        nextSettings.TargetDisplays = _autoTargetsCheckBox.Checked
            ? []
            : _displayList.CheckedItems
                .OfType<MonitorInfo>()
                .Select(static monitor => monitor.DeviceName)
                .ToList();

        if (!_autoTargetsCheckBox.Checked && nextSettings.TargetDisplays.Count == 0)
        {
            MessageBox.Show(
                this,
                "Select at least one display or enable auto-select.",
                "DwmFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.None;
            return;
        }

        SettingsApplied?.Invoke(this, new SettingsAppliedEventArgs(nextSettings, _startWithWindowsCheckBox.Checked));
    }
}
