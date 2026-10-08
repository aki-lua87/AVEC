using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonGestureRemote.Editor
{
    // Adds only local help visuals; existing remote geometry and wiring are preserved.
    public static class RemoteHelpBuilder
    {
        public static void Ensure(RemoteSetup setup)
        {
            if (setup == null || setup.remote == null) return;
            var remote = setup.remote;
            Undo.RecordObject(remote, "Add hand help references");
            // A scene instance can inherit the visuals while its Udon variable table still lacks their references.
            if (remote.helpPanel == null)
            {
                var existing = remote.transform.Find("Hand command list");
                if (existing != null) { remote.helpPanel = existing; remote.helpCommands = existing.GetComponentInChildren<Text>(true); }
            }
            if (remote.helpHint == null)
            {
                var existing = remote.transform.Find("Right stick help hint");
                if (existing != null)
                {
                    remote.helpHint = existing; remote.helpHintGroup = existing.GetComponent<CanvasGroup>();
                    var cap = existing.Find("Stick cap");
                    if (cap != null) remote.helpStick = cap.GetComponent<RectTransform>();
                }
            }
            Font font = remote.label != null ? remote.label.font : null;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf");
            if (remote.helpPanel == null)
            {
                var panel = Canvas("Hand command list", remote.transform, new Vector2(610, 460));
                panel.gameObject.AddComponent<Image>().color = new Color(.035f, .055f, .08f, .96f);
                panel.GetComponent<Image>().raycastTarget = false;
                var commands = Text("Commands", panel, font, 24, "", Vector2.zero, new Vector2(570, 420));
                commands.alignment = TextAnchor.MiddleLeft;
                commands.lineSpacing = 1.12f;
                remote.helpPanel = panel;
                remote.helpCommands = commands;
                panel.gameObject.SetActive(false);
            }
            if (remote.helpHint == null)
            {
                var hint = Canvas("Right stick help hint", remote.transform, new Vector2(240, 140));
                var group = hint.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;
                var background = hint.gameObject.AddComponent<Image>();
                background.color = new Color(.035f, .055f, .08f, .9f); background.raycastTarget = false;
                Text("Stick ring", hint, font, 76, "○", new Vector2(-65, 12), new Vector2(90, 90));
                var cap = Text("Stick cap", hint, font, 35, "●", new Vector2(-65, 12), new Vector2(45, 45));
                cap.color = new Color(.3f, 1f, .65f);
                Text("Right hand", hint, font, 18, "R", new Vector2(-65, -43), new Vector2(40, 25));
                Text("Down", hint, font, 40, "↓", new Vector2(-5, 10), new Vector2(48, 56));
                Text("Hold", hint, font, 18, "長押し", new Vector2(-5, -39), new Vector2(65, 28));
                for (int i = 0; i < 3; i++)
                {
                    var line = new GameObject("Command line " + i, typeof(RectTransform), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(line, "Add help pictogram");
                    line.transform.SetParent(hint, false);
                    var rect = line.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(54, 5); rect.anchoredPosition = new Vector2(69, 24 - i * 18);
                    line.GetComponent<Image>().color = new Color(.3f, 1f, .65f);
                    line.GetComponent<Image>().raycastTarget = false;
                }
                remote.helpHint = hint; remote.helpStick = cap.rectTransform; remote.helpHintGroup = group;
                hint.gameObject.SetActive(false);
            }
            UpdateCommands(remote, setup.data);
            RemoteConnections.Save(remote);
        }

        public static void UpdateCommands(GestureRemote remote, GestureData data)
        {
            if (remote == null || remote.helpCommands == null || data == null) return;
            string text = GestureGuideBuilder.BuildText(data);
            if (remote.helpCommands.text == text) return;
            Undo.RecordObject(remote.helpCommands, "Update hand commands");
            remote.helpCommands.text = text;
            RemoteConnections.Save(remote.helpCommands);
        }

        private static RectTransform Canvas(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(go, "Add remote hand help");
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.transform.localScale = Vector3.one * .00065f;
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size;
            return rect;
        }

        private static Text Text(string name, Transform parent, Font font, int size, string value, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, "Add remote help text");
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value; text.color = new Color(.93f, .96f, 1f);
            text.alignment = TextAnchor.MiddleCenter; text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
