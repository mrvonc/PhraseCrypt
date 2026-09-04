using System;
using System.Security.Cryptography;

namespace PhraseCryptApp
{
    /// <summary>
    /// Honey Encryption for BIP39 entropy (concept by Juels &amp; Ristenpart, 2014).
    ///
    /// THE IDEA
    /// A wrong password does not produce an error. It produces a different but
    /// completely valid BIP39 phrase. An attacker trying millions of passwords gets
    /// a plausible recovery phrase every single time and cannot tell which is real.
    ///
    /// WHY BIP39 IS A PERFECT FIT
    /// Honey Encryption needs a plaintext space where every possible value looks
    /// legitimate (a "Distribution-Transforming Encoder"). BIP39 gives this for
    /// free: every 16-byte value is valid entropy and maps to exactly one correct
    /// 12-word phrase with a matching checksum (32 bytes to 24 words). The mapping
    /// is bijective and uniform, which is precisely what the scheme requires. That
    /// is why no complex encoder is needed - the entropy IS the seed space.
    ///
    /// DELIBERATE DESIGN DECISIONS (not oversights)
    /// - No AES-GCM, no HMAC, no authentication tag. A tag would tell the attacker
    ///   when the password was correct, destroying the entire protection.
    /// - No plaintext checksum stored in the container.
    /// - Decryption never reports "wrong password", because it cannot know and
    ///   must not know.
    ///
    /// VERSIONING
    /// Because there is no authentication, decryption cannot "try and see whether
    /// it worked" the way CryptoUtility does. The work factor must therefore be
    /// recorded in the header. Version 1 used 200,000 PBKDF2 iterations, version 2
    /// uses 600,000. Both are readable; new containers are always written as v2.
    ///
    /// LIMITS OF THE SCHEME
    /// This defends against offline guessing. If an attacker can verify candidates
    /// externally - for example by checking each generated phrase against a
    /// blockchain for funds - they will still find the real one. Honey Encryption
    /// does not replace a strong password; it makes guessing far more expensive.
    /// </summary>
    public static class HoneyEncryption
    {
        private const byte VersionLegacy = 1;   // 200,000 iterations
        private const byte VersionCurrent = 2;  // 600,000 iterations

        private const int SaltSizeBytes = 16;
        private const int LegacyIterations = 200_000;
        private const int CurrentIterations = 600_000;

        // Valid entropy lengths per BIP39 (128/160/192/224/256 bits).
        private static readonly int[] ValidEntropyLengths = { 16, 20, 24, 28, 32 };

        /// <summary>
        /// Encrypts BIP39 entropy. Layout: Version(1) | Salt(16) | Ciphertext(n)
        /// </summary>
        public static string EncryptToBase64(byte[] entropy, byte[] password)
        {
            if (entropy == null || Array.IndexOf(ValidEntropyLengths, entropy.Length) < 0)
            {
                throw new ArgumentException(Localization.T("ErrorHoneyInvalidEntropy"));
            }
            if (password == null || password.Length == 0)
            {
                throw new ArgumentException(Localization.T("ErrorPasswordEmpty"));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] keystream = DeriveKeystream(password, salt, entropy.Length, CurrentIterations);

            byte[] ciphertext = new byte[entropy.Length];
            for (int i = 0; i < entropy.Length; i++)
            {
                ciphertext[i] = (byte)(entropy[i] ^ keystream[i]);
            }

            SecureUtil.Wipe(keystream);

            byte[] result = new byte[1 + SaltSizeBytes + ciphertext.Length];
            result[0] = VersionCurrent;
            Buffer.BlockCopy(salt, 0, result, 1, SaltSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, result, 1 + SaltSizeBytes, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a honey container. ALWAYS returns entropy - even for a wrong
        /// password, in which case it is different but equally valid entropy.
        /// There is intentionally no way here to tell "correct" from "incorrect".
        /// </summary>
        public static byte[] DecryptFromBase64(string base64, byte[] password)
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

            // These checks concern the container FORMAT only, never the plaintext,
            // so they reveal nothing about whether the password was right.
            if (data.Length < 1 + SaltSizeBytes + 16)
            {
                throw new FormatException(Localization.T("ErrorHoneyBadContainer"));
            }

            int iterations = data[0] switch
            {
                VersionCurrent => CurrentIterations,
                VersionLegacy => LegacyIterations,
                _ => throw new FormatException(Localization.T("ErrorHoneyBadContainer"))
            };

            int cipherLength = data.Length - 1 - SaltSizeBytes;
            if (Array.IndexOf(ValidEntropyLengths, cipherLength) < 0)
            {
                throw new FormatException(Localization.T("ErrorHoneyBadContainer"));
            }

            byte[] salt = new byte[SaltSizeBytes];
            Buffer.BlockCopy(data, 1, salt, 0, SaltSizeBytes);

            byte[] ciphertext = new byte[cipherLength];
            Buffer.BlockCopy(data, 1 + SaltSizeBytes, ciphertext, 0, cipherLength);

            byte[] keystream = DeriveKeystream(password, salt, cipherLength, iterations);

            byte[] entropy = new byte[cipherLength];
            for (int i = 0; i < cipherLength; i++)
            {
                entropy[i] = (byte)(ciphertext[i] ^ keystream[i]);
            }

            SecureUtil.Wipe(keystream);
            return entropy;
        }

        /// <summary>
        /// Derives a keystream exactly as long as the plaintext. The random
        /// per-container salt rules out keystream reuse, which is what makes the
        /// simple XOR safe here.
        /// </summary>
        private static byte[] DeriveKeystream(byte[] password, byte[] salt, int length, int iterations)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(length);
        }
    }
}
