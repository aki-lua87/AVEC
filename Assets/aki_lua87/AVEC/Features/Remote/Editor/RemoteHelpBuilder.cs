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
                Text("Hold", hint, font, 18, "下に倒す", new Vector2(-5, -39), new Vector2(65, 28));
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
            EnsureHoldProgress(remote);
            EnsureCallIcon(remote);
            UpdateCommands(remote, setup.data);
            RemoteConnections.Save(remote);
        }

        // This narrow upgrade preserves existing help layout, text, connections and overrides.
        public static void EnsureHoldProgress(GestureRemote remote)
        {
            if (remote == null || remote.helpHint == null) return;
            Undo.RecordObject(remote, "Add hold progress references");
            var ring = remote.helpHint.Find("Stick ring") as RectTransform;
            if (remote.holdProgress == null && ring != null)
            {
                var existing = remote.helpHint.Find("Hold progress");
                if (existing != null) remote.holdProgress = existing.GetComponent<Image>();
                else
                {
                    var go = new GameObject("Hold progress", typeof(RectTransform), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(go, "Add hold progress ring");
                    go.transform.SetParent(remote.helpHint, false);
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = ring.anchorMin; rect.anchorMax = ring.anchorMax; rect.pivot = ring.pivot;
                    rect.anchoredPosition = ring.anchoredPosition; rect.sizeDelta = ring.sizeDelta + Vector2.one * 8f;
                    var image = go.GetComponent<Image>();
                    image.sprite = HoldRingSprite(); image.color = remote.matchedColor;
                    image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Radial360;
                    image.fillOrigin = (int)Image.Origin360.Top; image.fillClockwise = true;
                    image.fillAmount = 0f; image.raycastTarget = false;
                    remote.holdProgress = image;
                }
            }
            if (remote.holdCaption == null)
            {
                var caption = remote.helpHint.Find("Hold");
                if (caption != null) remote.holdCaption = caption.GetComponent<Text>();
            }
            RemoteConnections.Save(remote);
        }

        // Adds only the call pictogram; existing layout and guide visuals are untouched.
        public static void EnsureCallIcon(GestureRemote remote)
        {
            if (remote == null || remote.helpHint == null || remote.helpHint.Find("Remote call icon") != null) return;
            var go = new GameObject("Remote call icon", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Add remote call pictogram");
            go.transform.SetParent(remote.helpHint, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(34, 78);
            rect.anchoredPosition = new Vector2(69, 8);
            rect.localRotation = Quaternion.Euler(0, 0, -15);
            Color ink = new Color(.93f, .96f, 1f);
            Color accent = remote.matchedColor;
            IconPart("Left edge", rect, new Vector2(-15, 0), new Vector2(3, 72), ink);
            IconPart("Right edge", rect, new Vector2(15, 0), new Vector2(3, 72), ink);
            IconPart("Top edge", rect, new Vector2(0, 36), new Vector2(33, 3), ink);
            IconPart("Bottom edge", rect, new Vector2(0, -36), new Vector2(33, 3), ink);
            IconPart("Indicator", rect, new Vector2(0, 24), new Vector2(18, 5), accent);
            IconPart("Main button", rect, new Vector2(0, 9), new Vector2(10, 10), accent);
            for (int row = 0; row < 2; row++) for (int column = 0; column < 2; column++)
                IconPart("Button " + row + " " + column, rect, new Vector2(-6 + column * 12, -8 - row * 12), new Vector2(5, 5), ink);
            go.SetActive(false);
            EditorUtility.SetDirty(remote.helpHint);
            if (remote.gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(remote.gameObject.scene);
        }

        private static void IconPart(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Add remote call pictogram part");
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
        }

        private static Sprite HoldRingSprite()
        {
            string folder = aki_lua87.AVEC.Editor.PackageLayout.Root + "/Features/Remote/Art";
            string path = folder + "/HoldProgressRing.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            System.IO.Directory.CreateDirectory(folder);
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f).magnitude;
                float alpha = Mathf.Clamp01(Mathf.Min(radius - 56f, 62f - radius));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void UpdateCommands(GestureRemote remote, GestureData data)
        {
            if (remote == null || remote.helpCommands == null || data == null) return;
            string text = GestureGuideBuilder.BuildText(data, remote);
            if (remote.helpCommands.text != text)
            {
                Undo.RecordObject(remote.helpCommands, "Update hand commands");
                remote.helpCommands.text = text;
                RemoteConnections.Save(remote.helpCommands);
            }
            var rect = remote.helpCommands.rectTransform;
            float extra = Mathf.Max(0f, Mathf.Ceil(remote.helpCommands.preferredHeight) - rect.rect.height);
            if (extra <= 0f) return;
            Undo.RecordObject(rect, "Fit added remote command");
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.height + extra);
            RemoteConnections.Save(rect);
            var panel = remote.helpPanel as RectTransform;
            if (panel != null)
            {
                Undo.RecordObject(panel, "Fit added remote command panel");
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panel.rect.height + extra);
                RemoteConnections.Save(panel);
            }
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
