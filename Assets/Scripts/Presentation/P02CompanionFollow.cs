using System.Collections.Generic;
using UnityEngine;

/// <summary>Animation observer only. Authored story controllers retain all movement and follow decisions.</summary>
[DisallowMultipleComponent]
public sealed class P02CompanionFollow : MonoBehaviour
{
    struct Sample {public float seconds,distance;}
    readonly Queue<Sample> samples=new Queue<Sample>();
    Transform target;
    P02ActorPresentation view;
    Vector3 previous;
    float seconds,distance;
    bool catchingUp;
    int side=1;
    public bool IsFollowing => target;
    public bool CatchingUp => catchingUp;
    public int FacingSide => side;
    public void RestoreSide(float value){side=value<0?-1:1;}
    public void Begin(Transform leader,float authoredDistance=2.2f){target=leader;view=GetComponent<P02ActorPresentation>();Relocated();}
    public void End(){target=null;catchingUp=false;samples.Clear();if(view)view.CompanionRunning=false;}
    public void Relocated(){previous=transform.position;seconds=distance=0;samples.Clear();catchingUp=false;if(view)view.ResetTravel();}
    public void Observe(float dt,bool blocked)
    {
        float dx=transform.position.x-previous.x;previous=transform.position;
        if(!view)view=GetComponent<P02ActorPresentation>();
        if(!target||blocked||dt<=0||Mathf.Abs(dx)>.8f){catchingUp=false;seconds=distance=0;samples.Clear();if(view)view.CompanionRunning=false;return;}
        if(Mathf.Abs(dx)>.0001f)side=dx<0?-1:1;
        samples.Enqueue(new Sample{seconds=dt,distance=Mathf.Abs(dx)});seconds+=dt;distance+=Mathf.Abs(dx);
        while(samples.Count>1&&seconds>.12f){var sample=samples.Dequeue();seconds-=sample.seconds;distance-=sample.distance;}
        // Observe authored movement bursts; never replace them or use input as distance.
        var leader=target.GetComponent<PlayerController>();
        float walk=leader?leader.WalkSpeed:2.2f;
        catchingUp=seconds>.04f&&distance/seconds>walk*1.08f;
        if(view)view.CompanionRunning=catchingUp;
    }
    void OnDisable(){if(view)view.CompanionRunning=false;}
}
