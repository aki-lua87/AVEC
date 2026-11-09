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
        [Tooltip("各タブに種類を示すアイコンを表示します。")]
        public bool showTabIcons = true;
        public bool enableQueue = true;
        public bool enableHistory = true;
        public bool enableUserList = true;
        // Optional extensions own the provider's type, settings UI and connection logic.
        [UnityEngine.Serialization.FormerlySerializedAs("yttlManager"), HideInInspector]
        public Component titleMetadataSource;
    }
}
