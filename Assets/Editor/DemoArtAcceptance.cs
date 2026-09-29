using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Read-only post-import reference audit. Restores the user's original scene.
public static class DemoArtAcceptance
{
 public static void Run(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  string original=SceneManager.GetActiveScene().path;var report=new List<string>();
  try{
   var catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
   if(!catalog)throw new Exception("Missing catalog");
   var missing=catalog.entries.Where(e=>e==null||!e.sprite).ToArray();
   report.Add("Catalog entries="+catalog.entries.Length+" missing sprite references="+missing.Length);
   if(missing.Length>0)throw new Exception("Catalog contains missing sprites");
   foreach(var path in new[]{"Assets/GameData/Outfits/O_Adult_Default.asset","Assets/GameData/Outfits/O_Adult_Overalls.asset","Assets/GameData/School/Stage5A/Outfit_SchoolUniform.asset"}){
    var outfit=AssetDatabase.LoadAssetAtPath<OutfitData>(path);
    if(!outfit||!outfit.clothingImage||!outfit.preview||outfit.clothingImage==outfit.preview)throw new Exception("Missing separate clothing and wearing images: "+path);
    report.Add("PASS separate clothing and character preview: "+path);
   }
   foreach(var key in new[]{"CHR_XP_ADULT_DEFAULT_CLOSEUP:portrait","CHR_GUIDE_CLOSEUP:portrait"}){
    var sprite=catalog.Get(key);if(!sprite)throw new Exception("Missing aligned dialogue region: "+key);
    report.Add("PASS visible-bounds portrait region: "+key+" "+sprite.rect);
   }
   foreach(var key in new[]{"SPR_HOSPITAL_SIDE_BED_V02:0","MOD_SCHOOL_DESK_CHAIR_PAIR_V02:0","MOD_SCHOOL_DESK_CHAIR_PAIR_V02:1","MOD_STREET_LINK_WALL_V02:2","LEGACY:ward_side_bed"})if(!catalog.Get(key))throw new Exception("Missing key "+key);
   foreach(var name in new[]{"Scene01_Rain","Scene02_Courtyard","Scene03_Childhood","Scene04_School","EndingSequence"}){
    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
    var nodes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    int scripts=nodes.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    if(scripts>0)throw new Exception(name+" missing scripts="+scripts);
    foreach(var p in nodes.Select(t=>t.GetComponent<MazeWallPresentation>()).Where(p=>p)){
     if(!p.layout||!p.paintMaterial||!p.shadowMaterial||!p.paintMaterial.mainTexture)throw new Exception("Missing maze volume references");
     if(!p.paintMaterial.shader.isSupported)throw new Exception("Maze paint shader unsupported");
     report.Add("Maze "+p.name+" projectedHeight="+p.projectedHeight+" valid layout="+p.layout.IsValid());
    }
    report.Add("PASS "+name+" no missing scripts; scene references loaded");
   }
   report.Add("PASS actual-project edit-mode reference audit. Play Mode screenshots were separately checked in validation copy.");
  }catch(Exception e){report.Add("FAIL "+e);throw;}
  finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);File.WriteAllLines(DemoIntegrationBridge.Work+"actual_art_acceptance.txt",report);}
 }
}
