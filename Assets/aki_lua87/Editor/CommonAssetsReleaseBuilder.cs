using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using UdonSharp.Compiler;

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
                new PackageSpec { Name="AVEC-Core", Folders=new[]{Root+"/Core",Root+"/Features/Playlist",Root+"/Features/Remote",Root+"/Features/SimpleScreen",Root+"/Extensions/PlaylistRemote"}, Assets=new[]{Root+"/Prefabs/CommonVideoPlaylist.prefab",Root+"/Prefabs/CommonVideoPlaylistTablet.prefab",Root+"/Prefabs/CommonVideoPlaylistCompact.prefab",Root+"/Prefabs/CommonGestureRemote.prefab",Root+"/Prefabs/CommonGestureGuide.prefab",Root+"/Prefabs/AVEC-SimpleScreen.prefab"}, Requires=new string[0] }
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
        // Refuse to export personal authoring data, including data baked into prefabs.
        public static void ValidatePlaylistTemplates(string[] paths)
        {
            foreach (string path in paths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                foreach (var setup in prefab.GetComponentsInChildren<aki_lua87.CommonVideoPlaylist.PlaylistSetup>(true))
                {
                    if (setup.data != null || setup.view == null ||
                        setup.view.tabNames.Length != 0 || setup.view.titles.Length != 0 ||
                        setup.view.urls.Length != 0 || setup.view.titleSourceUrls.Length != 0)
                        throw new InvalidOperationException("配布用Prefabにプレイリストの内容が残っています: " + path);
                }
            }
        }
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
        void OnGUI()
        {
            EditorGUILayout.HelpBox("必要な時だけ、この画面からまとめて出力します。第三者プレイヤー本体・シーン・開発用テストを含めません。動作テストは実行しません。",MessageType.Info);
            settings.version=EditorGUILayout.TextField("バージョン",settings.version);
            settings.output=EditorGUILayout.TextField("出力先",settings.output);
            if(GUILayout.Button("出力フォルダーを選択")){var path=EditorUtility.OpenFolderPanel("出力先",Path.GetFullPath(settings.output),"");if(!string.IsNullOrEmpty(path))settings.output=path;}
            for(int i=0;i<Names.Length;i++)using(new EditorGUI.DisabledScope(!Exists(i)))settings.selected[i]=EditorGUILayout.ToggleLeft(Names[i]+"  ("+string.Join(", ",Packages[i].Requires)+")",settings.selected[i]);
            using(new EditorGUI.DisabledScope(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
                if(GUILayout.Button("UnityPackage と ZIP を作成",GUILayout.Height(36)))
                    try { Build(settings.version,settings.output,settings.selected);OnDisable(); }
                    catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("パッケージ出力に失敗",e.Message,"閉じる");}
            if(GUILayout.Button("出力先を開く")&&Directory.Exists(settings.output))EditorUtility.RevealInFinder(Path.GetFullPath(settings.output));
        }
        // Optional command-line entry point, using this same project and saved settings.
        public static void ExportDefault() { var json=EditorPrefs.GetString("CommonAssets.Release."+Application.dataPath,"");var s=string.IsNullOrEmpty(json)?new Settings { selectionVersion=2 }:JsonUtility.FromJson<Settings>(json);Build(s.version,s.output,NormalizeSelection(s.selected,s.selectionVersion)); }
        public static void Build(string version,string output,bool[] selected)
        {
            if(!Regex.IsMatch(version??"",@"^[A-Za-z0-9][A-Za-z0-9._-]*$"))throw new ArgumentException("バージョンには英数字・ピリオド・ハイフン・アンダースコアを使用してください。");
            if(selected==null||selected.Length!=Names.Length)throw new ArgumentException("パッケージ選択が不正です。");
            if(!selected.Where((v,i)=>v&&Exists(i)).Any())throw new ArgumentException("出力するパッケージを選択してください。");
            foreach(string path in LegalAssets)if(!File.Exists(path))throw new FileNotFoundException("ライセンス・帰属表示が見つかりません。AVEC-Coreを導入してください。",path);
            for (int i = 0; i < Packages.Length; i++)
                if (selected[i] && Exists(i)) ValidatePlaylistTemplates(CollectPaths(i));
            string error=null;Application.LogCallback capture=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)error=message;};
            Application.logMessageReceived+=capture;
            try { UdonSharpCompilerV1.CompileSync(); }
            finally { Application.logMessageReceived-=capture; }
            if(error!=null)throw new InvalidOperationException("UdonSharpコンパイルに失敗しました: "+error);
            for(int i=0;i<Packages.Length;i++)
            {
                if(!selected[i]||!Exists(i))continue;
                foreach(var guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset",Packages[i].Folders))
                {
                    var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(AssetDatabase.GUIDToAssetPath(guid));
                    if(program.SerializedProgramAsset==null)throw new InvalidOperationException("コンパイル済みProgramがありません: "+program.name);
                    string source=AssetDatabase.GetAssetPath(program.SerializedProgramAsset);
                    string parent=Path.GetDirectoryName(AssetDatabase.GetAssetPath(program)).Replace('\\','/');string folder=parent+"/Programs";
                    if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(parent,"Programs");
                    string target=folder+"/"+Path.GetFileName(source);if(source==target)continue;
                    string result=AssetDatabase.MoveAsset(source,target);if(!string.IsNullOrEmpty(result))throw new IOException(result);
                }
            }
            AssetDatabase.SaveAssets();string destination=Path.GetFullPath(Path.Combine(output,version));Directory.CreateDirectory(destination);
            var entries=new System.Collections.Generic.List<Entry>();
            for(int i=0;i<Packages.Length;i++)
            {
                if(!selected[i]||!Exists(i))continue;
                var paths=CollectPaths(i);
                string file=Names[i]+"-"+version+".unitypackage";AssetDatabase.ExportPackage(paths,Path.Combine(destination,file),ExportPackageOptions.Default);
                entries.Add(new Entry{file=file,assets=paths.Length,sha256=Hash(Path.Combine(destination,file)),requires=Packages[i].Requires});
            }
            var manifest=new Manifest{version=version,unity=Application.unityVersion,createdUtc=DateTime.UtcNow.ToString("o"),packages=entries.ToArray()};
            File.WriteAllText(Path.Combine(destination,"manifest.json"),JsonUtility.ToJson(manifest,true));
            string zip=Path.Combine(destination,"AVEC-"+version+".zip");
            using(var stream=new FileStream(zip,FileMode.Create))using(var archive=new ZipArchive(stream,ZipArchiveMode.Create))
            {
                foreach(var entry in entries)archive.CreateEntryFromFile(Path.Combine(destination,entry.file),entry.file,System.IO.Compression.CompressionLevel.Optimal);
                archive.CreateEntryFromFile(Path.Combine(destination,"manifest.json"),"manifest.json");
                if(File.Exists("README.md"))archive.CreateEntryFromFile("README.md","README.md");
                archive.CreateEntryFromFile(LegalAssets[0],"LICENSE");
                archive.CreateEntryFromFile(LegalAssets[1],"THIRD_PARTY_NOTICES.txt");
            }
            Debug.Log("Release created: "+zip);EditorUtility.RevealInFinder(destination);
        }
        static string Hash(string path) { using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant(); }
    }
}
