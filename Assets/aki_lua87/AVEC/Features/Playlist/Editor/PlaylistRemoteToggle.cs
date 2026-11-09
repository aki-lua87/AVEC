using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Optional Remote integration uses editor reflection, never a runtime assembly dependency.
    [InitializeOnLoad]
    public static class PlaylistRemoteToggle
    {
        static bool pending;
        static PlaylistRemoteToggle()
        {
            EditorApplication.hierarchyChanged += Schedule;
            Undo.postprocessModifications += OnModified;
            Schedule();
        }
        static UndoPropertyModification[] OnModified(UndoPropertyModification[] changes)
        {
            if (changes.Any(c => c.currentValue.target is PlaylistSetup ||
                (c.currentValue.target != null && c.currentValue.target.GetType().FullName == "aki_lua87.CommonGestureRemote.RemoteSetup"))) Schedule();
            return changes;
        }
        static void Schedule()
        {
            if (pending || EditorApplication.isPlayingOrWillChangePlaymode) return;
            pending = true;
            EditorApplication.delayCall += () =>
            {
                pending = false;
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                EnsureLoaded();
            };
        }
        public static void EnsureLoaded()
        {
            foreach (var setup in Object.FindObjectsOfType<PlaylistSetup>(true))
                if (setup.gameObject.scene.IsValid() && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null) Ensure(setup);
        }
        static Component Reference(Component component, string field)
        {
            return component == null ? null : component.GetType().GetField(field)?.GetValue(component) as Component;
        }
        public static void Ensure(PlaylistSetup setup, bool wireRemotes = true)
        {
            var view = setup == null ? null : setup.view;
            if (view == null || view.connectionLabel == null) return;
            if (wireRemotes)
            {
                var objects = new List<GameObject>();
                var type = PlaylistConnections.Resolve("aki_lua87.CommonGestureRemote.RemoteSetup");
                if (type != null)
                    foreach (var item in Object.FindObjectsOfType(type, true).OfType<Component>())
                    {
                        if (item.gameObject.scene != setup.gameObject.scene || PrefabStageUtility.GetPrefabStage(item.gameObject) != null) continue;
                        var remote = Reference(item, "remote");
                        if (remote == null || remote.transform == view.transform || view.transform.IsChildOf(remote.transform)) continue;
                        // Explicit playlist links also support shared-adapter tablet copies.
                        var linked = Reference(item, "playlist") as PlaylistView;
                        bool samePlayer = setup.player != null && Reference(item, "player") == setup.player;
                        bool sameView = linked == view || (linked != null && view.adapter != null && linked.adapter == view.adapter);
                        if ((samePlayer || sameView) && !objects.Contains(remote.gameObject)) objects.Add(remote.gameObject);
                    }
                objects.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
                if (view.remoteObjects == null || !view.remoteObjects.SequenceEqual(objects))
                {
                    Undo.RecordObject(view, "Connect playlist remote visibility");
                    view.remoteObjects = objects.ToArray();
                    PlaylistConnections.Save(view);
                }
            }
            if (view.remoteToggleButton == null)
            {
                var existing = view.transform.Find("Remote toggle");
                if (existing != null)
                {
                    Undo.RecordObject(view, "Restore remote toggle references");
                    view.remoteToggleButton = existing.gameObject;
                    view.remoteOffSlash = existing.Find("Off slash")?.gameObject;
                    PlaylistConnections.Save(view);
                }
                else CreateButton(setup);
            }
            bool available = view.remoteObjects != null && view.remoteObjects.Any(o => o != null);
            if (view.remoteToggleButton != null && view.remoteToggleButton.activeSelf != available)
            {
                Undo.RecordObject(view.remoteToggleButton, "Show connected remote toggle");
                view.remoteToggleButton.SetActive(available);
                PlaylistConnections.Save(view.remoteToggleButton);
            }
        }
        static void CreateButton(PlaylistSetup setup)
        {
            var view = setup.view;
            var parent = view.transform as RectTransform;
            if (parent == null) return;
            var go = new GameObject("Remote toggle", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Add playlist remote toggle");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(52, 42);
            var background = go.GetComponent<Image>();
            background.color = view.normalColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, nameof(PlaylistView._ToggleRemote));
            // Same geometry as RemoteHelpBuilder's call pictogram.
            var icon = new GameObject("Remote icon", typeof(RectTransform)).GetComponent<RectTransform>();
            icon.SetParent(rect, false); icon.sizeDelta = new Vector2(34, 78);
            icon.anchorMin = icon.anchorMax = new Vector2(0, 1);
            icon.gameObject.layer = go.layer;
            icon.anchoredPosition = new Vector2(18, -18);
            icon.localScale = Vector3.one * .34f;
            icon.localRotation = Quaternion.Euler(0, 0, -15);
            Part("Left edge", icon, new Vector2(-15, 0), new Vector2(3, 72), view.iconColor);
            Part("Right edge", icon, new Vector2(15, 0), new Vector2(3, 72), view.iconColor);
            Part("Top edge", icon, new Vector2(0, 36), new Vector2(33, 3), view.iconColor);
            Part("Bottom edge", icon, new Vector2(0, -36), new Vector2(33, 3), view.iconColor);
            Part("Indicator", icon, new Vector2(0, 24), new Vector2(18, 5), view.accentColor);
            Part("Main button", icon, new Vector2(0, 9), new Vector2(10, 10), view.accentColor);
            for (int row = 0; row < 2; row++) for (int column = 0; column < 2; column++)
                Part("Button " + row + " " + column, icon, new Vector2(-6 + column * 12, -8 - row * 12), new Vector2(5, 5), view.iconColor);
            var slash = Part("Off slash", rect, new Vector2(18, -18), new Vector2(31, 2.5f), view.iconColor);
            slash.localRotation = Quaternion.Euler(0, 0, 48);
            slash.gameObject.SetActive(view.remoteObjects != null && !view.remoteObjects.Any(o => o != null && o.activeSelf));
            Undo.RecordObject(view, "Wire remote toggle button");
            view.remoteToggleButton = go;
            view.remoteOffSlash = slash.gameObject;
            PlaylistConnections.Save(view);
            MoveToFooter(view);
            PlaylistAppearance.RefreshSupplementalTheme(setup);
            PlaylistControlTips.Ensure(setup);
            // Include the new button in the existing VR finger-touch dispatch table.
            PlaylistTabletBuilder.RefreshBindings(setup);
        }
        // Explicit migration and new-button default only; never reset authored positions on refresh.
        public static void MoveToFooter(PlaylistView view)
        {
            var root = view == null ? null : view.transform as RectTransform;
            var play = root == null ? null : root.Find("Play") as RectTransform;
            var rect = view == null || view.remoteToggleButton == null ? null : view.remoteToggleButton.transform as RectTransform;
            if (root == null || play == null || rect == null) return;
            Undo.RecordObject(rect, "Align remote button with playback controls");
            var topLeft = root.InverseTransformPoint(play.TransformPoint(new Vector3(play.rect.xMin, play.rect.yMax, 0)));
            var size = play.rect.size;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(root.rect.width - (topLeft.x - root.rect.xMin) - size.x, topLeft.y - root.rect.yMax);
            PlaylistConnections.Save(rect);
            var icon = rect.Find("Remote icon") as RectTransform;
            if (icon != null)
            {
                Undo.RecordObject(icon, "Fit remote icon to playback button size");
                icon.anchorMin = icon.anchorMax = new Vector2(.5f, .5f);
                icon.anchoredPosition = Vector2.zero;
                icon.localScale = Vector3.one * Mathf.Min((size.x - 16) / 34f, (size.y - 12) / 78f);
                PlaylistConnections.Save(icon);
            }
            var slash = rect.Find("Off slash") as RectTransform;
            if (slash != null)
            {
                Undo.RecordObject(slash, "Center remote off slash");
                slash.anchorMin = slash.anchorMax = new Vector2(.5f, .5f);
                slash.anchoredPosition = Vector2.zero;
                slash.sizeDelta = new Vector2(Mathf.Min(size.x, size.y) * .86f, slash.sizeDelta.y);
                PlaylistConnections.Save(slash);
            }
        }
        static RectTransform Part(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            // Button parts use its top-left pivot; icon parts use a centered coordinate system.
            rect.anchorMin = rect.anchorMax = parent.name == "Remote toggle" ? new Vector2(0, 1) : new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return rect;
        }
    }
}
