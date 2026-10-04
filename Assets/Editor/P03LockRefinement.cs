using System.IO;
using UnityEngine;
using UnityEditor;
public static partial class P03ProductionBuild
{
 public static void ImportLockRefinement()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Requires edit mode");
  string path=Art+"P03_LockCloseup.png";File.Copy(Work+"sources/P03_LockCloseup.png",path,true);AssetDatabase.ImportAsset(path);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=256;
  importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;
  importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");Add("P03_LockCloseup",new Rect(120,229,1296,620));
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
  typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(catalog,null);
  Debug.Log("P03 lock refinement: independent native 1296x620 plate, four matching live readouts.");
 }
}
