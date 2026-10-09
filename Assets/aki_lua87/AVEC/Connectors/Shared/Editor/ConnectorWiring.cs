using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace aki_lua87.AVEC.Connectors.Editor
{
    // Shared implementation for the shipped connector definitions, not for feature packages.
    public static class ConnectorWiring
    {
        static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
        static UnityEngine.Object Ref(Component c,string field) => c == null ? null : new SerializedObject(c).FindProperty(field)?.objectReferenceValue;
        static Component Authority(Component player,int kind) => kind==2 ? Ref(player,"core") as Component : kind==3 ? Ref(player,"_controller") as Component : player;
        public static void ConfigurePlaylist(Component adapter,Component player,int kind)
        {
            var type=adapter.GetType();
            if (kind == 1)
            {
                var audios = player.GetComponentsInChildren<AudioSource>(true);
                type.GetField("audioSources").SetValue(adapter, audios);
                var sliders = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Slider>(true).Where(s => s.gameObject.scene == player.gameObject.scene &&
                    Enumerable.Range(0, s.onValueChanged.GetPersistentEventCount()).Any(i => s.onValueChanged.GetPersistentMethodName(i) == "set_volume" && audios.Contains(s.onValueChanged.GetPersistentTarget(i) as AudioSource))).ToArray();
                type.GetField("volumeSliders").SetValue(adapter, sliders);
                var moduleType = Resolve("Kinel.VideoPlayer.Udon.Module.AudioModule");
                if (moduleType != null)
                {
                    var modules = UnityEngine.Object.FindObjectsOfType(moduleType, true).OfType<Component>().Where(m =>
                    {
                        var sources = moduleType.GetField("sources").GetValue(m) as AudioSource[];
                        return m.gameObject.scene == player.gameObject.scene && sources != null && sources.Length > 0 && sources.All(a => a != null && audios.Contains(a));
                    }).ToArray();
                    var array = Array.CreateInstance(moduleType, modules.Length);
                    for (int i = 0; i < modules.Length; i++) array.SetValue(modules[i], i);
                    type.GetField("muteModules").SetValue(adapter, array);
                }
            }
            if (kind == 3)
            {
                var serialized = new SerializedObject(player);
                type.GetField("controller").SetValue(adapter, serialized.FindProperty("_controller")?.objectReferenceValue);
            }

        }
        public static string ValidatePlaylist(Component adapter,Component player,int kind)
        {
            var type=adapter.GetType();
            if (kind == 3 && type.GetField("controller").GetValue(adapter) == null) return "YamaPlayer UIのControllerが未設定です。プレイヤーの設定を確認してください。";
            if (kind == 3 && (UnityEngine.Object)type.GetField("controller").GetValue(adapter) != new SerializedObject(player).FindProperty("_controller")?.objectReferenceValue) return "YamaPlayerのController参照が変更されています。「接続設定を修復」で更新してください。";

            return null;
        }
        public static void ConfigureRemote(Component target,Component player,int kind)
        {
            if(kind==3) target.GetType().GetField("controller").SetValue(target,Ref(player,"_controller"));
        }
        public static AudioSource[] AudioSources(Component player,int kind)
        {
            if(player==null) return new AudioSource[0];
            var authority = Authority(player, kind);
            var sources = new List<AudioSource>();

            if (authority != null) {
                string fieldName = kind == 0 ? "speaker" : kind == 2 ? "audioSources" : kind == 3 ? "_audioSources" : null;
                if (fieldName != null) {
                    var field = authority.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    var assigned = field?.GetValue(authority) as AudioSource[];
                    if (assigned != null) sources.AddRange(assigned);
                }
                if (kind == 1) sources.AddRange(player.GetComponentsInChildren<AudioSource>(true));
                if (kind == 0) {
                    // iwaSync3 also fills its runtime speaker array during build.
                    var speakerType = Resolve("HoshinoLabs.IwaSync3.Speaker");
                    var mainType = Resolve("HoshinoLabs.IwaSync3.IwaSync3");
                    if (speakerType != null && mainType != null)
                        foreach (var speaker in Resources.FindObjectsOfTypeAll(speakerType).OfType<Component>()) {
                            if (EditorUtility.IsPersistent(speaker) || speaker.gameObject.scene != player.gameObject.scene) continue;
                            var owner = speaker.GetComponentInParent(mainType) ?? new SerializedObject(speaker).FindProperty("iwaSync3")?.objectReferenceValue as Component;
                            if (owner == null) {
                                var owners = Resources.FindObjectsOfTypeAll(mainType).OfType<Component>().Where(c => !EditorUtility.IsPersistent(c) && c.gameObject.scene == player.gameObject.scene).ToArray();
                                if (owners.Length == 1) owner = owners[0];
                            }
                            for (int step = 0; owner != null && step < 64; step++) {
                                var next = new SerializedObject(owner).FindProperty("iwaSync3")?.objectReferenceValue as Component;
                                if (next == null || next == owner) break;
                                owner = next;
                            }
                            if (owner != null && owner.GetComponentsInChildren(authority.GetType(), true).Contains(authority))
                                sources.AddRange(speaker.GetComponentsInChildren<AudioSource>(true));
                        }
                }
                if (kind == 3) {
                    // YamaPlayer registers speakers at build time; match their explicit controller now.
                    var speakerType = Resolve("Yamadev.YamaStream.YamaPlayerSpeaker");
                    if (speakerType != null)
                        foreach (var speaker in Resources.FindObjectsOfTypeAll(speakerType).OfType<Component>())
                            if (!EditorUtility.IsPersistent(speaker) && speaker.gameObject.scene == player.gameObject.scene && speakerType.GetField("controller")?.GetValue(speaker) as Component == authority)
                                sources.Add(speaker.GetComponent<AudioSource>());
                }
            }

            return sources.Where(s=>s!=null).Distinct().ToArray();
        }
        public static void DetectOutputs(Component target, Component player, int kind)
        {
            var renderers = new List<Renderer>();
            var graphics = new List<Graphic>();
            if (kind == 0)
            {
                var screenType = Resolve("HoshinoLabs.IwaSync3.Udon.VideoScreen");
                if (screenType != null)
                    foreach (Component screen in UnityEngine.Object.FindObjectsOfType(screenType, true))
                    {
                        Component owner = Ref(screen, "core") as Component;
                        // iwaSync keeps runtime references empty until its authoring/build bake.
                        // Resolve its authored controller without modifying the native player.
                        if (owner == null)
                        {
                            var mainType = Resolve("HoshinoLabs.IwaSync3.IwaSync3");
                            var authorType = Resolve("HoshinoLabs.IwaSync3.Screen");
                            var author = authorType == null ? null : screen.GetComponentsInParent(authorType, true).OfType<Component>().FirstOrDefault();
                            var main = Ref(author, "iwaSync3") as Component;
                            if (main == null && mainType != null) main = screen.GetComponentsInParent(mainType, true).OfType<Component>().FirstOrDefault();
                            for (int i = 0; main != null && i < 16; i++)
                            {
                                var next = Ref(main, "iwaSync3") as Component;
                                if (next == null || next == main) break;
                                main = next;
                            }
                            owner = main == null ? null : main.GetComponentsInChildren(player.GetType(), true).OfType<Component>().FirstOrDefault();
                        }
                        if (owner == player && Ref(screen, "screen") is Renderer renderer) renderers.Add(renderer);
                    }
            }
            if (kind == 1)
            {
                var screenType = Resolve("Kinel.VideoPlayer.Udon.Module.KinelScreenModule");
                if (screenType != null)
                    foreach (Component screen in UnityEngine.Object.FindObjectsOfType(screenType, true))
                        if (Ref(screen, "videoPlayer") == player && screen.transform.childCount > 0)
                        { var r = screen.transform.GetChild(0).GetComponent<Renderer>(); if (r != null) renderers.Add(r); }
                var audios = player.GetComponentsInChildren<AudioSource>(true);
                var type = target.GetType();
                type.GetField("audioSources").SetValue(target, audios);
                var sliders = UnityEngine.Object.FindObjectsOfType<Slider>(true).Where(s => s.gameObject.scene == player.gameObject.scene &&
                    Enumerable.Range(0, s.onValueChanged.GetPersistentEventCount()).Any(i => s.onValueChanged.GetPersistentMethodName(i) == "set_volume" && audios.Contains(s.onValueChanged.GetPersistentTarget(i) as AudioSource))).ToArray();
                type.GetField("volumeSliders").SetValue(target, sliders);
                var moduleType = Resolve("Kinel.VideoPlayer.Udon.Module.AudioModule");
                var modules = UnityEngine.Object.FindObjectsOfType(moduleType, true).OfType<Component>().Where(m =>
                {
                    var sources = moduleType.GetField("sources").GetValue(m) as AudioSource[];
                    return sources != null && sources.Length > 0 && sources.All(a => a != null && audios.Contains(a));
                }).ToArray();
                var array = Array.CreateInstance(moduleType, modules.Length);
                for (int i = 0; i < modules.Length; i++) array.SetValue(modules[i], i);
                type.GetField("muteModules").SetValue(target, array);
            }
            if (kind == 2 || kind == 3)
            {
                var core = Ref(player, kind == 2 ? "core" : "_controller") as Component;
                if (core != null)
                {
                    if (kind == 2)
                    {
                        var configType = Resolve("JLChnToZ.VRC.VVMW.Designer.ScreenConfigurator");
                        if (configType != null)
                            foreach (Component config in UnityEngine.Object.FindObjectsOfType(configType, true))
                            {
                                var owner = Ref(config, "core") as Component ?? Nearby(config, core.GetType());
                                if (owner == core && configType.GetProperty("Renderer").GetValue(config) is Renderer r) renderers.Add(r);
                            }
                    }
                    else
                    {
                        var screenType = Resolve("Yamadev.YamaStream.YamaPlayerScreen");
                        if (screenType != null)
                            foreach (Component screen in UnityEngine.Object.FindObjectsOfType(screenType, true))
                            {
                                var owner = Ref(screen, "controller") as Component ?? Nearby(screen, core.GetType());
                                if (owner != core) continue;
                                var output = screenType.GetProperty("Reference").GetValue(screen);
                                if (output is Renderer r) renderers.Add(r);
                                else if (output is Graphic g) graphics.Add(g);
                            }
                    }
                    var property = new SerializedObject(core).FindProperty(kind == 2 ? "screenTargets" : "_screens");
                    if (property != null && property.isArray)
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var output = property.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (output is Renderer renderer) renderers.Add(renderer);
                            else if (output is Graphic graphic) graphics.Add(graphic);
                            // Material targets may feed multiple screens; discover their actual consumers.
                            else if (output is Material material)
                            {
                                renderers.AddRange(UnityEngine.Object.FindObjectsOfType<Renderer>(true).Where(r => r.gameObject.scene == player.gameObject.scene && r.sharedMaterials.Contains(material)));
                                graphics.AddRange(UnityEngine.Object.FindObjectsOfType<Graphic>(true).Where(g => g.gameObject.scene == player.gameObject.scene && g.material == material));
                            }
                        }
                }
            }
            target.GetType().GetField("screens").SetValue(target, renderers.Distinct().ToArray());
            target.GetType().GetField("screenGraphics").SetValue(target, graphics.Distinct().ToArray());
        }

        private static Component Nearby(Component component, Type type)
        {
            for (Transform t = component.transform; t != null; t = t.parent)
            {
                var candidates = t.GetComponentsInChildren(type, true).OfType<Component>().ToArray();
                if (candidates.Length == 1) return candidates[0];
                if (candidates.Length > 1) return null;
            }
            return null;
        }

    }
}
