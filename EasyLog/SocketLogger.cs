using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EasyLog;

/// <summary>
/// A logger that sends log entries over a TCP connection to the centralized log server.
/// If the server is unreachable, entries are dropped (and reconnection is retried after a delay)
/// so that the backup process is never blocked.
/// </summary>
public sealed class SocketLogger : ILogger, IDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly string _serverAddress;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private StreamWriter? _writer;
    private DateTime _nextConnectAttempt = DateTime.MinValue;
    private readonly object _lock = new();

    /// <summary>Initializes a logger targeting the given log server.</summary>
    /// <param name="serverAddress">Host name or IP address of the log server.</param>
    /// <param name="port">TCP port of the log server.</param>
    public SocketLogger(string serverAddress = "127.0.0.1", int port = 8080)
    {
        _serverAddress = serverAddress;
        _port = port;
    }

    /// <inheritdoc/>
    public void Log(LogEntry entry)
    {
        entry.SourceFilePath = LogEntry.ToUncPath(entry.SourceFilePath);
        entry.TargetFilePath = LogEntry.ToUncPath(entry.TargetFilePath);

        lock (_lock)
        {
            try
            {
                if (_tcpClient is not { Connected: true })
                    Connect();

                if (_writer != null)
                {
                    _writer.WriteLine(JsonSerializer.Serialize(entry));
                    _writer.Flush();
                }
            }
            catch
            {
                // TCP failed (server offline, etc.) -> silently ignore so backups don't crash
                Disconnect();
            }
        }
    }

    private void Connect()
    {
        // Avoid paying the connection timeout for every entry while the server is down
        if (DateTime.UtcNow < _nextConnectAttempt) return;

        try
        {
            Disconnect();
            _tcpClient = new TcpClient();
            var result = _tcpClient.BeginConnect(_serverAddress, _port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(ConnectTimeout))
            {
                Disconnect();
                _nextConnectAttempt = DateTime.UtcNow + RetryDelay;
                return;
            }

            _tcpClient.EndConnect(result);
            _writer = new StreamWriter(_tcpClient.GetStream(), new UTF8Encoding(false));
        }
        catch
        {
            Disconnect();
            _nextConnectAttempt = DateTime.UtcNow + RetryDelay;
        }
    }

    private void Disconnect()
    {
        try
        {
            _writer?.Dispose();
            _tcpClient?.Dispose();
        }
        catch { /* ignored */ }
        finally
        {
            _writer = null;
            _tcpClient = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock) Disconnect();
    }
}
