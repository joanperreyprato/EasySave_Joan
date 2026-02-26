using EasySave.LogServer;

Console.Title = "EasySave Log Server";
Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║           EasySave Distributed Log Server v1.0           ║");
Console.WriteLine("║                   Docker Ready Edition                    ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Read configuration from environment variables (Docker-friendly)
var port = int.TryParse(Environment.GetEnvironmentVariable("LOG_SERVER_PORT"), out var p) ? p : 5555;
var logDirectory = Environment.GetEnvironmentVariable("LOG_SERVER_DIRECTORY") ?? "/app/logs";

// Ensure log directory exists
Directory.CreateDirectory(logDirectory);

var server = new LogServerService(port, logDirectory);

// Handle graceful shutdown
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n\nShutdown signal received...");
    cts.Cancel();
};

try
{
    await server.StartAsync(cts.Token);
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
finally
{
    server.Stop();
}

Console.WriteLine("Server shutdown complete.");
return 0;
