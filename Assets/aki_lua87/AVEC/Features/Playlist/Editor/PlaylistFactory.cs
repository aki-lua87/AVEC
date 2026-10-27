namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Shared authoring paths and controls used by the existing inspectors.
    public static class PlaylistFactory
    {
        public const string Root = aki_lua87.AVEC.Editor.PackageLayout.Playlist;
        public static void EnsurePlaybackModeButton(PlaylistSetup setup)
        {
            PlaylistAppearance.EnsureControls(setup);
        }
    }
}
