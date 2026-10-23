using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using aki_lua87.AVEC;
using aki_lua87.CommonVideoPlaylist;
namespace aki_lua87.AVEC.Extensions.DynamicPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicPlaylistTitles : FeatureExtensionBehaviour
    {
        public PlaylistView view;
        private bool[] titleRequests;
        private float[] nextTitleRequests;
        private string[] originalTitles;
        public override void _OnSelectionChanged(int tab)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || view == null || view.titleSourceUrls == null || tab < 0 || tab >= view.titleSourceUrls.Length || VRCUrl.IsNullOrEmpty(view.titleSourceUrls[tab])) return;
            if (titleRequests == null) { titleRequests = new bool[view.titleSourceUrls.Length]; nextTitleRequests = new float[view.titleSourceUrls.Length]; originalTitles = (string[])view.titles.Clone(); }
            if (titleRequests[tab] || Time.realtimeSinceStartup < nextTitleRequests[tab]) return;
            if (view.loadingTabs == null) view.loadingTabs = new bool[view.titleSourceUrls.Length];
            view.loadingTabs[tab] = true; view._RefreshTabInfo();
            titleRequests[tab] = true; nextTitleRequests[tab] = Time.realtimeSinceStartup + 5f;
            VRCStringDownloader.LoadUrl(view.titleSourceUrls[tab], (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
        }
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            CompleteTitles(result.Url, result.Result, "");
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            CompleteTitles(result.Url, "", result.Error);
        }

        private void CompleteTitles(VRCUrl source, string json, string error)
        {
            if (titleRequests == null || VRCUrl.IsNullOrEmpty(source)) return;
            DataToken root = new DataToken();
            DataToken list = new DataToken();
            DataToken urls = new DataToken();
            bool valid = string.IsNullOrEmpty(error) && VRCJson.TryDeserializeFromJson(json, out root);
            if (valid) valid = root.TokenType == TokenType.DataDictionary && root.DataDictionary.TryGetValue("titles", TokenType.DataList, out list) && root.DataDictionary.TryGetValue("urls", TokenType.DataList, out urls) && list.DataList.Count == urls.DataList.Count;
            if (valid)
            {
                for (int i = 0; i < list.DataList.Count; i++)
                {
                    DataToken item;
                    DataToken url;
                    if (!urls.DataList.TryGetValue(i, TokenType.String, out url) || (!url.String.StartsWith("https://") && !url.String.StartsWith("http://")) || !list.DataList.TryGetValue(i, TokenType.String, out item)) { valid = false; break; }
                }
            }
            for (int tab = 0; tab < titleRequests.Length; tab++)
            {
                if (!titleRequests[tab] || VRCUrl.IsNullOrEmpty(view.titleSourceUrls[tab]) || view.titleSourceUrls[tab].Get() != source.Get()) continue;
                titleRequests[tab] = false;
                if (!valid)
                {
                    if (view.loadingTabs != null) view.loadingTabs[tab] = false;
                    view._RefreshTabInfo();
                    Debug.LogWarning("[CVP] Title download failed: " + source.Get() + " " + error);
                    if (view._SelectedTab() == tab) view._SequenceStatus("タイトルを取得できませんでした。タブを再選択して再試行してください（Allow Untrusted URLsを確認）。");
                    continue;
                }
                for (int i = 0; i < view.titles.Length; i++)
                {
                    if (i >= view.videoTabs.Length || view.videoTabs[i] != tab || i >= view.dynamicTitleIndices.Length) continue;
                    int position = view.dynamicTitleIndices[i];
                    if (position < 0) continue;
                    // A shorter response must not leave a previous video's title on a reused slot.
                    string title = originalTitles[i];
                    if (view.hiddenEntries == null || view.hiddenEntries.Length != view.titles.Length) view.hiddenEntries = new bool[view.titles.Length];
                    view.hiddenEntries[i] = position >= list.DataList.Count || string.IsNullOrEmpty(list.DataList[position].String);
                    if (position < list.DataList.Count && !string.IsNullOrEmpty(list.DataList[position].String)) title = list.DataList[position].String;
                    if (view.resolvedUrls == null || view.resolvedUrls.Length != view.titles.Length) view.resolvedUrls = new string[view.titles.Length];
                    view.resolvedUrls[i] = position < urls.DataList.Count ? urls.DataList[position].String : "";
                    view.titles[i] = title;
                    if (i < view.videoLabels.Length && view.videoLabels[i] != null) view.videoLabels[i].text = title;
                }
                if (view.loadingTabs != null) view.loadingTabs[tab] = false;
                view._RefreshTabInfo(); view._RefreshVisibleEntries();
            }
        }


    }
}
