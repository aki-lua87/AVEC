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
                new PackageSpec { Name="AVEC-Core", Folders=new[]{Root+"/Core"}, Requires=new string[0] },
                new PackageSpec { Name="AVEC-Playlist", Folders=new[]{Root+"/Features/Playlist"}, Assets=new[]{Root+"/Prefabs/CommonVideoPlaylist.prefab",Root+"/Prefabs/CommonVideoPlaylistTablet.prefab"}, Requires=new[]{"AVEC-Core"} },
                new PackageSpec { Name="AVEC-Remote", Folders=new[]{Root+"/Features/Remote"}, Assets=new[]{Root+"/Prefabs/CommonGestureRemote.prefab",Root+"/Prefabs/CommonGestureGuide.prefab"}, Requires=new[]{"AVEC-Core"} }
            };
            foreach (var player in new[]{"IwaSync","KineL","VizVid","YamaPlayer"})
                foreach (var feature in new[]{"Playlist","Remote"}) packages.Add(new PackageSpec {
                    Name="AVEC-Connector-"+player+"-"+feature,
                    Folders=new[]{Root+"/Connectors/Shared", Root+"/Connectors/"+player+"/Editor", Root+"/Connectors/"+player+"/"+feature},
                    Requires=new[]{"AVEC-Core", "AVEC-"+feature, player+" (external)"}
                });
            packages.Add(new PackageSpec { Name="AVEC-Extension-DynamicPlaylist", Folders=new[]{Root+"/Extensions/DynamicPlaylist"}, Requires=new[]{"AVEC-Core", "AVEC-Playlist"} });
            packages.Add(new PackageSpec { Name="AVEC-Extension-PlaylistRemote", Folders=new[]{Root+"/Extensions/PlaylistRemote"}, Requires=new[]{"AVEC-Core", "AVEC-Playlist", "AVEC-Remote"} });
            packages.Add(new PackageSpec { Name="AVEC-Extension-RTabPlaylist", Folders=new[]{Root+"/Extensions/RTabPlaylist"}, Requires=new[]{"AVEC-Core", "AVEC-Playlist", "AVEC-Connector-YamaPlayer-Playlist", "AVEC-Extension-DynamicPlaylist", "RTab (external)", "YamaPlayer (external)"} });
            foreach (var feature in new[]{"Playlist","Remote"}) packages.Add(new PackageSpec {
                Name="AVEC-Connector-ProTV-"+feature,
                Folders=new[]{Root+"/Connectors/Shared", Root+"/Connectors/ProTV/Editor", Root+"/Connectors/ProTV/"+feature},
                Requires=new[]{"AVEC-Core", "AVEC-"+feature, "ProTV 3 (external)"}
            });
            return packages.ToArray();
        }
        static string[] Names => Packages.Select(p=>p.Name).ToArray();
        static bool Exists(int index) => Packages[index].Folders.All(Directory.Exists) && Packages[index].Assets.All(File.Exists);
        static string[] LegalAssets => new[]{Root+"/Core/LICENSE.txt",Root+"/Core/THIRD_PARTY_NOTICES.txt"};
        public static string[] CollectPaths(int index) => AssetDatabase.FindAssets("",Packages[index].Folders).Select(AssetDatabase.GUIDToAssetPath).Concat(Packages[index].Assets).Concat(LegalAssets).Where(p=>!p.EndsWith(".unity",StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
        static bool[] NormalizeSelection(bool[] selected)
        {
            if (selected != null && selected.Length == Packages.Length) return selected;
            if (selected != null && selected.Length == 11) {
                var migrated = new bool[Packages.Length]; migrated[0]=selected.Any(v=>v); migrated[1]=selected[0]; migrated[2]=selected[5];
                for(int i=0;i<4;i++){migrated[3+i*2]=selected[1+i];migrated[4+i*2]=selected[6+i];}
                migrated[11]=selected[0];migrated[12]=selected[10];return migrated;
            }
            if (selected != null && selected.Length < Packages.Length) {
                var migrated = new bool[Packages.Length];
                Array.Copy(selected, migrated, selected.Length);
                return migrated;
            }
            return Enumerable.Repeat(true,Packages.Length).ToArray();
        }
        [Serializable] class Settings { public string version="0.2.0";public string output="Release/AVEC";public bool[] selected=Enumerable.Repeat(true,Packages.Length).ToArray(); }
        [Serializable] class Manifest { public string version;public string unity;public string createdUtc;public Entry[] packages; }
        [Serializable] class Entry { public string file;public int assets;public string sha256;public string[] requires; }
        Settings settings=new Settings();
        string PrefKey=>"CommonAssets.Release."+Application.dataPath;
        [MenuItem("Tools/Common Assets/Release Builder")]
        public static void ShowWindow() { GetWindow<CommonAssetsReleaseBuilder>("AVEC Release").Show(); }
        void OnEnable() { var json=EditorPrefs.GetString(PrefKey,"");if(!string.IsNullOrEmpty(json))settings=JsonUtility.FromJson<Settings>(json);settings.selected=NormalizeSelection(settings.selected); }
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
        public static void ExportDefault() { var json=EditorPrefs.GetString("CommonAssets.Release."+Application.dataPath,"");var s=string.IsNullOrEmpty(json)?new Settings():JsonUtility.FromJson<Settings>(json);Build(s.version,s.output,NormalizeSelection(s.selected)); }
        public static void Build(string version,string output,bool[] selected)
        {
            if(!Regex.IsMatch(version??"",@"^[A-Za-z0-9][A-Za-z0-9._-]*$"))throw new ArgumentException("バージョンには英数字・ピリオド・ハイフン・アンダースコアを使用してください。");
            if(selected==null||selected.Length!=Names.Length)throw new ArgumentException("パッケージ選択が不正です。");
            if(!selected.Where((v,i)=>v&&Exists(i)).Any())throw new ArgumentException("出力するパッケージを選択してください。");
            foreach(string path in LegalAssets)if(!File.Exists(path))throw new FileNotFoundException("ライセンス・帰属表示が見つかりません。AVEC-Coreを導入してください。",path);
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
            string tool="Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs";
            string toolsFile="AVEC-EditorTools-"+version+".unitypackage";
            var toolPaths=new[]{tool}.Concat(LegalAssets).ToArray();
            AssetDatabase.ExportPackage(toolPaths,Path.Combine(destination,toolsFile),ExportPackageOptions.Default);
            entries.Add(new Entry{file=toolsFile,assets=toolPaths.Length,sha256=Hash(Path.Combine(destination,toolsFile))});
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
