using UnityEngine;

using UnityEditor;

using UnityEditor.SceneManagement;

using UnityEngine.SceneManagement;

using System;

using System.Linq;

public static partial class P05ProductionBuild {

 static void ShiftAll(Transform root,float amount){var ts=root.GetComponentsInChildren<Transform>(true);var ps=ts.Select(t=>t.position).ToArray();for(int i=0;i<ts.Length;i++)ts[i].position=ps[i]+Vector3.up*amount;foreach(var b in root.GetComponentsInChildren<RoomCameraBounds>(true))Field(b,"actorRegion",p=>{var r=p.rectValue;r.y+=amount;p.rectValue=r;});}

 public static void LevelConnection(){if(EditorApplication.isPlaying)throw new Exception("Edit required");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");

 EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();var r=layout.data.Room("Floor01");if(r.origin.y!=.5f){float delta=.5f-r.origin.y;foreach(var root in new[]{layout.otherworldRoot,layout.memoryRoot})ShiftAll(root.transform.Find("Floor01"),delta);r.origin=new Vector2(38,.5f);var spawn=UnityEngine.Object.FindAnyObjectByType<PlayerSpawnPoint>();if(spawn)spawn.transform.position+=Vector3.up*delta;var cam=Camera.main;var framing=cam.GetComponent<RoomPresentationFraming>();for(int i=0;i<framing.rooms.Length;i++){var a=framing.rooms[i];if(a.area.x<40&&a.area.y<5){a.area.y+=delta;a.floorY=.5f;framing.rooms[i]=a;}}}

 layout.otherworldRoot.transform.Find("Floor01/P05_Artwork/QuietGround").GetComponent<P03RepeatedRoomStrip>().enabled=false;

 foreach(var tr in layout.GetComponentsInChildren<RoomTransitionInteractable>(true)){var portal=tr.GetComponentInParent<SchoolPortal>();if(portal){var dest=layout.data.Room(portal.Definition.targetRoom);Field(tr,"destinationVerticalCameraLimits",p=>p.vector2Value=Vector2.one*dest.CameraY);}}

 EditorUtility.SetDirty(layout.data);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();

 EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");var fog=UnityEngine.Object.FindAnyObjectByType<FogIntroController>();if(fog.transform.Find("P05_LevelGround")){ShiftAll(fog.transform.Find("FogSpace"),.5f);UnityEngine.Object.DestroyImmediate(fog.transform.Find("P05_LevelGround").gameObject);}

 var link=fog.GetComponentInChildren<P05WorldLink>();var ground=link.transform.Find("ConnectedGround");ground.position=new Vector3(24.35f,.15f,0);

 var boundary=GameObject.Find("Environment/Floor01/RightBoundary").GetComponent<Collider2D>();boundary.enabled=false;

 var homeGround=GameObject.Find("Environment/Floor01/P03_ContinuousHall/ShallowQuietGround").GetComponent<P03RepeatedRoomStrip>();var originalGround=homeGround.source;homeGround.enabled=false;

 var shared=Strip(link.transform,"QuietGround","P04_QuietOther",32.925f,-.58f,98.25f,1.6f,-19,8);shared.source=originalGround;shared.nativeTileHeight=1;shared.Rebuild();

 var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"P05FeatheredPainting.mat");if(!mat){mat=new Material(Shader.Find("ProjectMystery/P05FeatheredPainting"));AssetDatabase.CreateAsset(mat,Art+"P05FeatheredPainting.mat");}

 var wall=GameObject.Find("Environment/Floor01/P03_ContinuousHall/ContinuousWall").GetComponent<P03RepeatedRoomStrip>();wall.material=mat;Get<P05WorldFeather>(wall.transform).rightEdge=new Vector2(9,12.8f);wall.Rebuild();

 var background=Strip(fog.transform.Find("FogSpace/P05_Artwork"),"FogBackdrop","P05_FogBackdrop",24.65f,.75f,31.7f,4.1f,-34,7);background.material=mat;Get<P05WorldFeather>(background.transform).rightEdge=new Vector2(38,40.5f);background.Rebuild();Field(fog,"cameraY",p=>p.floatValue=2.05f);

 EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P05 continuous ground level corrected");}

 public static void BasketballSpacing(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");var l=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();var s=UnityEngine.Object.FindAnyObjectByType<SchoolSceneController>();
  foreach(var root in new[]{l.otherworldRoot,l.memoryRoot})root.transform.Find("Floor01/P05_Artwork/CourtHoop").position=new Vector3(72.4f,.73f,0);
  var o=new SerializedObject(s.basketball);var board=(BoxCollider2D)o.FindProperty("backboard").objectReferenceValue;var painted=l.otherworldRoot.transform.Find("Floor01/P05_Artwork/CourtHoop").GetComponent<SpriteRenderer>();
  var sr=painted.sprite;float unit=2.8f/sr.rect.height;
  var boardPoint=painted.transform.position+new Vector3((856-sr.rect.x-sr.pivot.x)*unit,(sr.texture.height-164-sr.rect.y-sr.pivot.y)*unit,0);
  var rimPoint=painted.transform.position+new Vector3((774-sr.rect.x-sr.pivot.x)*unit,(sr.texture.height-253-sr.rect.y-sr.pivot.y)*unit,0);
  board.transform.position=boardPoint;board.size=new Vector2(.055f,.80f);board.offset=Vector2.zero;
  ((Transform)o.FindProperty("hoopCenter").objectReferenceValue).position=rimPoint;
  s.basketKeyOnHoop.position=rimPoint+Vector3.up*.035f;s.basketKeyOnGround.position=new Vector3(70.3f,.64f,0);
  ((Transform)o.FindProperty("ballRestPoint").objectReferenceValue).position=new Vector3(72.4f,.64f,0);
  ((Transform)o.FindProperty("cameraFrame").objectReferenceValue).position=new Vector3(69.9f,2.10f,-10);
  if(s.uniformVisual){s.uniformVisual.transform.position=new Vector3(243.1f,.18f,0);var cloth=s.uniformVisual.transform.Find("P05_FoldedUniform");if(cloth){cloth.position=new Vector3(243.1f,.12f,0);Shadow(cloth.GetComponent<SpriteRenderer>());}}
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
}

