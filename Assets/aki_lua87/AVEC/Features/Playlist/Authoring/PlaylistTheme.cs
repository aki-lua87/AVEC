using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist
{
    [CreateAssetMenu(menuName = "AVEC/Playlist/Color Theme")]
    public class PlaylistTheme : ScriptableObject
    {
        [InspectorName("枠線のみ（背景透過）")]
        public bool outlineOnly;
        [InspectorName("枠線の太さ"), Range(.5f, 4f)]
        public float outlineWidth = 1.5f;
        // Editor-generated shared sprite; retained in the asset for export dependencies.
        [HideInInspector] public Sprite outlineSprite;
        // VRC Slate: an original slate/cyan palette inspired by VRChat UI.
        public Color background = new Color(.071f, .094f, .125f);
        public Color surface = new Color(.106f, .137f, .180f);
        public Color button = new Color(.161f, .204f, .259f);
        public Color selected = new Color(.055f, .333f, .443f);
        public Color accent = new Color(.216f, .765f, .906f);
        public Color text = new Color(.941f, .965f, .980f);
        public Color mutedText = new Color(.639f, .714f, .769f);
    }

    [System.Serializable]
    public class PlaylistThemeImageState
    {
        public UnityEngine.UI.Image image;
        public Sprite sprite;
        public Sprite overrideSprite;
        public UnityEngine.UI.Image.Type type;
        public bool fillCenter;
        public bool preserveAspect;
        public float pixelsPerUnitMultiplier;
        public float alphaHitTestMinimumThreshold;
        public bool colorSuppressed;
        public Color originalColor;
    }

    [System.Serializable]
    public class PlaylistThemeLayoutState
    {
        public UnityEngine.UI.VerticalLayoutGroup layout;
        public int rightPadding;
    }
}
