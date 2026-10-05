using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using UnityEditor.U2D.Sprites;
public static class P06Production {
 public static void CameraTarget(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
  var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var camera=all.Single(t=>t.name=="S03_Camera");var parents=all.Single(t=>t.name=="S03_Parents");var table=all.Single(t=>t.name=="S03_Table");
  // Authored points distinguish the small tabletop item from people and the broad table sensor.
  Anchor(camera,new Vector3(200,1.50f,0),.60f);Anchor(parents,new Vector3(199.15f,.9f,0),.55f);Anchor(table,new Vector3(200.90f,.85f,0),.55f);
  var c=camera.GetComponent<BoxCollider2D>();c.offset=new Vector2(0,.10f);c.size=new Vector2(.45f,.35f);EditorUtility.SetDirty(c);
  var pr=parents.GetComponent<BoxCollider2D>();pr.offset=new Vector2(-.75f,-.05f);pr.size=new Vector2(.7f,1.3f);EditorUtility.SetDirty(pr);
  var tc=table.GetComponent<BoxCollider2D>();tc.offset=new Vector2(.9f,.40f);tc.size=new Vector2(.6f,.65f);EditorUtility.SetDirty(tc);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P06 camera sensor and authored reach points saved; no furniture or story gates moved.");
 }
 [Serializable] class ImportPlan {public ImportAsset[] assets;}
 [Serializable] class ImportAsset {public string path;public ImportCell[] cells;}
 [Serializable] class ImportCell {public string name;public float x,y,w,h;}
 public static void ImportAssets(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode only");
  AssetDatabase.Refresh();var plan=JsonUtility.FromJson<ImportPlan>(File.ReadAllText(P06Regression.Work+"source/sprite-regions.json"));
  var catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var entries=catalog.entries.Where(e=>!e.key.StartsWith("P06_")).ToList();
  foreach(var asset in plan.assets){
   var importer=(TextureImporter)AssetImporter.GetAtPath(asset.path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.spritePixelsPerUnit=100;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
   var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();var old=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
   var rects=asset.cells.Select(c=>new SpriteRect{name=c.name,rect=new Rect(c.x,c.y,c.w,c.h),pivot=new Vector2(.5f,0),alignment=SpriteAlignment.Custom,spriteID=old.TryGetValue(c.name,out var id)?id:GUID.Generate()}).ToArray();provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
   foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(asset.path).OfType<Sprite>())entries.Add(new DemoArtCatalog.Entry{key=sprite.name,sprite=sprite});
  }
  catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("P06 approved art imported with explicit cells; existing catalogue entries retained.");
 }
 static void Anchor(Transform target,Vector3 pos,float reach){var t=target.Find("P06_InteractionPoint");if(!t){t=new GameObject("P06_InteractionPoint").transform;t.SetParent(target);}t.position=pos;var r=target.GetComponent<InteractionReach>()??target.gameObject.AddComponent<InteractionReach>();r.anchor=t;r.horizontalReach=reach;r.verticalReach=1.8f;EditorUtility.SetDirty(r);}
}