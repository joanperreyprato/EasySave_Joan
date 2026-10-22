using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CryptoSoft;

/// <summary>
/// Entry point for the CryptoSoft encryption/decryption tool.
/// Usage: CryptoSoft.exe &lt;source_file&gt; &lt;destination_file&gt;
/// Exit code: positive = processing time in ms, negative = error.
/// </summary>
internal static class Program
{
    /// <summary>System-wide mutex name - guarantees only one CryptoSoft runs at a time (v3.0).</summary>
    private const string MutexName = "Global\\CryptoSoftMutex_EasySave";

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: CryptoSoft.exe <source_file> <destination_file>");
            return -1;
        }

        string source      = args[0];
        string destination = args[1];

        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"Error: source file not found: {source}");
            return -2;
        }

        // v3.0: Mono-instance - wait up to 30s for any other CryptoSoft instance to finish
        using var mutex = new Mutex(initiallyOwned: false, name: MutexName);
        bool acquired = false;
        try
        {
            acquired = AcquireMutex(mutex);
            if (!acquired)
            {
                Console.Error.WriteLine("Error: another CryptoSoft instance is already running.");
                return -4; // Timeout waiting for mutex
            }

            byte[] key = LoadKey();
            var sw = Stopwatch.StartNew();
            XorEncryptDecrypt(source, destination, key);
            sw.Stop();

            long ms = sw.ElapsedMilliseconds;
            Console.WriteLine($"OK: {ms}ms");
            return (int)Math.Min(ms == 0 ? 1 : ms, int.MaxValue); // Always return > 0 on success
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return -3;
        }
        finally
        {
            if (acquired) mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Waits up to 30s for the single-instance mutex.
    /// A mutex left by a CryptoSoft instance that crashed is "abandoned": we then own it and can continue.
    /// </summary>
    private static bool AcquireMutex(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.FromSeconds(30));
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    /// <summary>
    /// Applies XOR bit-by-bit encryption/decryption between the source file and the key.
    /// Since XOR is symmetric, the same operation encrypts and decrypts.
    /// </summary>
    private static void XorEncryptDecrypt(string source, string destination, byte[] key)
    {
        byte[] data = File.ReadAllBytes(source);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];

        // Ensure destination directory exists
        string? dir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(destination, data);
    }

    /// <summary>
    /// Loads the encryption key from config.json next to the executable.
    /// Falls back to a default 64-bit key if config is missing or invalid.
    /// The key must be at least 64 bits (8 bytes).
    /// </summary>
    private static byte[] LoadKey()
    {
        string configPath = Path.Combine(
            AppContext.BaseDirectory, "cryptosoft.config.json");

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("key", out var keyEl))
                {
                    string? keyStr = keyEl.GetString();
                    if (!string.IsNullOrWhiteSpace(keyStr))
                    {
                        byte[] keyBytes = Encoding.UTF8.GetBytes(keyStr);
                        if (keyBytes.Length >= 8) return keyBytes;
                    }
                }
            }
            catch { /* Fall through to default */ }
        }

        // Default key: 64 bits minimum as required by the specs
        return Encoding.UTF8.GetBytes("EaseSave2024!");
    }
}
