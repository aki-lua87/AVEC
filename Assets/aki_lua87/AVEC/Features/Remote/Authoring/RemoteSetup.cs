using UnityEngine;
using VRC.SDKBase;

namespace aki_lua87.CommonGestureRemote
{
    [DisallowMultipleComponent]
    public class RemoteSetup : MonoBehaviour, IEditorOnly
    {
        public GestureData data;
        public GestureRemote remote;
        public Component player;
        public Component playlist;
    }
}
