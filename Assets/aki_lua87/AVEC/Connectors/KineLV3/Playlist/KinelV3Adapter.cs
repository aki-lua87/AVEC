using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using Kinel.VideoPlayer.V3.Udon.System;
using Kinel.VideoPlayer.V3.Udon.System.Sync;
using Kinel.VideoPlayer.V3.Udon.System.Component;

namespace aki_lua87.CommonVideoPlaylist
{
    // Shares the Sync Module GameObject with KineL V3's playback authority.
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class KinelV3Adapter : PlaylistAdapter
    {
        public KinelPlayerController player;
        public KinelVariableSyncer syncer;
        public KinelAudioManager audioManager;
        public KinelV3SequenceListener eventsListener;
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && syncer != null && player.NowSelectedMediaModule != null; }
        public override VRCUrl _CurrentUrl() { return _Available() ? player.GetPlayingUrl() : VRCUrl.Empty; }
        public override bool _IsLocalPlayback() { return false; }
        public override bool _NativeReady() { return _Available() && (player.IsPlaying() || player.IsPaused()); }
        public override bool _IsPlaybackRunning() { return _Available() && player.IsPlaying() && !player.IsPaused(); }
        public override float _GetVolume() { return audioManager == null ? -1f : audioManager.MasterVolume; }
        public override bool _GetMute() { return audioManager != null && audioManager.Mute; }
        public override int _SetVolume(float value)
        {
            if (NetworkCalling.InNetworkCall || !_Available() || audioManager == null) return 0;
            audioManager.MasterVolume = Mathf.Clamp01(value); return 1;
        }
        public override int _SetMute(bool value)
        {
            if (NetworkCalling.InNetworkCall || !_Available() || audioManager == null) return 0;
            audioManager.Mute = value; return 1;
        }
        public override bool _AcquireSequenceControl()
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return false;
            if (player.IsLock && !player.CanOperate(Networking.LocalPlayer)) return false;
            syncer.RequestOwnershipForUserAction();
            return Networking.IsOwner(gameObject) && Networking.IsOwner(syncer.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            if (!_AcquireSequenceControl()) return 2;
            player.LoadUrl(url); return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!_NativeReady()) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (stop) player.NowSelectedMediaModule.Pause(); else player.NowSelectedMediaModule.Play();
            return 1;
        }
        public override void _SetNativeMode(int mode)
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !Networking.IsOwner(syncer.gameObject)) return;
            player.SetLoopMode(mode == 2 ? LoopMode.Single : LoopMode.None);
        }
        public override bool _GetNativeRepeat() { return _Available() && player.LoopMode == LoopMode.Single; }
        public override float _GetProgress()
        {
            if (!_NativeReady()) return -1f;
            float duration = player.GetDuration();
            return duration <= 0f || float.IsInfinity(duration) || float.IsNaN(duration) ? -1f : Mathf.Clamp01(player.GetTime() / duration);
        }
    }
}
