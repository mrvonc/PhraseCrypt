namespace PhraseCryptApp
{
    /// <summary>The two release channels selectable in LauncherWindow.</summary>
    public enum ChannelKind
    {
        Stable,
        Alpha
    }

    /// <summary>
    /// Holds the channel selected at startup. This is a UI-visibility switch only:
    /// it may show or hide experimental features, but must never change crypto
    /// behavior. Generation, encryption and validation are identical in both
    /// channels.
    /// </summary>
    public static class BuildChannel
    {
        public static ChannelKind Current { get; set; } = ChannelKind.Stable;
    }
}
