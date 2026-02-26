using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace EasySave.LogServer
{
    public class LogServerService
    {
        private readonly int _port;
        private readonly string _logDirectory;
        private TcpListener? _listener;
        private bool _isRunning;
        private readonly List<TcpClient> _connectedClients = new();
        private readonly object _lockObject = new();

        public LogServerService(int port = 5555, string? logDirectory = null)
        {
            _port = port;
            _logDirectory = logDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log Server started on port {_port}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Logs will be saved to: {_logDirectory}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Waiting for client connections...\n");

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    
                    lock (_lockObject)
                    {
                        _connectedClients.Add(client);
                    }

                    var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✓ New client connected: {clientEndpoint}");
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Total active connections: {_connectedClients.Count}\n");

                    // Handle client in background
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            var stream = client.GetStream();
            var buffer = new byte[8192];

            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // Process each line as a separate log entry
                    var lines = message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            await ProcessLogEntryAsync(line, clientEndpoint);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Error handling client {clientEndpoint}: {ex.Message}");
            }
            finally
            {
                lock (_lockObject)
                {
                    _connectedClients.Remove(client);
                }
                
                client.Close();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Client disconnected: {clientEndpoint}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Remaining active connections: {_connectedClients.Count}\n");
            }
        }

        private async Task ProcessLogEntryAsync(string jsonLog, string clientEndpoint)
        {
            try
            {
                string trimmed = jsonLog.TrimStart();
                bool isJson = false;
                bool isXml = false;

                if (trimmed.Length > 0)
                {
                    char c = trimmed[0];
                    if (c == '{' || c == '[') isJson = true;
                    else if (c == '<') isXml = true;
                }

                string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

                string clientIdentifier = GetClientIdentifier(jsonLog, clientEndpoint);
                string clientDirectory = Path.Combine(_logDirectory, SanitizeFileName(clientIdentifier));
                Directory.CreateDirectory(clientDirectory);

                if (isJson)
                {
                    // Validate JSON
                    using var doc = JsonDocument.Parse(jsonLog);
                    string logFilePath = Path.Combine(clientDirectory, $"{date}.json");
                    await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                }
                else if (isXml)
                {
                    // Accept raw XML and save as .xml daily file
                    string logFilePath = Path.Combine(clientDirectory, $"{date}.xml");
                    await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                }
                else
                {
                    // Try to parse as JSON, otherwise save raw
                    try
                    {
                        using var doc = JsonDocument.Parse(jsonLog);
                        string logFilePath = Path.Combine(clientDirectory, $"{date}.json");
                        await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                    }
                    catch
                    {
                        string logFilePath = Path.Combine(clientDirectory, $"{date}.log");
                        await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                    }
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 📝 Log received from {clientIdentifier}");
                DisplayLogSummary(jsonLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Error processing log: {ex.Message}");
            }
        }

        private string GetClientIdentifier(string jsonLog, string fallbackEndpoint)
        {
            try
            {
                string trimmed = jsonLog.TrimStart();
                if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
                {
                    using var doc = JsonDocument.Parse(jsonLog);
                    var root = doc.RootElement;

                    // case-insensitive lookup for machine/backup
                    string? machine = GetJsonStringValue(root, new[] { "machineName", "machinename", "machine" });
                    if (!string.IsNullOrWhiteSpace(machine)) return machine;

                    string? backup = GetJsonStringValue(root, new[] { "backupName", "backupname", "backup" });
                    if (!string.IsNullOrWhiteSpace(backup)) return $"{fallbackEndpoint.Split(':')[0]}_{backup}";
                }

                if (trimmed.Length > 0 && trimmed[0] == '<')
                {
                    try
                    {
                        var el = XElement.Parse(trimmed);
                        var machine = el.Element("machineName")?.Value ?? el.Element("MachineName")?.Value;
                        if (!string.IsNullOrWhiteSpace(machine)) return machine;
                        var backup = el.Element("backupName")?.Value ?? el.Element("BackupName")?.Value;
                        if (!string.IsNullOrWhiteSpace(backup)) return $"{fallbackEndpoint.Split(':')[0]}_{backup}";
                    }
                    catch { }
                }
            }
            catch { }

            return fallbackEndpoint.Split(':')[0];
        }

        private void DisplayLogSummary(string jsonLog)
        {
            try
            {
                string trimmed = jsonLog.TrimStart();
                string backupName = "N/A";
                string sourceFile = "N/A";
                string type = "N/A";

                if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
                {
                    using var doc = JsonDocument.Parse(jsonLog);
                    var root = doc.RootElement;
                    backupName = GetJsonStringValue(root, new[] { "backupName", "backupname", "backup" }) ?? "N/A";
                    sourceFile = GetJsonStringValue(root, new[] { "sourceFilePath", "sourcefilePath", "sourcefilepath", "sourceFile" }) ?? "N/A";
                    type = GetJsonStringValue(root, new[] { "type", "entryType", "entrytype" }) ?? "N/A";

                    // Additional details for JSON (case-insensitive)
                    var machineName = GetJsonStringValue(root, new[] { "machineName", "machinename", "machine" });
                    var fileSizeProp = GetJsonStringValue(root, new[] { "fileSize", "filesize", "size" });
                    var transferTimeProp = GetJsonStringValue(root, new[] { "transferTime", "transfertime", "transfer_time", "tx" });
                    var encryptionTimeProp = GetJsonStringValue(root, new[] { "encryptionTime", "encryptiontime", "enc" });

                    if (!string.IsNullOrWhiteSpace(machineName))
                        backupName = (backupName == "N/A" ? string.Empty : backupName + " ") + "(" + machineName + ")";

                    // append size and times to type display for quick info
                    var extras = new List<string>();
                    if (!string.IsNullOrWhiteSpace(fileSizeProp)) extras.Add($"size={fileSizeProp}");
                    if (!string.IsNullOrWhiteSpace(transferTimeProp)) extras.Add($"tx={transferTimeProp}ms");
                    if (!string.IsNullOrWhiteSpace(encryptionTimeProp)) extras.Add($"enc={encryptionTimeProp}ms");
                    if (extras.Count > 0)
                    {
                        type = type == "N/A" ? string.Join(";", extras) : type + " (" + string.Join(", ", extras) + ")";
                    }
                }
                else if (trimmed.Length > 0 && trimmed[0] == '<')
                {
                    try
                    {
                        var el = XElement.Parse(trimmed);
                        backupName = el.Element("backupName")?.Value ?? "N/A";
                        sourceFile = el.Element("sourceFilePath")?.Value ?? "N/A";
                        type = el.Element("type")?.Value ?? "N/A";
                    }
                    catch { }
                }

                if (sourceFile != null && sourceFile.Length > 50)
                {
                    sourceFile = "..." + sourceFile.Substring(sourceFile.Length - 47);
                }

                Console.WriteLine($"    ↳ Backup: {backupName}, Type: {type}, File: {sourceFile}");
            }
            catch { }
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        // Helper: case-insensitive string lookup in a JsonElement
        private static string? GetJsonStringValue(JsonElement root, string[] candidateNames)
        {
            try
            {
                foreach (var prop in root.EnumerateObject())
                {
                    foreach (var name in candidateNames)
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (prop.Value.ValueKind == JsonValueKind.String)
                                    return prop.Value.GetString();
                                if (prop.Value.ValueKind == JsonValueKind.Number)
                                    return prop.Value.GetRawText();
                                return prop.Value.ToString();
                            }
                            catch { return prop.Value.ToString(); }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            
            lock (_lockObject)
            {
                foreach (var client in _connectedClients)
                {
                    try
                    {
                        client.Close();
                    }
                    catch { }
                }
                _connectedClients.Clear();
            }
            
            Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log Server stopped");
        }

        public int GetConnectedClientsCount()
        {
            lock (_lockObject)
            {
                return _connectedClients.Count;
            }
        }
    }
}
