using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using VVMW.ThirdParties.Yttl;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VizVidPlaylistTitles : PlaylistTitleResolver
    {
        public YttlManager manager;
        // YTTL writes these exact field names before calling Yttl_OnDataLoaded.
        [System.NonSerialized] public VRCUrl url;
        [System.NonSerialized] public string title;
        [System.NonSerialized] public string author;
        [System.NonSerialized] public string viewCount;
        [System.NonSerialized] public string description;
        private VRCUrl[] pendingUrls = new VRCUrl[32];
        private PlaylistUserData[] destinations = new PlaylistUserData[32];
        private int[] ids = new int[32];
        private float[] expires = new float[32];

        public override void _Resolve(VRCUrl source, PlaylistUserData destination, int entryId)
        {
            if (NetworkCalling.InNetworkCall || manager == null || destination == null || !destination._Ready() || VRCUrl.IsNullOrEmpty(source)) return;
            for (int i = 0; i < pendingUrls.Length; i++)
            {
                if (destinations[i] != null && Time.realtimeSinceStartup < expires[i]) continue;
                pendingUrls[i] = source; destinations[i] = destination; ids[i] = entryId; expires[i] = Time.realtimeSinceStartup + 60f;
                // Register first: cached results invoke the callback synchronously.
                manager.LoadData(source, this); return;
            }
        }
        public void Yttl_OnDataLoaded()
        {
            if (NetworkCalling.InNetworkCall || VRCUrl.IsNullOrEmpty(url)) return;
            for (int i = 0; i < pendingUrls.Length; i++)
            {
                if (destinations[i] == null || VRCUrl.IsNullOrEmpty(pendingUrls[i]) || pendingUrls[i].Get() != url.Get()) continue;
                var destination = destinations[i]; int id = ids[i]; destinations[i] = null;
                // Entry ID prevents a delayed response from updating a deleted/re-added URL.
                destination._SetTitle(id, url, title);
            }
        }
    }
}
