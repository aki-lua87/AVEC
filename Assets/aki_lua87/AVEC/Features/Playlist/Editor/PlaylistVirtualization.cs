using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Narrow migration: no row recreation, layout/style changes, or data rebake.
    public static class PlaylistVirtualization
    {
        public static void Ensure(PlaylistView view)
        {
            if (view == null || view.videoScroll == null || view.videoScroll.content == null) return;
            var content = view.videoScroll.content;
            var group = content.GetComponent<VerticalLayoutGroup>();
            if (group == null) return;
            bool changed = false;
            if (!Mathf.Approximately(view.videoWindowPaddingTop, group.padding.top))
            {
                PlaylistUndo.RecordObject(view, "Preserve playlist padding");
                view.videoWindowPaddingTop = group.padding.top; changed = true;
            }
            if (view.videoTopSpacer == null || view.videoTopSpacer.transform.parent != content)
            {
                PlaylistUndo.RecordObject(view, "Window playlist rows");
                view.videoTopSpacer = Spacer(content, "Video window top spacer", true); changed = true;
            }
            if (view.videoBottomSpacer == null || view.videoBottomSpacer.transform.parent != content)
            {
                PlaylistUndo.RecordObject(view, "Window playlist rows");
                view.videoBottomSpacer = Spacer(content, "Video window bottom spacer", false); changed = true;
            }
            // Data append operations may add rows after the existing bottom spacer.
            if (view.videoTopSpacer.transform.GetSiblingIndex() != 0)
            {
                PlaylistUndo.RecordObject(view.videoTopSpacer.transform, "Position video window spacer");
                view.videoTopSpacer.transform.SetAsFirstSibling(); PlaylistConnections.Save(view.videoTopSpacer.transform);
            }
            if (view.videoBottomSpacer.transform.GetSiblingIndex() != content.childCount - 1)
            {
                PlaylistUndo.RecordObject(view.videoBottomSpacer.transform, "Position video window spacer");
                view.videoBottomSpacer.transform.SetAsLastSibling(); PlaylistConnections.Save(view.videoBottomSpacer.transform);
            }
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(view);
            if (backing != null)
            {
                var serialized = new SerializedObject(view.videoScroll);
                var calls = serialized.FindProperty("m_OnValueChanged.m_PersistentCalls.m_Calls");
                bool wired = false;
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    if (call.FindPropertyRelative("m_Target").objectReferenceValue == backing &&
                        call.FindPropertyRelative("m_MethodName").stringValue == "SendCustomEvent" &&
                        call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue == "_RefreshVideoWindow") wired = true;
                }
                if (!wired)
                {
                    PlaylistUndo.RecordObject(view.videoScroll, "Refresh visible playlist rows on scroll");
                    UnityEventTools.AddStringPersistentListener(view.videoScroll.onValueChanged, backing.SendCustomEvent, "_RefreshVideoWindow");
                    PlaylistConnections.Save(view.videoScroll);
                }
            }
            if (changed) PlaylistConnections.Save(view);
        }

        static LayoutElement Spacer(RectTransform parent, string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            PlaylistUndo.RegisterCreatedObjectUndo(go, "Window playlist rows");
            go.transform.SetParent(parent, false);
            if (top) go.transform.SetAsFirstSibling(); else go.transform.SetAsLastSibling();
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 0; layout.preferredHeight = 0; layout.flexibleHeight = 0;
            layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 0;
            go.SetActive(false);
            return layout;
        }
    }
}
