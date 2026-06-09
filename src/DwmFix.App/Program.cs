using System.Threading;

namespace DwmFix.App;

internal static class Program
{
    private const string MutexName = "DwmFix.Native.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var ownsMutex);
        if (!ownsMutex)
        {
            SingleInstanceClient.SendShowCommand();
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(defaultValue: false);

        var startedFromStartup = args.Any(static arg => string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));
        Application.Run(new TrayApplicationContext(startedFromStartup));
    }
}
