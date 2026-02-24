using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
                // Verify it's valid JSON
                using var doc = JsonDocument.Parse(jsonLog);
                
                // Extract timestamp for organizing logs
                string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                
                // Try to get client machine name or use endpoint
                string clientIdentifier = GetClientIdentifier(jsonLog, clientEndpoint);
                
                // Create client-specific directory
                string clientDirectory = Path.Combine(_logDirectory, SanitizeFileName(clientIdentifier));
                Directory.CreateDirectory(clientDirectory);
                
                // Create daily log file
                string logFilePath = Path.Combine(clientDirectory, $"{date}.json");
                
                // Append log entry
                await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                
                // Display log info
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 📝 Log received from {clientIdentifier}");
                DisplayLogSummary(jsonLog);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Invalid JSON from {clientEndpoint}: {ex.Message}");
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
                using var doc = JsonDocument.Parse(jsonLog);
                var root = doc.RootElement;
                
                // Try to get machine name from log if available
                if (root.TryGetProperty("machineName", out var machineNameProp))
                {
                    return machineNameProp.GetString() ?? fallbackEndpoint;
                }
                
                if (root.TryGetProperty("backupName", out var backupNameProp))
                {
                    var backupName = backupNameProp.GetString();
                    if (!string.IsNullOrEmpty(backupName))
                    {
                        return $"{fallbackEndpoint.Split(':')[0]}_{backupName}";
                    }
                }
            }
            catch { }
            
            return fallbackEndpoint.Split(':')[0];
        }

        private void DisplayLogSummary(string jsonLog)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonLog);
                var root = doc.RootElement;
                
                var backupName = root.TryGetProperty("backupName", out var bn) ? bn.GetString() : "N/A";
                var sourceFile = root.TryGetProperty("sourceFilePath", out var sf) ? sf.GetString() : "N/A";
                var type = root.TryGetProperty("type", out var t) ? t.GetString() : "N/A";
                
                // Truncate long paths for display
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
