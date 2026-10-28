using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

namespace aki_lua87.CommonAssets.Editor
{
    public class CommonAssetsReleaseBuilder : EditorWindow
    {
        const string Root = "Assets/aki_lua87/AVEC";
        public sealed class PackageSpec { public string Name; public string[] Folders; public string[] Assets = new string[0]; public string[] Requires; }
        public static readonly PackageSpec[] Packages = CreatePackages();
        static PackageSpec[] CreatePackages()
        {
            var packages = new System.Collections.Generic.List<PackageSpec> {
                new PackageSpec { Name="AVEC-Core", Folders=new[]{Root+"/Core",Root+"/Features/Playlist",Root+"/Features/Remote",Root+"/Features/SimpleScreen",Root+"/Extensions/PlaylistRemote"}, Assets=new[]{Root+"/Prefabs/AVEC-VideoPlaylist.prefab",Root+"/Prefabs/AVEC-VideoPlaylistTablet.prefab",Root+"/Prefabs/AVEC-VideoPlaylistCompact.prefab",Root+"/Prefabs/AVEC-GestureRemote.prefab",Root+"/Prefabs/AVEC-GestureGuide.prefab",Root+"/Prefabs/AVEC-SimpleScreen.prefab"}, Requires=new string[0] }
            };
            foreach (var player in new[]{"IwaSync","KineL","VizVid","YamaPlayer","ProTV","KineLV3"})
                packages.Add(new PackageSpec {
                    Name="AVEC-Connector-"+player,
                    Folders=(player == "KineLV3" ? new string[0] : new[]{Root+"/Connectors/Shared"}).Concat(new[]{Root+"/Connectors/"+player+"/Editor", Root+"/Connectors/"+player+"/Playlist", Root+"/Connectors/"+player+"/Remote"}).ToArray(),
                    Requires=new[]{"AVEC-Core", (player == "KineL" ? "KineL V2" : player == "KineLV3" ? "KineL V3" : player == "ProTV" ? "ProTV 3" : player)+" (external)"}
                });
            packages.Add(new PackageSpec { Name="AVEC-Extension-DynamicPlaylist", Folders=new[]{Root+"/Extensions/DynamicPlaylist"}, Requires=new[]{"AVEC-Core"} });
            packages.Add(new PackageSpec { Name="AVEC-Extension-RTabPlaylist", Folders=new[]{Root+"/Extensions/RTabPlaylist"}, Requires=new[]{"AVEC-Core", "AVEC-Connector-YamaPlayer", "AVEC-Extension-DynamicPlaylist", "RTab (external)", "YamaPlayer (external)"} });
            packages.Add(new PackageSpec { Name="AVEC-Extension-YttlTitles", Folders=new[]{Root+"/Extensions/YttlTitles"}, Requires=new[]{"AVEC-Core", "VizVid bundled YTTL (external)"} });
            return packages.ToArray();
        }
        static string[] Names => Packages.Select(p=>p.Name).ToArray();
        static bool Exists(int index) => Packages[index].Folders.All(Directory.Exists) && Packages[index].Assets.All(File.Exists);
        static string[] LegalAssets => new[]{Root+"/Core/LICENSE.txt",Root+"/Core/THIRD_PARTY_NOTICES.txt"};
        // Keep these authoring notes in the project, outside distributed packages.
        static readonly string[] ExcludedDocumentation = {
            Root + "/Features/Playlist/Tablet.md",
            Root + "/Features/Playlist/UserList.md"
        };
        public static string[] CollectPaths(int index) => AssetDatabase.FindAssets("",Packages[index].Folders).Select(AssetDatabase.GUIDToAssetPath).Concat(Packages[index].Assets).Concat(LegalAssets).Where(p=>!p.EndsWith(".unity",StringComparison.OrdinalIgnoreCase) && !ExcludedDocumentation.Contains(p, StringComparer.OrdinalIgnoreCase) && AssetDatabase.GetMainAssetTypeAtPath(p) != typeof(aki_lua87.CommonVideoPlaylist.PlaylistData)).Distinct().ToArray();
        public static void ValidatePlaylistTemplates(string[] paths) => CommonAssetsReleaseValidation.ValidatePlaylistTemplates(paths);
        static bool[] NormalizeSelection(bool[] selected, int selectionVersion)
        {
            if (selectionVersion == 2 && selected != null && selected.Length == Packages.Length) return selected;
            if (selectionVersion == 2 || selected == null) return Enumerable.Repeat(true,Packages.Length).ToArray();
            // Version 1 had a separate PlaylistRemote entry at index 8.
            if (selectionVersion == 1) {
                if (selected.Length != 11) return Enumerable.Repeat(true,Packages.Length).ToArray();
                return new[]{selected[0] || selected[8],selected[1],selected[2],selected[3],selected[4],selected[5],selected[6],selected[7],selected[9],selected[10]};
            }
            // First expand the original 11-entry layout into the split 19-entry layout.
            if (selected != null && selected.Length == 11) {
                var migrated = new bool[19]; migrated[0]=selected.Any(v=>v); migrated[1]=selected[0]; migrated[2]=selected[5];
                for(int i=0;i<4;i++){migrated[3+i*2]=selected[1+i];migrated[4+i*2]=selected[6+i];}
                migrated[11]=selected[0];migrated[12]=selected[10];selected=migrated;
            }
            // Either former feature selects its combined package. Missing later entries stay off.
            int[][] oldIndices = {new[]{0,1,2,12},new[]{3,4},new[]{5,6},new[]{7,8},new[]{9,10},new[]{14,15},new[]{17,18},new[]{11},new[]{13},new[]{16}};
            return oldIndices.Select(indices=>indices.Any(i=>i<selected.Length && selected[i])).ToArray();
        }
        [Serializable] class Settings { public string version="0.2.0";public string output="Release/AVEC";public bool[] selected=Enumerable.Repeat(true,Packages.Length).ToArray();public int selectionVersion; }
        [Serializable] class Manifest { public string version;public string unity;public string createdUtc;public Entry[] packages; }
        [Serializable] class Entry { public string file;public int assets;public string sha256;public string[] requires; }
        Settings settings=new Settings { selectionVersion=2 };
        string PrefKey=>"CommonAssets.Release."+Application.dataPath;
        [MenuItem("Tools/AVEC/Release Builder")]
        public static void ShowWindow() { GetWindow<CommonAssetsReleaseBuilder>("AVEC Release").Show(); }
        void OnEnable() { var json=EditorPrefs.GetString(PrefKey,"");if(!string.IsNullOrEmpty(json))settings=JsonUtility.FromJson<Settings>(json);settings.selected=NormalizeSelection(settings.selected,settings.selectionVersion);settings.selectionVersion=2; }
        void OnDisable() { EditorPrefs.SetString(PrefKey,JsonUtility.ToJson(settings)); }
        string status;
        void Run(Action action)
        {
            try { action(); OnDisable(); status = "完了しました。各処理の所要時間はConsoleに表示します。"; }
            catch (Exception e) { status = e.Message; Debug.LogException(e); }
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("配布準備 → 配布検証 → パッケージ出力の順で使用します。準備では必要時だけコンパイルし、配布対象だけを保存します。出力は梱包のみです。", MessageType.Info);
            settings.version=EditorGUILayout.TextField("バージョン",settings.version);
            settings.output=EditorGUILayout.TextField("出力先",settings.output);
            if(GUILayout.Button("出力フォルダーを選択")){var path=EditorUtility.OpenFolderPanel("出力先",Path.GetFullPath(settings.output),"");if(!string.IsNullOrEmpty(path))settings.output=path;}
            for(int i=0;i<Names.Length;i++)using(new EditorGUI.DisabledScope(!Exists(i)))settings.selected[i]=EditorGUILayout.ToggleLeft(Names[i]+"  ("+string.Join(", ",Packages[i].Requires)+")",settings.selected[i]);
            using(new EditorGUI.DisabledScope(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("1. 配布準備（必要時のコンパイル・対象保存）",GUILayout.Height(30))) Run(() => Prepare(settings.selected));
                if(GUILayout.Button("2. 配布検証",GUILayout.Height(30))) Run(() => Validate(settings.selected));
                if(GUILayout.Button("3. UnityPackage と ZIP を作成",GUILayout.Height(36))) Run(() => Build(settings.version,settings.output,settings.selected));
            }
            if(!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            if(GUILayout.Button("出力先を開く")&&Directory.Exists(settings.output))EditorUtility.RevealInFinder(Path.GetFullPath(settings.output));
        }
        // Optional command-line entry point. Preparation is intentionally a separate operation.
        public static void ExportDefault() { var json=EditorPrefs.GetString("CommonAssets.Release."+Application.dataPath,"");var s=string.IsNullOrEmpty(json)?new Settings { selectionVersion=2 }:JsonUtility.FromJson<Settings>(json);Build(s.version,s.output,NormalizeSelection(s.selected,s.selectionVersion)); }
        public static void Measure(string label, Action action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try { action(); }
            finally { Debug.Log("[AVEC Release] "+label+": "+timer.Elapsed.TotalSeconds.ToString("F2")+" 秒"); }
        }
        public static void Prepare(bool[] selected) => Measure("配布準備", () => CommonAssetsReleasePreparation.Prepare(selected));
        public static void Validate(bool[] selected) => Measure("配布検証", () => CommonAssetsReleaseValidation.Validate(selected));
        public static void Build(string version,string output,bool[] selected)
        {
            if(!Regex.IsMatch(version??"",@"^[A-Za-z0-9][A-Za-z0-9._-]*$"))throw new ArgumentException("バージョンには英数字・ピリオド・ハイフン・アンダースコアを使用してください。");
            if(string.IsNullOrWhiteSpace(output))throw new ArgumentException("出力先を指定してください。");
            foreach(string path in LegalAssets)if(!File.Exists(path))throw new FileNotFoundException("ライセンス・帰属表示が見つかりません。",path);
            Validate(selected);
            var indices=CommonAssetsReleaseValidation.Selection(selected);
            string destination=Path.GetFullPath(Path.Combine(output,version));Directory.CreateDirectory(destination);
            var entries=new System.Collections.Generic.List<Entry>();
            foreach(int i in indices)
            {
                var paths=CollectPaths(i);
                string file=Names[i]+"-"+version+".unitypackage";
                Measure(Names[i]+" 出力", () => AssetDatabase.ExportPackage(paths,Path.Combine(destination,file),ExportPackageOptions.Default));
                entries.Add(new Entry{file=file,assets=paths.Length,sha256=Hash(Path.Combine(destination,file)),requires=Packages[i].Requires});
            }
            var manifest=new Manifest{version=version,unity=Application.unityVersion,createdUtc=DateTime.UtcNow.ToString("o"),packages=entries.ToArray()};
            File.WriteAllText(Path.Combine(destination,"manifest.json"),JsonUtility.ToJson(manifest,true));
            string zip=Path.Combine(destination,"AVEC-"+version+".zip");
            Measure("ZIP格納", () => {
                using(var stream=new FileStream(zip,FileMode.Create))using(var archive=new ZipArchive(stream,ZipArchiveMode.Create))
                {
                    foreach(var entry in entries)archive.CreateEntryFromFile(Path.Combine(destination,entry.file),entry.file,System.IO.Compression.CompressionLevel.NoCompression);
                    archive.CreateEntryFromFile(Path.Combine(destination,"manifest.json"),"manifest.json");
                    if(File.Exists("README.md"))archive.CreateEntryFromFile("README.md","README.md");
                    archive.CreateEntryFromFile(LegalAssets[0],"LICENSE");
                    archive.CreateEntryFromFile(LegalAssets[1],"THIRD_PARTY_NOTICES.txt");
                }
            });
            Debug.Log("Release created: "+zip);EditorUtility.RevealInFinder(destination);
        }
        static string Hash(string path) { using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant(); }
    }
}
