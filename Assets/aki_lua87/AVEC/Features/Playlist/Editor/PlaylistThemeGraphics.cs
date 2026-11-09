using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Shared image roles for color themes. Themes never replace sprites or layout.
    public static class PlaylistThemeGraphics
    {
        public static bool IsViewport(Image image) => image.name.IndexOf("viewport", System.StringComparison.OrdinalIgnoreCase) >= 0;
        public static bool IsForeground(PlaylistView view, Image image)
        {
            if (view != null && view.remoteToggleButton != null && image.gameObject != view.remoteToggleButton &&
                image.transform.IsChildOf(view.remoteToggleButton.transform)) return true;
            string name = image.name;
            if (name == "Handle" || name == "Volume fill" || name == "Track") return false;
            return name == "Icon" || name == "Tab kind icon" || name == "Playback progress" ||
                name == "Accent" || name == "Divider" || image.type == Image.Type.Filled ||
                (view != null && (image == view.playbackModeIcon || image == view.volumeIcon || image == view.transportIcon));
        }
    }
}
