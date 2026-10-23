using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace aki_lua87.CommonGestureRemote
{
    // Static text is baked from the same data as the remote and survives build stripping.
    [DisallowMultipleComponent]
    public class GestureGuideSetup : MonoBehaviour, IEditorOnly
    {
        public GestureData data;
        public GestureRemote remote;
        public Text contents;
    }
}
