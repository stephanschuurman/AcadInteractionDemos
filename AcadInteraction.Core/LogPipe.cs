using System.IO.Pipes;
using System.Text;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Realtime log channel over a named pipe. The WinForms app hosts the pipe (<see cref="ListenAsync"/>),
    /// MCP server processes send lines to it (<see cref="LogPipeClient"/>). Messages are dropped when nobody listens.
    /// </summary>
    public static class LogPipe
    {
        public const string PipeName = "AcadInteraction.Log";

        /// <summary>
        /// Accepts clients until cancelled and reports every received line.
        /// </summary>
        public static async Task ListenAsync(Action<string> onLine, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var server = new NamedPipeServerStream(PipeName, PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                try
                {
                    await server.WaitForConnectionAsync(cancellationToken);
                }
                catch
                {
                    await server.DisposeAsync();
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    continue;
                }

                _ = Task.Run(async () =>
                {
                    await using (server)
                    using (var reader = new StreamReader(server, Encoding.UTF8))
                    {
                        try
                        {
                            string? line;
                            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                                onLine(line);
                        }
                        catch
                        {
                            // Client disconnected or listener stopped
                        }
                    }
                }, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sends log lines to the <see cref="LogPipe"/> listener. Never throws; reconnects lazily.
    /// </summary>
    public sealed class LogPipeClient : IDisposable
    {
        private readonly object _lock = new();
        private NamedPipeClientStream? _pipe;
        private StreamWriter? _writer;
        private DateTime _nextAttempt = DateTime.MinValue;

        public void Send(string message)
        {
            lock (_lock)
            {
                try
                {
                    if (_writer == null)
                    {
                        if (DateTime.UtcNow < _nextAttempt)
                            return;
                        _pipe = new NamedPipeClientStream(".", LogPipe.PipeName, PipeDirection.Out);
                        try
                        {
                            _pipe.Connect(50);
                        }
                        catch
                        {
                            _nextAttempt = DateTime.UtcNow.AddSeconds(2);
                            Close();
                            return;
                        }
                        _writer = new StreamWriter(_pipe, new UTF8Encoding(false)) { AutoFlush = true };
                    }
                    _writer.WriteLine(message.Replace("\r", " ").Replace("\n", " "));
                }
                catch
                {
                    Close();
                }
            }
        }

        private void Close()
        {
            try { _writer?.Dispose(); } catch { }
            try { _pipe?.Dispose(); } catch { }
            _writer = null;
            _pipe = null;
        }

        public void Dispose()
        {
            lock (_lock)
                Close();
        }
    }
}
