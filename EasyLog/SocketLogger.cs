using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

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
        private readonly LogFormat _format;

        public LogFormat Format => _format;

        public SocketLogger(string serverHost, int serverPort = 5555, LogFormat format = LogFormat.Json)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
            _isConnected = false;
            _format = format;
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

                    // Serialize based on configured format
                    string serialized;
                    if (_format == LogFormat.Xml)
                    {
                        var doc = new XDocument(new XElement("log",
                            new XElement("timestamp", entry.Timestamp),
                            new XElement("backupName", entry.BackupName),
                            new XElement("sourceFilePath", entry.SourceFilePath),
                            new XElement("targetFilePath", entry.TargetFilePath),
                            new XElement("fileSize", entry.FileSize),
                            new XElement("transferTime", entry.TransferTime),
                            new XElement("encryptionTime", entry.EncryptionTime),
                            new XElement("type", entry.Type.ToString()),
                            new XElement("machineName", Environment.MachineName)
                        ));
                        serialized = doc.ToString(SaveOptions.DisableFormatting);
                    }
                    else
                    {
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

                        serialized = JsonSerializer.Serialize(logData, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }

                    byte[] data = Encoding.UTF8.GetBytes(serialized + "\n");
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
