# PhraseCrypt

A Windows desktop tool for working with BIP39 recovery phrases: generate them,
validate them, encrypt them, and hide them inside images.

Built as a learning project to understand how BIP39 and password-based
encryption actually work under the hood.

> **Do not use this for wallets holding real funds.** It is educational
> software. Use established, audited tools for anything that matters.
> Test networks and throwaway wallets are fine.

---

## What it does

| Feature | What happens |
|---|---|
| **Generate BIP39 phrase** | Creates a real, valid recovery phrase (12–24 words) with a correct SHA-256 checksum |
| **Validate a phrase** | Checks whether a phrase someone gave you is genuine BIP39, or has a typo |
| **Position ↔ hex converter** | Converts word positions to a compact hex string and back |
| **AES-256 encryption** | Standard password encryption (AES-GCM + PBKDF2) |
| **Honey encryption** | A wrong password returns a *different valid phrase* instead of an error |
| **Steganography** | Hides the output inside a PNG image |
| **Themes & languages** | Dark/light mode, English / German / Russian |

---

## Getting it running

**You need:** Windows, and [Visual Studio 2022](https://visualstudio.microsoft.com/)
with the *.NET desktop development* workload (free Community edition works).

1. Download or clone this repository.
2. Add the BIP39 wordlist: download
   [`english.txt`](https://github.com/bitcoin/bips/blob/master/bip-0039/english.txt)
   and place it next to `PhraseCryptApp.csproj`. This file is not included here;
   it is the official list of 2048 words and the app will not start without it.
3. Open `PhraseCryptApp.csproj` in Visual Studio.
4. Press **F5**.

That's it — no NuGet packages, no external dependencies.

---

## How it works

### BIP39 in one paragraph

A recovery phrase is not just random words. Behind it sits a random number
(the *entropy*) — 128 bits for 12 words, 256 bits for 24. The software hashes
that number with SHA-256 and appends the first few bits of the hash as a
**checksum**. Entropy plus checksum are then chopped into 11-bit chunks, and
each chunk indexes one word in a fixed list of 2048 words (2^11 = 2048).

This is why picking 12 random words from the list does *not* give you a working
recovery phrase — the checksum would almost certainly be wrong, and a wallet
would reject it. `Bip39Utility.cs` implements this properly.

### Honey encryption — the interesting part

Normal encryption tells you when the password was wrong. That is helpful for
you, and equally helpful for an attacker running billions of guesses: every
failure narrows the search.

Honey encryption removes that signal. Decrypt a container with the wrong
password and you get a phrase that is completely valid — correct checksum,
real words, indistinguishable from the real one. An attacker who tries a
million passwords gets a million plausible recovery phrases and no way to tell
which is real.

This works here because BIP39 is an unusually good fit. The scheme needs a
message space where *every* possible value looks legitimate. With BIP39 that
comes for free: every 16-byte value is valid entropy and maps to exactly one
correct 12-word phrase. So the implementation is short — derive a keystream
from the password with PBKDF2, XOR it with the entropy, done.

What is deliberately *missing* from `HoneyEncryption.cs` matters as much as
what is there: **no authentication tag, no HMAC, no plaintext checksum.** Any
of those would tell an attacker when a guess was correct and defeat the whole
scheme. Decryption therefore never reports "wrong password" — it cannot, and
must not.

**Its limit:** this defends against offline guessing. If the attacker can check
candidates externally — for example by looking up each generated phrase on a
blockchain to see if it holds funds — they will still find the real one. It
raises the cost of an attack; it does not replace a strong password.

The concept comes from
[Juels & Ristenpart (2014)](https://www.arijuels.com/wp-content/uploads/2013/09/JR14.pdf).

### Steganography

`SteganographyUtility.cs` writes your data into the least significant bit of
each red, green and blue value in a PNG. Changing the lowest bit shifts a
colour by 1/255 — invisible to the eye. Output is always PNG, because JPEG
recompresses the pixels and would destroy the hidden bits.

---

## Project layout

```
Bip39Utility.cs          BIP39 generation and validation (entropy + checksum)
HoneyEncryption.cs       Honey encryption — heavily commented, start here
CryptoUtility.cs         Standard AES-256-GCM encryption
SteganographyUtility.cs  Hiding data in PNG images
Localization.cs          All UI text, one dictionary per language
ThemeManager.cs          Dark and light colour definitions
Logger.cs                File logging (never logs secrets)
MainWindow.xaml          The interface: layout, styles, animations
MainWindow.xaml.cs       Wiring between the interface and the logic above
```

The crypto files have no dependency on the UI. You can lift any of them into
your own project.

---

## Extending it

**Add a language.** Open `Localization.cs`, copy an existing dictionary, and
translate the values. Then add a `<ComboBoxItem>` to the language dropdown in
`MainWindow.xaml` with `Tag` set to your language code. Missing keys fall back
to English automatically.

**Change the colours.** Everything lives in `ThemeManager.cs` as named brushes.
The UI references them via `DynamicResource`, so edits apply immediately without
touching the XAML.

**Add a feature.** The pattern is consistent: write a static utility class, add
its text keys to `Localization.cs`, add controls to `MainWindow.xaml`, wire the
handler in `MainWindow.xaml.cs`.

Some ideas worth building:
- **SLIP-39 / Shamir secret sharing** — split a phrase into 5 parts, any 3 restore it
- **Dice entropy** — let the user roll physical dice instead of trusting the system RNG
- **Duress password** — a second password that reveals a decoy phrase
- **Passphrase support** — the optional BIP39 25th word

---

## Security notes

- The wordlist is checked for exactly 2048 entries and rejects duplicates, so a
  tampered list cannot silently corrupt output.
- Random values come from the OS cryptographic RNG (`RandomNumberGenerator`),
  never `System.Random`.
- Key derivation uses PBKDF2-SHA256 with 200,000 iterations.
- Logs contain actions only ("12 words generated"), never phrases or passwords.

None of this has been audited. Treat it as a study of how these schemes work,
not as production security software.

---

## License

MIT — see [LICENSE](LICENSE).
