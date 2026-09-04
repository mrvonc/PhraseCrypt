using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhraseCryptApp
{
    /// <summary>
    /// Small runtime translation system. Default language is English.
    ///
    /// Usage: Localization.T("KeyName") or Localization.T("KeyName", arg1, arg2...)
    /// for strings containing placeholders ({0}, {1}, ...).
    ///
    /// To add a language: copy a dictionary below, translate the values, and add a
    /// ComboBoxItem to the language dropdown in MainWindow.xaml with Tag set to your
    /// language code. Missing keys fall back to English automatically.
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
                ["AppSubtitle"] = "Encrypted BIP39 container generator",
                ["LanguageLabel"] = "LANGUAGE",
                ["ThemeLabel"] = "THEME",
                ["ModeWrite"] = "WRITE",
                ["ModeRead"] = "READ",

                // WRITE
                ["LabelWordCount"] = "WORD COUNT",
                ["LabelProtection"] = "PROTECTION (REQUIRED)",
                ["OptionAes"] = "AES-256-GCM (reports a wrong password)",
                ["OptionHoney"] = "Honey Encryption (a wrong password yields a decoy phrase)",
                ["LabelPassword"] = "PASSWORD",
                ["LabelPasswordConfirm"] = "CONFIRM PASSWORD",
                ["WriteHint"] = "The phrase is never displayed here. Store the container, then use READ with your password to reveal it.",
                ["ButtonGenerate"] = "GENERATE CONTAINER",
                ["StatusContainerCreated"] = "Encrypted container created ({0} words). The phrase was not displayed.",

                // READ
                ["LabelInputType"] = "INPUT TYPE",
                ["OptionReadAes"] = "AES container",
                ["OptionReadHoney"] = "Honey container",
                ["OptionReadValidate"] = "Validate a mnemonic phrase",
                ["LabelContainerInput"] = "CONTAINER (BASE64)",
                ["LabelMnemonicInput"] = "MNEMONIC PHRASE (WORDS)",
                ["ButtonReveal"] = "REVEAL",
                ["StatusRevealed"] = "Phrase revealed. Clear the screen when you are done.",
                ["StatusHoneyRevealed"] = "Container decrypted. Whether the password was correct cannot be determined - that is how Honey Encryption works.",
                ["OutputValid"] = "Valid BIP39 phrase. The checksum matches.",
                ["StatusValid"] = "Checksum valid.",
                ["StatusInvalid"] = "Validation failed.",

                // Output area
                ["LabelOutput"] = "OUTPUT",
                ["ButtonCopy"] = "Copy",
                ["ButtonClear"] = "Clear",
                ["SensitiveBanner"] = "Secret on screen. Copying and image embedding are disabled. Press Clear when done.",
                ["ButtonHideImage"] = "Hide container in image...",
                ["ButtonExtractImage"] = "Extract from image...",
                ["StatusReady"] = "Ready.",
                ["StatusCleared"] = "Screen and input fields cleared.",
                ["StatusCopied"] = "Container copied to clipboard.",
                ["StatusCopyFailed"] = "Copy failed.",
                ["StatusNothingToCopy"] = "Nothing to copy.",
                ["StatusNeedContainerFirst"] = "Generate a container first.",
                ["StatusHiddenInImage"] = "Container hidden in '{0}'.",
                ["StatusExtractedFromImage"] = "Container extracted. Enter your password and press Reveal.",

                // Errors
                ["ErrorCopyBlocked"] = "Copying a clear-text phrase is blocked. Write it down by hand.",
                ["ErrorEmbedBlocked"] = "Only encrypted containers can be embedded in an image, never a clear-text phrase.",
                ["ErrorNoInput"] = "Please enter something first.",
                ["ErrorPasswordRequired"] = "A password is required.",
                ["ErrorPasswordTooShort"] = "Password must be at least {0} characters.",
                ["ErrorPasswordMismatch"] = "The two passwords do not match.",
                ["ErrorPasswordEmpty"] = "Password must not be empty.",
                ["ErrorInvalidBase64"] = "Input is not valid Base64 text.",
                ["ErrorTooShortForEncryptedData"] = "Input is too short to contain encrypted data.",
                ["ErrorDecryptionFailed"] = "Decryption failed - wrong password or corrupted data.",
                ["ErrorWordlistNotFound"] = "'{0}' was not found in the project or program folder.",
                ["ErrorWordlistCount"] = "Expected 2048 words in '{0}', found {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' contains {1} duplicate entries (e.g.: {2}). Use an unmodified, official BIP39 wordlist.",
                ["ErrorWordlistHashMismatch"] = "'{0}' does not match the official BIP39 English wordlist. Replace it with the unmodified file from the Bitcoin BIPs repository.",
                ["ErrorHoneyInvalidEntropy"] = "Invalid entropy length for Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Not a valid Honey container (wrong format or damaged).",
                ["ErrorNoSecretText"] = "There is no text to hide.",
                ["ErrorImageTooSmall"] = "Carrier image is too small for this amount of data (~{0:F1} KB needed). Choose a larger image.",
                ["ErrorImageTooSmallForData"] = "Image is too small to contain hidden data.",
                ["ErrorNoValidPackageFound"] = "No valid container found in this image (or the image was recompressed).",
            },

            [German] = new Dictionary<string, string>
            {
                ["AppSubtitle"] = "Generator für verschlüsselte BIP39-Container",
                ["LanguageLabel"] = "SPRACHE",
                ["ThemeLabel"] = "DESIGN",
                ["ModeWrite"] = "SCHREIBEN",
                ["ModeRead"] = "LESEN",

                ["LabelWordCount"] = "ANZAHL WÖRTER",
                ["LabelProtection"] = "SCHUTZ (ERFORDERLICH)",
                ["OptionAes"] = "AES-256-GCM (meldet ein falsches Passwort)",
                ["OptionHoney"] = "Honey Encryption (falsches Passwort liefert Täuschungs-Phrase)",
                ["LabelPassword"] = "PASSWORT",
                ["LabelPasswordConfirm"] = "PASSWORT BESTÄTIGEN",
                ["WriteHint"] = "Die Phrase wird hier nie angezeigt. Container speichern und danach unter LESEN mit dem Passwort sichtbar machen.",
                ["ButtonGenerate"] = "CONTAINER ERZEUGEN",
                ["StatusContainerCreated"] = "Verschlüsselter Container erstellt ({0} Wörter). Die Phrase wurde nicht angezeigt.",

                ["LabelInputType"] = "EINGABETYP",
                ["OptionReadAes"] = "AES-Container",
                ["OptionReadHoney"] = "Honey-Container",
                ["OptionReadValidate"] = "Mnemonic-Phrase prüfen",
                ["LabelContainerInput"] = "CONTAINER (BASE64)",
                ["LabelMnemonicInput"] = "MNEMONIC-PHRASE (WÖRTER)",
                ["ButtonReveal"] = "ANZEIGEN",
                ["StatusRevealed"] = "Phrase sichtbar. Bildschirm nach Gebrauch leeren.",
                ["StatusHoneyRevealed"] = "Container entschlüsselt. Ob das Passwort korrekt war, lässt sich nicht feststellen - so funktioniert Honey Encryption.",
                ["OutputValid"] = "Gültige BIP39-Phrase. Die Checksumme stimmt.",
                ["StatusValid"] = "Checksumme gültig.",
                ["StatusInvalid"] = "Prüfung fehlgeschlagen.",

                ["LabelOutput"] = "AUSGABE",
                ["ButtonCopy"] = "Kopieren",
                ["ButtonClear"] = "Leeren",
                ["SensitiveBanner"] = "Geheimnis auf dem Bildschirm. Kopieren und Bild-Einbettung sind gesperrt. Danach auf Leeren drücken.",
                ["ButtonHideImage"] = "Container in Bild verstecken...",
                ["ButtonExtractImage"] = "Aus Bild extrahieren...",
                ["StatusReady"] = "Bereit.",
                ["StatusCleared"] = "Bildschirm und Eingabefelder geleert.",
                ["StatusCopied"] = "Container in die Zwischenablage kopiert.",
                ["StatusCopyFailed"] = "Kopieren fehlgeschlagen.",
                ["StatusNothingToCopy"] = "Nichts zum Kopieren vorhanden.",
                ["StatusNeedContainerFirst"] = "Zuerst einen Container erzeugen.",
                ["StatusHiddenInImage"] = "Container in '{0}' versteckt.",
                ["StatusExtractedFromImage"] = "Container extrahiert. Passwort eingeben und auf Anzeigen drücken.",

                ["ErrorCopyBlocked"] = "Das Kopieren einer Klartext-Phrase ist gesperrt. Bitte von Hand abschreiben.",
                ["ErrorEmbedBlocked"] = "Nur verschlüsselte Container lassen sich in ein Bild einbetten, niemals eine Klartext-Phrase.",
                ["ErrorNoInput"] = "Bitte zuerst etwas eingeben.",
                ["ErrorPasswordRequired"] = "Ein Passwort ist erforderlich.",
                ["ErrorPasswordTooShort"] = "Das Passwort muss mindestens {0} Zeichen lang sein.",
                ["ErrorPasswordMismatch"] = "Die beiden Passwörter stimmen nicht überein.",
                ["ErrorPasswordEmpty"] = "Passwort darf nicht leer sein.",
                ["ErrorInvalidBase64"] = "Eingabe ist kein gültiger Base64-Text.",
                ["ErrorTooShortForEncryptedData"] = "Eingabe ist zu kurz, um verschlüsselte Daten zu enthalten.",
                ["ErrorDecryptionFailed"] = "Entschlüsselung fehlgeschlagen - falsches Passwort oder beschädigte Daten.",
                ["ErrorWordlistNotFound"] = "'{0}' wurde nicht im Projekt- oder Programmordner gefunden.",
                ["ErrorWordlistCount"] = "Erwartet: 2048 Wörter in '{0}', gefunden: {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' enthält {1} doppelte Einträge (z.B.: {2}). Bitte eine unveränderte, offizielle BIP39-Wortliste verwenden.",
                ["ErrorWordlistHashMismatch"] = "'{0}' entspricht nicht der offiziellen englischen BIP39-Wortliste. Bitte durch die unveränderte Datei aus dem Bitcoin-BIPs-Repository ersetzen.",
                ["ErrorHoneyInvalidEntropy"] = "Ungültige Entropielänge für Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Kein gültiger Honey-Container (falsches Format oder beschädigt).",
                ["ErrorNoSecretText"] = "Es gibt keinen Text zum Verstecken.",
                ["ErrorImageTooSmall"] = "Trägerbild ist zu klein für diese Datenmenge (ca. {0:F1} KB nötig). Bitte ein grösseres Bild wählen.",
                ["ErrorImageTooSmallForData"] = "Bild ist zu klein, um versteckte Daten zu enthalten.",
                ["ErrorNoValidPackageFound"] = "Kein gültiger Container in diesem Bild gefunden (oder das Bild wurde neu komprimiert).",
            },

            [Russian] = new Dictionary<string, string>
            {
                ["AppSubtitle"] = "Генератор зашифрованных BIP39-контейнеров",
                ["LanguageLabel"] = "ЯЗЫК",
                ["ThemeLabel"] = "ТЕМА",
                ["ModeWrite"] = "ЗАПИСЬ",
                ["ModeRead"] = "ЧТЕНИЕ",

                ["LabelWordCount"] = "КОЛИЧЕСТВО СЛОВ",
                ["LabelProtection"] = "ЗАЩИТА (ОБЯЗАТЕЛЬНО)",
                ["OptionAes"] = "AES-256-GCM (сообщает о неверном пароле)",
                ["OptionHoney"] = "Honey Encryption (неверный пароль выдаёт ложную фразу)",
                ["LabelPassword"] = "ПАРОЛЬ",
                ["LabelPasswordConfirm"] = "ПОДТВЕРДИТЕ ПАРОЛЬ",
                ["WriteHint"] = "Фраза здесь никогда не отображается. Сохраните контейнер и откройте его в разделе ЧТЕНИЕ с помощью пароля.",
                ["ButtonGenerate"] = "СОЗДАТЬ КОНТЕЙНЕР",
                ["StatusContainerCreated"] = "Зашифрованный контейнер создан ({0} слов). Фраза не отображалась.",

                ["LabelInputType"] = "ТИП ВВОДА",
                ["OptionReadAes"] = "AES-контейнер",
                ["OptionReadHoney"] = "Honey-контейнер",
                ["OptionReadValidate"] = "Проверить мнемоническую фразу",
                ["LabelContainerInput"] = "КОНТЕЙНЕР (BASE64)",
                ["LabelMnemonicInput"] = "МНЕМОНИЧЕСКАЯ ФРАЗА (СЛОВА)",
                ["ButtonReveal"] = "ПОКАЗАТЬ",
                ["StatusRevealed"] = "Фраза показана. После использования очистите экран.",
                ["StatusHoneyRevealed"] = "Контейнер расшифрован. Определить, был ли пароль верным, невозможно - так работает Honey Encryption.",
                ["OutputValid"] = "Действительная фраза BIP39. Контрольная сумма совпадает.",
                ["StatusValid"] = "Контрольная сумма верна.",
                ["StatusInvalid"] = "Проверка не пройдена.",

                ["LabelOutput"] = "ВЫВОД",
                ["ButtonCopy"] = "Копировать",
                ["ButtonClear"] = "Очистить",
                ["SensitiveBanner"] = "Секрет на экране. Копирование и встраивание в изображение отключены. Нажмите «Очистить» после использования.",
                ["ButtonHideImage"] = "Скрыть контейнер в изображении...",
                ["ButtonExtractImage"] = "Извлечь из изображения...",
                ["StatusReady"] = "Готово.",
                ["StatusCleared"] = "Экран и поля ввода очищены.",
                ["StatusCopied"] = "Контейнер скопирован в буфер обмена.",
                ["StatusCopyFailed"] = "Не удалось скопировать.",
                ["StatusNothingToCopy"] = "Нечего копировать.",
                ["StatusNeedContainerFirst"] = "Сначала создайте контейнер.",
                ["StatusHiddenInImage"] = "Контейнер скрыт в '{0}'.",
                ["StatusExtractedFromImage"] = "Контейнер извлечён. Введите пароль и нажмите «Показать».",

                ["ErrorCopyBlocked"] = "Копирование фразы в открытом виде заблокировано. Перепишите её вручную.",
                ["ErrorEmbedBlocked"] = "В изображение можно встроить только зашифрованный контейнер, но не открытую фразу.",
                ["ErrorNoInput"] = "Сначала введите данные.",
                ["ErrorPasswordRequired"] = "Требуется пароль.",
                ["ErrorPasswordTooShort"] = "Пароль должен содержать не менее {0} символов.",
                ["ErrorPasswordMismatch"] = "Пароли не совпадают.",
                ["ErrorPasswordEmpty"] = "Пароль не должен быть пустым.",
                ["ErrorInvalidBase64"] = "Ввод не является корректным текстом Base64.",
                ["ErrorTooShortForEncryptedData"] = "Ввод слишком короткий, чтобы содержать зашифрованные данные.",
                ["ErrorDecryptionFailed"] = "Ошибка расшифровки - неверный пароль или повреждённые данные.",
                ["ErrorWordlistNotFound"] = "Файл '{0}' не найден в папке проекта или программы.",
                ["ErrorWordlistCount"] = "Ожидалось 2048 слов в '{0}', найдено: {1}.",
                ["ErrorWordlistDuplicates"] = "'{0}' содержит {1} повторяющихся записей (например: {2}). Используйте неизменённый официальный список BIP39.",
                ["ErrorWordlistHashMismatch"] = "'{0}' не соответствует официальному английскому списку слов BIP39. Замените его неизменённым файлом из репозитория Bitcoin BIPs.",
                ["ErrorHoneyInvalidEntropy"] = "Недопустимая длина энтропии для Honey Encryption.",
                ["ErrorHoneyBadContainer"] = "Некорректный Honey-контейнер (неверный формат или повреждение).",
                ["ErrorNoSecretText"] = "Нет текста для скрытия.",
                ["ErrorImageTooSmall"] = "Изображение-носитель слишком мало для этого объёма данных (нужно ~{0:F1} КБ). Выберите изображение большего размера.",
                ["ErrorImageTooSmallForData"] = "Изображение слишком мало, чтобы содержать скрытые данные.",
                ["ErrorNoValidPackageFound"] = "В этом изображении не найден корректный контейнер (или изображение было пересжато).",
            },
        };

        public static string T(string key)
        {
            if (Strings.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out string? value))
            {
                return value;
            }

            // Fall back to English if a key is missing in the selected language.
            return Strings[English].TryGetValue(key, out string? fallback) ? fallback : key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T(key), args);
        }
    }
}
