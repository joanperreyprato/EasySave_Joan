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
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: CryptoSoft.exe <source_file> <destination_file>");
            return -1;
        }

        string source = args[0];
        string destination = args[1];

        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"Error: source file not found: {source}");
            return -2;
        }

        try
        {
            byte[] key = LoadKey();
            var sw = Stopwatch.StartNew();
            XorEncryptDecrypt(source, destination, key);
            sw.Stop();

            long ms = sw.ElapsedMilliseconds;
            Console.WriteLine($"OK: {ms}ms");
            return (int)Math.Min(ms, int.MaxValue);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return -3;
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
