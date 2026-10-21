using System.Diagnostics;

namespace EasySave.Core.Services;

/// <summary>
/// Launches the external CryptoSoft tool to encrypt a backed-up file in place.
/// CryptoSoft is mono-instance (v3.0), so calls are serialized inside EasySave and
/// a busy CryptoSoft (started by another application) is retried a few times.
/// </summary>
public static class CryptoSoftRunner
{
    /// <summary>Error code: CryptoSoft.exe could not be found.</summary>
    public const int NotFound = -10;
    /// <summary>Error code: CryptoSoft could not be started.</summary>
    public const int StartFailed = -11;
    /// <summary>Error code: CryptoSoft did not finish in time and was killed.</summary>
    public const int Timeout = -12;

    private const int BusyExitCode = -4; // Returned by CryptoSoft when another instance holds its mutex
    private const int MaxAttempts = 3;
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim SingleInstanceGate = new(1, 1);

    /// <summary>
    /// Encrypts <paramref name="targetFile"/> in place.
    /// </summary>
    /// <returns>The encryption time in ms (&gt; 0), or a negative error code.</returns>
    public static async Task<long> EncryptAsync(string targetFile)
    {
        string? exePath = Locate();
        if (exePath is null) return NotFound;

        await SingleInstanceGate.WaitAsync();
        try
        {
            long result = BusyExitCode;
            for (int attempt = 0; attempt < MaxAttempts && result == BusyExitCode; attempt++)
                result = await RunOnceAsync(exePath, targetFile);
            return result;
        }
        finally
        {
            SingleInstanceGate.Release();
        }
    }

    private static async Task<long> RunOnceAsync(string exePath, string targetFile)
    {
        string tempFile = targetFile + ".tmp";
        var psi = new ProcessStartInfo(exePath)
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        psi.ArgumentList.Add(targetFile);
        psi.ArgumentList.Add(tempFile);

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return StartFailed;

            // Drain the output so CryptoSoft never blocks on a full pipe and does not write in our console
            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            using var timeout = new CancellationTokenSource(ProcessTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                return Timeout;
            }

            int exitCode = process.ExitCode;
            if (exitCode > 0)
                File.Move(tempFile, targetFile, overwrite: true); // Replace the copy by its encrypted version
            return exitCode;
        }
        catch
        {
            return StartFailed;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    /// <summary>Locates CryptoSoft.exe next to the running executable, or in the solution (development).</summary>
    private static string? Locate()
    {
        string candidate = Path.Combine(AppContext.BaseDirectory, "CryptoSoft.exe");
        if (File.Exists(candidate)) return candidate;

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (dir.GetFiles("*.sln").Length == 0) continue;

            return new[] { "Debug", "Release" }
                .Select(config => Path.Combine(dir.FullName, "CryptoSoft", "bin", config, "net8.0", "CryptoSoft.exe"))
                .FirstOrDefault(File.Exists);
        }
        return null;
    }
}
