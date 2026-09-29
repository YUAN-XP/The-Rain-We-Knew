using UnityEngine;

/// <summary>Continuous chord interval. A short release grace tolerates keyboard transitions.</summary>
public sealed class RhythmHoldTracker
{
    float start,end,gap,grace;
    public bool Failed {get;private set;}
    public void Reset(float from,float to,float releaseGrace){start=from;end=to;grace=Mathf.Max(0,releaseGrace);gap=0;Failed=false;}
    public bool Tick(float time,float delta,bool held)
    {
        if(time<start||time>end)return false;
        gap=held?0:gap+Mathf.Max(0,delta);
        if(!Failed&&gap>grace){Failed=true;return true;}
        return false;
    }
}
