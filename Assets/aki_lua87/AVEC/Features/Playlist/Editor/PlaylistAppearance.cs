using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistAppearance
    {
        public const string ThemePath = PlaylistFactory.Root + "/Samples/Themes/DefaultTheme.asset";
        public static PlaylistTheme DefaultTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PlaylistTheme>(ThemePath);
            if (theme != null) return theme;
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath)); AssetDatabase.Refresh();
            theme = ScriptableObject.CreateInstance<PlaylistTheme>(); AssetDatabase.CreateAsset(theme, ThemePath); return theme;
        }
        public static void ApplyCompact(PlaylistSetup setup)
        {
            Undo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Compact playlist UI");
            var root = setup.transform;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 560);
            root.localScale = Vector3.one * .00135f;
            var collider = root.GetComponent<BoxCollider>(); if (collider != null) collider.size = new Vector3(900,560,1);
            Position(root, "Accent", 24,12,36,4);
            Position(root, "Library title",24,18,640,42);
            Position(root, "Divider",24,64,852,1);
            Position(root, "Tab title",232,68,644,36);
            Position(root, "Tabs",24,106,196,328);
            Position(root, "Videos",232,106,644,328);
            Position(root, "Status",24,510,852,30);
            Hide(root,"Subtitle"); Hide(root,"Hint"); Hide(root,"Collections label");
            var title = root.Find("Library title")?.GetComponent<Text>(); if (title != null) { title.text = "PLAYLIST"; title.fontSize = 26; }
            if (setup.view.heading != null) setup.view.heading.fontSize = 22;
            if (setup.view.status != null) { setup.view.status.fontSize = 18; setup.view.status.resizeTextForBestFit = true; setup.view.status.resizeTextMinSize = 14; setup.view.status.resizeTextMaxSize = 18; }
            Row(setup.tabTemplate, 50, 22); Row(setup.videoTemplate, 58, 24);
            FitTabLabel(setup.tabTemplate);
            foreach (var layout in root.GetComponentsInChildren<VerticalLayoutGroup>(true)) { layout.spacing = 6;PlaylistConnections.Save(layout); }
            foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true)) { scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;scroll.verticalScrollbarSpacing=4;PlaylistConnections.Save(scroll); }
            EnsureControls(setup);
            Position(root,"Playback mode",824,18,52,42);
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf");
                if (font != null) text.font = font;
                PlaylistConnections.Save(text);
            }
            PlaylistConnections.Save(root.GetComponent<RectTransform>());if(collider!=null)PlaylistConnections.Save(collider);
            ApplyTheme(setup); PlaylistConnections.Save(setup.view); PlaylistConnections.Save(setup);
        }
        static void Row(GameObject go,float height,int size)
        {
            if (go == null) return;
            go.GetComponent<LayoutElement>().minHeight = go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(196,height);
            go.GetComponentInChildren<Text>(true).fontSize = size;
            var label=go.GetComponentInChildren<Text>(true);label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=size;
            PlaylistConnections.Save(go.GetComponent<LayoutElement>());PlaylistConnections.Save(go.GetComponent<RectTransform>());PlaylistConnections.Save(label);
        }
        public static void FitTabLabel(GameObject row)
        {
            if (row == null) return;
            var label = row.GetComponentInChildren<Text>(true);
            var layout = row.GetComponent<LayoutElement>();
            if (label == null || layout == null) return;
            Undo.RecordObject(label, "Fit playlist title");
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 1;
            label.resizeTextMaxSize = Mathf.Max(1, label.fontSize);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            var viewport = row.transform.Find("Title viewport") as RectTransform;
            if (viewport == null)
            {
                viewport = new GameObject("Title viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(viewport.gameObject, "Scroll playlist title");
                viewport.SetParent(row.transform, false);
                viewport.anchorMin = label.rectTransform.anchorMin;
                viewport.anchorMax = label.rectTransform.anchorMax;
                viewport.pivot = label.rectTransform.pivot;
                viewport.offsetMin = label.rectTransform.offsetMin;
                viewport.offsetMax = label.rectTransform.offsetMax;
                Undo.SetTransformParent(label.transform, viewport, "Scroll playlist title");
            }
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var marquee = viewport.GetComponent<PlaylistTitleMarquee>();
            if (marquee == null) marquee = viewport.gameObject.AddUdonSharpComponent<PlaylistTitleMarquee>();
            Undo.RecordObject(marquee, "Scroll playlist title");
            marquee.label = label;
            marquee.viewport = viewport;
            UdonSharpEditorUtility.CopyProxyToUdon(marquee);
            PlaylistConnections.Save(label);
            PlaylistConnections.Save(rect);
            PlaylistConnections.Save(marquee);
        }
        static void Hide(Transform root,string name) { var t=root.Find(name); if(t!=null){t.gameObject.SetActive(false);PlaylistConnections.Save(t.gameObject);} }
        static void Position(Transform root,string name,float x,float y,float w,float h) { var t=root.Find(name); if(t!=null) Place(t.GetComponent<RectTransform>(),x,y,w,h); }
        public static void ApplyTheme(PlaylistSetup setup)
        {
            var theme = setup.theme; if (theme == null) return;
            Undo.RecordObject(setup.view,"Apply playlist theme");
            setup.view.normalColor=theme.button; setup.view.selectedColor=theme.selected; setup.view.accentColor=theme.accent;setup.view.iconColor=theme.text;
            foreach (var image in setup.GetComponentsInChildren<Image>(true))
            {
                Undo.RecordObject(image,"Apply playlist theme");
                string name=image.name;
                if(name=="Background")image.color=theme.background;
                else if(name=="Accent"||name=="Handle"||name=="Volume fill")image.color=theme.accent;
                else if(name=="Divider"||name=="Scrollbar"||name=="Tabs"||name=="Videos"||name=="Volume rail"||name=="Queue panel")image.color=theme.surface;
                else if(name=="Icon")image.color=theme.text;
                else if(name=="Viewport")continue;
                else image.color=theme.button;
                PlaylistConnections.Save(image);
            }
            foreach(var text in setup.GetComponentsInChildren<Text>(true)) { Undo.RecordObject(text,"Apply playlist theme"); text.color=text==setup.view.status||text==setup.view.volumeLabel?theme.mutedText:theme.text;PlaylistConnections.Save(text); }
            var colors=ColorBlock.defaultColorBlock; colors.normalColor=Color.white; colors.highlightedColor=new Color(.85f,.95f,1f); colors.pressedColor=new Color(.65f,.75f,.8f); colors.selectedColor=Color.white;
            foreach(var button in setup.GetComponentsInChildren<Button>(true)) { Undo.RecordObject(button,"Apply playlist theme"); button.colors=colors;PlaylistConnections.Save(button); }
            if(setup.view.tabImages.Length>0&&setup.view.tabImages[0]!=null)setup.view.tabImages[0].color=theme.selected;
            PlaylistConnections.Save(setup.view);
        }
        public static void EnsureControls(PlaylistSetup setup)
        {
            var view=setup.view;
            PlaylistQueueUI.Ensure(setup);
            if(view.playbackModeIcon==null)
            {
                var old=view.transform.Find("Playback mode"); if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
                view.playbackModeLabel=null;
                view.orderedIcon=PlaylistIcons.Get("ordered"); view.randomIcon=PlaylistIcons.Get("shuffle");
                view.playbackModeIcon=IconButton(view,"Playback mode","ordered","_TogglePlaybackMode",824,18);
            }
            if(view.transform.Find("Play")==null)IconButton(view,"Play","play","_StartPlayback",24,451);
            if(view.transform.Find("Stop")==null)IconButton(view,"Stop","pause","_StopPlayback",84,451);
            EnsureConnectionLabel(setup);
            if(view.volumeSlider!=null) { EnsureVolumeIcon(setup); return; }
            var caption=Text(view.transform,"Volume caption","音量 (自分)",18); Place(caption.rectTransform,150,454,108,42);
            var rail=Box(view.transform,"Volume rail"); Place(rail,264,461,470,28);
            var slider=rail.gameObject.AddComponent<Slider>(); slider.minValue=0;slider.maxValue=1;
            var background=Box(rail,"Track"); Stretch(background); background.offsetMin=new Vector2(0,10); background.offsetMax=new Vector2(0,-10); background.GetComponent<Image>().color=new Color(.2f,.25f,.3f);
            var fill=Box(background,"Volume fill"); Stretch(fill); slider.fillRect=fill;
            var area=new GameObject("Handle area",typeof(RectTransform)).GetComponent<RectTransform>(); area.SetParent(rail,false); Stretch(area);
            var handle=Box(area,"Handle"); handle.sizeDelta=new Vector2(14,28); slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.direction=Slider.Direction.LeftToRight;
            var backing=UdonSharpEditorUtility.GetBackingUdonBehaviour(view);
            UnityEventTools.AddPersistentListener(slider.onValueChanged); int index=slider.onValueChanged.GetPersistentEventCount()-1;
            UnityEventTools.RegisterStringPersistentListener(slider.onValueChanged,index,backing.SendCustomEvent,"_ChangeVolume");
            slider.onValueChanged.SetPersistentListenerState(index,UnityEngine.Events.UnityEventCallState.RuntimeOnly);
            view.volumeSlider=slider; view.volumeLabel=Text(view.transform,"Volume value","—",22); Place(view.volumeLabel.rectTransform,754,452,122,44);
            PlaylistConnections.Save(view);
            EnsureVolumeIcon(setup);
        }
        public static void EnsureVolumeIcon(PlaylistSetup setup)
        {
            var caption = setup.transform.Find("Volume caption");
            if (caption == null) return;
            var text = caption.GetComponent<Text>();
            if (text != null) { Undo.RecordObject(text, "Replace volume caption with icon"); text.enabled = false; PlaylistConnections.Save(text); }
            var existing = caption.Find("Icon");
            var icon = existing == null ? Box(caption, "Icon").GetComponent<Image>() : existing.GetComponent<Image>();
            if (existing == null) {
                Undo.RegisterCreatedObjectUndo(icon.gameObject, "Add speaker icon");
                var rect = icon.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(30,30);
            }
            icon.sprite = PlaylistIcons.Get("speaker"); icon.preserveAspect = true; icon.raycastTarget = false;
            icon.color = text == null ? setup.view.iconColor : text.color;
            PlaylistConnections.Save(icon);
        }
        public static void EnsureConnectionLabel(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return;
            if (view.connectionLabel == null) {
                view.connectionLabel = Text(setup.transform, "Connected player", "未接続", 16);
                Undo.RegisterCreatedObjectUndo(view.connectionLabel.gameObject, "Add connected player label");
                Place(view.connectionLabel.rectTransform, 310, 18, 500, 22);
                view.connectionLabel.alignment = TextAnchor.MiddleRight;
                view.connectionLabel.resizeTextForBestFit = true; view.connectionLabel.resizeTextMinSize = 12; view.connectionLabel.resizeTextMaxSize = 16;
                view.connectionLabel.supportRichText = false;
                view.connectionLabel.color = setup.theme == null ? view.iconColor : setup.theme.mutedText;
            }
            string destination = "";
            if (setup.player != null) {
                destination = setup.playerKind;
            }
            Undo.RecordObject(view, "Update connected player label");
            Undo.RecordObject(view.connectionLabel, "Update connected player label");
            view.connectionName = destination;
            view.connectionLabel.text = setup.player == null || view.adapter == null ? "未接続" : destination;
            PlaylistConnections.Save(view.connectionLabel); PlaylistConnections.Save(view);
            if (view.audioLinkLabel == null) {
                view.audioLinkLabel = Text(setup.transform, "AudioLink status", "AudioLink: 未導入", 14);
                Undo.RegisterCreatedObjectUndo(view.audioLinkLabel.gameObject, "Add AudioLink status label");
                Place(view.audioLinkLabel.rectTransform, 310, 40, 500, 20);
                view.audioLinkLabel.alignment = TextAnchor.MiddleRight;
                view.audioLinkLabel.color = setup.theme == null ? view.iconColor : setup.theme.mutedText;
            }
            PlaylistConnections.RefreshAudioLinkStatus(setup);
        }
        static Image IconButton(PlaylistView view,string name,string icon,string method,float x,float y)
        {
            var rect=Box(view.transform,name); Place(rect,x,y,52,42); Undo.RegisterCreatedObjectUndo(rect.gameObject,"Add playlist controls");
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
            var image=Box(rect,"Icon").GetComponent<Image>(); image.sprite=PlaylistIcons.Get(icon);image.preserveAspect=true;image.raycastTarget=false;Stretch(image.rectTransform);image.rectTransform.offsetMin=new Vector2(12,7);image.rectTransform.offsetMax=new Vector2(-12,-7);
            UnityEventTools.AddStringPersistentListener(button.onClick,UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent,method);
            return image;
        }
        static RectTransform Box(Transform parent,string name) { var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=new Color(.12f,.16f,.22f);return go.GetComponent<RectTransform>(); }
        static Text Text(Transform parent,string name,string value,int size) { var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core+"/Fonts/ZenMaruGothic-Regular.ttf")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t; }
        public static void Place(RectTransform rect,float x,float y,float w,float h) { rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);PlaylistConnections.Save(rect); }
        static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
    }
    [CustomEditor(typeof(PlaylistTheme))]
    public class PlaylistThemeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();DrawDefaultInspector();
            if(!EditorGUI.EndChangeCheck()||EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var setup in Object.FindObjectsOfType<PlaylistSetup>(true))if(setup.theme==target)PlaylistAppearance.ApplyTheme(setup);
        }
    }
}
