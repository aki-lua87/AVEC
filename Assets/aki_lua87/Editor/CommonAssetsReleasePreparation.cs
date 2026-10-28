using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UdonSharp;
using UdonSharp.Compiler;

namespace aki_lua87.CommonAssets.Editor
{
    // Local receipts are invalidated by source, target, dependency, metadata or distribution-file changes.
    public static class CommonAssetsReleasePreparation
    {
        const string ReceiptPath = "Library/AVECRelease/prepared.json";
        [Serializable] public class Receipt { public string sources; public Item[] packages = new Item[0]; }
        [Serializable] public class Item { public string name; public string fingerprint; }

        static object InvokeSdk(string typeName, string method, params object[] args)
        {
            var type = typeof(UdonSharpProgramAsset).Assembly.GetType(typeName);
            var info = type?.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null) throw new InvalidOperationException("SDKの準備状態を確認できません: " + typeName + "." + method);
            return info.Invoke(null, args);
        }
        static bool SourcesDirty() => (bool)InvokeSdk("UdonSharp.UdonSharpProgramAsset", "IsAnyScriptDirty");
        static bool ProgramsOutdated() => (bool)InvokeSdk("UdonSharp.UdonSharpProgramAsset", "IsAnyProgramAssetOutOfDate");

        static string Fingerprint(IEnumerable<string> input)
        {
            var paths = new SortedSet<string>(input.Select(p => p.Replace('\\', '/')), StringComparer.Ordinal);
            var text = new StringBuilder(Application.unityVersion + "|" + EditorUserBuildSettings.activeBuildTarget + "\n");
            using (var sha = SHA256.Create())
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) continue;
                    using (var stream = File.OpenRead(path))
                        text.Append(path).Append('|').Append(BitConverter.ToString(sha.ComputeHash(stream))).Append('\n');
                }
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "");
        }

        static string SourceFingerprint()
        {
            var sources = ((IEnumerable<string>)InvokeSdk("UdonSharp.Compiler.CompilationContext", "GetAllFilteredSourcePaths", true)).ToList();
            sources.AddRange(new[] { "Packages/manifest.json", "Packages/packages-lock.json" });
            return Fingerprint(sources);
        }

        static string PackageFingerprint(int index)
        {
            var paths = CommonAssetsReleaseBuilder.CollectPaths(index);
            var dependencies = AssetDatabase.GetDependencies(paths, true).Where(p => !AssetDatabase.IsValidFolder(p)).ToArray();
            return Fingerprint(dependencies.Concat(dependencies.Select(p => p + ".meta")).Concat(new[] {
                "README.md", "Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs",
                "Assets/aki_lua87/Editor/CommonAssetsReleaseValidation.cs", "Assets/aki_lua87/Editor/CommonAssetsReleasePreparation.cs"
            }));
        }

        public static void AssertPrepared(bool[] selected)
        {
            if (!File.Exists(ReceiptPath)) throw new InvalidOperationException("配布準備を先に実行してください。");
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
            if (receipt == null || receipt.sources != SourceFingerprint() || SourcesDirty() || ProgramsOutdated())
                throw new InvalidOperationException("ソース・SDK・ビルド対象が変更されています。配布準備を再実行してください。");
            foreach (int index in CommonAssetsReleaseValidation.Selection(selected))
            {
                string name = CommonAssetsReleaseBuilder.Packages[index].Name;
                if (!(receipt.packages ?? new Item[0]).Any(p => p.name == name && p.fingerprint == PackageFingerprint(index)))
                    throw new InvalidOperationException("配布内容が未準備または変更されています: " + name + "。配布準備を再実行してください。");
            }
        }

        public static void Prepare(bool[] selected)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("コンパイル・Playモードの終了を待ってください。");
            var indices = CommonAssetsReleaseValidation.Selection(selected);
            foreach (int i in indices) CommonAssetsReleaseValidation.ValidatePlaylistTemplates(CommonAssetsReleaseBuilder.CollectPaths(i));
            bool missing = indices.SelectMany(CommonAssetsReleaseValidation.Programs).Any(p => CommonAssetsReleaseValidation.CompiledProgram(p) == null);
            if (SourcesDirty() || ProgramsOutdated() || missing || UdonSharpProgramAsset.AnyUdonSharpScriptHasError())
            {
                string error = null;
                Application.LogCallback capture = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception) error = message; };
                Application.logMessageReceived += capture;
                try { CommonAssetsReleaseBuilder.Measure("UdonSharpコンパイル", () => UdonSharpCompilerV1.CompileSync()); }
                finally { Application.logMessageReceived -= capture; }
                if (error != null || UdonSharpProgramAsset.AnyUdonSharpScriptHasError() || SourcesDirty() || ProgramsOutdated())
                    throw new InvalidOperationException("UdonSharpコンパイルが完了していません: " + error);
            }
            else Debug.Log("[AVEC Release] ソース変更なし。UdonSharpコンパイルを省略しました。");
            CommonAssetsReleaseBuilder.Measure("Program配置・対象保存", () => {
                foreach (int i in indices)
                {
                    foreach (var program in CommonAssetsReleaseValidation.Programs(i))
                    {
                        var compiled = CommonAssetsReleaseValidation.CompiledProgram(program);
                        if (compiled == null) throw new InvalidOperationException("コンパイル済みProgramがありません: " + program.name);
                        string source = AssetDatabase.GetAssetPath(compiled);
                        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(program)).Replace('\\', '/') + "/Programs";
                        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), "Programs");
                        string target = folder + "/" + Path.GetFileName(source);
                        if (source != target) { string error = AssetDatabase.MoveAsset(source, target); if (!string.IsNullOrEmpty(error)) throw new IOException(error); }
                        AssetDatabase.SaveAssetIfDirty(program); AssetDatabase.SaveAssetIfDirty(compiled);
                    }
                    foreach (string path in CommonAssetsReleaseBuilder.CollectPaths(i).Where(p => !AssetDatabase.IsValidFolder(p)))
                        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(path));
                }
            });
            CommonAssetsReleaseValidation.Validate(selected, false);
            var receipt = new Receipt { sources = SourceFingerprint(), packages = indices.Select(i => new Item {
                name = CommonAssetsReleaseBuilder.Packages[i].Name, fingerprint = PackageFingerprint(i)
            }).ToArray() };
            Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath)); File.WriteAllText(ReceiptPath, JsonUtility.ToJson(receipt, true));
            Debug.Log("[AVEC Release] 配布準備完了。検証・パッケージ出力を実行できます。");
        }
    }
}
