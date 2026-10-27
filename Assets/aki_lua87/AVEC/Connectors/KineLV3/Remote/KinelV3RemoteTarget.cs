using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using Kinel.VideoPlayer.V3.Udon.System;
using Kinel.VideoPlayer.V3.Udon.System.Sync;
using Kinel.VideoPlayer.V3.Udon.System.Component;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class KinelV3RemoteTarget : RemoteTarget
    {
        public KinelPlayerController player;
        public KinelVariableSyncer syncer;
        public KinelAudioManager audioManager;
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy && syncer != null && audioManager != null && player.NowSelectedMediaModule != null; }
        public override float _GetVolume() { return audioManager == null ? 0f : audioManager.MasterVolume; }
        public override bool _GetMute() { return audioManager != null && audioManager.Mute; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && audioManager != null) audioManager.MasterVolume = Mathf.Clamp01(value); }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && audioManager != null) audioManager.Mute = value; }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = player.IsPaused() ? 13 : 12;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && !player.IsPlaying() && !player.IsPaused()) return 4;
            if (action == 17) {
                float duration = player.GetDuration();
                if (duration <= 0f || float.IsInfinity(duration) || float.IsNaN(duration)) return 4;
            }
            if (player.IsLock && !player.CanOperate(Networking.LocalPlayer)) return 2;
            syncer.RequestOwnershipForUserAction();
            if (!Networking.IsOwner(syncer.gameObject)) return 2;
            if (action == 16 || action == 19) player.SetLoopMode(action == 19 || player.LoopMode != LoopMode.Single ? LoopMode.Single : LoopMode.None);
            else if (action == 17) { player.SetTime(0f); player.OnKinelSeek(0f); if (player.IsPaused()) player.NowSelectedMediaModule.Play(); }
            else if (action == 8) { player.ResetMedia(); player.OnKinelMediaReset(); }
            else if (action == 12) { if (!player.IsPaused()) player.NowSelectedMediaModule.Pause(); }
            else if (!player.IsPlaying() || player.IsPaused()) player.NowSelectedMediaModule.Play();
            return 1;
        }
    }
}
