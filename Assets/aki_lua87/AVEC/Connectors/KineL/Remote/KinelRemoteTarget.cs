using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using Kinel.VideoPlayer.Udon;
using Kinel.VideoPlayer.Udon.Module;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class KinelRemoteTarget : RemoteTarget
    {
        public KinelVideoPlayer player;
        public Kinel.VideoPlayer.Udon.Playlist.KinelPlaylist nativePlaylist;
        [HideInInspector] public int nativeVideoCount; // Editor-read native playlist size.
        public override bool _CanStep() { return nativePlaylist != null && nativeVideoCount > 0; }
        public override int _StepNative(int direction)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !_CanStep()) return 4;
            if (player.IsLock && !Networking.IsMaster) return 2;
            if (!Utilities.IsValid(Networking.LocalPlayer)) return 0;
            int index = nativePlaylist.NowPlayingFlag ? nativePlaylist.NowPlaylingItemIndex : -1;
            int next = index < 0 ? (direction < 0 ? nativeVideoCount - 1 : 0) : (index + (direction < 0 ? -1 : 1) + nativeVideoCount) % nativeVideoCount;
            nativePlaylist.PlayVideo(next); return 1;
        }
        // KineL's own volume UI writes AudioSource.volume with UnityEvents.
        public AudioSource[] audioSources = new AudioSource[0];
        public Slider[] volumeSliders = new Slider[0];
        public AudioModule[] muteModules = new AudioModule[0];
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy && player.GetVideoPlayerController() != null && audioSources.Length > 0; }
        public override float _GetVolume() { return audioSources.Length > 0 && audioSources[0] != null ? audioSources[0].volume : 0f; }
        public override void _SetVolume(float value)
        {
            if (NetworkCalling.InNetworkCall || player == null) return;
            for (int i = 0; i < audioSources.Length; i++) if (audioSources[i] != null) audioSources[i].volume = value;
            for (int i = 0; i < volumeSliders.Length; i++) if (volumeSliders[i] != null) volumeSliders[i].SetValueWithoutNotify(value);
        }
        public override bool _GetMute() { return audioSources.Length > 0 && audioSources[0] != null && audioSources[0].mute; }
        public override void _SetMute(bool value)
        {
            if (NetworkCalling.InNetworkCall || player == null) return;
            // Keep the native module's private toggle state and its icons coherent.
            for (int i = 0; i < muteModules.Length; i++)
            {
                AudioModule module = muteModules[i];
                if (module == null) continue;
                object current = module.GetProgramVariable("mute");
                if (current != null && (bool)current != value) module.ToggleMute();
            }
            for (int i = 0; i < audioSources.Length; i++) if (audioSources[i] != null) audioSources[i].mute = value;
        }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = player.IsPause ? 13 : 12;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && !player.IsPlaying && !player.IsPause) return 4;
            if (action == 17) {
                float duration = player.GetVideoPlayerController().GetCurrentVideoPlayer().GetDuration();
                if (duration <= 0 || float.IsInfinity(duration) || float.IsNaN(duration)) return 4;
            }
            if (player.IsLock && !Networking.IsMaster) return 2;
            // KineL.Play resets the clock when called during playback.
            if ((action == 13 && player.IsPlaying) || (action == 12 && player.IsPause)) return 1;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            if (action == 16 || action == 19) { player.Loop = action == 19 || !player.Loop; player.RequestSerialization(); }
            else if (action == 17) { player.SetVideoTime(0f); player.Play(); }
            else if (action == 8) player.ResetGlobal();
            else if (action == 12) player.Pause();
            else player.Play();
            return 1;
        }
    }
}
