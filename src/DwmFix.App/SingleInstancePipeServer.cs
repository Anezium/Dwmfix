using System.IO.Pipes;

namespace DwmFix.App;

internal sealed class SingleInstancePipeServer : IDisposable
{
    public const string PipeName = "DwmFix.Native.CommandPipe";

    private readonly SynchronizationContext _context;
    private readonly Action<string> _onCommand;
    private CancellationTokenSource? _cancellation;

    public SingleInstancePipeServer(SynchronizationContext context, Action<string> onCommand)
    {
        _context = context;
        _onCommand = onCommand;
    }

    public void Start()
    {
        if (_cancellation is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _ = Task.Run(() => ListenAsync(_cancellation.Token));
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(pipe);
                var command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(command))
                {
                    _context.Post(state => _onCommand((string)state!), command);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
