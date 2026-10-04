using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static partial class P03ProductionBuild
{
 public static void FinishDetails()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  var lamp=T("Environment/Floor01/BrokenLamp_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>();Paint(lamp,S("SPR_HOME_DRESSING_OTHER:2"),-6,1.66f,.42f);lamp.sortingOrder=T("Environment/Floor01/NewArt_RoomShell/LampTable").GetComponent<SpriteRenderer>().sortingOrder+1;
  var masterLamp=T("Environment/MasterBedroomInterior/Lamp_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>();Paint(masterLamp,S("SPR_HOME_DRESSING_OTHER:2"),65.45f,1.40f,.42f);masterLamp.sortingOrder=-4;
  Door(T("LivingRoomInterior_Return/LivingRoomExitVisual"),1.02f);
  var returnArt=T("LivingRoomInterior_Return/ReturnLivingRoomDoor/Demo2026_InteractionArt").GetComponent<SpriteRenderer>();Paint(returnArt,S("SPR_HOME_DOOR_TVCAB_OTHER:0"),344.6f,1.02f,2.22f);
  Add("P03_CollapseWall:upper",new Rect(75,1090,790,660));Add("P03_CollapseWall:lower",new Rect(75,55,790,735));Add("P03_CollapseWall:middle",new Rect(75,560,790,720));
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(catalog,null);
  var area=T("Gameplay/Scene02_Midgame/DistortedCorridorArea");
  float[] x={-.5f,2.9f,6.3f,9.7f,13.1f,16.5f},bottom={8.05f,7.35f,8.3f,7.8f,7.4f,8.15f},height={1.5f,1.8f,1.25f,1.6f,1.8f,1.35f};
  string[] keys={"upper","lower","middle","upper","lower","middle"};
  for(int i=0;i<6;i++){var r=area.Find("P03_CollapseInk_"+i).GetComponent<SpriteRenderer>();Paint(r,S("P03_CollapseWall:"+keys[i]),x[i],bottom[i],height[i]);r.color=new Color(.50f,.51f,.53f,.83f);r.flipX=i%2==1;r.sortingOrder=-25;}
  for(int i=0;i<2;i++){var r=area.Find("P03_MilestoneLamp_"+i).GetComponent<SpriteRenderer>();Paint(r,S("SPR_HOME_DRESSING_OTHER:2"),i*7,8.98f,.34f);Paint(area.Find("P03_LampWallShelf_"+i).GetComponent<SpriteRenderer>(),S("P03_WallShelf"),i*7,8.93f,.06f);}
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  File.WriteAllText(Work+"final_details.txt","Lamp bases moved into tabletop planes; return-living-room threshold matches wall foot; both corridor lamps remain below letterbox; varied native crack regions replace repeated full-height columns. No actor, task, item or door-event logic changed.");
 }
 public static void TabletopAuditFixes()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  var camera=T("Environment/MasterBedroomInterior/CameraItem_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>();var p=camera.transform.position;p.y-=camera.bounds.min.y-1.675f;camera.transform.position=p;
  var echo=T("LivingRoomInterior_Return/ReturnCameraGentlemanEcho/Demo2026_InteractionArt").GetComponent<SpriteRenderer>();p=echo.transform.position;p.x=350.28f;p.y-=echo.bounds.min.y-1.08f;echo.transform.position=p;echo.sortingOrder=1;
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  string backup="D:/游戏设计/Codex工作区/维护记录/P03_20261004_before/Scene03_Childhood.unity";if(!File.Exists(backup))File.Copy("Assets/Scenes/Scene03_Childhood.unity",backup);
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
  var lamps=All().Select(t=>t.GetComponent<SpriteRenderer>()).Where(r=>r&&r.enabled&&!r.forceRenderingOff&&r.sprite&&r.sprite.name.StartsWith("SPR_HOME_DRESSING_MEMORY_V02:2")).ToArray();
  foreach(var r in lamps){p=r.transform.position;p.y-=r.bounds.min.y-1.315f;r.transform.position=p;r.sortingOrder=-3;}
  var tabletopCamera=All().Select(t=>t.GetComponent<SpriteRenderer>()).First(r=>r&&r.sprite&&r.sprite.name.StartsWith("SPR_CAMERA_TABLETOP_V02:0")&&PathOf(r.transform).Contains("S03_Camera")&&r.enabled&&!r.forceRenderingOff);p=tabletopCamera.transform.position;p.y-=tabletopCamera.bounds.min.y-1.08f;tabletopCamera.transform.position=p;
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  File.WriteAllText(Work+"tabletop_audit_fixes.txt","Scene02 master camera base 1.675 inside cabinet top 1.74; return echo picture entirely over coffee table x=350.28 base=1.08 on painted tabletop; chair backs excluded. Scene03 lamp base 1.315 inside top=1.38; coffee-table camera base 1.08 on painted tabletop; chair backs excluded. Only artwork positions and echo sorting changed; roots/colliders/events/characters unchanged.");
 } public static void BlendCrackEdges()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  var area=T("Gameplay/Scene02_Midgame/DistortedCorridorArea");var shader=Shader.Find("ProjectMystery/P03FragmentPainting");if(!shader)throw new Exception("Fragment shader not imported");
  foreach(var r in area.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.StartsWith("P03_CollapseInk_")))
  {
   string key=r.sprite.name.Split(':').Last();string path=Art+"P03_Crack_"+key+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;
   var rect=r.sprite.rect;var tex=r.sprite.texture;mat.SetVector("_Region",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));mat.SetFloat("_EdgeWidth",.085f);EditorUtility.SetDirty(mat);r.sharedMaterial=mat;
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  File.WriteAllText(Work+"crack_composite_fix.txt","Native artwork preserved; sprite-local edge blending removes rectangular crop terminations. No walking surfaces, cameras, events or puzzle flags changed.");
 }
 public static void FinishFloorCoverage()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  int retired=0;foreach(var r in All().Select(t=>t.GetComponent<SpriteRenderer>()).Where(r=>r&&r.name.StartsWith("Demo2026_Floor_")&&r.sprite&&r.sprite.name.StartsWith("TEX_FLOOR_QUIET")))
  {Get<RetiredArtVisibility>(r.transform).SetRenderers(new Renderer[]{r});retired++;}
  foreach(var strip in All().Select(t=>t.GetComponent<P03RepeatedRoomStrip>()).Where(c=>c&&c.name=="ShallowQuietGround"))
  {float top=strip.transform.position.y+strip.height;strip.height=1.6f;var pos=strip.transform.position;pos.y=top-strip.height;strip.transform.position=pos;strip.nativeTileHeight=1;strip.tileWidth=8;strip.Rebuild();}
  // This room retained the P01 wall and furniture. Match its existing painted wall foot, only replace floor tiles.
  var room=T("LivingRoomInterior_Return");var ground=Strip(room,"P03_ReturnQuietGround",351f,-.92f,24f,1.6f,"TEX_FLOOR_QUIET_V04:1",-19);
  var repeat=ground.GetComponent<P03RepeatedRoomStrip>();repeat.nativeTileHeight=1;repeat.tileWidth=8;repeat.Rebuild();
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  File.WriteAllText(Work+"floor_retirement_fix.txt","Retired "+retired+" scene-owned old floor strips; final floor meshes extend below the effective letterbox at unchanged native sampling density. No painting pixels, wall top, camera scale, physics surfaces or actor coordinates changed. Return living room wall/furniture retained.");
 }
}