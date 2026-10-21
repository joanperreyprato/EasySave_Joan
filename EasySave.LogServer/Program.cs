using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasyLog;

namespace EasySave.LogServer;

/// <summary>
/// Centralized log server (v3.0). Receives log entries (one JSON object per line) from every
/// EasySave client over TCP and appends them to a single daily file shared by all users.
/// Each entry carries the machine and user name of its sender.
/// </summary>
internal static class Program
{
    private const int DefaultPort = 8080;
    private const string DefaultLogDirectory = "/logs";

    private static async Task Main()
    {
        int port = int.TryParse(Environment.GetEnvironmentVariable("EASYSAVE_LOG_PORT"), out int p) ? p : DefaultPort;
        string logDirectory = Environment.GetEnvironmentVariable("EASYSAVE_LOG_DIR") ?? DefaultLogDirectory;

        // A single logger instance (and its lock) serializes writes from every client
        var dailyLog = new JsonLogger(logDirectory);
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("=============================================");
        Console.WriteLine($"EasySave Log Server listening on port {port}");
        Console.WriteLine($"Daily log files written to {logDirectory}");
        Console.WriteLine("=============================================\n");

        while (true)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, dailyLog); // One task per connected EasySave client
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client, ILogger dailyLog)
    {
        var endPoint = client.Client.RemoteEndPoint;
        Console.WriteLine($"[+] New connection: {endPoint}");

        using (client)
        using (var reader = new StreamReader(client.GetStream(), Encoding.UTF8))
        {
            try
            {
                while (await reader.ReadLineAsync() is { } line)
                    Store(line, dailyLog);
            }
            catch (IOException)
            {
                // Client disconnected abruptly
            }
        }
        Console.WriteLine($"[-] Disconnected: {endPoint}");
    }

    private static void Store(string line, ILogger dailyLog)
    {
        LogEntry? entry;
        try { entry = JsonSerializer.Deserialize<LogEntry>(line); }
        catch (JsonException) { entry = null; }

        if (entry is null)
        {
            Console.WriteLine($"[!] Ignored malformed entry: {line}");
            return;
        }

        dailyLog.Log(entry);

        // Short, aligned console line: the full details are in the daily file
        string file = Path.GetFileName(entry.SourceFilePath.Replace('\\', '/'));
        if (string.IsNullOrEmpty(file)) file = Path.GetFileName(entry.TargetFilePath.Replace('\\', '/'));
        Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] {entry.UserName + "@" + entry.MachineName,-22} " +
                          $"{entry.BackupName,-14} {entry.Action,-24} {file,-28} " +
                          $"{entry.FileSize,12:N0} B {entry.TransferDurationMs,6} ms  crypto {entry.EncryptionDurationMs} ms");
    }
}
