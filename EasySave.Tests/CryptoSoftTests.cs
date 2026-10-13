using System.Diagnostics;
using Xunit;

namespace EasySave.Tests;

/// <summary>
/// Unit and concurrency tests for CryptoSoft.
/// Validates mono-instance behavior via system Mutex and proper XOR ciphering.
/// </summary>
public sealed class CryptoSoftTests
{
    private static string GetCryptoSoftExePath()
    {
        string baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "CryptoSoft", "bin", "Debug", "net8.0", "CryptoSoft.exe");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("CryptoSoft.exe not found.");
    }

    [Fact]
    public void CryptoSoft_WhenSourceMissing_ReturnsNegativeExitCode()
    {
        string exe = GetCryptoSoftExePath();
        var psi = new ProcessStartInfo(exe, "\"non_existent_file.txt\" \"dest.txt\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit(5000);

        Assert.True(process.ExitCode < 0, $"Expected negative exit code for missing source, got {process.ExitCode}");
    }

    [Fact]
    public void CryptoSoft_XorEncryptAndDecrypt_RestoresOriginalContent()
    {
        string exe = GetCryptoSoftExePath();
        string tempDir = Path.Combine(Path.GetTempPath(), "CryptoSoftTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string originalFile = Path.Combine(tempDir, "original.txt");
            string encryptedFile = Path.Combine(tempDir, "encrypted.txt");
            string decryptedFile = Path.Combine(tempDir, "decrypted.txt");

            string sampleText = "Hello ProSoft! EasySave XOR Encryption test 2026.";
            File.WriteAllText(originalFile, sampleText);

            // Step 1: Encrypt
            var encPsi = new ProcessStartInfo(exe, $"\"{originalFile}\" \"{encryptedFile}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var encProc = Process.Start(encPsi)!;
            encProc.WaitForExit(5000);
            Assert.True(encProc.ExitCode > 0, "Encryption should return > 0 (processing time)");

            // Verify file is different (encrypted)
            string encText = File.ReadAllText(encryptedFile);
            Assert.NotEqual(sampleText, encText);

            // Step 2: Decrypt (XOR symmetric)
            var decPsi = new ProcessStartInfo(exe, $"\"{encryptedFile}\" \"{decryptedFile}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var decProc = Process.Start(decPsi)!;
            decProc.WaitForExit(5000);
            Assert.True(decProc.ExitCode > 0, "Decryption should return > 0 (processing time)");

            // Verify decrypted text matches original
            string decText = File.ReadAllText(decryptedFile);
            Assert.Equal(sampleText, decText);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CryptoSoft_MonoInstance_BlocksOrWaitsWhenConcurrent()
    {
        // Acquire the exact system Mutex that CryptoSoft uses
        const string mutexName = "Global\\CryptoSoftMutex_EasySave";
        using var holderMutex = new Mutex(true, mutexName, out bool createdNew);
        Assert.True(createdNew, "Should be able to acquire mutex in test");

        try
        {
            string exe = GetCryptoSoftExePath();
            string tempDir = Path.Combine(Path.GetTempPath(), "CryptoSoftMonoTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string src = Path.Combine(tempDir, "src.txt");
                string dst = Path.Combine(tempDir, "dst.txt");
                File.WriteAllText(src, "Test mono instance locking");

                // Start CryptoSoft while mutex is held
                var psi = new ProcessStartInfo(exe, $"\"{src}\" \"{dst}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)!;
                // Since mutex is held, CryptoSoft must NOT complete immediately
                bool exitedPrematurely = proc.WaitForExit(500);
                Assert.False(exitedPrematurely, "CryptoSoft must be blocked by the mutex while another instance runs!");

                // Kill process to avoid waiting 30s timeout
                proc.Kill();
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }
        finally
        {
            holderMutex.ReleaseMutex();
        }
    }
}
