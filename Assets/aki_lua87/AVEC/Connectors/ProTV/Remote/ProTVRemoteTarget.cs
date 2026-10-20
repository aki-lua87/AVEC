using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using ArchiTech.ProTV;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ProTVRemoteTarget : RemoteTarget
    {
        public TVManager player;
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy && player.isReady; }
        public override float _GetVolume() { return _Ready() ? player.volume : -1f; }
        public override bool _GetMute() { return _Ready() && player.mute; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && _Ready() && !player.syncVolumeControl) player.ChangeVolume(UnityEngine.Mathf.Clamp01(value)); }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && _Ready()) player.ChangeMute(value); }
        public override int _Execute(int action, float amount)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if ((action == 3 || action == 4) && player.syncVolumeControl) return 2;
            return base._Execute(action, amount);
        }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = player.IsPlaying ? 12 : 13;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && (player.IsStopped || player.IsLoadingMedia)) return 4;
            if (action == 17 && (player.isLive || player.endTime <= player.startTime)) return 4;
            if (!player.CanPlayMedia || (action == 17 && !player.CanSeekMedia)) return 2;
            if (player.syncToOwner) {
                Networking.SetOwner(Networking.LocalPlayer, player.gameObject);
                if (!Networking.IsOwner(player.gameObject)) return 2;
            }
            if (action == 16 || action == 19) player.ChangeLoop(action == 19 || player.loop == 0);
            else if (action == 17) { player.ChangeSeekPercent(0f); player._Play(); }
            else if (action == 8) player._Stop();
            else if (action == 12) player._Pause();
            else player._Play();
            return 1;
        }
    }
}
