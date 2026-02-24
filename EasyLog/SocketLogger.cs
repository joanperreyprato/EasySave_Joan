using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace EasyLog
{
    /// <summary>
    /// Logger that sends log entries to a remote log server via TCP socket
    /// </summary>
    public class SocketLogger : ILogger
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly object _lockObject = new();
        private bool _isConnected;
        private readonly int _reconnectDelayMs = 5000;
        private DateTime _lastConnectionAttempt = DateTime.MinValue;

        public LogFormat Format => LogFormat.Json;

        public SocketLogger(string serverHost, int serverPort = 5555)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
            _isConnected = false;
        }

        public void LogAction(LogEntry entry)
        {
            if (entry == null) return;

            try
            {
                lock (_lockObject)
                {
                    // Ensure connection
                    EnsureConnected();

                    if (!_isConnected || _stream == null)
                    {
                        // If we can't connect, fail silently (don't block the backup)
                        return;
                    }

                    // Normalize paths
                    try
                    {
                        entry.SourceFilePath = entry.ToUNCPath(entry.SourceFilePath);
                    }
                    catch { }

                    try
                    {
                        entry.TargetFilePath = entry.ToUNCPath(entry.TargetFilePath);
                    }
                    catch { }

                    // Add machine name to the log entry
                    var logData = new
                    {
                        entry.Timestamp,
                        entry.BackupName,
                        entry.SourceFilePath,
                        entry.TargetFilePath,
                        entry.FileSize,
                        entry.TransferTime,
                        entry.EncryptionTime,
                        entry.Type,
                        MachineName = Environment.MachineName
                    };

                    // Serialize and send
                    string json = JsonSerializer.Serialize(logData, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
            }
            catch (Exception)
            {
                // Connection lost, mark as disconnected
                _isConnected = false;
                CloseConnection();
            }
        }

        public void CreateDailyLogFile()
        {
            // Not applicable for socket logger - the server handles file organization
        }

        private void EnsureConnected()
        {
            if (_isConnected && _client != null && _client.Connected)
            {
                return;
            }

            // Avoid hammering the server with connection attempts
            var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
            if (timeSinceLastAttempt.TotalMilliseconds < _reconnectDelayMs)
            {
                return;
            }

            _lastConnectionAttempt = DateTime.UtcNow;

            try
            {
                CloseConnection();

                _client = new TcpClient();
                _client.Connect(_serverHost, _serverPort);
                _stream = _client.GetStream();
                _isConnected = true;

                Console.WriteLine($"Connected to log server at {_serverHost}:{_serverPort}");
            }
            catch (Exception)
            {
                _isConnected = false;
                CloseConnection();
                // Fail silently - we don't want to interrupt the backup process
            }
        }

        private void CloseConnection()
        {
            try
            {
                _stream?.Close();
                _stream = null;
            }
            catch { }

            try
            {
                _client?.Close();
                _client = null;
            }
            catch { }

            _isConnected = false;
        }

        ~SocketLogger()
        {
            CloseConnection();
        }
    }
}
