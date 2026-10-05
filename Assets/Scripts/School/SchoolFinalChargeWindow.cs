using UnityEngine;
/// <summary>One reaction deadline per finale attempt. Accepted starts retain the original continuous lamp charge.</summary>
public sealed class SchoolFinalChargeWindow {
 public const float Deadline=2f;
 public float Elapsed {get;private set;}
 public bool Accepted {get;private set;}
 public bool Expired=>!Accepted&&Elapsed>Deadline;
 public bool TryBegin(){if(Accepted)return true;if(Elapsed>Deadline)return false;Accepted=true;return true;}
 public void Cancel(){Accepted=false;}
 public void Advance(float delta,bool suspended){if(!suspended)Elapsed+=Mathf.Max(0,delta);}
}
