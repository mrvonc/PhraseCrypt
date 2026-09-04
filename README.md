# PhraseCrypt

A Windows desktop tool that generates BIP39 recovery phrases and only ever hands
them back to you **inside an encrypted container**.

Built as a learning project to understand how BIP39, password-based encryption
and honey encryption actually work.

> **Do not use this for wallets holding real funds.** It is educational
> software and has not been audited. Use established, audited tools for anything
> that matters. Test networks and throwaway wallets are fine.

---

## The core idea

Most seed tools print your phrase on screen and leave the rest to you.
This one refuses to.

```
WRITE  →  generates a valid BIP39 phrase  →  outputs an encrypted container
                                              (the phrase is never shown)

READ   →  container + password             →  the phrase, on screen only
```

The phrase exists in clear text in exactly one place: the READ panel, on screen,
after you supplied the password. Nowhere else — not in the clipboard, not in a
log, not in a temp file.

---

## What it does

| Feature | What happens |
|---|---|
| **Generate** | Creates a valid 12 or 24 word BIP39 phrase with a correct SHA-256 checksum, then immediately encrypts it |
| **AES-256-GCM** | Standard authenticated encryption. Tells you when the password is wrong |
| **Honey encryption** | A wrong password returns a *different valid phrase* instead of an error |
| **Reveal** | Decrypts a container and shows the phrase — the only clear-text output |
| **Validate** | Checks whether a phrase someone gave you is genuine BIP39 or has a typo |
| **Steganography** | Hides a container inside a PNG (containers only, never a phrase) |
| **Themes & languages** | Dark/light mode, English / German / Russian |

---

## Getting it running

**You need:** Windows, and [Visual Studio 2022](https://visualstudio.microsoft.com/)
with the *.NET desktop development* workload (free Community edition works).

1. Download or clone this repository.
2. Add the BIP39 wordlist: download
   [`english.txt`](https://github.com/bitcoin/bips/blob/master/bip-0039/english.txt)
   and place it next to `PhraseCryptApp.csproj`. The app will not start without it.
3. Open `PhraseCryptApp.csproj` in Visual Studio.
4. Press **F5**.

No NuGet packages, no external dependencies.

---

## Security model

These rules are enforced in code, not just documented:

**1. Every generated phrase is valid BIP39.** There is no switch to turn the
checksum off and no hidden word count. The word count comes from a fixed,
non-editable dropdown, so no free-text input reaches the generator.

**2. Encryption is mandatory.** You cannot produce an unprotected phrase. The
generate button requires a password of at least 12 characters, entered twice.
The confirmation matters especially in honey mode, where a typo produces a
plausible but useless phrase and you would never be told.

**3. Clear text appears in exactly one place.** While a phrase is on screen, the
copy button and image embedding are blocked and a red banner is shown. The
phrase has to be written down by hand — which is where it should live anyway.

**4. Nothing is persisted.** There is no logging, no temp file, no autosave. The
only file the app ever writes is a PNG you explicitly ask for, and that path
refuses to run while a phrase is on screen.

**5. Secrets are dropped early.** Entropy arrays are zeroed immediately after
use, password fields are cleared after every operation, and everything is wiped
when the window closes or you press Clear.

**Honest limitation:** .NET strings are immutable and cannot be reliably zeroed
once created, so a phrase may linger in managed memory until garbage collection.
That is a constraint of the runtime, not something this code works around.

---

## How it works

### BIP39 in one paragraph

A recovery phrase is not just random words. Behind it sits a random number (the
*entropy*) — 128 bits for 12 words, 256 bits for 24. The software hashes that
number with SHA-256 and appends the first few bits of the hash as a **checksum**.
Entropy plus checksum are chopped into 11-bit chunks, and each chunk indexes one
word in a fixed list of 2048 words (2^11 = 2048).

This is why picking 12 random words from the list does *not* give you a working
recovery phrase — the checksum would almost certainly be wrong and a wallet would
reject it. `Bip39Utility.cs` implements this properly.

### Honey encryption — the interesting part

Normal encryption tells you when the password was wrong. Helpful for you, equally
helpful for an attacker running billions of guesses: every failure narrows the
search.

Honey encryption removes that signal. Decrypt a container with the wrong password
and you get a phrase that is completely valid — correct checksum, real words,
indistinguishable from the real one. An attacker who tries a million passwords
gets a million plausible recovery phrases and no way to tell which is real.

This works here because BIP39 is an unusually good fit. The scheme needs a
message space where *every* possible value looks legitimate. With BIP39 that
comes for free: every 16-byte value is valid entropy and maps to exactly one
correct 12-word phrase. So the implementation is short — derive a keystream from
the password with PBKDF2, XOR it with the entropy, done.

What is deliberately *missing* from `HoneyEncryption.cs` matters as much as what
is there: **no authentication tag, no HMAC, no plaintext checksum.** Any of those
would tell an attacker when a guess was correct and defeat the whole scheme.
Decryption therefore never reports "wrong password" — it cannot, and must not.

**Its limit:** this defends against offline guessing. If the attacker can check
candidates externally — for example by looking up each generated phrase on a
blockchain to see whether it holds funds — they will still find the real one. It
raises the cost of an attack; it does not replace a strong password.

The concept comes from
[Juels & Ristenpart (2014)](https://www.arijuels.com/wp-content/uploads/2013/09/JR14.pdf).

### Steganography

`SteganographyUtility.cs` writes data into the least significant bit of each red,
green and blue value in a PNG. Changing the lowest bit shifts a colour by 1/255 —
invisible to the eye. Output is always PNG, because JPEG recompresses the pixels
and would destroy the hidden bits.

---

## Project layout

```
Bip39Utility.cs          BIP39 generation and validation (entropy + checksum)
HoneyEncryption.cs       Honey encryption — heavily commented, start here
CryptoUtility.cs         AES-256-GCM with PBKDF2 key derivation
SteganographyUtility.cs  Hiding containers in PNG images
Localization.cs          All UI text, one dictionary per language
ThemeManager.cs          Dark and light colour definitions
MainWindow.xaml          The interface: layout, styles, animations
MainWindow.xaml.cs       Wiring, plus the security rules listed above
```

The four utility classes have no dependency on the UI. You can lift any of them
into your own project.

---

## Extending it

**Add a language.** Open `Localization.cs`, copy an existing dictionary, and
translate the values. Then add a `<ComboBoxItem>` to the language dropdown in
`MainWindow.xaml` with `Tag` set to your language code. Missing keys fall back to
English automatically.

**Change the colours.** Everything lives in `ThemeManager.cs` as named brushes.
The UI references them via `DynamicResource`, so edits apply immediately.

**Add a feature.** The pattern is consistent: write a static utility class, add
its text keys to `Localization.cs`, add controls to `MainWindow.xaml`, wire the
handler in `MainWindow.xaml.cs`. If your feature touches a phrase in clear text,
gate it behind the `_outputIsSensitive` flag the way copy and embedding are.

Ideas worth building:
- **SLIP-39 / Shamir secret sharing** — split a phrase into 5 parts, any 3 restore it
- **Dice entropy** — let the user roll physical dice instead of trusting the system RNG
- **Duress password** — a second password that reveals a decoy phrase
- **BIP39 passphrase** — the optional "25th word", the standardised way to add a
  password on top of a phrase

---

## License

MIT — see [LICENSE](LICENSE).
