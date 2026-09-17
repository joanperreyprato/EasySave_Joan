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
                    // Print received log entry in console
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
                }
            }
            catch (Exception)
            {
                // Client disconnected abruptly
            }
        }
        Console.WriteLine($"[-] Disconnected: {endPoint}");
    }
}
