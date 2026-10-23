using UnityEngine;
using VRC.SDKBase;

namespace aki_lua87.CommonGestureRemote
{
    public enum RemoteModelStyle { Standard, Slim, Custom }
    [DisallowMultipleComponent]
    public class RemoteSetup : MonoBehaviour, IEditorOnly
    {
        public GestureData data;
        public GestureRemote remote;
        public Component player;
        public Component playlist;
        public Component nativePlaylist;
        // Editor-only provenance: distinguish an automatic default from a manual selection.
        [HideInInspector] public Transform autoSummonObject;
        public RemoteModelStyle modelStyle;
        [Tooltip("見た目だけのPrefabを指定します。操作・Tip・当たり判定はリモコン本体に残ります。")]
        public GameObject customModelPrefab;
        [HideInInspector] public Transform modelRoot;
    }
}
