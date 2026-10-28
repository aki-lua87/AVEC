using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UdonSharp;
using aki_lua87.CommonVideoPlaylist;

namespace aki_lua87.CommonAssets.Editor
{
    // Read-only release checks. Never invoke SerializedProgramAsset's lazy, asset-creating getter here.
    public static class CommonAssetsReleaseValidation
    {
        public const string Root = "Assets/aki_lua87/AVEC";

        public static int[] Selection(bool[] selected)
        {
            var specs = CommonAssetsReleaseBuilder.Packages;
            if (selected == null || selected.Length != specs.Length) throw new ArgumentException("パッケージ選択が不正です。");
            var indices = Enumerable.Range(0, specs.Length).Where(i => selected[i]).ToArray();
            if (indices.Length == 0) throw new InvalidOperationException("出力するパッケージを選択してください。");
            foreach (int i in indices)
                if (!specs[i].Folders.All(Directory.Exists) || !specs[i].Assets.All(File.Exists))
                    throw new InvalidOperationException("配布対象が不足しています: " + specs[i].Name);
            return indices;
        }

        public static UnityEngine.Object CompiledProgram(UdonSharpProgramAsset program)
        {
            return new SerializedObject(program).FindProperty("serializedUdonProgramAsset")?.objectReferenceValue;
        }

        public static UdonSharpProgramAsset[] Programs(int index)
        {
            return CommonAssetsReleaseBuilder.CollectPaths(index)
                .Where(p => AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(UdonSharpProgramAsset))
                .Select(AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>).ToArray();
        }

