using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistControlTips
    {
        public static void Ensure(PlaylistSetup setup)
        {
            var view = setup == null ? null : setup.view;
            if (view == null) return;
            var root = view.transform as RectTransform;
            if (root == null) return;
            if (view.controlTip == null)
            {
                var existing = root.Find("Control tip") as RectTransform;
                Undo.RecordObject(view, "Wire local control tips");
                view.controlTip = existing == null ? CreatePanel(view) : existing;
                view.controlTipLabel = view.controlTip.GetComponentInChildren<Text>(true);
                PlaylistConnections.Save(view);
            }
            var targets = new[] {
                root.Find("Play") as RectTransform,
                root.Find("Playback mode") as RectTransform,
                view.remoteToggleButton == null ? null : view.remoteToggleButton.transform as RectTransform,
                view.volumeIcon == null ? null : view.volumeIcon.transform as RectTransform
            };
            if (view.controlTipTargets == null || !view.controlTipTargets.SequenceEqual(targets))
            {
                Undo.RecordObject(view, "Wire control tip targets");
                view.controlTipTargets = targets;
                PlaylistConnections.Save(view);
            }
            string[] events = { nameof(PlaylistView._ShowPlaybackTip), nameof(PlaylistView._ShowPlaybackModeTip), nameof(PlaylistView._ShowRemoteTip), nameof(PlaylistView._ShowMuteTip) };
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].GetComponent<Button>() == null) continue;
                var trigger = targets[i].GetComponent<EventTrigger>();
                if (trigger == null) trigger = Undo.AddComponent<EventTrigger>(targets[i].gameObject);
                AddEvent(trigger, view, EventTriggerType.PointerEnter, events[i]);
                AddEvent(trigger, view, EventTriggerType.PointerExit, nameof(PlaylistView._HideControlTip));
                AddEvent(trigger, view, EventTriggerType.Deselect, nameof(PlaylistView._HideControlTip));
            }
            // Existing instances can inherit proxy references while their older Udon table
            // remains overridden. Persist those inherited fields before entering the world.
            var behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(view);
            RectTransform savedPanel;
            if (behaviour != null && (!behaviour.publicVariables.TryGetVariableValue<RectTransform>("controlTip", out savedPanel) || savedPanel != view.controlTip))
                PlaylistConnections.Save(view);
        }
        static void AddEvent(EventTrigger trigger, PlaylistView view, EventTriggerType type, string method)
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(view);
            if (backing == null) return;
            var entry = trigger.triggers.FirstOrDefault(e => e.eventID == type);
            if (entry != null)
                for (int i = 0; i < entry.callback.GetPersistentEventCount(); i++)
                    if (entry.callback.GetPersistentTarget(i) == backing && entry.callback.GetPersistentMethodName(i) == "SendCustomEvent") return;
            Undo.RecordObject(trigger, "Add local ray-hover tip");
            if (entry == null)
            {
                entry = new EventTrigger.Entry { eventID = type };
                trigger.triggers.Add(entry);
            }
            UnityEventTools.AddStringPersistentListener(entry.callback, backing.SendCustomEvent, method);
            PlaylistConnections.Save(trigger);
        }
        static RectTransform CreatePanel(PlaylistView view)
        {
            var go = new GameObject("Control tip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.layer = view.gameObject.layer;
            go.transform.SetParent(view.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Add local control tip panel");
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(244, 34);
            var background = go.GetComponent<Image>();
            background.color = new Color(.035f, .045f, .06f, .96f);
            background.raycastTarget = false;
            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var labelGo = new GameObject("Tip label", typeof(RectTransform), typeof(Text));
            labelGo.layer = go.layer;
            labelGo.transform.SetParent(rect, false);
            var label = labelGo.GetComponent<Text>();
            label.font = view.connectionLabel == null ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") : view.connectionLabel.font;
            label.fontSize = 16;
            label.color = new Color(.96f, .97f, 1f);
            label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.text = "";
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 4);
            label.rectTransform.offsetMax = new Vector2(-8, -4);
            go.SetActive(false);
            return rect;
        }
    }
}
