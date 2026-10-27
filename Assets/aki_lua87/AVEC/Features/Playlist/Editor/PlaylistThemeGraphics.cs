using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistThemeGraphics
    {
        public const string PresetPath = PlaylistFactory.Root + "/Samples/Themes/Wireframe.asset";
        const string SpritePath = PlaylistFactory.Root + "/Icons/wireframe-rounded.png";

        // A cloned themed row also needs the authored sprites for future theme changes.
        internal static void CopyRowStates(PlaylistSetup setup, GameObject source, GameObject clone)
        {
            if (setup.themeImageStates == null || setup.themeImageStates.Length == 0) return;
            var originals = source.GetComponentsInChildren<Image>(true);
            var copies = clone.GetComponentsInChildren<Image>(true);
            var states = new List<PlaylistThemeImageState>(setup.themeImageStates.Where(s => s != null && s.image != null));
            for (int i = 0; i < originals.Length; i++)
            {
                var state = states.FirstOrDefault(s => s.image == originals[i]);
                if (state == null) continue;
                states.Add(new PlaylistThemeImageState {
                    image = copies[i], sprite = state.sprite, overrideSprite = state.overrideSprite, type = state.type,
                    fillCenter = state.fillCenter, preserveAspect = state.preserveAspect,
                    pixelsPerUnitMultiplier = state.pixelsPerUnitMultiplier, alphaHitTestMinimumThreshold = state.alphaHitTestMinimumThreshold,
                    colorSuppressed = state.colorSuppressed, originalColor = state.originalColor
                });
            }
            PlaylistUndo.RecordObject(setup, "Keep cloned row theme sprites");
            setup.themeImageStates = states.ToArray(); PlaylistConnections.Save(setup);
        }

        // Restore original sprites before changing themes or cloning authored row templates.
        public static void Restore(PlaylistSetup setup)
        {
            if ((setup.themeImageStates == null || setup.themeImageStates.Length == 0) &&
                (setup.themeLayoutStates == null || setup.themeLayoutStates.Length == 0)) return;
            PlaylistUndo.RecordObject(setup, "Restore playlist theme graphics");
            foreach (var state in setup.themeImageStates ?? new PlaylistThemeImageState[0])
            {
                if (state == null || state.image == null) continue;
                var image = state.image; PlaylistUndo.RecordObject(image, "Restore authored UI sprite");
                image.sprite = state.sprite; image.overrideSprite = state.overrideSprite; image.type = state.type;
                image.fillCenter = state.fillCenter; image.preserveAspect = state.preserveAspect;
                image.pixelsPerUnitMultiplier = state.pixelsPerUnitMultiplier;
                if (image.alphaHitTestMinimumThreshold != state.alphaHitTestMinimumThreshold)
                    image.alphaHitTestMinimumThreshold = state.alphaHitTestMinimumThreshold;
                if (state.colorSuppressed) image.color = state.originalColor;
                PlaylistConnections.Save(image);
            }
            foreach (var state in setup.themeLayoutStates ?? new PlaylistThemeLayoutState[0])
            {
                if (state == null || state.layout == null) continue;
                PlaylistUndo.RecordObject(state.layout, "Restore menu spacing");
                var padding = state.layout.padding;
                state.layout.padding = new RectOffset(padding.left, state.rightPadding, padding.top, padding.bottom);
                PlaylistConnections.Save(state.layout);
            }
            setup.themeLayoutStates = new PlaylistThemeLayoutState[0];
            setup.themeImageStates = new PlaylistThemeImageState[0]; PlaylistConnections.Save(setup);
        }

        // Only new/un-styled Images are changed, so adding a special list preserves existing overrides.
        public static void ApplyNew(PlaylistSetup setup, Image only = null)
        {
            var theme = setup.theme;
            if (theme == null || !theme.outlineOnly) return;
            if (theme.outlineSprite == null)
            {
                PlaylistUndo.RecordObject(theme, "Assign wireframe sprite"); theme.outlineSprite = EnsureSprite();
                EditorUtility.SetDirty(theme); AssetDatabase.SaveAssetIfDirty(theme);
            }
            var states = new List<PlaylistThemeImageState>(setup.themeImageStates ?? new PlaylistThemeImageState[0]);
            var known = new HashSet<Image>(states.Where(s => s != null && s.image != null).Select(s => s.image));
            PlaylistUndo.RecordObject(setup, "Keep original playlist sprites");
            foreach (var image in setup.GetComponentsInChildren<Image>(true))
            {
                if (setup.view != null && setup.view.controlTip != null && image.transform.IsChildOf(setup.view.controlTip)) continue;
                if (only != null && image != only) continue;
                if ((only == null && known.Contains(image)) || IsForeground(setup.view, image) || IsViewport(image)) continue;
                if (!known.Contains(image)) states.Add(new PlaylistThemeImageState { image = image, sprite = image.sprite, overrideSprite = image.overrideSprite == image.sprite ? null : image.overrideSprite,
                    type = image.type, fillCenter = image.fillCenter, preserveAspect = image.preserveAspect,
                    pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier, alphaHitTestMinimumThreshold = image.alphaHitTestMinimumThreshold });
                PlaylistUndo.RecordObject(image, "Apply transparent outline graphics");
                image.overrideSprite = null; image.sprite = theme.outlineSprite; image.type = Image.Type.Sliced;
                image.fillCenter = false; image.preserveAspect = false;
                image.pixelsPerUnitMultiplier = 2f / Mathf.Clamp(theme.outlineWidth, .5f, 4f);
                // Hit testing covers the control rectangle, including its transparent interior.
                if (image.alphaHitTestMinimumThreshold != 0) image.alphaHitTestMinimumThreshold = 0;
                PlaylistConnections.Save(image);
            }
            setup.themeImageStates = states.ToArray(); PlaylistConnections.Save(setup);
            if (only == null) RefreshFrameSpacing(setup);
        }

        // Narrow migration for existing wireframe instances: preserve their palette and controls.
        public static void RefreshFrameSpacing(PlaylistSetup setup)
        {
            if (setup == null || setup.theme == null || !setup.theme.outlineOnly) return;
            PlaylistUndo.RecordObject(setup, "Simplify wireframe frames");
            foreach (var state in setup.themeImageStates ?? new PlaylistThemeImageState[0])
            {
                var image = state == null ? null : state.image;
                if (image == null || !IsRedundantFrame(image)) continue;
                if (!state.colorSuppressed) { state.originalColor = image.color; state.colorSuppressed = true; }
                if (image.color.a == 0) continue;
                PlaylistUndo.RecordObject(image, "Remove overlapping container frame");
                var color = image.color; color.a = 0; image.color = color;
                PlaylistConnections.Save(image);
            }
            foreach (var content in new[] { setup.tabContent, setup.videoContent })
            {
                if (content == null) continue;
                var layout = content.GetComponent<VerticalLayoutGroup>();
                var scroll = content.GetComponentInParent<ScrollRect>(true);
                if (layout != null && scroll != null && scroll.verticalScrollbar != null)
                {
                    // Newly cloned UI can still have pending ScrollRect-driven viewport geometry.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.GetComponent<RectTransform>());
                    var corners = new Vector3[4];
                    scroll.verticalScrollbar.GetComponent<RectTransform>().GetWorldCorners(corners);
                    float scrollbarLeft = content.InverseTransformPoint(corners[0]).x;
                    var state = setup.themeLayoutStates == null ? null : setup.themeLayoutStates.FirstOrDefault(s => s != null && s.layout == layout);
                    int originalPadding = state == null ? layout.padding.right : state.rightPadding;
                    // Ignore sub-pixel transform noise so switching themes cannot grow the inset.
                    // Hidden video panels have no driven viewport geometry yet. Reserve the
                    // scrollbar width in that case without activating or resetting the panel.
                    float overlap = content.rect.width > .01f
                        ? content.rect.xMax - scrollbarLeft
                        : scroll.verticalScrollbar.GetComponent<RectTransform>().rect.width;
                    int extra = Mathf.Max(0, Mathf.CeilToInt(overlap - originalPadding + 6f - .01f));
                    if (extra > 0 || state != null)
                    {
                        if (state == null)
                        {
                            var layouts = new List<PlaylistThemeLayoutState>(setup.themeLayoutStates ?? new PlaylistThemeLayoutState[0]);
                            layouts.Add(new PlaylistThemeLayoutState { layout = layout, rightPadding = originalPadding });
                            setup.themeLayoutStates = layouts.ToArray();
                        }
                        if (layout.padding.right != originalPadding + extra)
                        {
                            PlaylistUndo.RecordObject(layout, "Reserve a separate scrollbar lane");
                            var padding = layout.padding;
                            layout.padding = new RectOffset(padding.left, originalPadding + extra, padding.top, padding.bottom);
                            PlaylistConnections.Save(layout);
                            LayoutRebuilder.MarkLayoutForRebuild(content);
                        }
                    }
                }
            }
            PlaylistConnections.Save(setup);
        }
        static bool IsRedundantFrame(Image image)
        {
            return image.name == "Tabs" || image.name == "Videos" || image.GetComponent<Scrollbar>() != null ||
                image.name == "Volume rail" || image.name == "Volume fill";
        }
        public static bool IsViewport(Image image) => image.name.IndexOf("viewport", System.StringComparison.OrdinalIgnoreCase) >= 0;
        public static bool IsForeground(PlaylistView view, Image image)
        {
            if (view != null && view.remoteToggleButton != null && image.gameObject != view.remoteToggleButton &&
                image.transform.IsChildOf(view.remoteToggleButton.transform)) return true;
            string name = image.name;
            if (name == "Handle" || name == "Volume fill" || name == "Track") return false;
            return name == "Icon" || name == "Tab kind icon" || name == "Playback progress" ||
                name == "Accent" || name == "Divider" || image.type == Image.Type.Filled ||
                (view != null && (image == view.playbackModeIcon || image == view.volumeIcon || image == view.transportIcon));
        }

        internal static Sprite EnsureSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); if (existing != null) return existing;
            // Rounded rectangle signed-distance ring, supersampled by analytic edge coverage.
            // Nine-slicing keeps line width independent of panel dimensions.
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + .5f - 32) - 19f;
                float qy = Mathf.Abs(y + .5f - 32) - 19f;
                float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - 12f;
                float alpha = Mathf.Clamp01(.5f - d) * Mathf.Clamp01(d + 2.5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply();
            System.IO.File.WriteAllBytes(SpritePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(16, 16, 16, 16); importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