        public static void Validate(bool[] selected, bool requirePrepared = true)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("コンパイル・Playモードの終了を待ってください。");
            foreach (int i in Selection(selected))
            {
                var paths = CommonAssetsReleaseBuilder.CollectPaths(i);
                foreach (string path in paths)
                {
                    if (!path.StartsWith(Root + "/", StringComparison.Ordinal) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                        AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(PlaylistData))
                        throw new InvalidOperationException("配布対象外のアセットです: " + path);
                    if (!File.Exists(path) && !AssetDatabase.IsValidFolder(path)) throw new FileNotFoundException("配布対象が見つかりません。", path);
                }
                ValidatePlaylistTemplates(paths);
                ValidateDependencies(i, paths);
                var programs = Programs(i);
                var usedPrograms = new HashSet<string>(programs.Select(CompiledProgram).Where(p => p != null).Select(AssetDatabase.GetAssetPath));
                foreach (string path in paths)
                    if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(VRC.Udon.ProgramSources.SerializedUdonProgramAsset) && !usedPrograms.Contains(path))
                        throw new InvalidOperationException("参照のないコンパイル済みProgramが配布対象に含まれています: " + path);
                foreach (var program in programs)
                {
                    var compiled = CompiledProgram(program);
                    if (program.sourceCsScript == null || compiled == null || program.CompiledVersion != UdonSharpProgramVersion.CurrentVersion ||
                        compiled.name != AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(program)))
                        throw new InvalidOperationException("Programが未準備です。配布準備を実行してください: " + program.name);
                    string compiledPath = AssetDatabase.GetAssetPath(compiled);
                    string expectedFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(program)).Replace('\\', '/') + "/Programs/";
                    if (!compiledPath.StartsWith(expectedFolder, StringComparison.Ordinal) || !paths.Contains(compiledPath))
                        throw new InvalidOperationException("コンパイル済みProgramの配置が未準備です: " + compiledPath);
                }
                if (requirePrepared)
                {
                    foreach (string path in paths.Where(p => !AssetDatabase.IsValidFolder(p)))
                        if (EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path)))
                            throw new InvalidOperationException("配布アセットに未保存変更があります。配布準備を実行してください: " + path);
                }
            }
            if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new InvalidOperationException("UdonSharpのコンパイルエラーを解消してください。");
            if (requirePrepared) CommonAssetsReleasePreparation.AssertPrepared(selected);
        }

        public static void ValidatePlaylistTemplates(string[] paths)
        {
            foreach (string path in paths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) throw new InvalidOperationException("Prefabを読み込めません: " + path);
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.GetComponents<Component>().Any(c => c == null)) throw new InvalidOperationException("Missing Scriptがあります: " + path);
                foreach (var setup in root.GetComponentsInChildren<PlaylistSetup>(true))
                {
                    var v = setup.view;
                    if (v != null) UdonSharpEditor.UdonSharpEditorUtility.CopyUdonToProxy(v);
                    if (setup.data != null || v == null || v.tabNames.Length != 0 || v.titles.Length != 0 || v.urls.Length != 0 ||
                        v.titleSourceUrls.Length != 0 || v.tabImages.Length != 0 || v.videoImages.Length != 0 || v.videoLabels.Length != 0 ||
                        v.tabDescriptions.Length != 0 || v.videoTabs.Length != 0 || v.dynamicTitleIndices.Length != 0)
                        throw new InvalidOperationException("配布Prefabにプレイリストデータ・行参照が残っています: " + path);
                    if (setup.tabContent == null || setup.videoContent == null || setup.tabTemplate == null || setup.videoTemplate == null)
                        throw new InvalidOperationException("行生成用の参照が不足しています: " + path);
                    foreach (var t in setup.videoContent.Cast<Transform>())
                        if (t.gameObject != setup.videoTemplate && t.gameObject != v.videoTopSpacer?.gameObject && t.gameObject != v.videoBottomSpacer?.gameObject)
                            throw new InvalidOperationException("配布Prefabに生成済み動画行が残っています: " + path + "/" + t.name);
                    foreach (var t in setup.tabContent.Cast<Transform>())
                        if (t.gameObject != setup.tabTemplate && t.gameObject != v.queueTabImage?.gameObject &&
                            t.gameObject != v.historyTabImage?.gameObject && t.gameObject != v.userTabImage?.gameObject)
                            throw new InvalidOperationException("配布Prefabに生成済みタブが残っています: " + path + "/" + t.name);
                    if (setup.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Add to queue") ||
                        setup.GetComponentsInChildren<PlaylistButton>(true).Any(b => b.enqueue) || v.queueAddButtons.Length != 0)
                        throw new InvalidOperationException("旧キュー追加ボタンが残っています: " + path);
                    if (v.GetComponents<aki_lua87.AVEC.FeatureExtensionBehaviour>().Length != 0 || v.extensions.Length != 0)
                        throw new InvalidOperationException("空の配布Prefabに不要な任意拡張が残っています: " + path);
                    if ((setup.enableQueue && (v.queueRows.Length != 32 || v.queueLabels.Length != 32)) ||
                        (setup.enableHistory && (v.historyRows.Length != 32 || v.historyLabels.Length != 32)) ||
                        (setup.enableUserList && (v.userRows.Length != 32 || v.userLabels.Length != 32)))
                        throw new InvalidOperationException("特殊リストの32件分の表示枠が不足しています: " + path);
                    // Proxy data and the actual backing data must both be empty.
                    var backing = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(v);
                    if (backing == null) throw new InvalidOperationException("PlaylistViewのBackingがありません: " + path);
                    string[] titles; VRC.SDKBase.VRCUrl[] urls;
                    backing.publicVariables.TryGetVariableValue("titles", out titles);
                    backing.publicVariables.TryGetVariableValue("urls", out urls);
                    if ((titles != null && titles.Length != 0) || (urls != null && urls.Length != 0))
                        throw new InvalidOperationException("Backingに動画データが残っています: " + path);
                }
            }
        }

        static void AddDeclaredPaths(int index, HashSet<int> visited, HashSet<string> allowed)
        {
            if (!visited.Add(index)) return;
            allowed.UnionWith(CommonAssetsReleaseBuilder.CollectPaths(index));
            var specs = CommonAssetsReleaseBuilder.Packages;
            foreach (string requirement in specs[index].Requires.Where(r => r.StartsWith("AVEC-", StringComparison.Ordinal)))
            {
                int dependency = Array.FindIndex(specs, s => s.Name == requirement);
                if (dependency < 0) throw new InvalidOperationException("未定義のパッケージ依存: " + requirement);
                AddDeclaredPaths(dependency, visited, allowed);
            }
        }

        static void ValidateDependencies(int index, string[] paths)
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            AddDeclaredPaths(index, new HashSet<int>(), allowed);
            foreach (string dependency in AssetDatabase.GetDependencies(paths, true))
                if (dependency.StartsWith(Root + "/", StringComparison.Ordinal) && !allowed.Contains(dependency))
                    throw new InvalidOperationException(CommonAssetsReleaseBuilder.Packages[index].Name + " に未宣言・未梱包の依存があります: " + dependency);
        }
    }
}
