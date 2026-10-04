using UnityEngine;
public static class P04SceneLayoutMigration {
 public const int Revision=1;
 public static void Apply(GameSaveData data){if(data.scene!="Scene03_Childhood"||data.childhoodLayoutRevision>=Revision)return;data.position=Map(data.position);data.cameraPosition=Map(data.cameraPosition);foreach(var c in data.companions)c.position=Map(c.position);data.childhoodLayoutRevision=Revision;}
 static Vector3 Map(Vector3 p){if(p.y<5.5f&&p.x>=-30&&p.x<=30)p.x*=.6f;else if(p.y>=29&&p.y<=49&&p.x>=88)p.x=88+(p.x-88)*.4f;else if(p.y>=69&&p.y<74&&p.x<294.8f)p.x=296;return p;}
}
