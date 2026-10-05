using UnityEngine;
using VRC.SDKBase;

namespace aki_lua87.CommonVideoPlaylist
{
    [DisallowMultipleComponent]
    public class PlaylistSetup : MonoBehaviour, IEditorOnly
    {
        public PlaylistData data;
        public PlaylistTheme theme;
        public PlaylistView view;
        public RectTransform tabContent;
        public RectTransform videoContent;
        public GameObject tabTemplate;
        public GameObject videoTemplate;
        public string playerKind;
        public Component player;
    }
}
