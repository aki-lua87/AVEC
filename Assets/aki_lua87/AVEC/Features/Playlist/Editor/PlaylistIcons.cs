using System.IO;
using UnityEngine;
using UnityEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Original small line icons; no third-party artwork or font glyph dependency.
    public static class PlaylistIcons
    {
        public static Sprite Get(string name)
        {
            string path=PlaylistFactory.Root+"/Icons/"+name+".png";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color32[4096];
            if(name=="play") { for(int y=12;y<=52;y++)for(int x=16;x<=50-Mathf.Abs(y-32)*1.7f;x++)pixels[y*64+x]=Color.white; }
            else if(name=="stop") { for(int y=15;y<49;y++)for(int x=15;x<49;x++)pixels[y*64+x]=Color.white; }
            else if(name=="pause") { for(int y=13;y<51;y++)for(int x=17;x<47;x++)if(x<27||x>=37)pixels[y*64+x]=Color.white; }
            else if(name=="repeat") { Line(pixels,16,18,50,18);Line(pixels,50,18,50,46);Line(pixels,50,46,14,46);Line(pixels,14,46,14,18);Arrow(pixels,50,18);Line(pixels,14,46,23,37);Line(pixels,14,46,23,55);Line(pixels,32,24,32,40);Line(pixels,27,36,32,40); }
            else if(name=="speaker") {
                Line(pixels,10,24,20,24); Line(pixels,10,24,10,40); Line(pixels,10,40,20,40);
                Line(pixels,20,24,32,14); Line(pixels,20,40,32,50); Line(pixels,32,14,32,50);
                for(int y=12;y<=52;y++)for(int x=36;x<60;x++) {
                    float radius=new Vector2(x-32,y-32).magnitude;
                    if(x-32>=Mathf.Abs(y-32)*.65f && (Mathf.Abs(radius-14)<1.6f || Mathf.Abs(radius-24)<1.6f))pixels[y*64+x]=Color.white;
                }
            }
            else if(name=="shuffle") { Line(pixels,10,18,21,18);Line(pixels,21,18,43,46);Line(pixels,43,46,55,46);Line(pixels,10,46,21,46);Line(pixels,21,46,43,18);Line(pixels,43,18,55,18);Arrow(pixels,55,46);Arrow(pixels,55,18); }
            else { Line(pixels,10,18,55,18);Line(pixels,10,46,55,46);Arrow(pixels,55,18);Arrow(pixels,55,46); }
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Arrow(Color32[] pixels,int x,int y) { Line(pixels,x,y,x-9,y+9);Line(pixels,x,y,x-9,y-9); }
        static void Line(Color32[] pixels,float ax,float ay,float bx,float by)
        {
            var a=new Vector2(ax,ay);var d=new Vector2(bx-ax,by-ay);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++) { var p=new Vector2(x,y);float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);if((p-a-d*t).sqrMagnitude<=3f)pixels[y*64+x]=Color.white; }
        }
    }
}
