using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.Components.Video;
using VRC.SDK3.UdonNetworkCalling;
using Kinel.VideoPlayer.V3.Udon.System;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KinelV3SequenceListener : KinelVideoListener
    {
        public KinelPlayerController player;
        public KinelV3Adapter adapter;
        public override void OnKinelVideoPause() { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void OnKinelSeek(float time) { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NotifyLocalStateChanged(); }
        private void Start() { if (player != null) player.AddListener(this); }
        public override void OnKinelVideoStart() { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NativeStarted(); }
        public override void OnKinelVideoPlay() { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NativeStarted(); }
        public override void OnKinelPostUrlInput(VRCUrl url) { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NativeLoading(); }
        public override void OnKinelVideoEnd() { if (!NetworkCalling.InNetworkCall && adapter != null && player != null && !player.IsReloading) adapter._NativeEnded(); }
        public override void OnKinelMediaReset() { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._CancelSequence(); }
        public override void OnKinelVideoError(VideoError error) { if (!NetworkCalling.InNetworkCall && adapter != null) adapter._NativeError(); }
    }
}
