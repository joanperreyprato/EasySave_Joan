using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CryptoSoft;
using Xunit;

namespace EasySave.Tests
{
    public class CryptoServiceTests : IDisposable
    {
        private readonly string tempDir;

        public CryptoServiceTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveCryptoTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        [Fact]
        public void Encrypt_Decrypt_Roundtrip_ReturnsOriginalContent()
        {
            byte[] original = Encoding.UTF8.GetBytes("Hello, EasySave! This is a test.");
            using var inputStream = new MemoryStream(original);

            byte[] encrypted = CryptoService.Encrypt(inputStream, "mySecretKey");
            using var encStream = new MemoryStream(encrypted);

            byte[] decrypted = CryptoService.Decrypt(encStream, "mySecretKey");

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_ProducesOutputLargerThanInput()
        {
            byte[] original = Encoding.UTF8.GetBytes("test content");
            using var inputStream = new MemoryStream(original);

            byte[] encrypted = CryptoService.Encrypt(inputStream, "key");

            Assert.True(encrypted.Length > original.Length);
        }

        [Fact]
        public void Encrypt_WithEmptyInput_StillProducesOutput()
        {
            using var inputStream = new MemoryStream(Array.Empty<byte>());
            byte[] encrypted = CryptoService.Encrypt(inputStream, "key");
            Assert.NotEmpty(encrypted);
        }

        [Fact]
        public void Decrypt_WrongKey_ThrowsCryptographicException()
        {
            byte[] original = Encoding.UTF8.GetBytes("secret data");
            using var inputStream = new MemoryStream(original);
            byte[] encrypted = CryptoService.Encrypt(inputStream, "correctKey");
            using var encStream = new MemoryStream(encrypted);

            Assert.Throws<CryptographicException>(() => CryptoService.Decrypt(encStream, "wrongKey"));
        }

        [Fact]
        public void EncryptFile_DecryptFile_Roundtrip()
        {
            string inputFile = Path.Combine(tempDir, "input.txt");
            string encryptedFile = Path.Combine(tempDir, "encrypted.bin");
            string decryptedFile = Path.Combine(tempDir, "decrypted.txt");
            string content = "File content to encrypt and decrypt.";
            File.WriteAllText(inputFile, content, Encoding.UTF8);

            CryptoService.EncryptFile(inputFile, encryptedFile, "fileKey");
            CryptoService.DecryptFile(encryptedFile, decryptedFile, "fileKey");

            string result = File.ReadAllText(decryptedFile, Encoding.UTF8);
            Assert.Equal(content, result);
        }

        [Fact]
        public void EncryptFile_ProducesDifferentContentThanInput()
        {
            string inputFile = Path.Combine(tempDir, "input2.txt");
            string encryptedFile = Path.Combine(tempDir, "encrypted2.bin");
            File.WriteAllText(inputFile, "plaintext data");

            CryptoService.EncryptFile(inputFile, encryptedFile, "key");

            byte[] original = File.ReadAllBytes(inputFile);
            byte[] encrypted = File.ReadAllBytes(encryptedFile);
            Assert.NotEqual(original, encrypted);
        }

        [Fact]
        public void EncryptFile_NullInputPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CryptoService.EncryptFile(null!, Path.Combine(tempDir, "out.bin"), "key"));
        }

        [Fact]
        public void EncryptFile_NullOutputPath_ThrowsArgumentNullException()
        {
            string inputFile = Path.Combine(tempDir, "input3.txt");
            File.WriteAllText(inputFile, "data");
            Assert.Throws<ArgumentNullException>(() =>
                CryptoService.EncryptFile(inputFile, null!, "key"));
        }

        [Fact]
        public void DecryptFile_NullInputPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CryptoService.DecryptFile(null!, Path.Combine(tempDir, "out.txt"), "key"));
        }

        [Fact]
        public void DecryptFile_NullOutputPath_ThrowsArgumentNullException()
        {
            string inputFile = Path.Combine(tempDir, "input4.txt");
            File.WriteAllText(inputFile, "data");
            CryptoService.EncryptFile(inputFile, Path.Combine(tempDir, "enc4.bin"), "key");
            Assert.Throws<ArgumentNullException>(() =>
                CryptoService.DecryptFile(Path.Combine(tempDir, "enc4.bin"), null!, "key"));
        }

        [Fact]
        public void Encrypt_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CryptoService.Encrypt(null!, "key"));
        }

        [Fact]
        public void Decrypt_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CryptoService.Decrypt(null!, "key"));
        }

        [Fact]
        public void Encrypt_TwoCallsSameInput_ProduceDifferentOutput()
        {
            // AES CBC generates a random IV each time, so two encryptions differ
            byte[] data = Encoding.UTF8.GetBytes("same input");
            byte[] enc1 = CryptoService.Encrypt(new MemoryStream(data), "key");
            byte[] enc2 = CryptoService.Encrypt(new MemoryStream(data), "key");
            Assert.NotEqual(enc1, enc2);
        }

        [Fact]
        public void Encrypt_Decrypt_Roundtrip_LargeContent()
        {
            byte[] original = new byte[1024 * 64]; // 64 KB
            new Random(42).NextBytes(original);
            byte[] encrypted = CryptoService.Encrypt(new MemoryStream(original), "largeKey");
            byte[] decrypted = CryptoService.Decrypt(new MemoryStream(encrypted), "largeKey");
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void DecryptFile_WrongKey_ThrowsCryptographicException()
        {
            string inputFile = Path.Combine(tempDir, "input5.txt");
            string encFile = Path.Combine(tempDir, "enc5.bin");
            string decFile = Path.Combine(tempDir, "dec5.txt");
            File.WriteAllText(inputFile, "secure content");
            CryptoService.EncryptFile(inputFile, encFile, "correctKey");

            Assert.Throws<CryptographicException>(() =>
                CryptoService.DecryptFile(encFile, decFile, "wrongKey"));
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
