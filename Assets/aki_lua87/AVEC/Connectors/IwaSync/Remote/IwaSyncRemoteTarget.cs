using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using HoshinoLabs.IwaSync3.Udon;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class IwaSyncRemoteTarget : RemoteTarget
    {
        public VideoCore player;
        public HoshinoLabs.IwaSync3.Udon.Playlist nativePlaylist;
        // Native public UI sender protocol: the clicked row temporarily disables its Button.
        [UnityEngine.HideInInspector] public UnityEngine.UI.Button[] nativeButtons = new UnityEngine.UI.Button[0];
        [UnityEngine.HideInInspector] public VRCUrl[] nativeUrls = new VRCUrl[0];
        public override bool _CanStep() { return nativePlaylist != null && nativeUrls.Length > 0 && nativeButtons.Length == nativeUrls.Length; }
        public override int _StepNative(int direction)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !_CanStep() || !Utilities.IsValid(Networking.LocalPlayer)) return 4;
            if (player.locked && !Networking.IsMaster && !(player.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner)) return 2;
            int current = -1;
            for (int i = 0; i < nativeUrls.Length; i++) if (!VRCUrl.IsNullOrEmpty(player.url) && !VRCUrl.IsNullOrEmpty(nativeUrls[i]) && nativeUrls[i].Get() == player.url.Get()) { current = i; break; }
            int next = current < 0 ? (direction < 0 ? nativeUrls.Length - 1 : 0) : (current + (direction < 0 ? -1 : 1) + nativeUrls.Length) % nativeUrls.Length;
            if (nativeButtons[next] == null) return 4;
            for (int i = 0; i < nativeButtons.Length; i++) if (nativeButtons[i] != null && !nativeButtons[i].enabled) return 4;
            nativeButtons[next].enabled = false; nativePlaylist.OnButtonClicked(); nativeButtons[next].enabled = true;
            return 1;
        }
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy; }
        public override float _GetVolume() { return player == null ? 0f : UnityEngine.Mathf.InverseLerp(player.minVolume, player.maxVolume, player.volume); }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && player != null) player.volume = UnityEngine.Mathf.Lerp(player.minVolume, player.maxVolume, UnityEngine.Mathf.Clamp01(value)); }
        public override bool _GetMute() { return player != null && player.muted; }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && player != null) player.muted = value; }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = player.paused ? 13 : 12;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && !player.isPlaying) return 4;
            if (action == 17 && (player.isLive || player.duration <= 0f || !player.allowSeeking)) return 4;
            if (player.locked && !Networking.IsMaster && !(player.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner)) return 2;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            if (action == 16 || action == 19) player.loop = action == 19 || !player.loop;
            else if (action == 17) { player.clockTime = Networking.GetServerTimeInMilliseconds(); player.time = 0f; player.Play(); }
            else if (action == 8) player.Stop();
            else if (action == 12) player.Pause();
            else player.Play();
            player.RequestSerialization();
            return 1;
        }
    }
}
