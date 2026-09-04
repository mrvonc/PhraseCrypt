using System;
using System.Security.Cryptography;
using System.Text;

namespace PhraseCryptApp
{
    /// <summary>
    /// AES-256-GCM with password-based key derivation (PBKDF2-HMAC-SHA256).
    ///
    /// Container layout (Base64): Salt(16) | Nonce(12) | Tag(16) | Ciphertext
    ///
    /// GCM is authenticated encryption: a wrong password or tampered data is
    /// detected and reported. That is correct behaviour for normal encryption, and
    /// exactly the opposite of what HoneyEncryption.cs does on purpose.
    ///
    /// ITERATION COUNT
    /// New containers use 600,000 iterations, the figure OWASP gives for
    /// PBKDF2-HMAC-SHA256 in its Password Storage guidance. Containers written by
    /// earlier versions used 200,000. Because GCM authenticates, decryption can
    /// simply try the current parameters first and fall back to the legacy count
    /// if authentication fails - so older containers keep working without needing
    /// a format change.
    ///
    /// Argon2id would be the stronger choice, since PBKDF2 is not memory-hard and
    /// therefore weak against GPU attacks. It is deliberately not used here because
    /// it would require a third-party package, and pulling an external dependency
    /// into the trusted core of a security tool is its own risk. See the README.
    /// </summary>
    public static class CryptoUtility
    {
        private const int SaltSizeBytes = 16;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int KeySizeBytes = 32; // AES-256

        /// <summary>Current work factor (OWASP guidance for PBKDF2-HMAC-SHA256).</summary>
        public const int Pbkdf2Iterations = 600_000;

        /// <summary>Work factor used by earlier versions; kept only for reading.</summary>
        private const int LegacyPbkdf2Iterations = 200_000;

        public static string EncryptToBase64(string plaintext, byte[] password)
        {
            if (password == null || password.Length == 0)
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] key = DeriveKey(password, salt, Pbkdf2Iterations);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSizeBytes];

            try
            {
                using AesGcm aes = new AesGcm(key, TagSizeBytes);
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }
            finally
            {
                SecureUtil.Wipe(key);
                SecureUtil.Wipe(plainBytes);
            }

            byte[] result = new byte[SaltSizeBytes + NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
            int offset = 0;
            Buffer.BlockCopy(salt, 0, result, offset, SaltSizeBytes); offset += SaltSizeBytes;
            Buffer.BlockCopy(nonce, 0, result, offset, NonceSizeBytes); offset += NonceSizeBytes;
            Buffer.BlockCopy(tag, 0, result, offset, TagSizeBytes); offset += TagSizeBytes;
            Buffer.BlockCopy(cipherBytes, 0, result, offset, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string DecryptFromBase64(string base64, byte[] password)
        {
            if (password == null || password.Length == 0)
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] data;
            try
            {
                data = Convert.FromBase64String((base64 ?? string.Empty).Trim());
            }
            catch (FormatException)
            {
                throw new FormatException(Localization.T("ErrorInvalidBase64"));
            }

            if (data.Length < SaltSizeBytes + NonceSizeBytes + TagSizeBytes)
            {
                throw new FormatException(Localization.T("ErrorTooShortForEncryptedData"));
            }

            int offset = 0;
            byte[] salt = data[offset..(offset += SaltSizeBytes)];
            byte[] nonce = data[offset..(offset += NonceSizeBytes)];
            byte[] tag = data[offset..(offset += TagSizeBytes)];
            byte[] cipherBytes = data[offset..];

            // Current parameters first, then the legacy work factor so containers
            // created by older builds remain readable.
            if (TryDecrypt(cipherBytes, salt, nonce, tag, password, Pbkdf2Iterations, out string? plaintext) ||
                TryDecrypt(cipherBytes, salt, nonce, tag, password, LegacyPbkdf2Iterations, out plaintext))
            {
                return plaintext!;
            }

            throw new CryptographicException(Localization.T("ErrorDecryptionFailed"));
        }

        private static bool TryDecrypt(byte[] cipherBytes, byte[] salt, byte[] nonce, byte[] tag,
                                       byte[] password, int iterations, out string? plaintext)
        {
            plaintext = null;
            byte[] key = DeriveKey(password, salt, iterations);
            byte[] plainBytes = new byte[cipherBytes.Length];

            try
            {
                using AesGcm aes = new AesGcm(key, TagSizeBytes);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
                plaintext = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch (CryptographicException)
            {
                return false; // wrong password, wrong work factor, or tampered data
            }
            finally
            {
                SecureUtil.Wipe(key);
                SecureUtil.Wipe(plainBytes);
            }
        }

        private static byte[] DeriveKey(byte[] password, byte[] salt, int iterations)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySizeBytes);
        }
    }
}
