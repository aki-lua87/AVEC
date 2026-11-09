using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistBaker
    {
        // Saving playlist data must not rebuild the hierarchy (including thousands of Udon objects).
        sealed class Data
        {
            public readonly List<string> names = new List<string>(), descriptions = new List<string>(), titles = new List<string>();
            public readonly List<int> tabs = new List<int>(), indices = new List<int>();
            public readonly List<bool> random = new List<bool>();
            public readonly List<VRCUrl> urls = new List<VRCUrl>(), sources = new List<VRCUrl>();
            public Data(PlaylistData data)
            {
                if (data?.tabs == null) return;
                for (int i = 0; i < data.tabs.Length; i++)
                {
                    var tab = data.tabs[i];
                    names.Add(tab == null ? "Untitled" : tab.name);
                    descriptions.Add(tab?.description ?? "");
                    random.Add(tab != null && tab.randomPlayback);
                    sources.Add(new VRCUrl(tab?.titleSourceUrl?.Trim() ?? ""));
                    if (tab?.videos == null) continue;
                    foreach (var entry in tab.videos)
                    {
                        tabs.Add(i); titles.Add(entry?.title ?? "Untitled");
                        urls.Add(new VRCUrl(entry?.url?.Trim() ?? ""));
                        indices.Add(entry == null ? -1 : entry.dynamicTitleIndex);
                    }
                }
            }
            public bool Matches(PlaylistView view)
            {
                return Equal(descriptions, view.tabDescriptions) && Equal(names, view.tabNames) && Equal(random, view.randomTabs) &&
                    Equal(tabs, view.videoTabs) && Equal(titles, view.titles) && Equal(indices, view.dynamicTitleIndices) &&
                    EqualUrls(urls, view.urls) && EqualUrls(sources, view.titleSourceUrls) &&
                    view.tabImages != null && view.tabImages.Length == names.Count &&
                    view.videoImages != null && view.videoImages.Length == titles.Count &&
                    view.videoLabels != null && view.videoLabels.Length == titles.Count;
            }
        }
        static bool Equal<T>(List<T> expected, T[] actual) => actual != null && expected.SequenceEqual(actual);
        static bool EqualUrls(List<VRCUrl> expected, VRCUrl[] actual) => actual != null &&
            expected.Count == actual.Length && expected.Select(u => u.Get()).SequenceEqual(actual.Select(u => u?.Get() ?? ""));

        public static bool DataMatches(PlaylistSetup setup) => setup != null && setup.view != null && new Data(setup.data).Matches(setup.view);

        public static void ValidateDataRows(PlaylistSetup setup)
        {
            if (setup.view == null || setup.tabContent == null || setup.videoContent == null)
                throw new InvalidOperationException("Playlist UI references are missing: " + setup.name);
            ValidateRows(setup.view.tabImages, setup.tabContent);
            ValidateRows(setup.view.videoImages, setup.videoContent);
        }
        static void ValidateRows(Image[] rows, Transform parent)
        {
            if (rows == null) return;
            var seen = new HashSet<Image>();
            foreach (var row in rows)
                if (row == null || row.transform.parent != parent || !seen.Add(row) ||
                    row.GetComponentInChildren<Text>(true) == null || row.GetComponent<PlaylistButton>() == null)
                    throw new InvalidOperationException("Playlist row references need repair: " + parent.name);
        }
        static void RecordUdon(UdonSharp.UdonSharpBehaviour proxy)
        {
            PlaylistUndo.RecordObject(proxy, "Update playlist data");
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
            if (backing != null) PlaylistUndo.RecordObject(backing, "Update playlist data");
        }
        static void SetLabel(Text label, string title)
        {
            if (label.text == title) return;
            PlaylistUndo.RecordObject(label, "Update playlist title");
            label.text = title; PlaylistConnections.Save(label);
        }
        static Image AddDataRow(PlaylistSetup setup, List<Image> rows, string title, int index, bool tab)
        {
            if (rows.Count == 0)
            {
                var first = Row(setup, tab ? setup.tabTemplate : setup.videoTemplate, tab ? setup.tabContent : setup.videoContent, title, index, tab);
                if (!tab) first.GetComponentInChildren<Text>(true).supportRichText = false;
                return first;
            }
            // Clone a styled row so newly appended entries retain the user's UI adjustments.
            var go = UnityEngine.Object.Instantiate(rows[rows.Count - 1].gameObject, tab ? setup.tabContent : setup.videoContent, false);
            go.name = (tab ? "Tab " : "Video ") + index;
            PlaylistUndo.RegisterCreatedObjectUndo(go, "Add playlist row");
            foreach (var click in go.GetComponentsInChildren<PlaylistButton>(true))
            {
                click.view = setup.view; click.index = index;
                PlaylistConnections.Save(click);
                var button = click.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                PlaylistConnections.Save(button);
            }
            go.GetComponentInChildren<Text>(true).text = title;
            if (tab) { go.SetActive(true); go.GetComponent<Image>().color = setup.view.normalColor; }
            return go.GetComponent<Image>();
        }
        static void TrimRows(List<Image> rows, int count)
        {
            for (int i = rows.Count - 1; i >= count; i--)
            {
                PlaylistUndo.DestroyObjectImmediate(rows[i].gameObject);
                rows.RemoveAt(i);
            }
        }
        public static void ApplyData(PlaylistSetup setup, bool recordUndo = true)
        {
            using (new PlaylistUndo.Scope(recordUndo)) ApplyDataCore(setup);
        }
        static void ApplyDataCore(PlaylistSetup setup)
        {
            ValidateDataRows(setup);
            PlaylistVirtualization.Ensure(setup.view);
            var data = new Data(setup.data); var view = setup.view;
            PlaylistQueueUI.HideLegacyRowButtons(view);
            if (data.Matches(view)) return;
            RecordUdon(view);
            var tabRows = new List<Image>(view.tabImages ?? new Image[0]);
            var videoRows = new List<Image>(view.videoImages ?? new Image[0]);
            bool structureChanged = tabRows.Count != data.names.Count || videoRows.Count != data.titles.Count;
            int selected = tabRows.FindIndex(row => row.color == view.selectedColor);
            if (selected < 0) selected = 0;
            selected = Mathf.Min(selected, data.names.Count - 1);
            int oldTabCount = tabRows.Count, oldVideoCount = videoRows.Count;
            TrimRows(tabRows, data.names.Count); TrimRows(videoRows, data.titles.Count);
            while (tabRows.Count < data.names.Count)
                tabRows.Add(AddDataRow(setup, tabRows, data.names[tabRows.Count], tabRows.Count, true));
            while (videoRows.Count < data.titles.Count)
                videoRows.Add(AddDataRow(setup, videoRows, data.titles[videoRows.Count], videoRows.Count, false));
            for (int i = 0; i < tabRows.Count; i++)
            {
                SetLabel(tabRows[i].GetComponentInChildren<Text>(true), data.names[i]);
                if (i >= oldTabCount) PlaylistConnections.Save(tabRows[i]);
                if (i >= oldTabCount || view.titleSourceUrls == null || i >= view.titleSourceUrls.Length ||
                    (view.titleSourceUrls[i]?.Get() ?? "") != data.sources[i].Get())
                    PlaylistTabIcons.Ensure(setup, tabRows[i], VRCUrl.IsNullOrEmpty(data.sources[i]) ? "tab-static" : "tab-dynamic");
            }
            var labels = new List<Text>(); var progress = new List<Image>();
            for (int i = 0; i < videoRows.Count; i++)
            {
                var row = videoRows[i]; var label = row.GetComponentInChildren<Text>(true);
                SetLabel(label, data.titles[i]); labels.Add(label);
                if (i >= oldVideoCount || view.videoTabs == null || i >= view.videoTabs.Length || view.videoTabs[i] != data.tabs[i])
                {
                    PlaylistUndo.RecordObject(row.gameObject, "Update playlist row visibility");
                    row.gameObject.SetActive(data.tabs[i] == selected);
                    PlaylistConnections.Save(row.gameObject);
                }
                progress.Add(PlaylistEnhancements.Progress(row.transform, view));
            }
            view.tabDescriptions = data.descriptions.ToArray();
            view.tabNames = data.names.ToArray(); view.randomTabs = data.random.ToArray();
            view.titles = data.titles.ToArray(); view.urls = data.urls.ToArray(); view.videoTabs = data.tabs.ToArray();
            view.titleSourceUrls = data.sources.ToArray(); view.dynamicTitleIndices = data.indices.ToArray();
            view.tabImages = tabRows.ToArray(); view.videoImages = videoRows.ToArray(); view.videoLabels = labels.ToArray();
            view.videoProgress = progress.ToArray();
            PlaylistVirtualization.Ensure(view);
            if (structureChanged)
            {
                view.queueAddButtons = setup.GetComponentsInChildren<PlaylistButton>(true).Where(b => b.enqueue).Select(b => b.gameObject).ToArray();
                PlaylistQueueUI.HideLegacyRowButtons(view);
            }
            // Extensions can update their Udon backing objects as well as the visible proxies.
            foreach (var extension in view.GetComponents<aki_lua87.AVEC.FeatureExtensionBehaviour>())
                if (extension != null) RecordUdon(extension);
            aki_lua87.AVEC.Editor.FeatureExtensionRegistry.Bake("Playlist", setup);
            PlaylistConnections.Save(view);
            if (structureChanged)
            {
                var touch = setup.GetComponentInParent<PlaylistTabletTouch>();
                if (touch != null) RecordUdon(touch);
                PlaylistTabletBuilder.RefreshBindings(setup);
            }
        }
        public static void Bake(PlaylistSetup setup)
        {
            if (setup.view == null || setup.tabContent == null || setup.videoContent == null || setup.tabTemplate == null || setup.videoTemplate == null)
                throw new InvalidOperationException("Playlist UI references are missing: " + setup.name);
            PlaylistUndo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Bake playlist");
            PlaylistFactory.EnsurePlaybackModeButton(setup);
            Clear(setup.tabContent); Clear(setup.videoContent);
            var names = new List<string>(); var descriptions = new List<string>(); var tabs = new List<int>(); var titles = new List<string>(); var urls = new List<VRCUrl>();
            var tabImages = new List<Image>(); var videoImages = new List<Image>();
            var titleSources = new List<VRCUrl>(); var titleIndices = new List<int>(); var labels = new List<Text>();
            var randomTabs = new List<bool>();
            if (setup.data != null && setup.data.tabs != null)
                for (int i = 0; i < setup.data.tabs.Length; i++)
                {
                    var tab = setup.data.tabs[i];
                    names.Add(tab == null ? "Untitled" : tab.name);
                    descriptions.Add(tab?.description ?? "");
                    randomTabs.Add(tab != null && tab.randomPlayback);
                    titleSources.Add(new VRCUrl(tab?.titleSourceUrl?.Trim() ?? ""));
                    tabImages.Add(Row(setup, setup.tabTemplate, setup.tabContent, names[i], i, true));
                    if (tab?.videos == null) continue;
                    foreach (var entry in tab.videos)
                    {
                        int index = urls.Count;
                        string title = entry?.title ?? "Untitled";
                        string url = entry?.url?.Trim() ?? "";
                        tabs.Add(i); titles.Add(title); urls.Add(new VRCUrl(url));
                        titleIndices.Add(entry == null ? -1 : entry.dynamicTitleIndex);
                        var row = Row(setup, setup.videoTemplate, setup.videoContent, title, index, false);
                        row.gameObject.SetActive(i == 0);
                        videoImages.Add(row);
                        var label = row.GetComponentInChildren<Text>(true);
                        label.supportRichText = false;
                        labels.Add(label);
                    }
                }
            var view = setup.view;
            PlaylistQueueUI.Ensure(setup);
            view.tabDescriptions = descriptions.ToArray();
            view.tabNames = names.ToArray(); view.videoTabs = tabs.ToArray(); view.titles = titles.ToArray(); view.urls = urls.ToArray();
            view.randomTabs = randomTabs.ToArray();
            view.tabImages = tabImages.ToArray(); view.videoImages = videoImages.ToArray();
            view.titleSourceUrls = titleSources.ToArray(); view.dynamicTitleIndices = titleIndices.ToArray(); view.videoLabels = labels.ToArray();
            PlaylistVirtualization.Ensure(view);
            PlaylistEnhancements.Apply(setup);
            PlaylistPresentation.Apply(setup);
            PlaylistQueueUI.RefreshLongTitleClipping(view);
            PlaylistQueueUI.EnsureRequestedPresentation(view);
            aki_lua87.AVEC.Editor.FeatureExtensionRegistry.Bake("Playlist", setup);
            // Data updates preserve manually tuned UI colors and layout.
            if (view.heading != null) view.heading.text = "再生キュー";
            if (view.queueTabImage != null) view.queueTabImage.color = view.selectedColor;
            if (view.queuePanel != null) view.queuePanel.SetActive(true);
            if (view.queueAddPanel != null) view.queueAddPanel.SetActive(false);
            if (view.videoScroll != null) view.videoScroll.gameObject.SetActive(false);
            PlaylistSpecialLists.Apply(setup);
            PlaylistQueueUI.RefreshQueueControlLayout(view);
            PlaylistConnections.Save(view);
            PlaylistConnections.Save(setup);
            // Also migrate existing connections to the shared, owner-aligned sequence adapter.
            if (setup.player != null && PlaylistConnections.Validate(setup) != null) PlaylistConnections.Connect(setup, setup.player);
            if(view.status!=null)view.status.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            setup.tabContent.anchoredPosition=Vector2.zero;setup.videoContent.anchoredPosition=Vector2.zero;
            foreach(var scroll in setup.GetComponentsInChildren<ScrollRect>(true))
            {
                scroll.StopMovement();scroll.verticalNormalizedPosition=1;
                if(scroll.verticalScrollbar==null||scroll.content==null)continue;
                var layout=scroll.content.GetComponent<VerticalLayoutGroup>();float height=layout==null?0:layout.padding.vertical;int count=0;
                foreach(Transform child in scroll.content)if(child.gameObject.activeSelf){height+=LayoutUtility.GetPreferredHeight(child.GetComponent<RectTransform>());count++;}
                if(layout!=null)height+=Mathf.Max(0,count-1)*layout.spacing;
                bool needed=height>scroll.GetComponent<RectTransform>().rect.height;
                scroll.verticalScrollbar.gameObject.SetActive(needed);
                scroll.verticalScrollbar.SetValueWithoutNotify(needed?1f:0f);
                PlaylistConnections.Save(scroll.verticalScrollbar);PlaylistConnections.Save(scroll.verticalScrollbar.gameObject);PlaylistConnections.Save(scroll.content);
            }
            PlaylistConnections.Save(view);
            PlaylistTabletBuilder.RefreshBindings(setup);
        }
        static void Clear(Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--) {
                if (content.GetChild(i).name == "Queue toggle" || content.GetChild(i).name == "History toggle" || content.GetChild(i).name == "User list toggle") continue;
                PlaylistUndo.DestroyObjectImmediate(content.GetChild(i).gameObject);
            }
        }
        static Image Row(PlaylistSetup setup, GameObject template, Transform parent, string label, int index, bool tab)
        {
            var row = UnityEngine.Object.Instantiate(template, parent, false);
            row.name = (tab ? "Tab " : "Video ") + index;
            PlaylistUndo.RegisterCreatedObjectUndo(row, "Create playlist row");
            row.SetActive(true);
            row.GetComponentInChildren<Text>(true).text = label;
            if (tab) PlaylistAppearance.FitTabLabel(row);
            var click = row.AddUdonSharpComponent<PlaylistButton>();
            click.view = setup.view; click.index = index; click.isTab = tab;
            UdonSharpEditorUtility.CopyProxyToUdon(click);
            var button = row.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
            var image = row.GetComponent<Image>();
            image.color = setup.view.normalColor;
            return image;
        }
        public static void BakeLoaded()
        {
            PlaylistRemoteToggle.EnsureLoaded();
            foreach (var setup in UnityEngine.Object.FindObjectsOfType<PlaylistSetup>(true))
                if (setup.gameObject.scene.IsValid() && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null)
                {
                    PlaylistConnections.AutoConnect(setup);
                    PlaylistControlTips.Ensure(setup);
                    if (setup.player != null && PlaylistConnections.Validate(setup) != null) PlaylistConnections.Connect(setup, setup.player);
                    if (setup.view != null) RecordUdon(setup.view);
                    ApplyData(setup, false);
                }
        }
    }
    [InitializeOnLoad]
    public static class PlaylistPlayBake
    {
        static PlaylistPlayBake() { EditorApplication.playModeStateChanged += OnPlay; }
        static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { PlaylistBaker.BakeLoaded(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.isPlaying = false; }
        }
    }
    public class PlaylistBuildBake : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => -100;
        public bool OnBuildRequested(VRCSDKRequestedBuildType type)
        {
            if (type != VRCSDKRequestedBuildType.Scene) return true;
            try {
                PlaylistBaker.BakeLoaded();
                foreach (var setup in UnityEngine.Object.FindObjectsOfType<PlaylistSetup>(true))
                {
                    if (setup.player == null && setup.view != null && setup.view.adapter == null) continue;
                    string problem = PlaylistConnections.Validate(setup);
                    if (problem != null) { Debug.LogError("[CVP] " + setup.name + ": " + problem, setup); return false; }
                }
                return true;
            }
            catch (Exception e) { Debug.LogException(e); return false; }
        }
    }
}
