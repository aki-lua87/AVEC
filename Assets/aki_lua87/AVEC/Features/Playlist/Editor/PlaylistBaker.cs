using System;
using System.Collections.Generic;
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
        public static void Bake(PlaylistSetup setup)
        {
            if (setup.view == null || setup.tabContent == null || setup.videoContent == null || setup.tabTemplate == null || setup.videoTemplate == null)
                throw new InvalidOperationException("Playlist UI references are missing: " + setup.name);
            Undo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Bake playlist");
            PlaylistFactory.EnsurePlaybackModeButton(setup);
            Clear(setup.tabContent); Clear(setup.videoContent);
            var names = new List<string>(); var tabs = new List<int>(); var titles = new List<string>(); var urls = new List<VRCUrl>();
            var tabImages = new List<Image>(); var videoImages = new List<Image>();
            var titleSources = new List<VRCUrl>(); var titleIndices = new List<int>(); var labels = new List<Text>();
            var randomTabs = new List<bool>();
            if (setup.data != null && setup.data.tabs != null)
                for (int i = 0; i < setup.data.tabs.Length; i++)
                {
                    var tab = setup.data.tabs[i];
                    names.Add(tab == null ? "Untitled" : tab.name);
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
            PlaylistConnections.RefreshAudioLinkStatus(setup);
            view.tabNames = names.ToArray(); view.videoTabs = tabs.ToArray(); view.titles = titles.ToArray(); view.urls = urls.ToArray();
            view.randomTabs = randomTabs.ToArray();
            view.tabImages = tabImages.ToArray(); view.videoImages = videoImages.ToArray();
            view.titleSourceUrls = titleSources.ToArray(); view.dynamicTitleIndices = titleIndices.ToArray(); view.videoLabels = labels.ToArray();
            PlaylistEnhancements.Apply(setup);
            PlaylistPresentation.Apply(setup);
            aki_lua87.AVEC.Editor.FeatureExtensionRegistry.Bake("Playlist", setup);
            // Data updates preserve manually tuned UI colors and layout.
            if (view.heading != null) view.heading.text = "再生キュー";
            if (view.queueTabImage != null) view.queueTabImage.color = view.selectedColor;
            if (view.queuePanel != null) view.queuePanel.SetActive(true);
            if (view.queueAddPanel != null) view.queueAddPanel.SetActive(false);
            if (view.videoScroll != null) view.videoScroll.gameObject.SetActive(false);
            PlaylistConnections.Save(view);
            PlaylistConnections.Save(setup);
            // Also migrate existing connections to the shared, owner-aligned sequence adapter.
            if (setup.player != null && PlaylistConnections.Validate(setup) != null) PlaylistConnections.Connect(setup, setup.player);
            if(view.status!=null)view.status.text=view.adapter==null?"接続先を設定してください":"動画を追加してください";
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
        }
        static void Clear(Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--) {
                if (content.GetChild(i).name == "Queue toggle" || content.GetChild(i).name == "History toggle") continue;
                Undo.DestroyObjectImmediate(content.GetChild(i).gameObject);
            }
        }
        static Image Row(PlaylistSetup setup, GameObject template, Transform parent, string label, int index, bool tab)
        {
            var row = UnityEngine.Object.Instantiate(template, parent, false);
            row.name = (tab ? "Tab " : "Video ") + index;
            Undo.RegisterCreatedObjectUndo(row, "Create playlist row");
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
            if (!tab) PlaylistQueueUI.EnsureAddButton(setup, row, index);
            return image;
        }
        public static void BakeLoaded()
        {
            foreach (var setup in UnityEngine.Object.FindObjectsOfType<PlaylistSetup>(true))
                if (setup.gameObject.scene.IsValid() && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null)
                { PlaylistConnections.AutoConnect(setup); Bake(setup); }
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
