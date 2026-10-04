using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
public static partial class P05ProductionBuild {
 static DialogueData Data(string guid)=>AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
 static void Anonymous(DialogueData d){if(!d)return;foreach(var l in d.lines)if(l.characterId!=DialogueCharacterId.XiaoPeng&&l.characterId!=DialogueCharacterId.Narrator){l.characterId=DialogueCharacterId.Unknown;l.portraitMode=DialoguePortraitMode.Silhouette;l.portraitKey="P05_Anonymous_CLOSEUP";}EditorUtility.SetDirty(d);}
 public static void World(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit required");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");ReplaceNative("P05_FogBackdrop","BG_FOG_PASSAGE",.29f);
 EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();var flow=UnityEngine.Object.FindAnyObjectByType<SchoolSceneController>();var link=Get<P05WorldLink>(Node(layout.transform,"P05_ContinuousLink"));link.school=flow;var block=Get<BoxCollider2D>(Node(link.transform,"FogBoundary"));block.transform.position=new Vector3(37.9f,2.5f,0);block.size=new Vector2(.1f,5);block.isTrigger=false;link.schoolBarrier=block;
 foreach(var v in new[]{layout.otherworldRoot,layout.memoryRoot}){var floor=v.transform.Find("Floor01");foreach(var c in floor.GetComponentsInChildren<BoxCollider2D>(true))if(c.name=="Boundary"&&c.transform.position.x<40)c.enabled=false;Paint(floor.Find("P05_Artwork"),"LeftMist","FX_FOG_SHAPES:0",38.4f,.73f,3.8f,-2);var bounds=floor.Find("P05_Artwork/ViewBounds").GetComponent<RoomCameraBounds>();Field(bounds,"horizontalEdges",q=>q.vector2Value=new Vector2(0,82));}
 Anonymous(flow.equipmentVoiceDialogue);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");var fog=UnityEngine.Object.FindAnyObjectByType<FogIntroController>();var root=fog.transform.Find("FogSpace");bool mapped=fog.transform.Find("P05_LinkRevision1");if(!mapped){var nodes=root.GetComponentsInChildren<Transform>(true);var positions=nodes.Select(t=>t.position).ToArray();for(int i=0;i<nodes.Length;i++){var v=positions[i];nodes[i].position=new Vector3(10.7f+(v.x+200)*27.3f/70,v.y+.5f,v.z);}Node(fog.transform,"P05_LinkRevision1");}
 Retire(root);foreach(var c in root.GetComponentsInChildren<BoxCollider2D>(true))if(c.name=="Boundary"||c.name=="FogGround")c.enabled=false;
 var art=Node(root,"P05_Artwork");Strip(art,"FogBackdrop","P05_FogBackdrop",24.35f,.75f,27.4f,4.1f,-35,7);
 var figures=new[]{"SeatedStudent","FirstStandingStudent","Crowd/StandingStudent_0","Crowd/StandingStudent_1","Crowd/StandingStudent_2","Crowd/StandingStudent_3","Crowd/StandingStudent_4","Crowd/ApproachingCrowd/ApproachingStudent"};int k=0;
 foreach(var n in figures){var t=root.Find(n);if(!t)continue;var person=Paint(t,"P05_HorrorFigure","P05_Ghost",t.position.x,.73f,n.Contains("Seated")?1.38f:1.72f+(k%3)*.04f,-3+k%3);person.flipX=k%2==0;person.color=new Color(.63f,.65f,.7f);k++;}
 var homeLink=Get<P05WorldLink>(Node(fog.transform,"P05_ContinuousLink"));homeLink.fog=fog;var barrier=GameObject.Find("Environment/Floor01/DarkFog_Blocker");if(barrier)homeLink.homeBarrier=barrier.GetComponent<Collider2D>();
 var physical=Get<BoxCollider2D>(Node(homeLink.transform,"ConnectedGround"));physical.transform.position=new Vector3(24.35f,.15f,0);physical.transform.localScale=Vector3.one;physical.size=new Vector2(27.36f,.7f);physical.isTrigger=false;
 Strip(homeLink.transform,"QuietGround","P04_QuietOther",24.35f,-.8f,27.4f,1.55f,-19,8);
 var bounds2=Get<RoomCameraBounds>(Node(homeLink.transform,"ConnectedView"));Field(bounds2,"actorRegion",q=>q.rectValue=new Rect(9.5f,-1.5f,29,7));Field(bounds2,"horizontalEdges",q=>q.vector2Value=new Vector2(0,82));Field(bounds2,"priority",q=>q.intValue=30);
 Field(fog,"studentRevealX",q=>q.floatValue=15.8f);Field(fog,"firstStandingX",q=>q.floatValue=18.5f);Field(fog,"encircleX",q=>q.floatValue=22);Field(fog,"inquiryX",q=>q.floatValue=25.5f);Field(fog,"cameraY",q=>q.floatValue=2.05f);Field(fog,"cameraSize",q=>q.floatValue=2.5f);Field(fog,"fogCameraLimits",q=>q.vector2Value=new Vector2(0,82));
 Anonymous(Data("3b6ab63549244f44db905157b16ae95d"));Anonymous(Data("37e9521d8cf115f48aba0ad56e8c9721"));
 EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P05 continuous world authoring saved");
 }
}
