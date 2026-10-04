using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static partial class P04ProductionBuild {
 public static void FinalProps(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  foreach(var n in new[]{"P04_ChessTable","P04_FisherBody"}){
   string path=Art+n+".png";AssetDatabase.ImportAsset(path);
   var import=(TextureImporter)AssetImporter.GetAtPath(path);import.textureType=TextureImporterType.Sprite;import.spriteImportMode=SpriteImportMode.Single;import.spritePixelsPerUnit=256;import.alphaIsTransparency=true;import.mipmapEnabled=false;import.filterMode=FilterMode.Bilinear;import.maxTextureSize=2048;import.textureCompression=TextureImporterCompression.Uncompressed;import.SaveAndReimport();
  }
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  Add("P04_ChessTable",new Rect(37,112,1152,951));Add("P04_FisherBody",new Rect(205,17,878,1284));
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(catalog,null);
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");var ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var street=ch.streetRoot.transform;
  V(street,"P04_ChessTable","P04_ChessTable",25.9f,30.60f,.78f,-4);
  var fisher=V(street,"P04_Fisher","P04_FisherBody",72.2f,30.60f,1.30f,-4);var life=fisher.GetComponent<P04PaintedLife>();life.frames=null;life.height=1.30f;
  var rod=Get<P04FishingRod>(fisher.transform);rod.actor=fisher;
  // The original decorative seated man duplicated the two authored chess actors.
  // Retire only the obsolete render output, preserving interactions and story nodes.
  HideArt(street.Find("NewArt_Street/ChattingNeighbours"));
  var vendor=V(street.Find("S03_Shop"),"NewArt_Vendor","CHR_TOWN_TRADERS_BASE:0",-4.6f,30.60f,1.82f,3);Life(vendor,null,4.8f,1.82f);
  // This old foreground chair belonged to the pre-P04 hallway, not the compact room.
  HideArt(T("Demo2026_SparseForeground/HomeNearChairBack"));
  SaveAuthored();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P04 removed extra stools and doubled painted fishing rod.");
 }
}
