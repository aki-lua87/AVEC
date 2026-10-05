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
            if (action != 8 && action != 12 && action != 13) return 5;
            if (player.IsLock && !Networking.IsMaster) return 2;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            if (action == 8) player.ResetGlobal();
            else if (action == 12) player.Pause();
            else player.Play();
            return 1;
        }
    }
}
