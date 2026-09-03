using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhraseCryptApp
{
    /// <summary>
    /// Lightweight runtime translation system. English is the default language.
    /// Usage: Localization.T("KeyName"), or Localization.T("KeyName", arg1, arg2, ...)
    /// for strings containing placeholders ({0}, {1}, ...).
    /// </summary>
    public static class Localization
    {
        public const string English = "en";
        public const string German = "de";
        public const string Russian = "ru";

        public static string CurrentLanguage { get; set; } = English;

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            [English] = new Dictionary<string, string>
            {
                ["AppTitle"] = "PhraseCryptApp",
                ["AppSubtitle"] = "BIP39 Word ⇄ Hex Converter",
                ["LanguageLabel"] = "LANGUAGE",
                ["ModeWrite"] = "WRITE",
                ["ModeRead"] = "READ",
                ["LabelWordCount"] = "WORD COUNT",
                ["CheckboxBip39"] = "Use real BIP39 checksum (valid recovery phrase)",
                ["CheckboxEncryptWrite"] = "Additionally encrypt output with AES-256",
                ["ButtonGenerate"] = "GENERATE",
                ["LabelHexValue"] = "HEX VALUE",
                ["LabelMnemonicPhrase"] = "MNEMONIC PHRASE (WORDS)",
                ["CheckboxIsMnemonic"] = "Input is a BIP39 mnemonic phrase (words instead of hex)",
                ["CheckboxIsEncryptedRead"] = "Input is AES-encrypted (Base64)",
                ["ButtonDecode"] = "DECODE",
                ["LabelOutput"] = "OUTPUT",
                ["ButtonCopy"] = "Copy",
                ["ButtonHideImage"] = "Hide in image...",
                ["ButtonExtractImage"] = "Extract from image...",
                ["StatusReady"] = "Ready.",

                ["StatusWordsProcessed"] = "{0} words processed successfully.",
                ["StatusWordsDecoded"] = "{0} words decoded.",
                ["StatusInvalidHex"] = "Invalid hexadecimal value.",
                ["StatusCopied"] = "Copied to clipboard.",
                ["StatusCopyFailed"] = "Copy failed.",
                ["StatusNothingToCopy"] = "Nothing to copy.",
                ["StatusHiddenInImage"] = "Data successfully hidden in '{0}'.",
                ["StatusExtractedFromImage"] = "Data extracted from image. Choose the matching format and click DECODE.",
                ["StatusNeedGenerateFirst"] = "Generate something first before hiding it in an image.",
                ["StatusBip39Valid"] = "Checksum valid - this is a correct BIP39 recovery phrase.",

                ["ErrorBip39WordCount"] = "Real BIP39 checksum requires {0} words (not {1}).",
                ["ErrorPasswordRequiredWrite"] = "Please enter an AES password.",
                ["ErrorPasswordRequiredRead"] = "Please enter the AES password.",
                ["ErrorInvalidWordCount"] = "Invalid input. Please enter exactly 12 or 24.",
                ["ErrorWordlistNotFound"] = "'{0}' was not found in the project or program folder.",
                ["ErrorWordlistCount"] = "Expected 2048 words in '{0}', found {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' contains {1} duplicate entries (e.g.: {2}). Please use an unmodified, official BIP39 wordlist.",

                ["OutputMnemonicHeader"] = "Mnemonic ({0} words, BIP39-valid):",
                ["OutputEntropyHeader"] = "Entropy (Hex):",
                ["OutputValidBip39"] = "✓ Valid BIP39 phrase.",

                ["ErrorPasswordEmpty"] = "Password must not be empty.",
                ["ErrorInvalidBase64"] = "Input is not valid Base64 text.",
                ["ErrorTooShortForEncryptedData"] = "Input is too short to contain encrypted data.",
                ["ErrorDecryptionFailed"] = "Decryption failed - wrong password or corrupted data.",

                ["ErrorNoSecretText"] = "There is no text to hide.",
                ["ErrorImageTooSmall"] = "Carrier image is too small for this amount of data (~{0:F1} KB needed). Please choose a larger image.",
                ["ErrorImageTooSmallForData"] = "Image is too small to contain hidden data.",
                ["ErrorNoValidPackageFound"] = "No valid PhraseCrypt data package found in this image (or image was recompressed).",

                ["CheckboxHoneyWrite"] = "Honey Encryption (wrong password yields a decoy phrase)",
                ["CheckboxHoneyRead"] = "Input is Honey-encrypted",
                ["LabelHoneyContainer"] = "HONEY CONTAINER (BASE64)",
                ["OutputHoneyRealPhrase"] = "REAL phrase ({0} words) - write this down offline:",
                ["OutputHoneyContainer"] = "Honey container (store/transmit only this):",
                ["OutputHoneyWarning"] = "Note: a wrong password produces a different but equally valid phrase - there is no error message, by design.",
                ["OutputHoneyDecrypted"] = "Decrypted phrase ({0} words):",
                ["OutputHoneyNoVerify"] = "This phrase is valid BIP39 in any case. Whether the password was correct cannot be determined here - that is exactly the point of Honey Encryption.",
                ["StatusHoneyEncrypted"] = "Honey container created ({0} words).",
                ["StatusHoneyDecrypted"] = "Container decrypted - result is unverifiable by design.",
                ["ErrorHoneyInvalidEntropy"] = "Invalid entropy length for Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Not a valid Honey container (wrong format or damaged).",
                ["ErrorHoneyNeedsBip39"] = "Honey Encryption requires a BIP39 word count ({0}).",
                ["ErrorHoneyAesConflict"] = "Honey Encryption and AES-256 cannot be combined - an AES authentication tag would reveal when the password is correct.",
            },

            [German] = new Dictionary<string, string>
            {
                ["AppTitle"] = "PhraseCryptApp",
                ["AppSubtitle"] = "BIP39 Wort ⇄ Hex Konverter",
                ["LanguageLabel"] = "SPRACHE",
                ["ModeWrite"] = "SCHREIBEN",
                ["ModeRead"] = "LESEN",
                ["LabelWordCount"] = "ANZAHL WÖRTER",
                ["CheckboxBip39"] = "Echte BIP39-Prüfsumme verwenden (gültige Recovery-Phrase)",
                ["CheckboxEncryptWrite"] = "Ausgabe zusätzlich mit AES-256 verschlüsseln",
                ["ButtonGenerate"] = "GENERIEREN",
                ["LabelHexValue"] = "HEX-WERT",
                ["LabelMnemonicPhrase"] = "MNEMONIC-PHRASE (WÖRTER)",
                ["CheckboxIsMnemonic"] = "Eingabe ist eine BIP39-Mnemonic-Phrase (Wörter statt Hex)",
                ["CheckboxIsEncryptedRead"] = "Eingabe ist AES-verschlüsselt (Base64)",
                ["ButtonDecode"] = "DEKODIEREN",
                ["LabelOutput"] = "AUSGABE",
                ["ButtonCopy"] = "Kopieren",
                ["ButtonHideImage"] = "In Bild verstecken...",
                ["ButtonExtractImage"] = "Aus Bild extrahieren...",
                ["StatusReady"] = "Bereit.",

                ["StatusWordsProcessed"] = "{0} Wörter erfolgreich verarbeitet.",
                ["StatusWordsDecoded"] = "{0} Wörter dekodiert.",
                ["StatusInvalidHex"] = "Ungültiger Hexadezimalwert.",
                ["StatusCopied"] = "In die Zwischenablage kopiert.",
                ["StatusCopyFailed"] = "Kopieren fehlgeschlagen.",
                ["StatusNothingToCopy"] = "Nichts zum Kopieren vorhanden.",
                ["StatusHiddenInImage"] = "Daten erfolgreich in '{0}' versteckt.",
                ["StatusExtractedFromImage"] = "Daten aus Bild extrahiert. Passendes Format wählen und auf DEKODIEREN klicken.",
                ["StatusNeedGenerateFirst"] = "Zuerst etwas generieren, bevor du es in einem Bild versteckst.",
                ["StatusBip39Valid"] = "Checksumme gültig - dies ist eine korrekte BIP39-Recovery-Phrase.",

                ["ErrorBip39WordCount"] = "Echte BIP39-Prüfsumme erfordert {0} Wörter (nicht {1}).",
                ["ErrorPasswordRequiredWrite"] = "Bitte ein AES-Passwort eingeben.",
                ["ErrorPasswordRequiredRead"] = "Bitte das AES-Passwort eingeben.",
                ["ErrorInvalidWordCount"] = "Ungültige Eingabe. Bitte genau 12 oder 24 eingeben.",
                ["ErrorWordlistNotFound"] = "'{0}' wurde nicht im Projekt- oder Programmordner gefunden.",
                ["ErrorWordlistCount"] = "Erwartet: 2048 Wörter in '{0}', gefunden: {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' enthält {1} doppelte Einträge (z.B.: {2}). Bitte eine unveränderte, offizielle BIP39-Wortliste verwenden.",

                ["OutputMnemonicHeader"] = "Mnemonic ({0} Wörter, BIP39-gültig):",
                ["OutputEntropyHeader"] = "Entropie (Hex):",
                ["OutputValidBip39"] = "✓ Gültige BIP39-Phrase.",

                ["ErrorPasswordEmpty"] = "Passwort darf nicht leer sein.",
                ["ErrorInvalidBase64"] = "Eingabe ist kein gültiger Base64-Text.",
                ["ErrorTooShortForEncryptedData"] = "Eingabe ist zu kurz, um verschlüsselte Daten zu enthalten.",
                ["ErrorDecryptionFailed"] = "Entschlüsselung fehlgeschlagen - falsches Passwort oder beschädigte Daten.",

                ["ErrorNoSecretText"] = "Es gibt keinen Text zum Verstecken.",
                ["ErrorImageTooSmall"] = "Trägerbild ist zu klein für diese Datenmenge (benötigt ca. {0:F1} KB Kapazität). Bitte ein größeres Bild wählen.",
                ["ErrorImageTooSmallForData"] = "Bild ist zu klein, um versteckte Daten zu enthalten.",
                ["ErrorNoValidPackageFound"] = "Kein gültiges PhraseCrypt-Datenpaket in diesem Bild gefunden (oder Bild wurde neu komprimiert).",

                ["CheckboxHoneyWrite"] = "Honey Encryption (falsches Passwort liefert Täuschungs-Phrase)",
                ["CheckboxHoneyRead"] = "Eingabe ist Honey-verschlüsselt",
                ["LabelHoneyContainer"] = "HONEY-CONTAINER (BASE64)",
                ["OutputHoneyRealPhrase"] = "ECHTE Phrase ({0} Wörter) - offline notieren:",
                ["OutputHoneyContainer"] = "Honey-Container (nur diesen speichern/übertragen):",
                ["OutputHoneyWarning"] = "Hinweis: Ein falsches Passwort erzeugt eine andere, genauso gültige Phrase - eine Fehlermeldung gibt es bewusst nicht.",
                ["OutputHoneyDecrypted"] = "Entschlüsselte Phrase ({0} Wörter):",
                ["OutputHoneyNoVerify"] = "Diese Phrase ist in jedem Fall gültiges BIP39. Ob das Passwort korrekt war, lässt sich hier nicht feststellen - genau das ist der Sinn von Honey Encryption.",
                ["StatusHoneyEncrypted"] = "Honey-Container erstellt ({0} Wörter).",
                ["StatusHoneyDecrypted"] = "Container entschlüsselt - Ergebnis ist prinzipbedingt nicht überprüfbar.",
                ["ErrorHoneyInvalidEntropy"] = "Ungültige Entropielänge für Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Kein gültiger Honey-Container (falsches Format oder beschädigt).",
                ["ErrorHoneyNeedsBip39"] = "Honey Encryption erfordert eine BIP39-Wortanzahl ({0}).",
                ["ErrorHoneyAesConflict"] = "Honey Encryption und AES-256 lassen sich nicht kombinieren - ein AES-Authentifizierungs-Tag würde verraten, wann das Passwort stimmt.",
            },

            [Russian] = new Dictionary<string, string>
            {
                ["AppTitle"] = "PhraseCryptApp",
                ["AppSubtitle"] = "BIP39 конвертер слов в Hex",
                ["LanguageLabel"] = "ЯЗЫК",
                ["ModeWrite"] = "ЗАПИСЬ",
                ["ModeRead"] = "ЧТЕНИЕ",
                ["LabelWordCount"] = "КОЛИЧЕСТВО СЛОВ",
                ["CheckboxBip39"] = "Использовать настоящую контрольную сумму BIP39 (действительная фраза восстановления)",
                ["CheckboxEncryptWrite"] = "Дополнительно зашифровать вывод с помощью AES-256",
                ["ButtonGenerate"] = "СГЕНЕРИРОВАТЬ",
                ["LabelHexValue"] = "HEX-ЗНАЧЕНИЕ",
                ["LabelMnemonicPhrase"] = "МНЕМОНИЧЕСКАЯ ФРАЗА (СЛОВА)",
                ["CheckboxIsMnemonic"] = "Ввод является мнемонической фразой BIP39 (слова вместо hex)",
                ["CheckboxIsEncryptedRead"] = "Ввод зашифрован AES (Base64)",
                ["ButtonDecode"] = "РАСШИФРОВАТЬ",
                ["LabelOutput"] = "ВЫВОД",
                ["ButtonCopy"] = "Копировать",
                ["ButtonHideImage"] = "Скрыть в изображении...",
                ["ButtonExtractImage"] = "Извлечь из изображения...",
                ["StatusReady"] = "Готово.",

                ["StatusWordsProcessed"] = "{0} слов успешно обработано.",
                ["StatusWordsDecoded"] = "{0} слов расшифровано.",
                ["StatusInvalidHex"] = "Недопустимое шестнадцатеричное значение.",
                ["StatusCopied"] = "Скопировано в буфер обмена.",
                ["StatusCopyFailed"] = "Не удалось скопировать.",
                ["StatusNothingToCopy"] = "Нечего копировать.",
                ["StatusHiddenInImage"] = "Данные успешно скрыты в '{0}'.",
                ["StatusExtractedFromImage"] = "Данные извлечены из изображения. Выберите нужный формат и нажмите РАСШИФРОВАТЬ.",
                ["StatusNeedGenerateFirst"] = "Сначала сгенерируйте что-нибудь, прежде чем скрывать это в изображении.",
                ["StatusBip39Valid"] = "Контрольная сумма верна - это правильная фраза восстановления BIP39.",

                ["ErrorBip39WordCount"] = "Настоящая контрольная сумма BIP39 требует {0} слов (не {1}).",
                ["ErrorPasswordRequiredWrite"] = "Введите пароль AES.",
                ["ErrorPasswordRequiredRead"] = "Введите пароль AES.",
                ["ErrorInvalidWordCount"] = "Неверный ввод. Введите ровно 12 или 24.",
                ["ErrorWordlistNotFound"] = "Файл '{0}' не найден в папке проекта или программы.",
                ["ErrorWordlistCount"] = "Ожидалось 2048 слов в '{0}', найдено: {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' содержит {1} повторяющихся записей (например: {2}). Используйте неизменённый официальный список слов BIP39.",

                ["OutputMnemonicHeader"] = "Мнемоника ({0} слов, действительна по BIP39):",
                ["OutputEntropyHeader"] = "Энтропия (Hex):",
                ["OutputValidBip39"] = "✓ Действительная фраза BIP39.",

                ["ErrorPasswordEmpty"] = "Пароль не должен быть пустым.",
                ["ErrorInvalidBase64"] = "Ввод не является корректным текстом Base64.",
                ["ErrorTooShortForEncryptedData"] = "Ввод слишком короткий, чтобы содержать зашифрованные данные.",
                ["ErrorDecryptionFailed"] = "Ошибка расшифровки - неверный пароль или повреждённые данные.",

                ["ErrorNoSecretText"] = "Нет текста для скрытия.",
                ["ErrorImageTooSmall"] = "Изображение-носитель слишком мало для этого объёма данных (нужно ~{0:F1} КБ). Выберите изображение большего размера.",
                ["ErrorImageTooSmallForData"] = "Изображение слишком мало, чтобы содержать скрытые данные.",
                ["ErrorNoValidPackageFound"] = "В этом изображении не найден корректный пакет данных PhraseCrypt (или изображение было пересжато).",

                ["CheckboxHoneyWrite"] = "Honey Encryption (неверный пароль выдаёт ложную фразу)",
                ["CheckboxHoneyRead"] = "Ввод зашифрован методом Honey",
                ["LabelHoneyContainer"] = "HONEY-КОНТЕЙНЕР (BASE64)",
                ["OutputHoneyRealPhrase"] = "НАСТОЯЩАЯ фраза ({0} слов) - запишите её офлайн:",
                ["OutputHoneyContainer"] = "Honey-контейнер (хранить и передавать только его):",
                ["OutputHoneyWarning"] = "Примечание: неверный пароль выдаёт другую, но столь же действительную фразу - сообщения об ошибке намеренно нет.",
                ["OutputHoneyDecrypted"] = "Расшифрованная фраза ({0} слов):",
                ["OutputHoneyNoVerify"] = "Эта фраза в любом случае является корректной BIP39. Определить, был ли пароль верным, здесь невозможно - в этом и состоит смысл Honey Encryption.",
                ["StatusHoneyEncrypted"] = "Honey-контейнер создан ({0} слов).",
                ["StatusHoneyDecrypted"] = "Контейнер расшифрован - результат принципиально непроверяем.",
                ["ErrorHoneyInvalidEntropy"] = "Недопустимая длина энтропии для Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Некорректный Honey-контейнер (неверный формат или повреждение).",
                ["ErrorHoneyNeedsBip39"] = "Honey Encryption требует количества слов по стандарту BIP39 ({0}).",
                ["ErrorHoneyAesConflict"] = "Honey Encryption и AES-256 несовместимы - тег аутентификации AES выдал бы, когда пароль верен.",
            },
        };

        public static string T(string key)
        {
            if (Strings.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out string? value))
            {
                return value;
            }
            // Fall back to English if a key is missing in the selected language
            return Strings[English].TryGetValue(key, out string? fallback) ? fallback : key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T(key), args);
        }
    }
}
