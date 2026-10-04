using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
public static partial class P04ProductionBuild {
 static void SaveAuthored(){foreach(var t in All())foreach(var c in t.GetComponents<Component>())if(c)EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
 public static void FinalPresentation(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var street=ch.streetRoot.transform;var living=ch.livingRoomRoot.transform;
  RetireShell(ch.homeRoot.transform);RetireShell(living);
  var oldHouse=street.Find("NewArt_Street/LiMei");if(oldHouse)HideArt(oldHouse);
  // NPC feet stay in front of the wall baseline, on the visible painted floor.
  foreach(var name in new[]{"P04_SunFu","P04_WangKun","P04_WangPing","P04_Fisher","P04_DrinkingDog","P04_ChessTable","P04_LeftRod"}){var t=street.Find(name);if(t){var p=t.position;p.y=30.60f;t.position=p;}}
  V(street,"P04_ChessTable","SPR_STREET_LIFE_DRESSING:3",25.9f,30.60f,.78f,-4);
  var cat=ch.catVisual.transform;foreach(var name in new[]{"P04_SleepingCat","P04_EatingCat"}){var t=cat.Find(name);if(t){var p=t.position;p.y=30.60f;t.position=p;}}
  Strip(street,"P04_ContinuousTown","P04_TownContinuous",38,30.95f,150,6.75f,-45,12.6f);
  var seated=street.Find("李梅/P04_SeatedLiMei");if(seated){var p=seated.position;p.y=30.60f;seated.position=p;}
  var leftRod=street.Find("P04_LeftRod").GetComponent<LineRenderer>();leftRod.SetPositions(new[]{Vector3.zero,new Vector3(.90f,.035f,0),new Vector3(1.75f,.055f,0)});
  var puddle=street.Find("P04_DogPuddle");if(puddle)V(street,"P04_DogPuddle","P04_DogPuddle",.72f,30.54f,.13f,-6);
  foreach(var strip in All().Select(t=>t.GetComponent<P03RepeatedRoomStrip>()).Where(s=>s&&s.name=="QuietGround"&&PathOf(s.transform).Contains("P04_ContinuousShell")||s&&s.name.StartsWith("P04_Quiet"))){strip.source=S("P04_QuietMemory");strip.nativeTileHeight=0;strip.enabled=true;strip.Rebuild();}
  var table=living.Find("LivingRoomFurniture/S03_Camera");Point(table,200.0f,1.43f);V(table,"P04_TableCamera","SPR_CAMERA_TABLETOP_V02:0",200.0f,1.43f,.20f,-4);
  var father=living.Find("LivingRoomFurniture/父亲");Point(father,200.8f,.86f);V(father,"P04_Body","CHR_FAMILY_NEIGHBOR_ACTION:1",200.8f,.86f,1.82f,-6);
  var prize=street.Find("NewArt_Street/Prizes");if(prize)V(prize.parent,"Prizes","GAME_RING_TARGETS_V02:0",94,30.60f,.45f,3);
  var sign=street.Find("P04_RiverSignText").GetComponent<TextMeshPro>();sign.gameObject.SetActive(true);sign.enabled=true;sign.rectTransform.localPosition=new Vector3(96.53f,1.66f,0);sign.rectTransform.sizeDelta=new Vector2(.52f,.18f);sign.fontSize=1.10f;sign.text="河堤";sign.ForceMeshUpdate(true);
  var entry=street.Find("Stage4B_Entry").GetComponent<InteractionReach>();entry.horizontalReach=.95f;
  ch.GetComponent<P04ChapterPresentation>().riverExitX=294.70f;
  var pond=Get<P04PondLife>(street);pond.centre=new Vector2(72.3f,31.1f);pond.width=6.8f;
  foreach(var r in street.GetComponentsInChildren<SpriteRenderer>(true))if(!Authored(r.transform,street)&&r.enabled&&!r.forceRenderingOff&&r.sprite&&r.sprite.name.StartsWith("CHR_")&&!r.name.Contains("Vendor")){Life(r,null,4.8f);}
  SaveAuthored();EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var room=T("LivingRoomInterior_Return");RetireShell(room);
  var echo=room.Find("ReturnCameraGentlemanEcho/Demo2026_InteractionArt");if(echo)HideArt(echo);
  foreach(var s in room.GetComponentsInChildren<P03RepeatedRoomStrip>(true))if(PathOf(s.transform).Contains("P04_ContinuousShell")){if(s.name=="QuietGround"){s.source=S("P04_QuietOther");s.nativeTileHeight=0;}s.enabled=true;s.Rebuild();}
  SaveAuthored();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P04 final presentation source settings persisted; tabletop, sign and pond corrected.");
 }
}

