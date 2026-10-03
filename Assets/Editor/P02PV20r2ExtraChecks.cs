using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
// Editor-only isolated presentation tests. No scene or release component.
public static class P02PV20r2ExtraChecks {
    const string Work="D:/游戏设计/Codex工作区/work/2026-10-03-P02/extra_PV20r2/";
    static void Require(){if(!EditorApplication.isPlaying||GameplayInput.Replay==null)throw new InvalidOperationException("Isolated replay required");Directory.CreateDirectory(Work);}
    static void Offer(bool left){Require();var a=UnityEngine.Object.FindObjectsByType<P02ActorPresentation>().Single(x=>x.ActorKey=="qt_adult"&&x.OwnsBody);a.RestoreFacing(left);a.CredentialOffer=true;File.AppendAllText(Work+"credential_scope.txt",DateTime.Now.ToString("O")+" Editor-only held-card pose "+(left?"left":"right")+"; no story or inventory change\n");}
    public static void CredentialRight()=>Offer(false);
    public static void CredentialLeft()=>Offer(true);
    public static void ClearCredential(){foreach(var a in UnityEngine.Object.FindObjectsByType<P02ActorPresentation>())a.CredentialOffer=false;}
    public static void LampTransitions(){Require();PersistentRuntime.Instance.StartCoroutine(LampRooms());}
    static IEnumerator LampRooms(){
        var rt=PersistentRuntime.Instance;var p=rt.GetComponent<GameControlManager>().ActivePlayer;var lamp=rt.GetComponent<OilLampController>();var tool=rt.GetComponent<P02ToolMotion>();
        if(!lamp||!lamp.IsRaised)throw new InvalidOperationException("Raise authored lamp with R before testing");
        var rows=new List<string>{"Diagnostic placement; original RoomTransitionInteractable.Interact, original fade and original tool controllers. Not natural keyboard progression."};
        var doors=UnityEngine.Object.FindObjectsByType<RoomTransitionInteractable>();
        var bedroom=GameObject.Find("MasterBedroomInterior");
        var exit=doors.First(x=>x.transform.IsChildOf(bedroom.transform));
        Transform Dest(RoomTransitionInteractable x)=>(Transform)typeof(RoomTransitionInteractable).GetField("destination",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(x);
        var enter=doors.First(x=>x!=exit&&Dest(x)&&Dest(x).position.x>64&&Dest(x).position.x<76&&Dest(x).position.y<5);
        var up=doors.Single(x=>x.name=="StairsUp_InteractZone");var down=doors.Single(x=>x.name=="StairExit_Point");
        foreach(var door in new[]{enter,exit,up,down}){
            var at=door.transform.position;at.z=p.transform.position.z;EndingSession.Place(p,at);GroundedPlacement.Settle(p);RoomPresentationFraming.PrepareCurrentView();
            bool arrived=false;Action<RoomTransitionInteractable> done=x=>arrived=true;door.Arrived+=done;
            door.Interact();bool blocked=!rt.GetComponent<GameSaveService>().CanSave(out _);
            double until=Time.realtimeSinceStartupAsDouble+12;while(!arrived&&Time.realtimeSinceStartupAsDouble<until)yield return null;door.Arrived-=done;
            for(int n=0;n<45;n++)yield return null;
            bool ok=arrived&&blocked&&lamp.IsRaised&&tool.Current==P02ToolMotion.Tool.Lamp&&tool.State==P02ToolMotion.Motion.Holding&&GroundedPlacement.IsSupported(p);
            rows.Add((ok?"PASS ":"FAIL ")+door.name+" arrived="+arrived+" lamp="+lamp.IsRaised+" tool="+tool.Current+" state="+tool.State+" grounded="+GroundedPlacement.IsSupported(p));
            File.WriteAllLines(Work+"lamp_room_results.txt",rows);P02MotionSampleProbe.Render(Work+door.name+".png",1920,1080);
        }
        rows.Add(rows.Any(x=>x.StartsWith("FAIL"))?"FAIL SUMMARY":"PASS SUMMARY");File.WriteAllLines(Work+"lamp_room_results.txt",rows);
    }
}
