using System.IO.Pipes;

namespace DwmFix.App;

internal static class SingleInstanceClient
{
    public static void SendShowCommand()
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", SingleInstancePipeServer.PipeName, PipeDirection.Out);
            pipe.Connect(timeout: 750);

            using var writer = new StreamWriter(pipe) { AutoFlush = true };
            writer.WriteLine("show");
        }
        catch
        {
            // A second launch should never crash just because the first instance is busy starting.
        }
    }
}
