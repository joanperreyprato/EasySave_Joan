using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasySave.LogServer;

class Program
{
    static async Task Main(string[] args)
    {
        int port = 8080;
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("=============================================");
        Console.WriteLine($"🚀 EasySave Log Server started on port {port}");
        Console.WriteLine("=============================================\n");

        while (true)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // Fire and forget
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var endPoint = client.Client.RemoteEndPoint;
        Console.WriteLine($"[+] New connection: {endPoint}");

        using (client)
        using (var networkStream = client.GetStream())
        using (var reader = new StreamReader(networkStream, Encoding.UTF8))
        {
            try
            {
                while (await reader.ReadLineAsync() is { } line)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        
                        string backupName = root.GetProperty("backupName").GetString() ?? "Unknown";
                        string sourceFile = root.GetProperty("sourceFilePath").GetString() ?? "";
                        long size = root.GetProperty("fileSize").GetInt64();
                        long transferMs = root.GetProperty("transferDurationMs").GetInt64();
                        long cryptoMs = root.GetProperty("encryptionDurationMs").GetInt64();

                        string fileName = Path.GetFileName(sourceFile);
                        string cryptoTag = cryptoMs > 0 ? $" [🔒 {cryptoMs}ms]" : "";

                        // Codes ANSI : 90=Gris, 36=Cyan, 33=Jaune, 97=Blanc, 0=Reset
                        string time = $"\x1b[90m[{DateTime.Now:HH:mm:ss}]\x1b[0m";
                        string job = $"\x1b[36m[{backupName}]\x1b[0m";
                        string msg = $"\x1b[97mCopied '\x1b[33m{fileName}\x1b[97m' ({size} B) in {transferMs}ms{cryptoTag}\x1b[0m";
                        
                        Console.WriteLine($"{time} {job} {msg}");
                    }
                    catch
                    {
                        // Fallback
                        Console.WriteLine($"\x1b[0m[{DateTime.Now:HH:mm:ss}] {line}\x1b[0m");
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected abruptly
            }
        }
        Console.WriteLine($"\x1b[31m[-] Disconnected: {endPoint}\x1b[0m");
    }
}
