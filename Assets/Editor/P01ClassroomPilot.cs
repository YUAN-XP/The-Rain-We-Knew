using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot scene migration. Furniture stays at its original scale; only positions and
// the room's structural span change. Both world variants and portal data move together.
public static class P01ClassroomPilot {
    // Idempotent visual repair, restricted to the two P01 classroom variants.
    public static void RepairContinuousSurfaces(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");
        var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
        if(layout.data.Room("Class3_2").width!=20)throw new InvalidOperationException("Expected compact pilot");
        foreach(var world in new[]{layout.otherworldRoot,layout.memoryRoot}){
            var env=world.transform.Find("Class3_2/NewArt_Environment");
            foreach(Transform t in env){
                if(!t.name.StartsWith("Wall_")&&!t.name.StartsWith("Demo2026_Floor_Wall_"))continue;
                var renderer=t.GetComponent<SpriteRenderer>();if(!renderer)continue;
                int index=Mathf.RoundToInt((t.localPosition.x-4)/8);
                renderer.flipX=(index&1)!=0;
            }
            var student=env.Find("StudentSleeping");
            if(student){var p=student.localPosition;p.y=.32f;student.localPosition=p;}
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void Refine(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
        if(layout.data.Room("Class3_2").width!=20)throw new InvalidOperationException("Expected compact pilot");
        foreach(var world in new[]{layout.otherworldRoot,layout.memoryRoot}){
            var room=world.transform.Find("Class3_2");var env=room.Find("NewArt_Environment");
            env.Find("Demo2026_Board_3")?.gameObject.SetActive(false);env.Find("Window_231")?.gameObject.SetActive(false);
            var board=room.Find("Blackboard");var p=board.localPosition;p.y=4.87f;board.localPosition=p;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");
        var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
        if(layout.data.Room("Class3_2").width!=40)throw new InvalidOperationException("Pilot already migrated or baseline changed");
        foreach(var world in new[]{layout.otherworldRoot,layout.memoryRoot}){
            var room=world.transform.Find("Class3_2");if(!room)throw new Exception("Classroom missing");
            foreach(Transform t in room){
                if(t.name.StartsWith("NewArt_")||t.name=="Demo2026_ClassroomDressing")continue;
                var p=t.localPosition;p.x*=.5f;t.localPosition=p;
                if(t.name=="Wall"||t.name=="Floor"||t.name=="Cornice"){var s=t.localScale;s.x*=.5f;t.localScale=s;}
                if(t.name=="Ground"){var c=t.GetComponent<BoxCollider2D>();c.size=new Vector2(c.size.x*.5f,c.size.y);}
                if(t.name.StartsWith("Desk_")){p=t.localPosition;p.y+=.32f;t.localPosition=p;}
            }
            var env=room.Find("NewArt_Environment");
            foreach(Transform t in env){
                if(t.name.StartsWith("Wall_")||t.name.StartsWith("Floor_")||t.name.StartsWith("Demo2026_Floor_Wall_")){
                    if(t.localPosition.x>20)t.gameObject.SetActive(false);
                    if(t.name.StartsWith("Demo2026_Floor_Wall_")){var p=t.localPosition;p.y+=.32f;t.localPosition=p;}
                }else{var p=t.localPosition;p.x*=.5f;t.localPosition=p;}
            }
            // Four authored investigation desks plus four intermediate desks, with
            // books/bags attached by desk identity rather than compressed independently.
            var dress=room.Find("Demo2026_ClassroomDressing");
            foreach(Transform t in dress){
                var p=t.localPosition;
                if(t.name.StartsWith("DeskRow_")){var parts=t.name.Split('_');int row=int.Parse(parts[1]);bool keep=parts[2]=="2";t.gameObject.SetActive(keep);if(keep){p.x=(9+(row-1)*7)*.5f+1.75f;p.y=.32f;}}
                else if(t.name.StartsWith("LearningBooks_")||t.name.StartsWith("HangingBag_")){
                    float old=p.x;float nearest=9+Mathf.Round((old-9)/7)*7;
                    bool keep=Mathf.Abs(old-nearest)<.5f;t.gameObject.SetActive(keep);p.x=nearest*.5f+(old-nearest);p.y+=.32f;
                }else if(t.name=="ForegroundDeskGlance"){t.gameObject.SetActive(false);}
                else{p.x*=.5f;if(t.name.Contains("Bookcase")||t.name.Contains("Plant")||t.name=="CleaningCorner")p.y=.32f;}
                t.localPosition=p;
            }
            // Do not put a cabinet over a door or an investigation desk.
            dress.Find("ReadingCornerBookcase").localPosition=new Vector3(2.8f,.32f,0);
            dress.Find("RearBookcase").localPosition=new Vector3(16.8f,.32f,0);
            dress.Find("ReadingCornerPlant").localPosition=new Vector3(3.7f,.32f,0);
            dress.Find("CleaningCorner").localPosition=new Vector3(18,.32f,0);
            var bound=room.GetComponent<RoomCameraBounds>()??room.gameObject.AddComponent<RoomCameraBounds>();
            var so=new SerializedObject(bound);so.FindProperty("actorRegion").rectValue=new Rect(200,15,20,7);so.FindProperty("horizontalEdges").vector2Value=new Vector2(200.18f,219.82f);so.ApplyModifiedPropertiesWithoutUndo();
        }
        layout.data.Room("Class3_2").width=20;
        foreach(var portal in layout.data.portals){if(portal.sourceRoom=="Class3_2")portal.sourceLocal.x*=.5f;if(portal.targetRoom=="Class3_2")portal.destinationLocal.x*=.5f;}
        EditorUtility.SetDirty(layout.data);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Application.dataPath,"../Temp/P01ClassroomPilot.txt"),"Migrated Class3_2 both variants 40 -> 20; actor floor 16, furniture foot 16.32, visible floor top 16.50; unchanged furniture scale; all portal coordinates remapped. Visual validation pending.");
    }
}
