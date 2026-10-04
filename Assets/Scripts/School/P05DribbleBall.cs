using UnityEngine;
// Decorative court life only; the playable basketball physics remain unchanged.
public sealed class P05DribbleBall:MonoBehaviour {
 public P05PaintedLoop owner;
 void LateUpdate(){
  if(!owner||GamePause.IsPaused)return;
  float t=Mathf.Repeat(GameplayClock.Now/owner.period+owner.phaseOffset,1);
  float bounce=(2*t-1)*(2*t-1);
  transform.position=new Vector3(owner.transform.position.x+.39f,owner.transform.position.y+Mathf.Lerp(.13f,.90f,bounce),owner.transform.position.z);
 }
}
