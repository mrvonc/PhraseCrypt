using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace PhraseCryptApp
{
    /// <summary>
    /// Main window.
    ///
    /// SECURITY MODEL - the rules this class enforces:
    ///
    /// 1. Generation always produces a standards-compliant BIP39 phrase.
    ///    There is no option to disable the checksum and no hidden word count.
    /// 2. WRITE never displays a phrase. It emits an encrypted container only.
    ///    To see the phrase the user must decrypt it in READ with the password.
    /// 3. READ is the single place where clear text appears. While it is on
    ///    screen, copying and image embedding are blocked and a banner is shown.
    /// 4. Nothing is ever written to disk unencrypted - no logs, no temp files,
    ///    no autosave. The only file this app writes is a PNG the user explicitly
    ///    asks for, and it may only contain an encrypted container.
    /// 5. Secrets are cleared from memory as early as possible and wiped when the
    ///    window closes or the user presses Clear.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string WordlistFileName = "english.txt";

        /// <summary>Minimum password length. Honey mode gives no wrong-password
        /// feedback, so a weak password is unusually dangerous here.</summary>
        private const int MinimumPasswordLength = 12;

        private List<string>? _wordlist;
        private bool _isInitialized;

        /// <summary>True while the output box holds clear-text secret material.
        /// Gates the copy and steganography actions.</summary>
        private bool _outputIsSensitive;

        public MainWindow()
        {
            InitializeComponent();

            ApplyTheme(isLight: false);
            _isInitialized = true;

            ApplyLanguage(Localization.English);
            UpdateReadModeUi();

            Closing += (_, _) => WipeAllSecrets();
        }

        // ================= Theme =================

        private void ThemeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyTheme(ThemeToggle.IsChecked == true);
        }

        private void ApplyTheme(bool isLight)
        {
            ResourceDictionary newDict = isLight
                ? ThemeManager.CreateLightTheme()
                : ThemeManager.CreateDarkTheme();

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(newDict);
        }

        // ================= Language =================

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
            ApplyLanguage(item.Tag?.ToString() ?? Localization.English);
        }

        private void ApplyLanguage(string langCode)
        {
            Localization.CurrentLanguage = langCode;

            AppSubtitleText.Text = Localization.T("AppSubtitle");
            LanguageLabelText.Text = Localization.T("LanguageLabel");
            ThemeLabelText.Text = Localization.T("ThemeLabel");

            ModeWriteRadio.Content = Localization.T("ModeWrite");
            ModeReadRadio.Content = Localization.T("ModeRead");

            // WRITE
            WordCountLabel.Text = Localization.T("LabelWordCount");
            ProtectionLabel.Text = Localization.T("LabelProtection");
            ProtectAesRadio.Content = Localization.T("OptionAes");
            ProtectHoneyRadio.Content = Localization.T("OptionHoney");
            PasswordLabel.Text = Localization.T("LabelPassword");
            PasswordConfirmLabel.Text = Localization.T("LabelPasswordConfirm");
            WriteHintText.Text = Localization.T("WriteHint");
            GenerateButton.Content = Localization.T("ButtonGenerate");

            // READ
            ReadModeLabel.Text = Localization.T("LabelInputType");
            ReadAesRadio.Content = Localization.T("OptionReadAes");
            ReadHoneyRadio.Content = Localization.T("OptionReadHoney");
            ReadValidateRadio.Content = Localization.T("OptionReadValidate");
            ReadPasswordLabel.Text = Localization.T("LabelPassword");
            DecodeButton.Content = Localization.T("ButtonReveal");

            // Output
            OutputLabel.Text = Localization.T("LabelOutput");
            CopyButton.Content = Localization.T("ButtonCopy");
            ClearButton.Content = Localization.T("ButtonClear");
            SensitiveBannerText.Text = Localization.T("SensitiveBanner");
            HideInImageButton.Content = Localization.T("ButtonHideImage");
            ExtractFromImageButton.Content = Localization.T("ButtonExtractImage");

            UpdateReadModeUi();
            SetStatus(Localization.T("StatusReady"), false);
        }

        // ================= Mode switching =================

        private void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (WritePanel == null || ReadPanel == null || !_isInitialized) return;

            bool showWrite = ModeWriteRadio.IsChecked == true;
            AnimatePanelSwitch(showWrite ? ReadPanel : WritePanel,
                               showWrite ? WritePanel : ReadPanel);

            // Switching modes always clears whatever was on screen.
            ClearOutput();
            SetStatus(Localization.T("StatusReady"), false);
        }

        private static void AnimatePanelSwitch(FrameworkElement panelOut, FrameworkElement panelIn)
        {
            var ease = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            };

            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = ease
            };

            fadeOut.Completed += (_, _) =>
            {
                panelOut.Visibility = Visibility.Collapsed;
                panelIn.Visibility = Visibility.Visible;
                panelIn.Opacity = 0;

                if (panelIn.RenderTransform is TranslateTransform t)
                {
                    t.Y = 10;
                    t.BeginAnimation(TranslateTransform.YProperty,
                        new System.Windows.Media.Animation.DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
                }

                panelIn.BeginAnimation(OpacityProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
            };

            panelOut.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void ReadModeRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdateReadModeUi();
            ClearOutput();
        }

        /// <summary>Validation needs no password; the container modes do.</summary>
        private void UpdateReadModeUi()
        {
            if (ReadInputLabel == null) return;

            bool validating = ReadValidateRadio.IsChecked == true;

            ReadInputLabel.Text = validating
                ? Localization.T("LabelMnemonicInput")
                : Localization.T("LabelContainerInput");

            ReadPasswordLabel.Visibility = validating ? Visibility.Collapsed : Visibility.Visible;
            ReadPasswordBox.Visibility = validating ? Visibility.Collapsed : Visibility.Visible;
        }

        // ================= WRITE: generate an encrypted container =================

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            byte[]? entropy = null;
            string? mnemonic = null;

            try
            {
                List<string> wordlist = EnsureWordlistLoaded();
                int wordCount = GetSelectedWordCount();

                string password = WritePasswordBox.Password;
                string confirm = WritePasswordConfirmBox.Password;

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException(Localization.T("ErrorPasswordRequired"));
                }
                if (password.Length < MinimumPasswordLength)
                {
                    throw new ArgumentException(Localization.T("ErrorPasswordTooShort", MinimumPasswordLength));
                }
                // A typo here is unrecoverable in honey mode, where a wrong
                // password silently produces a plausible but useless phrase.
                if (!string.Equals(password, confirm, StringComparison.Ordinal))
                {
                    throw new ArgumentException(Localization.T("ErrorPasswordMismatch"));
                }

                // Always standards-compliant BIP39. No alternative path exists.
                (mnemonic, string entropyHex) = Bip39Utility.Generate(wordlist, wordCount);
                entropy = Convert.FromHexString(entropyHex);

                string container = ProtectHoneyRadio.IsChecked == true
                    ? HoneyEncryption.EncryptToBase64(entropy, password)
                    : CryptoUtility.EncryptToBase64(mnemonic, password);

                // Only the container reaches the screen - never the phrase itself.
                ShowOutput(container, isSensitive: false);
                SetStatus(Localization.T("StatusContainerCreated", wordCount), false);
                FlashOutputPanel();
            }
            catch (Exception ex)
            {
                ClearOutput();
                SetStatus(ex.Message, true);
            }
            finally
            {
                // Drop the plaintext as early as possible.
                if (entropy != null) Array.Clear(entropy, 0, entropy.Length);
                mnemonic = null;

                WritePasswordBox.Clear();
                WritePasswordConfirmBox.Clear();
            }
        }

        /// <summary>Reads the word count from a fixed, non-editable list.
        /// No free-text parsing, so no hidden keywords are possible.</summary>
        private int GetSelectedWordCount()
        {
            string? text = (WordCountCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return text == "24" ? 24 : 12;
        }

        // ================= READ: the only place clear text appears =================

        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            byte[]? entropy = null;

            try
            {
                List<string> wordlist = EnsureWordlistLoaded();
                string input = (ContainerInputBox.Text ?? string.Empty).Trim();

                if (input.Length == 0)
                {
                    throw new ArgumentException(Localization.T("ErrorNoInput"));
                }

                // --- Validate a mnemonic (no secret is produced, only a verdict) ---
                if (ReadValidateRadio.IsChecked == true)
                {
                    bool ok = Bip39Utility.TryValidate(input, wordlist, out _, out string error);
                    ShowOutput(ok ? Localization.T("OutputValid") : error, isSensitive: false);
                    SetStatus(ok ? Localization.T("StatusValid") : Localization.T("StatusInvalid"), !ok);
                    FlashOutputPanel();
                    return;
                }

                string password = ReadPasswordBox.Password;
                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException(Localization.T("ErrorPasswordRequired"));
                }

                string phrase;
                string statusKey;

                if (ReadHoneyRadio.IsChecked == true)
                {
                    // Honey: this ALWAYS succeeds. A wrong password simply yields a
                    // different valid phrase. Reporting success or failure here
                    // would defeat the entire scheme, so the status stays neutral.
                    entropy = HoneyEncryption.DecryptFromBase64(input, password);
                    phrase = Bip39Utility.EntropyToMnemonic(entropy, wordlist);
                    statusKey = "StatusHoneyRevealed";
                }
                else
                {
                    // AES-GCM is authenticated, so a wrong password throws here.
                    phrase = CryptoUtility.DecryptFromBase64(input, password);
                    statusKey = "StatusRevealed";
                }

                ShowOutput(phrase, isSensitive: true);
                SetStatus(Localization.T(statusKey), false);
                FlashOutputPanel();
            }
            catch (Exception ex)
            {
                ClearOutput();
                SetStatus(ex.Message, true);
            }
            finally
            {
                if (entropy != null) Array.Clear(entropy, 0, entropy.Length);
                ReadPasswordBox.Clear();
            }
        }

        // ================= Output handling =================

        private void ShowOutput(string text, bool isSensitive)
        {
            _outputIsSensitive = isSensitive;
            OutputBox.Text = text;
            SensitiveBanner.Visibility = isSensitive ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearOutput()
        {
            _outputIsSensitive = false;
            OutputBox.Clear();
            SensitiveBanner.Visibility = Visibility.Collapsed;
            OutputPanelBorder.Effect = null;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            WipeAllSecrets();
            SetStatus(Localization.T("StatusCleared"), false);
        }

        /// <summary>Wipes every field that could hold secret material.</summary>
        private void WipeAllSecrets()
        {
            ClearOutput();
            ContainerInputBox.Clear();
            WritePasswordBox.Clear();
            WritePasswordConfirmBox.Clear();
            ReadPasswordBox.Clear();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            // Hard rule: a clear-text phrase never reaches the clipboard.
            if (_outputIsSensitive)
            {
                SetStatus(Localization.T("ErrorCopyBlocked"), true);
                return;
            }

            if (string.IsNullOrEmpty(OutputBox.Text))
            {
                SetStatus(Localization.T("StatusNothingToCopy"), true);
                return;
            }

            try
            {
                // copy:false means the clipboard is emptied when this app exits,
                // instead of the data surviving in other processes.
                Clipboard.SetDataObject(OutputBox.Text, false);
                SetStatus(Localization.T("StatusCopied"), false);
            }
            catch
            {
                SetStatus(Localization.T("StatusCopyFailed"), true);
            }
        }

        // ================= Steganography (containers only) =================

        private void HideInImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Writing a clear-text phrase into a file would violate the whole model.
            if (_outputIsSensitive)
            {
                SetStatus(Localization.T("ErrorEmbedBlocked"), true);
                return;
            }

            if (string.IsNullOrEmpty(OutputBox.Text))
            {
                SetStatus(Localization.T("StatusNeedContainerFirst"), true);
                return;
            }

            var openDialog = new OpenFileDialog
            {
                Title = "Choose carrier image (PNG recommended)",
                Filter = "Images (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg"
            };
            if (openDialog.ShowDialog() != true) return;

            var saveDialog = new SaveFileDialog
            {
                Title = "Save output image as",
                Filter = "PNG (*.png)|*.png",
                FileName = "container.png"
            };
            if (saveDialog.ShowDialog() != true) return;

            try
            {
                SteganographyUtility.HideText(openDialog.FileName, saveDialog.FileName, OutputBox.Text);
                SetStatus(Localization.T("StatusHiddenInImage", Path.GetFileName(saveDialog.FileName)), false);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
        }

        private void ExtractFromImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Choose image containing hidden data",
                Filter = "PNG (*.png)|*.png"
            };
            if (openDialog.ShowDialog() != true) return;

            try
            {
                string extracted = SteganographyUtility.ExtractText(openDialog.FileName);

                // Extracted data is a container, so it lands in the READ input,
                // where it still needs the password to reveal anything.
                ModeReadRadio.IsChecked = true;
                ContainerInputBox.Text = extracted;
                ClearOutput();

                SetStatus(Localization.T("StatusExtractedFromImage"), false);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
        }

        // ================= Wordlist =================

        private List<string> EnsureWordlistLoaded()
        {
            return _wordlist ??= LoadWordlist();
        }

        private static string FindWordlistPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new List<string>
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

            return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        /// <summary>Loads and sanity-checks the wordlist. A tampered list would
        /// silently corrupt every phrase produced, so it is verified strictly.</summary>
        private static List<string> LoadWordlist()
        {
            string path = FindWordlistPath();
            if (string.IsNullOrEmpty(path))
            {
                throw new FileNotFoundException(Localization.T("ErrorWordlistNotFound", WordlistFileName));
            }

            var words = new List<string>();
            foreach (string rawLine in File.ReadLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length > 0) words.Add(line);
            }

            if (words.Count != 2048)
            {
                throw new InvalidDataException(Localization.T("ErrorWordlistCount", WordlistFileName, words.Count));
            }

            List<string> duplicates = words.GroupBy(w => w).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                throw new InvalidDataException(Localization.T(
                    "ErrorWordlistDuplicates", WordlistFileName, duplicates.Count, string.Join(", ", duplicates.Take(3))));
            }

            return words;
        }

        // ================= Status =================

        private void SetStatus(string message, bool isError)
        {
            StatusText.Text = message;

            Color color = TryFindResource(isError ? "ErrorBrush" : "TextSecondaryBrush") is SolidColorBrush b
                ? b.Color
                : Color.FromRgb(0x8C, 0x8C, 0x8C);

            StatusText.Foreground = new SolidColorBrush(color);

            if (StatusDot.Background is SolidColorBrush dot)
            {
                var animated = dot.Clone();
                StatusDot.Background = animated;
                animated.BeginAnimation(SolidColorBrush.ColorProperty,
                    new System.Windows.Media.Animation.ColorAnimation(color, TimeSpan.FromMilliseconds(200)));
            }
        }

        /// <summary>Brief glow pulse to highlight newly produced output.</summary>
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

            glow.BeginAnimation(DropShadowEffect.OpacityProperty,
                new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.9,
                    To = 0.35,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase
                    {
                        EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                    }
                });
        }
    }
}
