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
            PlaylistUndo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Compact playlist UI");
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
            ApplyTheme(setup); ReclaimStatusSpace(setup.view); UpdateHeaderPresentation(setup.view); PlaylistConnections.Save(setup.view); PlaylistConnections.Save(setup);
        }
        public static void UpdateHeaderPresentation(PlaylistView view)
        {
            if (view == null) return;
            var root = view.transform as RectTransform;
            if (root == null) return;
            var title = root.Find("Library title")?.GetComponent<Text>();
            if (title != null)
            {
                Undo.RecordObject(title, "Update playlist header caption");
                Undo.RecordObject(title.rectTransform, "Fit playlist header caption");
                title.text = "AVEC-PLAYLIST"; title.alignment = TextAnchor.LowerLeft;
                title.resizeTextForBestFit = false;
                title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Ceil(title.preferredWidth) + 2);
                PlaylistConnections.Save(title); PlaylistConnections.Save(title.rectTransform);
            }
            if (view.heading != null)
            {
                Undo.RecordObject(view.heading.gameObject, "Hide redundant playlist heading");
                view.heading.gameObject.SetActive(false); PlaylistConnections.Save(view.heading.gameObject);
            }
            if (view.descriptionLabel != null && view.heading != null)
            {
                var description = view.descriptionLabel;
                var heading = view.heading.rectTransform;
                Undo.RecordObject(description, "Show three-line playlist description");
                Undo.RecordObject(description.rectTransform, "Align playlist description at left");
                float x = heading.anchoredPosition.x;
                Place(description.rectTransform, x, -heading.anchoredPosition.y - 2, root.rect.width - x - 24, 38);
                description.fontSize = 10; description.lineSpacing = .8f;
                description.resizeTextForBestFit = false; description.alignment = TextAnchor.LowerLeft;
                description.horizontalOverflow = HorizontalWrapMode.Wrap; description.verticalOverflow = VerticalWrapMode.Truncate;
                description.supportRichText = false; PlaylistConnections.Save(description);
            }
            if (view.connectionLabel != null && title != null)
            {
                var label = view.connectionLabel;
                Undo.RecordObject(label, "Prefix connected player label");
                Undo.RecordObject(label.rectTransform, "Align connection beside playlist caption");
                var rect = title.rectTransform;
                float x = rect.anchoredPosition.x + rect.rect.width + 16;
                var mode = root.Find("Playback mode") as RectTransform;
                float right = mode == null ? root.rect.width - 24 : mode.anchoredPosition.x - 12;
                Place(label.rectTransform, x, -rect.anchoredPosition.y, Mathf.Max(100, right - x), rect.rect.height);
                label.alignment = TextAnchor.LowerLeft;
                label.text = "connnect: " + (view.adapter == null || string.IsNullOrEmpty(view.connectionName) ? "未接続" : view.connectionName);
                float bottomOffset = TextBottomIn(root, title) - TextBottomIn(root, label);
                label.rectTransform.position += root.TransformVector(Vector3.up * bottomOffset);
                PlaylistConnections.Save(label.rectTransform);
                PlaylistConnections.Save(label);
            }
        }
        static float TextBottomIn(RectTransform root, Text label)
        {
            var generator = new TextGenerator();
            generator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
            float bottom = float.PositiveInfinity;
            foreach (var vertex in generator.verts) bottom = Mathf.Min(bottom, vertex.position.y);
            if (float.IsPositiveInfinity(bottom)) bottom = label.rectTransform.rect.yMin * label.pixelsPerUnit;
            return root.InverseTransformPoint(label.rectTransform.TransformPoint(new Vector3(0, bottom / label.pixelsPerUnit, 0))).y;
        }
        // Reclaim the hidden message row without reapplying the theme or rebuilding UI.
        public static void ReclaimStatusSpace(PlaylistView view)
        {
            if (view == null || (view.status != null && view.status.gameObject.activeSelf)) return;
            var root = view.transform as RectTransform;
            if (root == null) return;
            var controls = new System.Collections.Generic.List<RectTransform>();
            float bottom = float.PositiveInfinity;
            var corners = new Vector3[4];
            foreach (string name in new[] { "Play", "Stop", "Volume caption", "Volume rail", "Volume value" })
            {
                var rect = root.Find(name) as RectTransform;
                if (rect == null) continue;
                controls.Add(rect); rect.GetWorldCorners(corners);
                foreach (var corner in corners) bottom = Mathf.Min(bottom, root.InverseTransformPoint(corner).y);
            }
            if (controls.Count == 0) return;
            float extra = bottom - root.rect.yMin - 20f;
            if (extra < .1f) return;
            Vector3 oldCenter = view.videoScroll == null ? Vector3.zero : ((RectTransform)view.videoScroll.transform).TransformPoint(((RectTransform)view.videoScroll.transform).rect.center);
            foreach (string name in new[] { "Tabs", "Videos", "Queue panel", "History panel", "History URL", "Video details", "User list panel" })
            {
                var rect = root.Find(name) as RectTransform;
                if (rect == null) continue;
                Undo.RecordObject(rect, "Expand playlist into unused footer space");
                Vector3 topLeft = rect.TransformPoint(new Vector3(rect.rect.xMin, rect.rect.yMax, 0));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.height + extra);
                rect.position += topLeft - rect.TransformPoint(new Vector3(rect.rect.xMin, rect.rect.yMax, 0));
                PlaylistConnections.Save(rect);
            }
            foreach (var rect in controls)
            {
                Undo.RecordObject(rect, "Move playlist controls into unused footer space");
                rect.position += root.TransformVector(Vector3.down * extra);
                PlaylistConnections.Save(rect);
            }
            if (view.loadingIcon != null && view.videoScroll != null)
            {
                var area = (RectTransform)view.videoScroll.transform;
                Undo.RecordObject(view.loadingIcon.transform, "Keep loading indicator centered");
                view.loadingIcon.transform.position += area.TransformPoint(area.rect.center) - oldCenter;
                PlaylistConnections.Save(view.loadingIcon.transform);
            }
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
            PlaylistUndo.RecordObject(label, "Fit playlist title");
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Min(14, Mathf.Max(1, label.fontSize));
            label.resizeTextMaxSize = Mathf.Max(1, label.fontSize);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            var viewport = row.transform.Find("Title viewport") as RectTransform;
            if (viewport == null)
            {
                viewport = new GameObject("Title viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                PlaylistUndo.RegisterCreatedObjectUndo(viewport.gameObject, "Scroll playlist title");
                viewport.SetParent(row.transform, false);
                viewport.anchorMin = label.rectTransform.anchorMin;
                viewport.anchorMax = label.rectTransform.anchorMax;
                viewport.pivot = label.rectTransform.pivot;
                viewport.offsetMin = label.rectTransform.offsetMin;
                viewport.offsetMax = label.rectTransform.offsetMax;
                PlaylistUndo.SetTransformParent(label.transform, viewport, "Scroll playlist title");
            }
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var marquee = viewport.GetComponent<PlaylistTitleMarquee>();
            if (marquee == null) marquee = viewport.gameObject.AddUdonSharpComponent<PlaylistTitleMarquee>();
            PlaylistUndo.RecordObject(marquee, "Scroll playlist title");
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
            PlaylistThemeGraphics.Restore(setup);
            var view = setup.view;
            Color oldSelected = view.selectedColor;
            PlaylistUndo.RecordObject(setup.view,"Apply playlist theme");
            setup.view.normalColor=theme.button; setup.view.selectedColor=theme.selected; setup.view.accentColor=theme.accent;setup.view.iconColor=theme.text;
            foreach (var image in setup.GetComponentsInChildren<Image>(true))
            {
                PlaylistUndo.RecordObject(image,"Apply playlist theme");
                string name=image.name;
                bool selected = image.color == oldSelected && (System.Array.IndexOf(view.tabImages, image) >= 0 ||
                    System.Array.IndexOf(view.videoImages, image) >= 0 || image == view.queueTabImage || image == view.historyTabImage || image == view.userTabImage);
                if (PlaylistThemeGraphics.IsViewport(image)) continue;
                if(name=="Background")image.color=theme.background;
                else if(name=="Accent"||name=="Handle"||name=="Volume fill"||name=="Playback progress")image.color=theme.accent;
                else if(name=="Divider"||name=="Track")image.color=theme.outlineOnly?theme.button:theme.surface;
                else if(name=="Scrollbar"||name=="List scrollbar"||name=="Tabs"||name=="Videos"||name=="Volume rail"||name=="Queue panel"||name=="History panel"||name=="User list panel"||name=="Add video form"||name=="Add user video form")image.color=theme.surface;
                else if(PlaylistThemeGraphics.IsForeground(view,image))image.color=theme.text;
                else image.color=selected?theme.selected:theme.button;
                PlaylistConnections.Save(image);
            }
            foreach(var text in setup.GetComponentsInChildren<Text>(true)) { PlaylistUndo.RecordObject(text,"Apply playlist theme"); text.color=text==setup.view.status||text==setup.view.volumeLabel?theme.mutedText:theme.text;PlaylistConnections.Save(text); }
            var colors=ColorBlock.defaultColorBlock; colors.normalColor=Color.white; colors.highlightedColor=new Color(.85f,.95f,1f); colors.pressedColor=new Color(.65f,.75f,.8f); colors.selectedColor=Color.white;
            foreach(var button in setup.GetComponentsInChildren<Button>(true)) { PlaylistUndo.RecordObject(button,"Apply playlist theme"); button.colors=colors;PlaylistConnections.Save(button); }
            PlaylistThemeGraphics.ApplyNew(setup);
            PlaylistQueueUI.RefreshQueueControlLayout(setup.view);
            PlaylistConnections.Save(setup.view);
        }
        public static void EnsureControls(PlaylistSetup setup)
        {
            var view=setup.view;
            PlaylistQueueUI.Ensure(setup);
            if(view.playbackModeIcon==null)
            {
                var old=view.transform.Find("Playback mode"); if(old!=null)PlaylistUndo.DestroyObjectImmediate(old.gameObject);
                view.playbackModeLabel=null;
                view.orderedIcon=PlaylistIcons.Get("ordered"); view.randomIcon=PlaylistIcons.Get("shuffle");
                view.playbackModeIcon=IconButton(view,"Playback mode","ordered","_TogglePlaybackMode",824,18);
            }
            if(view.transform.Find("Play")==null)IconButton(view,"Play","play","_TogglePlayback",24,451);
            EnsureTransportControls(view);
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
        // Narrow migration: preserve every existing RectTransform, theme and data override.
        public static void EnsureTransportControls(PlaylistView view)
        {
            if (view == null) return;
            PlaylistUndo.RecordObject(view, "Add mute and playback toggle controls");
            var play = view.transform.Find("Play");
            if (play != null)
            {
                var button = play.GetComponent<Button>();
                if (button != null) SetControlEvent(button, view, "_TogglePlayback", "_StartPlayback");
                var icon = play.Find("Icon");
                if (icon != null) view.transportIcon = icon.GetComponent<Image>();
                view.playIcon = PlaylistIcons.Get("play"); view.pauseIcon = PlaylistIcons.Get("pause");
            }
            var stop = view.transform.Find("Stop");
            if (stop != null && stop.gameObject.activeSelf) { PlaylistUndo.RecordObject(stop.gameObject, "Consolidate playback buttons"); stop.gameObject.SetActive(false); PlaylistConnections.Save(stop.gameObject); }
            var speaker = view.transform.Find("Volume caption/Icon");
            if (speaker != null)
            {
                view.volumeIcon = speaker.GetComponent<Image>();
                view.speakerIcon = PlaylistIcons.Get("speaker"); view.mutedIcon = PlaylistIcons.Get("speaker-muted");
                var button = speaker.GetComponent<Button>();
                if (button == null) button = PlaylistUndo.AddComponent<Button>(speaker.gameObject);
                button.targetGraphic = view.volumeIcon;
                if (view.volumeIcon != null) { PlaylistUndo.RecordObject(view.volumeIcon, "Enable mute icon input"); view.volumeIcon.raycastTarget = true; PlaylistConnections.Save(view.volumeIcon); }
                SetControlEvent(button, view, "_ToggleMute", null);
            }
            PlaylistConnections.Save(view);
        }
        static void SetControlEvent(Button button, PlaylistView view, string method, string previous)
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(view);
            if (backing == null) return;
            PlaylistUndo.RecordObject(button, "Wire playlist control");
            var serialized = new SerializedObject(button);
            var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (call.FindPropertyRelative("m_Target").objectReferenceValue != backing || call.FindPropertyRelative("m_MethodName").stringValue != "SendCustomEvent") continue;
                var argument = call.FindPropertyRelative("m_Arguments.m_StringArgument");
                if (argument.stringValue == method) return;
                if (previous != null && argument.stringValue == previous) { argument.stringValue = method; serialized.ApplyModifiedProperties(); PlaylistConnections.Save(button); return; }
            }
            UnityEventTools.AddStringPersistentListener(button.onClick, backing.SendCustomEvent, method);
            PlaylistConnections.Save(button);
        }
        public static void EnsureVolumeIcon(PlaylistSetup setup)
        {
            var caption = setup.transform.Find("Volume caption");
            if (caption == null) return;
            var text = caption.GetComponent<Text>();
            if (text != null) { PlaylistUndo.RecordObject(text, "Replace volume caption with icon"); text.enabled = false; PlaylistConnections.Save(text); }
            var existing = caption.Find("Icon");
            var icon = existing == null ? Box(caption, "Icon").GetComponent<Image>() : existing.GetComponent<Image>();
            if (existing == null) {
                PlaylistUndo.RegisterCreatedObjectUndo(icon.gameObject, "Add speaker icon");
                var rect = icon.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(30,30);
            }
            icon.sprite = PlaylistIcons.Get("speaker"); icon.preserveAspect = true; icon.raycastTarget = false;
            icon.color = text == null ? setup.view.iconColor : text.color;
            PlaylistConnections.Save(icon);
            EnsureTransportControls(setup.view);
        }
        public static void EnsureConnectionLabel(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return;
            if (view.connectionLabel == null) {
                view.connectionLabel = Text(setup.transform, "Connected player", "未接続", 16);
                PlaylistUndo.RegisterCreatedObjectUndo(view.connectionLabel.gameObject, "Add connected player label");
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
            PlaylistUndo.RecordObject(view, "Update connected player label");
            PlaylistUndo.RecordObject(view.connectionLabel, "Update connected player label");
            view.connectionName = destination;
            view.connectionLabel.text = "connnect: " + (setup.player == null || view.adapter == null ? "未接続" : destination);
            PlaylistConnections.Save(view.connectionLabel); PlaylistConnections.Save(view);
            if (view.audioLinkLabel == null) {
                view.audioLinkLabel = Text(setup.transform, "AudioLink status", "AudioLink: 未導入", 14);
                PlaylistUndo.RegisterCreatedObjectUndo(view.audioLinkLabel.gameObject, "Add AudioLink status label");
                Place(view.audioLinkLabel.rectTransform, 310, 40, 500, 20);
                view.audioLinkLabel.alignment = TextAnchor.MiddleRight;
                view.audioLinkLabel.color = setup.theme == null ? view.iconColor : setup.theme.mutedText;
            }
            PlaylistConnections.RefreshAudioLinkStatus(setup);
            UpdateHeaderPresentation(view);
        }
        static Image IconButton(PlaylistView view,string name,string icon,string method,float x,float y)
        {
            var rect=Box(view.transform,name); Place(rect,x,y,52,42); PlaylistUndo.RegisterCreatedObjectUndo(rect.gameObject,"Add playlist controls");
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
            bool changed = EditorGUI.EndChangeCheck();
            if (((PlaylistTheme)target).outlineOnly) EditorGUILayout.HelpBox("背景・パネル・ボタンの内側は透過し、各色は枠線に使います。文字・アイコン・進捗線は表示します。", MessageType.Info);
            if(!changed||EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var setup in Object.FindObjectsOfType<PlaylistSetup>(true))if(setup.theme==target)PlaylistAppearance.ApplyTheme(setup);
        }
    }
}
