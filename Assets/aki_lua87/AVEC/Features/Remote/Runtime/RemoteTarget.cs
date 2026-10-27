using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public abstract class RemoteTarget : UdonSharpBehaviour
    {
        // Retained serialized output references for existing connectors.
        public Renderer[] screens = new Renderer[0];
        public Graphic[] screenGraphics = new Graphic[0];
        public virtual bool _CanStep() { return false; }
        public virtual int _StepNative(int direction) { return 4; }
        public virtual bool _Ready() { return false; }
        public virtual float _GetVolume() { return -1f; }
        public virtual void _SetVolume(float value) { }
        public virtual bool _GetMute() { return false; }
        public virtual void _SetMute(bool value) { }
        public virtual int _Playback(int action) { return 5; }
        // Optional bridge implemented by the Playlist extension, preserving package independence.
        public virtual void _NotifyPlayerStateChanged() { }
        public virtual int _Execute(int action, float amount)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if (action == 3 || action == 4 || action == 14)
            {
                if (action == 14) _SetMute(!_GetMute());
                else _SetVolume(Mathf.Clamp01(_GetVolume() + (action == 3 ? amount : -amount)));
                return 1;
            }
            return _Playback(action);
        }
    }
}
