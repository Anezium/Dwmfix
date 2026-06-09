using Microsoft.Win32;

namespace DwmFix.Core;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _command;
    private readonly string _valueName;

    public StartupService(string executablePath, string valueName = "DwmFix")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        _command = $"{Quote(executablePath)} --startup";
        _valueName = valueName;
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(_valueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the current user's Run registry key.");

        if (enabled)
        {
            key.SetValue(_valueName, _command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(_valueName, throwOnMissingValue: false);
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
