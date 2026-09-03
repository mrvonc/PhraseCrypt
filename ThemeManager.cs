using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PhraseCryptApp
{
    /// <summary>
    /// Builds the theme resources (dark/light) directly in code.
    /// Deliberately NOT separate .xaml files, which avoids any issues with build
    /// actions or resource embedding.
    /// All UI elements reference these keys via DynamicResource.
    /// </summary>
    public static class ThemeManager
    {
        public static ResourceDictionary CreateDarkTheme()
        {
            var d = new ResourceDictionary();

            AddBrush(d, "PanelBrush", "#1A1A1A");
            AddBrush(d, "PanelBorderBrush", "#2B2B2B");
            AddBrush(d, "PanelBorderHoverBrush", "#3D3D3D");
            AddBrush(d, "AccentBrush", "#F5A623");
            AddBrush(d, "AccentBrightBrush", "#FFC157");
            AddBrush(d, "AccentDimBrush", "#4D3A15");
            AddBrush(d, "TextPrimaryBrush", "#EAEAEA");
            AddBrush(d, "TextSecondaryBrush", "#8C8C8C");
            AddBrush(d, "ErrorBrush", "#E0524B");
            AddBrush(d, "InputBgBrush", "#141414");
            AddBrush(d, "DropdownBgBrush", "#1C1C1C");
            AddBrush(d, "ItemHoverBrush", "#2A2A2A");

            // Window background: subtle diagonal gradient
            var windowBg = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#101010"), 0));
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#0A0A0D"), 0.6));
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#0D0C10"), 1));
            windowBg.Freeze();
            d["WindowBgBrush"] = windowBg;

            // Panel gradient for a subtle sense of depth
            var panelBg = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            panelBg.GradientStops.Add(new GradientStop(ParseColor("#1E1E1E"), 0));
            panelBg.GradientStops.Add(new GradientStop(ParseColor("#181818"), 1));
            panelBg.Freeze();
            d["PanelGradientBrush"] = panelBg;

            var shadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 18,
                ShadowDepth = 4,
                Opacity = 0.35,
                Direction = 270
            };
            shadow.Freeze();
            d["PanelShadow"] = shadow;

            return d;
        }

        public static ResourceDictionary CreateLightTheme()
        {
            var d = new ResourceDictionary();

            AddBrush(d, "PanelBrush", "#FFFFFF");
            AddBrush(d, "PanelBorderBrush", "#DCDCE0");
            AddBrush(d, "PanelBorderHoverBrush", "#B8B8C0");
            AddBrush(d, "AccentBrush", "#D98A00");
            AddBrush(d, "AccentBrightBrush", "#F5A623");
            AddBrush(d, "AccentDimBrush", "#FBE8C4");
            AddBrush(d, "TextPrimaryBrush", "#1A1A1A");
            AddBrush(d, "TextSecondaryBrush", "#5F5F66");
            AddBrush(d, "ErrorBrush", "#C62828");
            AddBrush(d, "InputBgBrush", "#FFFFFF");
            AddBrush(d, "DropdownBgBrush", "#FFFFFF");
            AddBrush(d, "ItemHoverBrush", "#EFEFF3");

            var windowBg = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#F8F8FA"), 0));
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#EFEFF3"), 0.6));
            windowBg.GradientStops.Add(new GradientStop(ParseColor("#F3F2F6"), 1));
            windowBg.Freeze();
            d["WindowBgBrush"] = windowBg;

            var panelBg = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            panelBg.GradientStops.Add(new GradientStop(ParseColor("#FFFFFF"), 0));
            panelBg.GradientStops.Add(new GradientStop(ParseColor("#FAFAFC"), 1));
            panelBg.Freeze();
            d["PanelGradientBrush"] = panelBg;

            var shadow = new DropShadowEffect
            {
                Color = ParseColor("#9096A8"),
                BlurRadius = 14,
                ShadowDepth = 3,
                Opacity = 0.22,
                Direction = 270
            };
            shadow.Freeze();
            d["PanelShadow"] = shadow;

            return d;
        }

        private static void AddBrush(ResourceDictionary dict, string key, string hex)
        {
            var brush = new SolidColorBrush(ParseColor(hex));
            brush.Freeze();
            dict[key] = brush;
        }

        private static Color ParseColor(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
