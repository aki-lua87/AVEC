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
        [Header("特殊リスト")]
        public bool enableQueue = true;
        public bool enableHistory = true;
        public bool enableUserList = true;
        [Tooltip("任意。VizVid同梱のYttlManagerを指定すると、動画追加時にタイトルを取得します。")]
        public Component yttlManager;
        // Editor-only sprite backups allow a wireframe theme to return to authored graphics.
        [HideInInspector] public PlaylistThemeImageState[] themeImageStates = new PlaylistThemeImageState[0];
        [HideInInspector] public PlaylistThemeLayoutState[] themeLayoutStates = new PlaylistThemeLayoutState[0];
    }
}
