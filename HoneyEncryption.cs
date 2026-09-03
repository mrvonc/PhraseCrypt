using System;
using System.Security.Cryptography;

namespace PhraseCryptApp
{
    /// <summary>
    /// Honey encryption for BIP39 entropy (Juels &amp; Ristenpart, 2014).
    ///
    /// CORE IDEA:
    /// A wrong password does not produce an error. It produces a different but
    /// entirely valid BIP39 phrase. An attacker who tries millions of passwords
    /// receives a plausible recovery phrase every single time and cannot tell
    /// which one is real.
    ///
    /// WHY THIS WORKS FOR BIP39:
    /// Honey encryption requires a message space in which every possible value looks
    /// plausible (a "distribution-transforming encoder"). BIP39 provides this for
    /// free: every 16-byte value is valid entropy and maps to exactly one correct
    /// 12-word phrase with a matching checksum (likewise 32 bytes -> 24 words). The
    /// entropy-to-mnemonic mapping is bijective and uniform, which is precisely what
    /// the scheme needs.
    ///
    /// DELIBERATE DESIGN DECISIONS (not oversights):
    /// - NO AES-GCM, NO HMAC, NO authentication tag. A tag would tell the attacker
    ///   when the password was correct, which would defeat the entire scheme.
    /// - NO checksum over the plaintext inside the container.
    /// - Decryption never reports "wrong password", because it cannot know and
    ///   must not know.
    ///
    /// LIMITATIONS (important to understand):
    /// The protection applies to pure offline brute force. If an attacker can verify
    /// candidates externally, for example by checking each derived phrase against the
    /// blockchain for funds, they will still find the real one. Honey encryption is
    /// therefore not a replacement for a strong password; it raises the cost of
    /// guessing considerably.
    /// </summary>
    public static class HoneyEncryption
    {
        private const byte FormatVersion = 1;
        private const int SaltSizeBytes = 16;
        private const int Pbkdf2Iterations = 200_000;

        /// <summary>
        /// Entropy lengths permitted by BIP39 (128/160/192/224/256 bits).
        /// </summary>
        private static readonly int[] ValidEntropyLengths = { 16, 20, 24, 28, 32 };

        /// <summary>
        /// Encrypts BIP39 entropy. Layout: version(1) | salt(16) | ciphertext(n)
        /// </summary>
        public static string EncryptToBase64(byte[] entropy, string password)
        {
            if (entropy == null || Array.IndexOf(ValidEntropyLengths, entropy.Length) < 0)
            {
                throw new ArgumentException(Localization.T("ErrorHoneyInvalidEntropy"));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] keystream = DeriveKeystream(password, salt, entropy.Length);

            byte[] ciphertext = new byte[entropy.Length];
            for (int i = 0; i < entropy.Length; i++)
            {
                ciphertext[i] = (byte)(entropy[i] ^ keystream[i]);
            }

            Array.Clear(keystream, 0, keystream.Length);

            byte[] result = new byte[1 + SaltSizeBytes + ciphertext.Length];
            result[0] = FormatVersion;
            Buffer.BlockCopy(salt, 0, result, 1, SaltSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, result, 1 + SaltSizeBytes, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a honey container. ALWAYS returns entropy, including for a wrong
        /// password, in which case it is simply different but equally valid entropy.
        /// There is deliberately no way to distinguish correct from incorrect here.
        /// </summary>
        public static byte[] DecryptFromBase64(string base64, string password)
        {
            if (string.IsNullOrEmpty(password))
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

            // These checks concern the container FORMAT only, never the plaintext,
            // so they reveal nothing about the password.
            if (data.Length < 1 + SaltSizeBytes + 16)
            {
                throw new FormatException(Localization.T("ErrorHoneyBadContainer"));
            }
            if (data[0] != FormatVersion)
            {
                throw new FormatException(Localization.T("ErrorHoneyBadContainer"));
            }

            int cipherLength = data.Length - 1 - SaltSizeBytes;
            if (Array.IndexOf(ValidEntropyLengths, cipherLength) < 0)
            {
                throw new FormatException(Localization.T("ErrorHoneyBadContainer"));
            }

            byte[] salt = new byte[SaltSizeBytes];
            Buffer.BlockCopy(data, 1, salt, 0, SaltSizeBytes);

            byte[] ciphertext = new byte[cipherLength];
            Buffer.BlockCopy(data, 1 + SaltSizeBytes, ciphertext, 0, cipherLength);

            byte[] keystream = DeriveKeystream(password, salt, cipherLength);

            byte[] entropy = new byte[cipherLength];
            for (int i = 0; i < cipherLength; i++)
            {
                entropy[i] = (byte)(ciphertext[i] ^ keystream[i]);
            }

            Array.Clear(keystream, 0, keystream.Length);
            return entropy;
        }

        /// <summary>
        /// How many words does the phrase in this container have? Derivable from the
        /// length alone. This is the only metadata leak and is inherent to the design.
        /// </summary>
        public static int GetWordCount(string base64)
        {
            byte[] data = Convert.FromBase64String((base64 ?? string.Empty).Trim());
            int entropyBytes = data.Length - 1 - SaltSizeBytes;
            return entropyBytes * 8 / 32 * 3;
        }

        /// <summary>
        /// Derives a keystream of exactly the plaintext length from the password.
        /// The random per-container salt rules out keystream reuse, which is what
        /// makes the XOR safe here.
        /// </summary>
        private static byte[] DeriveKeystream(string password, byte[] salt, int length)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(length);
        }
    }
}
