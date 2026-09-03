using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace PhraseCryptApp
{
    public partial class MainWindow : Window
    {
        private const string WordlistFileName = "english.txt";
        private List<string>? _wordlist;
        private bool _isInitialized;

        public MainWindow()
        {
            InitializeComponent();

            // Apply the theme right after loading (dark is the default).
            // This resolves all DynamicResource references declared in the XAML.
            ApplyTheme(isLight: false);

            _isInitialized = true;

            ApplyLanguage(Localization.English); // the default selection is declared in the XAML

            Logger.Info("Application started.");
        }

        // ---------- Theme switching (dark/light) ----------
        private void ThemeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            bool isLight = ThemeToggle.IsChecked == true;
            ApplyTheme(isLight);
        }

        private void ApplyTheme(bool isLight)
        {
            ResourceDictionary newDict = isLight
                ? ThemeManager.CreateLightTheme()
                : ThemeManager.CreateDarkTheme();

            // Swap only the theme dictionary. The styles and templates declared in
            // Window.Resources stay untouched and automatically re-resolve their
            // DynamicResource references against the new dictionary.
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(newDict);

            Logger.Info($"Theme switched to: {(isLight ? "Light" : "Dark")}");
        }

        // ---------- Language switching ----------
        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || LanguageCombo.SelectedItem is not ComboBoxItem item)
            {
                return;
            }

            string langCode = item.Tag?.ToString() ?? Localization.English;
            ApplyLanguage(langCode);
        }

        private void ApplyLanguage(string langCode)
        {
            Localization.CurrentLanguage = langCode;

            AppSubtitleText.Text = Localization.T("AppSubtitle");
            LanguageLabelText.Text = Localization.T("LanguageLabel");

            ModeWriteRadio.Content = Localization.T("ModeWrite");
            ModeReadRadio.Content = Localization.T("ModeRead");

            WordCountLabel.Text = Localization.T("LabelWordCount");
            UseBip39Checkbox.Content = Localization.T("CheckboxBip39");
            WriteHoneyCheckbox.Content = Localization.T("CheckboxHoneyWrite");
            WriteEncryptCheckbox.Content = Localization.T("CheckboxEncryptWrite");
            GenerateButton.Content = Localization.T("ButtonGenerate");

            if (ReadIsHoneyCheckbox.IsChecked == true)
            {
                ReadInputLabel.Text = Localization.T("LabelHoneyContainer");
            }
            else
            {
                ReadInputLabel.Text = ReadIsMnemonicCheckbox.IsChecked == true
                    ? Localization.T("LabelMnemonicPhrase")
                    : Localization.T("LabelHexValue");
            }
            ReadIsMnemonicCheckbox.Content = Localization.T("CheckboxIsMnemonic");
            ReadIsHoneyCheckbox.Content = Localization.T("CheckboxHoneyRead");
            ReadIsEncryptedCheckbox.Content = Localization.T("CheckboxIsEncryptedRead");
            DecodeButton.Content = Localization.T("ButtonDecode");

            OutputLabel.Text = Localization.T("LabelOutput");
            CopyButton.Content = Localization.T("ButtonCopy");
            HideInImageButton.Content = Localization.T("ButtonHideImage");
            ExtractFromImageButton.Content = Localization.T("ButtonExtractImage");

            StatusText.Text = Localization.T("StatusReady");
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0x8C, 0x8C));

            Logger.Info($"Language switched to: {langCode}");
        }

        // ---------- UI: mode switching (animated crossfade) ----------
        private void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (WritePanel == null || ReadPanel == null || !_isInitialized)
            {
                return; // also fires during InitializeComponent()
            }

            bool showWrite = ModeWriteRadio.IsChecked == true;
            FrameworkElement panelIn = showWrite ? WritePanel : ReadPanel;
            FrameworkElement panelOut = showWrite ? ReadPanel : WritePanel;

            AnimatePanelSwitch(panelOut, panelIn);

            OutputBox.Text = string.Empty;
            SetStatus(Localization.T("StatusReady"), false);
        }

        private static void AnimatePanelSwitch(FrameworkElement panelOut, FrameworkElement panelIn)
        {
            var easeOut = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

            // Fade out the currently visible panel, then fade in the target panel
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = easeOut
            };
            fadeOut.Completed += (_, _) =>
            {
                panelOut.Visibility = Visibility.Collapsed;

                panelIn.Visibility = Visibility.Visible;
                panelIn.Opacity = 0;

                var translateIn = panelIn.RenderTransform as System.Windows.Media.TranslateTransform;
                if (translateIn != null)
                {
                    translateIn.Y = 10;
                }

                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = easeOut
                };
                panelIn.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                if (translateIn != null)
                {
                    var slideIn = new System.Windows.Media.Animation.DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = easeOut
                    };
                    translateIn.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideIn);
                }
            };

            panelOut.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void WriteEncryptCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (WritePasswordBox == null) return;
            WritePasswordBox.Visibility = WriteEncryptCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // AES and Honey are mutually exclusive: an authentication tag would reveal
            // when the password is correct and therefore defeat the honey property.
            if (WriteEncryptCheckbox.IsChecked == true && WriteHoneyCheckbox?.IsChecked == true)
            {
                WriteHoneyCheckbox.IsChecked = false;
                SetStatus(Localization.T("ErrorHoneyAesConflict"), true);
            }
        }

        private void WriteHoneyCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (WriteHoneyPasswordBox == null) return;

            bool on = WriteHoneyCheckbox.IsChecked == true;
            WriteHoneyPasswordBox.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

            if (on)
            {
                // Honey operates on real BIP39 entropy, so BIP39 mode is mandatory.
                UseBip39Checkbox.IsChecked = true;
                UseBip39Checkbox.IsEnabled = false;

                if (WriteEncryptCheckbox.IsChecked == true)
                {
                    WriteEncryptCheckbox.IsChecked = false;
                    SetStatus(Localization.T("ErrorHoneyAesConflict"), true);
                }
            }
            else
            {
                UseBip39Checkbox.IsEnabled = true;
            }
        }

        private void ReadIsHoneyCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (ReadHoneyPasswordBox == null) return;

            bool on = ReadIsHoneyCheckbox.IsChecked == true;
            ReadHoneyPasswordBox.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

            if (on)
            {
                // A honey container is neither hex nor AES, so disable the other modes.
                ReadIsMnemonicCheckbox.IsChecked = false;
                ReadIsEncryptedCheckbox.IsChecked = false;
                ReadInputLabel.Text = Localization.T("LabelHoneyContainer");
            }
            else
            {
                ReadInputLabel.Text = Localization.T("LabelHexValue");
            }
        }

        private void ReadIsEncryptedCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (ReadPasswordBox == null) return;
            ReadPasswordBox.Visibility = ReadIsEncryptedCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (ReadIsEncryptedCheckbox.IsChecked == true && ReadIsHoneyCheckbox?.IsChecked == true)
            {
                ReadIsHoneyCheckbox.IsChecked = false;
            }
        }

        private void ReadIsMnemonicCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (ReadInputLabel == null) return;

            if (ReadIsMnemonicCheckbox.IsChecked == true && ReadIsHoneyCheckbox?.IsChecked == true)
            {
                ReadIsHoneyCheckbox.IsChecked = false; // triggers its own handler
            }

            if (ReadIsHoneyCheckbox?.IsChecked == true) return;

            ReadInputLabel.Text = ReadIsMnemonicCheckbox.IsChecked == true
                ? Localization.T("LabelMnemonicPhrase")
                : Localization.T("LabelHexValue");
        }

        // ---------- WRITE ----------
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> wordlist = EnsureWordlistLoaded();

                string rawInput = (WordCountCombo.Text ?? string.Empty).Trim();
                int wordCount = ParseWordCount(rawInput);

                // ---- Honey encryption path (builds its own output) ----
                if (WriteHoneyCheckbox.IsChecked == true)
                {
                    if (!Bip39Utility.IsValidWordCount(wordCount))
                    {
                        throw new ArgumentException(
                            Localization.T("ErrorHoneyNeedsBip39", Bip39Utility.SupportedWordCountsDescription));
                    }

                    string honeyPassword = WriteHoneyPasswordBox.Password;
                    if (string.IsNullOrEmpty(honeyPassword))
                    {
                        throw new ArgumentException(Localization.T("ErrorPasswordRequiredWrite"));
                    }

                    (string honeyMnemonic, string honeyEntropyHex) = Bip39Utility.Generate(wordlist, wordCount);
                    byte[] honeyEntropy = Convert.FromHexString(honeyEntropyHex);
                    string container = HoneyEncryption.EncryptToBase64(honeyEntropy, honeyPassword);
                    Array.Clear(honeyEntropy, 0, honeyEntropy.Length);

                    OutputBox.Text =
                        $"{Localization.T("OutputHoneyRealPhrase", wordCount)}\n{honeyMnemonic}\n\n" +
                        $"{Localization.T("OutputHoneyContainer")}\n{container}\n\n" +
                        $"{Localization.T("OutputHoneyWarning")}";

                    SetStatus(Localization.T("StatusHoneyEncrypted", wordCount), false);
                    FlashOutputPanel();
                    Logger.Info($"Honey container created ({wordCount} words).");
                    return;
                }

                string outputText;

                if (UseBip39Checkbox.IsChecked == true)
                {
                    if (!Bip39Utility.IsValidWordCount(wordCount))
                    {
                        throw new ArgumentException(
                            Localization.T("ErrorBip39WordCount", Bip39Utility.SupportedWordCountsDescription, wordCount));
                    }

                    (string mnemonic, string entropyHex) = Bip39Utility.Generate(wordlist, wordCount);
                    outputText = $"{Localization.T("OutputMnemonicHeader", wordCount)}\n{mnemonic}\n\n{Localization.T("OutputEntropyHeader")}\n{entropyHex}";
                    Logger.Info($"BIP39 mnemonic generated ({wordCount} words, checksum valid).");
                }
                else
                {
                    List<int> selectedIndices = DrawRandomIndices(wordlist.Count, wordCount);
                    List<int> positions = selectedIndices.Select(i => i + 1).ToList();
                    string hexRepresentation = PositionsToHex(positions);
                    outputText = FormatWithLineBreaks(hexRepresentation, 100);
                    Logger.Info($"Position hex generated ({wordCount} words, legacy mode).");
                }

                if (WriteEncryptCheckbox.IsChecked == true)
                {
                    string password = WritePasswordBox.Password;
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new ArgumentException(Localization.T("ErrorPasswordRequiredWrite"));
                    }
                    outputText = CryptoUtility.EncryptToBase64(outputText, password);
                    Logger.Info("Output encrypted with AES-256-GCM.");
                }

                OutputBox.Text = outputText;
                SetStatus(Localization.T("StatusWordsProcessed", wordCount), false);
                FlashOutputPanel();
            }
            catch (Exception ex)
            {
                Logger.Error($"Generation failed: {ex.GetType().Name}");
                SetStatus(ex.Message, true);
            }
        }

        private static int ParseWordCount(string answer)
        {
            if (answer == "12" || answer == "24")
            {
                return int.Parse(answer);
            }
            if (answer.Equals("security", StringComparison.OrdinalIgnoreCase))
            {
                return 512;
            }
            if (int.TryParse(answer, out int n) && Bip39Utility.IsValidWordCount(n))
            {
                return n; // also allows 15/18/21 when typed manually, for BIP39 mode
            }
            throw new ArgumentException(Localization.T("ErrorInvalidWordCount"));
        }

        // ---------- READ ----------
        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> wordlist = EnsureWordlistLoaded();
                string raw = (HexInputBox.Text ?? string.Empty).Trim();

                // ---- Honey decryption: ALWAYS returns a result ----
                if (ReadIsHoneyCheckbox.IsChecked == true)
                {
                    string honeyPassword = ReadHoneyPasswordBox.Password;
                    if (string.IsNullOrEmpty(honeyPassword))
                    {
                        throw new ArgumentException(Localization.T("ErrorPasswordRequiredRead"));
                    }

                    byte[] entropy = HoneyEncryption.DecryptFromBase64(raw, honeyPassword);
                    string mnemonic = Bip39Utility.EntropyToMnemonic(entropy, wordlist);
                    int count = mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    Array.Clear(entropy, 0, entropy.Length);

                    OutputBox.Text =
                        $"{Localization.T("OutputHoneyDecrypted", count)}\n{mnemonic}\n\n" +
                        $"{Localization.T("OutputHoneyNoVerify")}";

                    // Deliberately neutral status: no hint of success or failure, because
                    // such a statement would destroy the honey property.
                    SetStatus(Localization.T("StatusHoneyDecrypted"), false);
                    FlashOutputPanel();
                    Logger.Info("Honey container decrypted.");
                    return;
                }

                if (ReadIsEncryptedCheckbox.IsChecked == true)
                {
                    string password = ReadPasswordBox.Password;
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new ArgumentException(Localization.T("ErrorPasswordRequiredRead"));
                    }
                    raw = CryptoUtility.DecryptFromBase64(raw, password);
                    Logger.Info("Input decrypted successfully.");
                }

                if (ReadIsMnemonicCheckbox.IsChecked == true)
                {
                    bool isValid = Bip39Utility.TryValidate(raw, wordlist, out string entropyHex, out string errorMessage);
                    if (!isValid)
                    {
                        OutputBox.Text = string.Empty;
                        SetStatus(errorMessage, true);
                        Logger.Warn("BIP39 validation failed.");
                        return;
                    }

                    OutputBox.Text = $"{Localization.T("OutputValidBip39")}\n\n{Localization.T("OutputEntropyHeader")}\n{entropyHex}";
                    SetStatus(Localization.T("StatusBip39Valid"), false);
                    FlashOutputPanel();
                    Logger.Info("BIP39 mnemonic validated successfully.");
                    return;
                }

                List<int> positions;
                try
                {
                    positions = HexToPositions(raw);
                    foreach (int p in positions)
                    {
                        if (p < 1 || p > wordlist.Count)
                        {
                            throw new IndexOutOfRangeException();
                        }
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException || ex is OverflowException)
                {
                    SetStatus(Localization.T("StatusInvalidHex"), true);
                    OutputBox.Text = string.Empty;
                    Logger.Warn("Invalid hex value during decoding.");
                    return;
                }

                List<string> words = positions.Select(p => wordlist[p - 1]).ToList();
                OutputBox.Text = string.Join(" ", words);
                SetStatus(Localization.T("StatusWordsDecoded", words.Count), false);
                FlashOutputPanel();
                Logger.Info($"{words.Count} words decoded successfully (legacy mode).");
            }
            catch (Exception ex)
            {
                Logger.Error($"Decoding failed: {ex.GetType().Name}");
                SetStatus(ex.Message, true);
            }
        }

        // ---------- Copy ----------
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OutputBox.Text))
            {
                SetStatus(Localization.T("StatusNothingToCopy"), true);
                return;
            }

            try
            {
                Clipboard.SetText(OutputBox.Text);
                SetStatus(Localization.T("StatusCopied"), false);
            }
            catch (Exception)
            {
                SetStatus(Localization.T("StatusCopyFailed"), true);
            }
        }

        // ---------- Steganography ----------
        private void HideInImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OutputBox.Text))
            {
                SetStatus(Localization.T("StatusNeedGenerateFirst"), true);
                return;
            }

            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Choose carrier image (PNG recommended)",
                Filter = "Images (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg"
            };
            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save output image as",
                Filter = "PNG (*.png)|*.png",
                FileName = "phrasecrypt_hidden.png"
            };
            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                SteganographyUtility.HideText(openDialog.FileName, saveDialog.FileName, OutputBox.Text);
                SetStatus(Localization.T("StatusHiddenInImage", Path.GetFileName(saveDialog.FileName)), false);
                Logger.Info("Data embedded into image via steganography.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Steganography error (hide): {ex.GetType().Name}");
                SetStatus(ex.Message, true);
            }
        }

        private void ExtractFromImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Choose image containing hidden data",
                Filter = "PNG (*.png)|*.png"
            };
            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                string extracted = SteganographyUtility.ExtractText(openDialog.FileName);

                // Switch to READ mode and place the text into the input field
                ModeReadRadio.IsChecked = true;
                HexInputBox.Text = extracted;

                SetStatus(Localization.T("StatusExtractedFromImage"), false);
                Logger.Info("Data extracted from image via steganography.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Steganography error (extract): {ex.GetType().Name}");
                SetStatus(ex.Message, true);
            }
        }

        // ---------- Wordlist loading (several candidate paths, with duplicate check) ----------
        private List<string> EnsureWordlistLoaded()
        {
            if (_wordlist != null)
            {
                return _wordlist;
            }

            _wordlist = LoadWordlist();
            return _wordlist;
        }

        private static string FindWordlistPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            List<string> candidates = new List<string>
            {
                Path.Combine(baseDir, WordlistFileName),
                Path.Combine(Directory.GetCurrentDirectory(), WordlistFileName)
            };

            DirectoryInfo? dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                candidates.Add(Path.Combine(dir.FullName, WordlistFileName));
                dir = dir.Parent;
            }

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static List<string> LoadWordlist()
        {
            string path = FindWordlistPath();
            if (string.IsNullOrEmpty(path))
            {
                throw new FileNotFoundException(Localization.T("ErrorWordlistNotFound", WordlistFileName));
            }

            // File.ReadLines handles \r\n, \n and \r automatically, so mixed line
            // endings are not a problem.
            List<string> words = new List<string>();
            foreach (string rawLine in File.ReadLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length > 0)
                {
                    words.Add(line);
                }
            }

            if (words.Count != 2048)
            {
                throw new InvalidDataException(Localization.T("ErrorWordlistCount", WordlistFileName, words.Count));
            }

            List<string> duplicates = words
                .GroupBy(w => w)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                string examples = string.Join(", ", duplicates.Take(3));
                throw new InvalidDataException(
                    Localization.T("ErrorWordlistDuplicates", WordlistFileName, duplicates.Count, examples));
            }

            return words;
        }

        // ---------- Cryptographically secure sampling without replacement (legacy mode) ----------
        private static List<int> DrawRandomIndices(int listSize, int count)
        {
            int[] pool = new int[listSize];
            for (int i = 0; i < listSize; i++)
            {
                pool[i] = i;
            }

            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            for (int i = 0; i < count; i++)
            {
                int j = i + GetRandomInt(rng, listSize - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return new List<int>(pool[..count]);
        }

        private static int GetRandomInt(RandomNumberGenerator rng, int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
            }

            uint range = (uint)exclusiveUpperBound;
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            byte[] buffer = new byte[4];

            while (true)
            {
                rng.GetBytes(buffer);
                uint value = BitConverter.ToUInt32(buffer, 0);
                if (value < limit)
                {
                    return (int)(value % range);
                }
            }
        }

        // ---------- Hex conversion (legacy mode) ----------
        private static string PositionsToHex(List<int> positions)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int p in positions)
            {
                sb.Append(p.ToString("X4"));
            }
            return sb.ToString();
        }

        private static List<int> HexToPositions(string hexString)
        {
            hexString = hexString.Trim();
            if (hexString.Length % 4 != 0)
            {
                throw new FormatException("Hex string length must be a multiple of 4.");
            }

            List<int> positions = new List<int>();
            for (int i = 0; i < hexString.Length; i += 4)
            {
                string chunk = hexString.Substring(i, 4);
                positions.Add(Convert.ToInt32(chunk, 16));
            }
            return positions;
        }

        private static string FormatWithLineBreaks(string text, int lineWidth)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i += lineWidth)
            {
                int length = Math.Min(lineWidth, text.Length - i);
                sb.AppendLine(text.Substring(i, length));
            }
            return sb.ToString().TrimEnd();
        }

        // ---------- Status display (with animated indicator dot) ----------
        private void SetStatus(string message, bool isError)
        {
            StatusText.Text = message;

            // Pull colors from the active theme so both dark and light look right
            string key = isError ? "ErrorBrush" : "TextSecondaryBrush";
            Color targetColor = (TryFindResource(key) is SolidColorBrush themeBrush)
                ? themeBrush.Color
                : Color.FromRgb(0x8C, 0x8C, 0x8C);

            StatusText.Foreground = new SolidColorBrush(targetColor);

            if (StatusDot.Background is SolidColorBrush existingBrush)
            {
                var animatedBrush = existingBrush.Clone();
                StatusDot.Background = animatedBrush;
                var colorAnim = new System.Windows.Media.Animation.ColorAnimation(targetColor, TimeSpan.FromMilliseconds(200));
                animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            }
        }

        /// <summary>
        /// Brief glow pulse on the output panel to highlight newly generated or decoded data.
        /// </summary>
        private void FlashOutputPanel()
        {
            var glow = new DropShadowEffect
            {
                Color = Color.FromRgb(0xF5, 0xA6, 0x23),
                BlurRadius = 18,
                ShadowDepth = 4,
                Opacity = 0.35,
                Direction = 270
            };
            OutputPanelBorder.Effect = glow;

            var pulse = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 0.35,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, pulse);
        }
    }
}
