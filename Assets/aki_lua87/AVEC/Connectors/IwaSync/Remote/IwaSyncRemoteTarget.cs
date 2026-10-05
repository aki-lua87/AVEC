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
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy; }
        public override float _GetVolume() { return player == null ? 0f : player.volume; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && player != null) player.volume = value; }
        public override bool _GetMute() { return player != null && player.muted; }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && player != null) player.muted = value; }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action != 8 && action != 12 && action != 13) return 5;
            if (player.locked && !Networking.IsMaster && !(player.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner)) return 2;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            if (action == 8) player.Stop();
            else if (action == 12) player.Pause();
            else player.Play();
            player.RequestSerialization();
            return 1;
        }
    }
}
