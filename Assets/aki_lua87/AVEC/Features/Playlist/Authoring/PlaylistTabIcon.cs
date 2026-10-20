using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace aki_lua87.CommonVideoPlaylist
{
    // Editor-only per-row metadata also follows row-template cloning.
    [DisallowMultipleComponent, AddComponentMenu("")]
    public class PlaylistTabIcon : MonoBehaviour, IEditorOnly
    {
        [HideInInspector] public Image icon;
        [HideInInspector] public RectTransform labelArea;
        [HideInInspector] public float appliedInset;
        [HideInInspector] public int placementVersion;
    }
}
