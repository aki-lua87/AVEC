using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common;

namespace aki_lua87.CommonVideoPlaylist
{
    // One stable PlayerObject template per scene. Never move/recreate it after publication:
    // VRChat identifies persisted behaviours by their Network IDs.
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlaylistUserData : UdonSharpBehaviour
    {
        // Editor wiring must survive into the world; only the local player's copy binds these UIs.
        [HideInInspector] public PlaylistView[] views = new PlaylistView[0];
        [UdonSynced] private VRCUrl[] savedUrls = new VRCUrl[0];
        [UdonSynced] private string[] savedTitles = new string[0];
        [UdonSynced] private int[] savedIds = new int[0];
        [UdonSynced] private int nextId = 1;
        private bool restored;
        private bool retryScheduled;
        private bool storageExceeded;

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal || !Networking.IsOwner(player, gameObject)) return;
            restored = true;
            ApplyLocal();
        }
        public override void OnDeserialization() { ApplyLocal(); }
        private void ApplyLocal()
        {
            if (!restored || !Networking.IsOwner(gameObject)) return;
            for (int i = 0; i < views.Length; i++)
                if (views[i] != null) { views[i].localUserData = this; views[i]._RefreshUserList(); }
        }
        public bool _Ready() { return restored && Networking.IsOwner(gameObject); }
        public int _Count() { return savedUrls == null ? 0 : savedUrls.Length; }
        public VRCUrl _Url(int index) { return index >= 0 && index < _Count() ? savedUrls[index] : VRCUrl.Empty; }
        public string _Title(int index)
        {
            if (index < 0 || index >= _Count()) return "";
            return savedTitles != null && index < savedTitles.Length && !string.IsNullOrEmpty(savedTitles[index])
                ? savedTitles[index] : VRCUrl.IsNullOrEmpty(savedUrls[index]) ? "" : savedUrls[index].Get();
        }
        // 1 added; 3 invalid; 5 full; 6 restoring; 8 duplicate; 9 storage budget.
        public int _Add(VRCUrl source, PlaylistTitleResolver resolver)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 6;
            if (VRCUrl.IsNullOrEmpty(source)) return 3;
            string raw = source.Get();
            if ((!raw.StartsWith("https://") && !raw.StartsWith("http://")) || raw.Length > 2048) return 3;
            int count = _Count();
            for (int i = 0; i < count; i++) if (!VRCUrl.IsNullOrEmpty(savedUrls[i]) && savedUrls[i].Get() == raw) return 8;
            if (count >= 32) return 5;
            // Conservative uncompressed budget leaves room for other persistent world features.
            int budget = 1024 + raw.Length * 4 + 1024;
            for (int i = 0; i < count; i++) budget += (VRCUrl.IsNullOrEmpty(savedUrls[i]) ? 0 : savedUrls[i].Get().Length * 4) + 1024;
            if (storageExceeded || budget > 48 * 1024) return 9;
            var urls = new VRCUrl[count + 1]; var titles = new string[count + 1]; var ids = new int[count + 1];
            for (int i = 0; i < count; i++) { urls[i] = savedUrls[i]; titles[i] = savedTitles != null && i < savedTitles.Length ? savedTitles[i] : ""; ids[i] = savedIds != null && i < savedIds.Length ? savedIds[i] : 0; }
            int id = nextId; nextId++;
            urls[count] = source; titles[count] = ""; ids[count] = id;
            savedUrls = urls; savedTitles = titles; savedIds = ids;
            Commit();
            if (resolver != null) resolver._Resolve(source, this, id);
            return 1;
        }
        public int _Remove(int index)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 6;
            int count = _Count(); if (index < 0 || index >= count) return 3;
            var urls = new VRCUrl[count - 1]; var titles = new string[count - 1]; var ids = new int[count - 1];
            int target = 0;
            for (int i = 0; i < count; i++)
            {
                if (i == index) continue;
                urls[target] = savedUrls[i]; titles[target] = savedTitles != null && i < savedTitles.Length ? savedTitles[i] : "";
                ids[target] = savedIds != null && i < savedIds.Length ? savedIds[i] : 0; target++;
            }
            savedUrls = urls; savedTitles = titles; savedIds = ids;
            storageExceeded = false;
            Commit(); return 1;
        }
        public void _SetTitle(int id, VRCUrl source, string value)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || storageExceeded || VRCUrl.IsNullOrEmpty(source) || string.IsNullOrEmpty(value)) return;
            if (value.Length > 256) value = value.Substring(0, 256);
            for (int i = 0; i < _Count(); i++)
            {
                if (savedIds == null || i >= savedIds.Length || savedIds[i] != id || VRCUrl.IsNullOrEmpty(savedUrls[i]) || savedUrls[i].Get() != source.Get()) continue;
                if (savedTitles == null || savedTitles.Length != _Count()) savedTitles = new string[_Count()];
                if (savedTitles[i] == value) return;
                savedTitles[i] = value; Commit(); return;
            }
        }
        private void Commit() { ApplyLocal(); RequestSerialization(); }
        public override void OnPostSerialization(SerializationResult result)
        {
            if (!_Ready() || result.success || retryScheduled) return;
            retryScheduled = true; SendCustomEventDelayedSeconds(nameof(_RetrySave), 2f);
        }
        public void _RetrySave()
        {
            retryScheduled = false;
            if (NetworkCalling.InNetworkCall || !_Ready() || storageExceeded) return;
            RequestSerialization();
        }
        public override void OnPlayerObjectStorageExceeded(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal || !_Ready()) return;
            storageExceeded = true;
            for (int i = 0; i < views.Length; i++) if (views[i] != null && views[i].enableUserList)
                views[i]._SequenceStatus("永続化の保存容量を超えています。保存リストを削除して容量を空けてください。");
        }
    }
}
