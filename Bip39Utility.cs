using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PhraseCryptApp
{
    /// <summary>
    /// Standards-compliant BIP39 mnemonic generation and validation
    /// (entropy plus SHA-256 checksum, per the official specification).
    /// Kept deliberately separate from the position-mapping logic, since the two
    /// approaches are fundamentally different.
    /// </summary>
    public static class Bip39Utility
    {
        // Permitted word counts and their corresponding entropy bit lengths (BIP39)
        private static readonly Dictionary<int, int> EntropyBitsByWordCount = new()
        {
            { 12, 128 },
            { 15, 160 },
            { 18, 192 },
            { 21, 224 },
            { 24, 256 },
        };

        public static bool IsValidWordCount(int wordCount) => EntropyBitsByWordCount.ContainsKey(wordCount);

        public static string SupportedWordCountsDescription => string.Join(", ", EntropyBitsByWordCount.Keys);

        /// <summary>
        /// Generates a cryptographically random, standards-compliant BIP39 mnemonic phrase.
        /// </summary>
        public static (string Mnemonic, string EntropyHex) Generate(IReadOnlyList<string> wordlist, int wordCount)
        {
            if (wordlist.Count != 2048)
            {
                throw new ArgumentException("The wordlist must contain exactly 2048 words.");
            }
            if (!EntropyBitsByWordCount.TryGetValue(wordCount, out int entropyBits))
            {
                throw new ArgumentException($"Invalid word count for BIP39. Allowed: {SupportedWordCountsDescription}.");
            }

            byte[] entropy = RandomNumberGenerator.GetBytes(entropyBits / 8);
            string mnemonic = EntropyToMnemonic(entropy, wordlist);
            return (mnemonic, Convert.ToHexString(entropy));
        }

        /// <summary>
        /// Converts raw entropy into a word sequence including the checksum, per BIP39.
        /// </summary>
        public static string EntropyToMnemonic(byte[] entropy, IReadOnlyList<string> wordlist)
        {
            int entropyBits = entropy.Length * 8;
            if (!EntropyBitsByWordCount.ContainsValue(entropyBits))
            {
                throw new ArgumentException($"Invalid entropy length: {entropyBits} bits.");
            }

            int checksumBits = entropyBits / 32;
            byte[] hash = SHA256.HashData(entropy);

            string entropyBinary = BytesToBinary(entropy);
            string checksumBinary = BytesToBinary(hash).Substring(0, checksumBits);
            string combined = entropyBinary + checksumBinary;

            int wordCount = combined.Length / 11;
            List<string> words = new List<string>(wordCount);
            for (int i = 0; i < wordCount; i++)
            {
                string chunk = combined.Substring(i * 11, 11);
                int index = Convert.ToInt32(chunk, 2);
                words.Add(wordlist[index]);
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Validates a mnemonic phrase including its checksum. On success it also
        /// returns the underlying entropy as hex.
        /// </summary>
        public static bool TryValidate(string mnemonic, IReadOnlyList<string> wordlist, out string entropyHex, out string errorMessage)
        {
            entropyHex = string.Empty;
            errorMessage = string.Empty;

            string[] words = (mnemonic ?? string.Empty)
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (!EntropyBitsByWordCount.ContainsKey(words.Length))
            {
                errorMessage = $"Invalid word count ({words.Length}). Allowed: {SupportedWordCountsDescription}.";
                return false;
            }

            Dictionary<string, int> indexLookup = new Dictionary<string, int>(2048);
            for (int i = 0; i < wordlist.Count; i++)
            {
                indexLookup[wordlist[i]] = i;
            }

            StringBuilder combinedBits = new StringBuilder(words.Length * 11);
            foreach (string word in words)
            {
                if (!indexLookup.TryGetValue(word, out int index))
                {
                    errorMessage = $"The word '{word}' does not appear in the wordlist.";
                    return false;
                }
                combinedBits.Append(Convert.ToString(index, 2).PadLeft(11, '0'));
            }

            string combined = combinedBits.ToString();
            int checksumBits = combined.Length / 33; // CS = ENT/32, and total = ENT+CS = ENT*33/32, therefore CS = total/33
            int entropyBits = combined.Length - checksumBits;

            string entropyBinary = combined.Substring(0, entropyBits);
            string checksumBinary = combined.Substring(entropyBits);

            byte[] entropy = BinaryToBytes(entropyBinary);
            byte[] hash = SHA256.HashData(entropy);
            string expectedChecksum = BytesToBinary(hash).Substring(0, checksumBits);

            if (checksumBinary != expectedChecksum)
            {
                errorMessage = "Invalid checksum: not a genuine BIP39 phrase (or a typo / wrong word order).";
                return false;
            }

            entropyHex = Convert.ToHexString(entropy);
            return true;
        }

        private static string BytesToBinary(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 8);
            foreach (byte b in bytes)
            {
                sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        private static byte[] BinaryToBytes(string binary)
        {
            int byteCount = binary.Length / 8;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = Convert.ToByte(binary.Substring(i * 8, 8), 2);
            }
            return bytes;
        }
    }
}
