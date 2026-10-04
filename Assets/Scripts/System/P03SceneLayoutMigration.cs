using UnityEngine;

/// <summary>Reads legacy Scene02 saves into the current layout without rewriting the user's slot.</summary>
public static class P03SceneLayoutMigration
{
 public const int Revision=3;
 public static void Apply(GameSaveData data)
 {
  if(data.courtyardLayoutRevision>=Revision||data.scene!="Scene02_Courtyard")return;
  data.position=Map(data.position);data.cameraPosition=Map(data.cameraPosition);
  foreach(var c in data.companions)if(c.role=="home-guide")c.position=Map(c.position);
  if(data.distortedJoined)
   data.corridorLampStage=data.position.x>=7?2:data.position.x>=0?1:0;
  data.courtyardLayoutRevision=Revision;
 }
 static Vector3 Map(Vector3 p)
 {
  if(p.x>=-30&&p.x<=30&&p.y<5.5f)p.x*=.6f;
  else if(p.x>=165&&p.x<=315&&p.y>=22)
  {
   float old=p.x;p.y-=24;
   p.x=old<210?Mathf.Clamp(old-200,-11.7f,-.5f):old<250?Mathf.Lerp(-.5f,10f,(old-210)/40):Mathf.Clamp(12.5f+(old-257)*2.5f/12,10.5f,20);
  }
  return p;
 }
}

