using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EasyLog;

/// <summary>
/// A logger that sends log entries over a TCP connection to a remote server.
/// If the server is unreachable, it silently drops the logs to prevent blocking the backup process.
/// </summary>
public sealed class SocketLogger : ILogger, IDisposable
{
    private readonly string _serverAddress;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private StreamWriter? _writer;
    private readonly object _lock = new();

    public SocketLogger(string serverAddress = "127.0.0.1", int port = 8080)
    {
        _serverAddress = serverAddress;
        _port = port;
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            try
            {
                if (_tcpClient == null || !_tcpClient.Connected)
                {
                    Connect();
                }

                if (_writer != null)
                {
                    string json = JsonSerializer.Serialize(entry);
                    _writer.WriteLine(json);
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
        try
        {
            Disconnect();
            _tcpClient = new TcpClient();
            // Short timeout so we don't freeze the app if server is down
            var result = _tcpClient.BeginConnect(_serverAddress, _port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
            if (!success)
            {
                _tcpClient.Close();
                return;
            }
            
            _tcpClient.EndConnect(result);
            _writer = new StreamWriter(_tcpClient.GetStream(), Encoding.UTF8);
        }
        catch
        {
            Disconnect();
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

    public void Dispose()
    {
        Disconnect();
    }
}
