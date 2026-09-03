using System;
using System.Security.Cryptography;
using System.Text;

namespace PhraseCryptApp
{
    /// <summary>
    /// AES-256-GCM encryption with password-based key derivation (PBKDF2/SHA-256).
    /// Output layout (Base64): salt(16) | nonce(12) | tag(16) | ciphertext
    /// </summary>
    public static class CryptoUtility
    {
        private const int SaltSizeBytes = 16;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int Pbkdf2Iterations = 200_000;
        private const int KeySizeBytes = 32; // AES-256

        public static string EncryptToBase64(string plaintext, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] key = DeriveKey(password, salt);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSizeBytes];

            using (AesGcm aes = new AesGcm(key, TagSizeBytes))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            byte[] result = new byte[SaltSizeBytes + NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
            int offset = 0;
            Buffer.BlockCopy(salt, 0, result, offset, SaltSizeBytes); offset += SaltSizeBytes;
            Buffer.BlockCopy(nonce, 0, result, offset, NonceSizeBytes); offset += NonceSizeBytes;
            Buffer.BlockCopy(tag, 0, result, offset, TagSizeBytes); offset += TagSizeBytes;
            Buffer.BlockCopy(cipherBytes, 0, result, offset, cipherBytes.Length);

            Array.Clear(key, 0, key.Length);
            return Convert.ToBase64String(result);
        }

        public static string DecryptFromBase64(string base64, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64.Trim());
            }
            catch (FormatException)
            {
                throw new FormatException(Localization.T("ErrorInvalidBase64"));
            }

            int minLength = SaltSizeBytes + NonceSizeBytes + TagSizeBytes;
            if (data.Length < minLength)
            {
                throw new FormatException(Localization.T("ErrorTooShortForEncryptedData"));
            }

            int offset = 0;
            byte[] salt = data[offset..(offset += SaltSizeBytes)];
            byte[] nonce = data[offset..(offset += NonceSizeBytes)];
            byte[] tag = data[offset..(offset += TagSizeBytes)];
            byte[] cipherBytes = data[offset..];

            byte[] key = DeriveKey(password, salt);
            byte[] plainBytes = new byte[cipherBytes.Length];

            try
            {
                using AesGcm aes = new AesGcm(key, TagSizeBytes);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }
            catch (CryptographicException)
            {
                throw new CryptographicException(Localization.T("ErrorDecryptionFailed"));
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySizeBytes);
        }
    }
}
