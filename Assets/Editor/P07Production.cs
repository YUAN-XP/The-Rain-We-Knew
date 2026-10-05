#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
public static class P07Production {
 public static void Apply(){
  foreach(var path in new[]{"Assets/Art/Demo2026/P07/P07_StartMenu.png","Assets/Art/Demo2026/P07/P07_EndMenu.png"}){
   var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=256;i.maxTextureSize=4096;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.alphaIsTransparency=true;i.SaveAndReimport();
  }
  const string pathData="Assets/Resources/P07Presentation.asset";var data=AssetDatabase.LoadAssetAtPath<P07PresentationData>(pathData);if(!data){data=ScriptableObject.CreateInstance<P07PresentationData>();AssetDatabase.CreateAsset(data,pathData);}
  data.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ChineseMain SDF.asset");data.startBackground=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Demo2026/P07/P07_StartMenu.png");data.endingBackground=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Demo2026/P07/P07_EndMenu.png");data.credits="全游戏制作：袁潇鹏\n\n部分内容由 AI 辅助生成";EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();Debug.Log("P07 presentation imported; existing scenes untouched.");
 }
}
#endif
