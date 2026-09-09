using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace PhraseCryptApp
{
    /// <summary>
    /// Startup window. Lets the user pick a build channel (Stable/Alpha) before
    /// MainWindow opens. Purely a UI switch - see BuildChannel.
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();

            Resources.MergedDictionaries.Add(ThemeManager.CreateDarkTheme());

            ApplyLanguage();
            SetChannelPreview(ChannelKind.Stable);
        }

        private void ApplyLanguage()
        {
            LauncherSubtitleText.Text = Localization.T("LauncherSubtitle");
            StableTitleText.Text = Localization.T("ChannelStable");
            StableSubtitleText.Text = Localization.T("ChannelStableSubtitle");
            AlphaTitleText.Text = Localization.T("ChannelAlpha");
            AlphaSubtitleText.Text = Localization.T("ChannelAlphaSubtitle");
            VersionText.Text = Localization.T("LauncherVersion", GetVersionString());
        }

        private void StableButton_MouseEnter(object sender, MouseEventArgs e) => SetChannelPreview(ChannelKind.Stable);

        private void AlphaButton_MouseEnter(object sender, MouseEventArgs e) => SetChannelPreview(ChannelKind.Alpha);

        private void SetChannelPreview(ChannelKind channel)
        {
            string name = channel == ChannelKind.Alpha
                ? Localization.T("ChannelAlpha")
                : Localization.T("ChannelStable");
            ChannelSelectedText.Text = Localization.T("LauncherChannelSelected", name);
        }

        private void StableButton_Click(object sender, RoutedEventArgs e) => Launch(ChannelKind.Stable);

        private void AlphaButton_Click(object sender, RoutedEventArgs e) => Launch(ChannelKind.Alpha);

        private void Launch(ChannelKind channel)
        {
            BuildChannel.Current = channel;

            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }

        private static string GetVersionString()
        {
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }
}
