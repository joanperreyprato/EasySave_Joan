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

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"[{backupName}] ");
                        
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"Copied '");
                        
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(fileName);
                        
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"' ({size} B) in {transferMs}ms{cryptoTag}");
                        
                        Console.ResetColor();
                    }
                    catch
                    {
                        // Fallback si le texte reçu n'est pas du JSON valide
                        Console.ResetColor();
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected abruptly
            }
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[-] Disconnected: {endPoint}");
        Console.ResetColor();
    }
}
