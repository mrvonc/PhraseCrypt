using System;
using System.Collections.Generic;

namespace PhraseCryptApp
{
    /// <summary>
    /// The official BIP39 English test vectors, taken verbatim from the reference
    /// implementation at https://github.com/trezor/python-mnemonic (vectors.json).
    ///
    /// WHY THIS EXISTS
    /// A checksum bug in BIP39 does not look like a bug. The output is still a list
    /// of real words, so nothing appears wrong until a wallet rejects the phrase -
    /// or worse, silently derives the wrong addresses. These vectors turn "looks
    /// plausible" into "provably correct": each fixed entropy value must produce
    /// exactly one known phrase.
    ///
    /// RunSelfTest() is executed once at application startup. If it fails, the app
    /// refuses to generate anything.
    /// </summary>
    public static class Bip39TestVectors
    {
        /// <summary>Entropy (hex) paired with the mnemonic it must produce.</summary>
        public static readonly (string EntropyHex, string Mnemonic)[] Vectors =
        {
            ("00000000000000000000000000000000",
             "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"),

            ("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
             "legal winner thank year wave sausage worth useful legal winner thank yellow"),

            ("80808080808080808080808080808080",
             "letter advice cage absurd amount doctor acoustic avoid letter advice cage above"),

            ("ffffffffffffffffffffffffffffffff",
             "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo wrong"),

            ("000000000000000000000000000000000000000000000000",
             "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon " +
             "abandon abandon abandon abandon abandon abandon agent"),

            ("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
             "legal winner thank year wave sausage worth useful legal winner thank year wave sausage " +
             "worth useful legal will"),

            ("808080808080808080808080808080808080808080808080",
             "letter advice cage absurd amount doctor acoustic avoid letter advice cage absurd amount " +
             "doctor acoustic avoid letter always"),

            ("ffffffffffffffffffffffffffffffffffffffffffffffff",
             "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo when"),

            ("0000000000000000000000000000000000000000000000000000000000000000",
             "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon " +
             "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon " +
             "abandon art"),

            ("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
             "legal winner thank year wave sausage worth useful legal winner thank year wave sausage " +
             "worth useful legal winner thank year wave sausage worth title"),

            ("8080808080808080808080808080808080808080808080808080808080808080",
             "letter advice cage absurd amount doctor acoustic avoid letter advice cage absurd amount " +
             "doctor acoustic avoid letter advice cage absurd amount doctor acoustic bless"),

            ("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
             "zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo " +
             "zoo vote"),

            ("9e885d952ad362caeb4efe34a8e91bd2",
             "ozone drill grab fiber curtain grace pudding thank cruise elder eight picnic"),

            ("6610b25967cdcca9d59875f5cb50b0ea75433311869e930b",
             "gravity machine north sort system female filter attitude volume fold club stay feature " +
             "office ecology stable narrow fog"),

            ("68a79eaca2324873eacc50cb9c6eca8cc68ea5d936f98787c60c7ebc74e6ce7c",
             "hamster diagram private dutch cause delay private meat slide toddler razor book happy " +
             "fancy gospel tennis maple dilemma loan word shrug inflict delay length"),

            ("c0ba5a8e914111210f2bd131f3d5e08d",
             "scheme spot photo card baby mountain device kick cradle pact join borrow"),

            ("6d9be1ee6ebd27a258115aad99b7317b9c8d28b6d76431c3",
             "horn tenant knee talent sponsor spell gate clip pulse soap slush warm silver nephew swap " +
             "uncle crack brave"),

            ("9f6a2878b2520799a44ef18bc7df394e7061a224d2c33cd015b157d746869863",
             "panda eyebrow bullet gorilla call smoke muffin taste mesh discover soft ostrich alcohol " +
             "speed nation flash devote level hobby quick inner drive ghost inside"),

            ("23db8160a31d3e0dca3688ed941adbf3",
             "cat swing flag economy stadium alone churn speed unique patch report train"),

            ("8197a4a47f0425faeaa69deebc05ca29c0a5b5cc76ceacc0",
             "light rule cinnamon wrap drastic word pride squirrel upgrade then income fatal apart " +
             "sustain crack supply proud access"),

            ("066dca1a2bb7e8a1db2832148ce9933eea0f3ac9548d793112d9a95c9407efad",
             "all hour make first leader extend hole alien behind guard gospel lava path output census " +
             "museum junior mass reopen famous sing advance salt reform"),

            ("f30f8c1da665478f49b001d94c5fc452",
             "vessel ladder alter error federal sibling chat ability sun glass valve picture"),

            ("c10ec20dc3cd9f652c7fac2f1230f7a3c828389a14392f05",
             "scissors invite lock maple supreme raw rapid void congress muscle digital elegant little " +
             "brisk hair mango congress clump"),

            ("f585c11aec520db57dd353c69554b21a89b20fb0650966fa0a9d6f74fd989d8f",
             "void come effort suffer camp survey warrior heavy shoot primary clutch crush open amazing " +
             "screen patrol group space point ten exist slush involve unfold"),
        };

        /// <summary>
        /// Runs every vector in both directions: entropy to mnemonic, and mnemonic
        /// back to entropy through the validator. Returns false with a description
        /// on the first mismatch.
        /// </summary>
        public static bool RunSelfTest(IReadOnlyList<string> wordlist, out string error)
        {
            error = string.Empty;

            for (int i = 0; i < Vectors.Length; i++)
            {
                (string entropyHex, string expected) = Vectors[i];

                byte[] entropy = Convert.FromHexString(entropyHex);
                string produced = Bip39Utility.EntropyToMnemonic(entropy, wordlist);

                if (!string.Equals(produced, expected, StringComparison.Ordinal))
                {
                    error = $"BIP39 self-test failed on vector {i + 1}: generated phrase does not match the reference.";
                    return false;
                }

                // The reverse direction must recover the exact same entropy.
                if (!Bip39Utility.TryValidate(expected, wordlist, out string recoveredHex, out string validationError))
                {
                    error = $"BIP39 self-test failed on vector {i + 1}: reference phrase rejected ({validationError}).";
                    return false;
                }

                if (!string.Equals(recoveredHex, entropyHex, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"BIP39 self-test failed on vector {i + 1}: recovered entropy does not match.";
                    return false;
                }
            }

            return true;
        }
    }
}
