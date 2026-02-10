using System;
using System.IO;
using System.Security.Cryptography;
using System.Text

namespace CryptoSoft
{
    // Simple file encryptor/decryptor using AES-256-CBC with HMAC-SHA256 for integrity.
    // Key parameter expected to be a UTF-8 string; it's converted to a 32-byte key via SHA256.
    public static class CryptoService
    {
        // Encrypts input file to output file using given key (string). Overwrites output if exists.
        // This implementation delegates to the stream-based Encrypt which returns the encrypted bytes.
        public static void EncryptFile(string inputPath, string outputPath, string key)
        {
            if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));
            if (key == null) throw new ArgumentNullException(nameof(key));

            using var input = File.OpenRead(inputPath);
            byte[] encrypted = Encrypt(input, key);
            File.WriteAllBytes(outputPath, encrypted);
        }

        // Encrypts data from the provided stream and returns the encrypted file bytes.
        // Format: [1 byte IV length][IV][ciphertext][HMAC-SHA256]
        public static byte[] Encrypt(Stream inputStream, string key)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (key == null) throw new ArgumentNullException(nameof(key));

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] keyBytes = DeriveKey(key);
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var hmac = new HMACSHA256(keyBytes);
            using var ms = new MemoryStream();

            // Write IV length and IV
            ms.WriteByte((byte)aes.IV.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            // Write ciphertext
            using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
            {
                inputStream.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
            }

            // Compute HMAC over IV + ciphertext
            byte[] allSoFar = ms.ToArray();
            int headerLen = 1 + aes.IV.Length;
            byte[] cipher = new byte[allSoFar.Length - headerLen];
            Buffer.BlockCopy(allSoFar, headerLen, cipher, 0, cipher.Length);
            byte[] mac = hmac.ComputeHash(Combine(aes.IV, cipher));

            ms.Write(mac, 0, mac.Length);
            return ms.ToArray();
        }

        // Decrypts input file to output file using given key (string). Throws on HMAC mismatch.
        // Delegates to Decrypt which accepts a stream and returns plaintext bytes.
        public static void DecryptFile(string inputPath, string outputPath, string key)
        {
            if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));
            if (key == null) throw new ArgumentNullException(nameof(key));

            using var input = File.OpenRead(inputPath);
            byte[] plain = Decrypt(input, key);
            File.WriteAllBytes(outputPath, plain);
        }

        // Decrypts data from the provided stream (which must contain the format produced by Encrypt)
        // and returns the plaintext bytes. Throws on HMAC mismatch.
        public static byte[] Decrypt(Stream inputStream, string key)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (key == null) throw new ArgumentNullException(nameof(key));

            byte[] keyBytes = DeriveKey(key);

            // Read entire input into memory (caller requested a file-like return)
            using var msInput = new MemoryStream();
            inputStream.CopyTo(msInput);
            byte[] all = msInput.ToArray();
            int pos = 0;

            if (all.Length < 1) throw new InvalidDataException("Input too short");
            int ivLen = all[pos++];
            if (ivLen <= 0 || ivLen > 64) throw new InvalidDataException("Invalid IV length");

            if (all.Length < 1 + ivLen + 32) throw new InvalidDataException("File too short");

            byte[] iv = new byte[ivLen];
            Buffer.BlockCopy(all, pos, iv, 0, ivLen);
            pos += ivLen;

            int macLen = 32;
            int cipherLen = all.Length - pos - macLen;
            if (cipherLen < 0) throw new InvalidDataException("File too short for ciphertext");

            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(all, pos, cipher, 0, cipherLen);
            pos += cipherLen;

            byte[] mac = new byte[macLen];
            Buffer.BlockCopy(all, pos, mac, 0, macLen);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] expected = hmac.ComputeHash(Combine(iv, cipher));
            if (!CryptographicEquals(mac, expected)) throw new CryptographicException("HMAC mismatch - invalid key or corrupted file");

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = iv;

            using var msCipher = new MemoryStream(cipher);
            using var cryptoStream = new CryptoStream(msCipher, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var outMs = new MemoryStream();
            cryptoStream.CopyTo(outMs);
            return outMs.ToArray();
        }

        static byte[] DeriveKey(string key)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(key ?? string.Empty));
        }

        static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }

        static bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
