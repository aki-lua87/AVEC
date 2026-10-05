using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.CommonGestureRemote
{
    // One target per player. Multiple remotes share its LOCAL power state.
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public abstract class RemoteTarget : UdonSharpBehaviour
    {
        public Renderer[] screens = new Renderer[0];
        public Graphic[] screenGraphics = new Graphic[0];
        private bool powerOn = true;
        private bool savedMute;
        private bool[] savedScreens;
        private bool[] savedGraphics;

        public virtual bool _Ready() { return false; }
        public virtual float _GetVolume() { return -1f; }
        public virtual void _SetVolume(float value) { }
        public virtual bool _GetMute() { return false; }
        public virtual void _SetMute(bool value) { }
        public virtual int _Playback(int action) { return 5; }

        // 0 unavailable, 1 submitted/applied, 2 denied, 3 invalid, 4 no next/previous,
        // 5 unsupported/incomplete setup, 6 local power off, 7 load cooldown.
        public virtual int _Execute(int action, float amount)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if (action == 5 || action == 6 || action == 7)
                return SetPower(action == 6 || (action == 7 && !powerOn));
            if (action == 3 || action == 4 || action == 14)
            {
                if (!powerOn) return 6;
                if (action == 14) _SetMute(!_GetMute());
                else _SetVolume(Mathf.Clamp01(_GetVolume() + (action == 3 ? amount : -amount)));
                return 1;
            }
            return _Playback(action);
        }

        private int SetPower(bool value)
        {
            if (value == powerOn) return 1;
            // Never claim that video is off when no visible output was configured.
            bool hasScreen = false;
            for (int i = 0; i < screens.Length; i++) if (screens[i] != null) hasScreen = true;
            for (int i = 0; i < screenGraphics.Length; i++) if (screenGraphics[i] != null) hasScreen = true;
            if (!value && !hasScreen) return 5;
            if (!value)
            {
                savedMute = _GetMute();
                savedScreens = new bool[screens.Length];
                savedGraphics = new bool[screenGraphics.Length];
                for (int i = 0; i < screens.Length; i++)
                    if (screens[i] != null) savedScreens[i] = screens[i].enabled;
                for (int i = 0; i < screenGraphics.Length; i++)
                    if (screenGraphics[i] != null) savedGraphics[i] = screenGraphics[i].enabled;
                powerOn = false;
                _SetMute(true);
                HideScreens();
            }
            else RestorePower();
            return 1;
        }
        private void HideScreens()
        {
            for (int i = 0; i < screens.Length; i++) if (screens[i] != null && screens[i].enabled) screens[i].enabled = false;
            for (int i = 0; i < screenGraphics.Length; i++) if (screenGraphics[i] != null && screenGraphics[i].enabled) screenGraphics[i].enabled = false;
        }
        private void RestorePower()
        {
            if (powerOn) return;
            powerOn = true;
            for (int i = 0; i < screens.Length; i++)
                if (screens[i] != null && savedScreens != null && i < savedScreens.Length) screens[i].enabled = savedScreens[i];
            for (int i = 0; i < screenGraphics.Length; i++)
                if (screenGraphics[i] != null && savedGraphics != null && i < savedGraphics.Length) screenGraphics[i].enabled = savedGraphics[i];
            _SetMute(savedMute);
        }
        protected virtual void LateUpdate()
        {
            if (powerOn) return;
            // Native player callbacks may re-enable their screens when a new video loads.
            HideScreens();
            if (_Ready() && !_GetMute()) _SetMute(true);
        }
        protected virtual void OnDisable() { RestorePower(); }
    }
}
