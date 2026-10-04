using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static partial class P03ProductionBuild
{
 const string Art="Assets/Art/正式/P03/";
 const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P03/";
 static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static Transform T(string path)=>All().FirstOrDefault(t=>PathOf(t)==path)??throw new Exception("Missing "+path);
 static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Float(UnityEngine.Object target,string field,float value){var s=new SerializedObject(target);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static DemoArtCatalog catalog;
 public static void ImportArt()
 {
  if(EditorApplication.isPlaying)throw new Exception("Requires edit mode");
  Directory.CreateDirectory(Art);
  foreach(var file in Directory.GetFiles(Work+"sources","*.png"))
  {
   string path=Art+Path.GetFileName(file);File.Copy(file,path,true);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
   importer.spritePixelsPerUnit=256;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;settings.spritePivot=new Vector2(.5f,0);importer.SetTextureSettings(settings);
   importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;
   importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
  }
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  Add("P03_CollapseWall",new Rect(75,0,790,1774));Add("P03_CompactStairs",new Rect(343,91,1006,861));Add("P03_Reporters",new Rect(13,53,1508,945));Add("P03_Bicycle",new Rect(22,55,1493,955));
  Add("P03_CredentialCase",new Rect(170,168,1460,502));Add("P03_PasswordDesk",new Rect(193,53,1150,907));
  Add("P03_ParentShadows:father",new Rect(140,28,480,1214));Add("P03_ParentShadows:mother",new Rect(729,28,398,1098));
  Add("P03_Puddle",new Rect(0,0,1536,1024));
  Add("P03_GunComic:breathing",new Rect(0,515,768,509));Add("P03_GunComic:voice",new Rect(769,515,767,509));Add("P03_GunComic:shot",new Rect(0,0,1536,512));
  Add("P03_PasswordDesk:drawer",new Rect(799,442,482,137));Add("P03_PasswordDesk:lock",new Rect(981,476,157,71));
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
  typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(catalog,null);
  Debug.Log("P03 art imported: native size, explicit regions, stable catalog keys.");
 }
 static void Add(string key,Rect rect)
 {
  string stem=key.Split(':')[0];
  var existing=catalog.entries.FirstOrDefault(e=>e.key==key);
  if(existing!=null && existing.sprite && existing.sprite.rect==rect)return;
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+stem+".png");
  var sprite=Sprite.Create(texture,rect,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sprite.name=key;
  AssetDatabase.AddObjectToAsset(sprite,catalog);
  if(existing!=null){existing.sprite=sprite;return;}
  catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sprite}}).ToArray();
 }
 public static void Paint(SpriteRenderer sr,Sprite sprite,float x,float bottom,float height)
 {
  var crop=sr.GetComponent<SpriteArtRegion>();if(crop)UnityEngine.Object.DestroyImmediate(crop);
  sr.sprite=sprite;sr.forceRenderingOff=false;sr.enabled=true;
  float scale=height/(sprite.rect.height/sprite.pixelsPerUnit);
  var parent=sr.transform.parent?sr.transform.parent.lossyScale:Vector3.one;
  sr.transform.localScale=new Vector3(scale/parent.x,scale/parent.y,1);
  sr.transform.position=new Vector3(x,bottom,0);sr.color=Color.white;sr.transform.rotation=Quaternion.identity;
 }
 public static void Rain()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  var crowd=T("Environment/CrowdPlaceholder/ReportersCrowd_Visual").GetComponent<SpriteRenderer>();
  Paint(crowd,catalog.Get("P03_Reporters"),-19.6f,-1,1.8f);
  Paint(T("Environment/Scene01_NewArt/EnvironmentProps/Bicycle").GetComponent<SpriteRenderer>(),catalog.Get("P03_Bicycle"),5.2f,-1,.96f);
  var holder=All().Select(t=>t.GetComponent<IDHolderInteractable>()).FirstOrDefault(c=>c);
  if(!holder)throw new Exception("ID holder missing");
  var caseVisual=holder.transform.Find("IDHolder_Visual").GetComponent<SpriteRenderer>();
  holder.GetComponent<SpriteRenderer>().forceRenderingOff=true;
  if(!caseVisual)caseVisual=holder.GetComponent<SpriteRenderer>();
  Paint(caseVisual,catalog.Get("P03_CredentialCase"),holder.transform.position.x,-1.04f,.16f);
  var shadow=caseVisual.GetComponent<PaintedContactShadow>()??caseVisual.gameObject.AddComponent<PaintedContactShadow>();shadow.target=caseVisual;shadow.depth=.035f;
  var atmosphere=All().Select(t=>t.GetComponent<StreetAtmospherePresentation>()).First(c=>c);
  var lamps=All().Where(t=>t.name.StartsWith("Lamp_")&&t.GetComponent<SpriteArtRegion>()&&PathOf(t).Contains("EnvironmentProps")).ToArray();
  var anchors=new List<Transform>();
  foreach(var lamp in lamps)
  {
   var sr=lamp.GetComponent<SpriteRenderer>();var b=sr.bounds;
   var anchor=lamp.Find("P03_BulbEmitter");
   if(!anchor){anchor=new GameObject("P03_BulbEmitter").transform;anchor.SetParent(lamp,false);}
   anchor.position=new Vector3(b.min.x+b.size.x*.74f,b.min.y+b.size.y*.865f,0);anchors.Add(anchor);
  }
  var so=new SerializedObject(atmosphere);var a=so.FindProperty("lampEmitters");a.arraySize=anchors.Count;for(int i=0;i<anchors.Count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=anchors[i];so.ApplyModifiedPropertiesWithoutUndo();
  T("Environment/Puddle_Placeholder/Reflection_Placeholder").gameObject.SetActive(false);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  P03SceneEvidence.References();Debug.Log("P03 rain scene authored; logic objects kept.");
 }
 public static void RemoveObsoleteCloseups()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  const string backup="D:/游戏设计/Codex工作区/维护记录/P03_20261004_before/retired_closeups/";
  Directory.CreateDirectory(backup);catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  var keys=new[]{"CU_HOME_DESK","CU_HOME_LOCK","CU_HOME_COMPUTER"};
  var sprites=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(catalog)).OfType<Sprite>().Where(s=>keys.Contains(s.name)).ToArray();
  catalog.entries=catalog.entries.Where(e=>!keys.Contains(e.key)).ToArray();EditorUtility.SetDirty(catalog);
  foreach(var sp in sprites)UnityEngine.Object.DestroyImmediate(sp,true);AssetDatabase.SaveAssets();
  var rows=new List<string>();
  foreach(var key in keys)
  {
   string path="Assets/Art/Demo2026/"+key+".png";
   var refs=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!p.StartsWith("Assets/Art/Demo2026/CU_HOME_")&&(p.EndsWith(".asset")||p.EndsWith(".unity")||p.EndsWith(".prefab")||p.EndsWith(".mat"))).Where(p=>AssetDatabase.GetDependencies(p,true).Contains(path)).ToArray();
   if(refs.Length>0)throw new Exception("Still referenced "+path+" by "+string.Join(",",refs));
   if(File.Exists(path)){File.Copy(path,backup+key+".png",true);File.Copy(path+".meta",backup+key+".png.meta",true);if(!AssetDatabase.DeleteAsset(path))throw new Exception("Could not remove "+path);}
   rows.Add("Removed from active Assets/catalog: "+path+"; no serialized dependency; backed up outside project.");
  }
  typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(catalog,null);
  File.WriteAllLines(Work+"obsolete_closeup_cleanup.txt",rows);AssetDatabase.SaveAssets();
 }
}


