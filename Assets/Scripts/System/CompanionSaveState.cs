using System;
using System.Collections.Generic;
using UnityEngine;

// Explicit narrative roles, never scene hierarchy names or arbitrary object paths.
public static class CompanionSaveState {
    static SavedCompanion Snapshot(string role,Transform actor,string parent,float direction=1)=>new SavedCompanion{role=role,parent=parent,position=actor.position,scale=actor.localScale,active=actor.gameObject.activeSelf,direction=direction};
    public static SavedCompanion[] Capture(){
        var result=new List<SavedCompanion>();
        var river=UnityEngine.Object.FindAnyObjectByType<Scene03RiverSequence>(FindObjectsInactive.Include);
        if(river&&river.boy){var chapter=river.chapter;string parent=river.boy.IsChildOf(chapter.homeRoot.transform)?"home":river.boy.IsChildOf(chapter.livingRoomRoot.transform)?"living":"river";result.Add(Snapshot("childhood-boy",river.boy,parent));}
        var home=UnityEngine.Object.FindAnyObjectByType<EndingHomeController>(FindObjectsInactive.Include);
        if(home&&home.guide)result.Add(Snapshot("home-guide",home.guide,"authored",home.SaveFollowDirection));
        return result.ToArray();
    }
    public static void Restore(SavedCompanion[] states){
        foreach(var state in states){
            Transform actor=null;
            if(state.role=="childhood-boy"){
                var river=UnityEngine.Object.FindAnyObjectByType<Scene03RiverSequence>(FindObjectsInactive.Include);
                if(!river||!river.boy)throw new InvalidOperationException("男孩角色未就绪");actor=river.boy;
                if(state.parent=="home")actor.SetParent(river.chapter.homeRoot.transform,true);
                else if(state.parent=="living")actor.SetParent(river.chapter.livingRoomRoot.transform,true);
            }else if(state.role=="home-guide"){
                var home=UnityEngine.Object.FindAnyObjectByType<EndingHomeController>(FindObjectsInactive.Include);
                if(!home||!home.guide)throw new InvalidOperationException("家属院引路人未就绪");actor=home.guide;home.RestoreFollowDirection(state.direction);
            }
            if(!actor)throw new InvalidOperationException("未知同伴状态");
            actor.position=state.position;actor.localScale=state.scale;actor.gameObject.SetActive(state.active);
        }
    }
}
