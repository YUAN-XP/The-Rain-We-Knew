using UnityEngine;

// The same input state machine is used by the director and diagnostic tests.
// No method can declare success without accumulating a newly pressed hold.
public sealed class EndingFocusProgress
{
    public float Progress {get;private set;}
    public bool Complete=>Progress>=1;
    bool released,holding;
    public void RequireNewPress(){released=false;holding=false;}
    public void Tick(bool held,bool down,float delta,float duration,bool suspended){
        if(suspended){RequireNewPress();return;}
        if(!held){released=true;holding=false;return;}
        if(released&&down)holding=true;
        if(!holding||Complete||duration<=0)return;
        Progress=Mathf.Clamp01(Progress+Mathf.Clamp(delta,0,.1f)/duration);
    }
}
