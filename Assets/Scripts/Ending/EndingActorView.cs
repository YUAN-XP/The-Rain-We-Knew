using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Linq;

// Non-controllable silhouette: never carries player input, outfit logic, physics or a listener.
[DefaultExecutionOrder(-90)]
public sealed class EndingActorView:MonoBehaviour
{
    public Transform hand,arm,pendant;
    public P02ActorPresentation nativePose;
    Vector3 reachTarget;bool reaching;float reachBlend;public bool HoldTemple;
    public Vector3 NecklacePoint=>nativePose&&nativePose.Rig?nativePose.Rig.transform.Find("head").position-Vector3.up*.13f:transform.position;
    public Vector3 PocketPoint=>nativePose&&nativePose.Rig?nativePose.Rig.transform.Find("pelvis").position+new Vector3(.07f,-.1f,0):transform.position;
    void LateUpdate(){
        if(!nativePose||!nativePose.Rig)return;
        var rig=nativePose.Rig;hand=rig.HandCamera;
        if(HoldTemple){reachTarget=rig.transform.Find("head").position+new Vector3(rig.definition.direction=="left"?-.06f:.06f,.16f,0);reaching=true;}
        if(pendant)pendant.position=NecklacePoint;
        reachBlend=Mathf.MoveTowards(reachBlend,reaching?1:0,GameplayClock.Delta*5);
        if(reachBlend<=0)return;
        // Ending-only reach on the existing calibrated mesh. Never writes to the P02 binding or actor motion.
        var d=rig.definition;var binding=d.parts.FirstOrDefault(p=>p.joint=="near_arm");
        var joint=rig.transform.Find("near_arm");if(!joint||binding==null||!binding.calibratedLimb)return;
        var skin=joint.GetComponentInChildren<SpriteSkin>();if(!skin||skin.boneTransforms.Length<2)return;
        var shoulder=rig.transform.Find("near_upper_arm").position;
        float scale=rig.targetHeight/d.referenceHeight*Mathf.Abs(rig.transform.lossyScale.y);
        float a=d.upperArmLength*scale,b=d.forearmLength*scale;
        var wrist=Vector3.Lerp(hand.position,reachTarget,reachBlend);
        wrist=shoulder+Vector3.ClampMagnitude(wrist-shoulder,a+b-.003f);
        var e1=P02CharacterRig.SolveJoint(shoulder,wrist,a,b,true);
        var e2=P02CharacterRig.SolveJoint(shoulder,wrist,a,b,false);
        Vector3 elbow=e1.y<e2.y?e1:e2;
        float upper=Mathf.Atan2(elbow.y-shoulder.y,elbow.x-shoulder.x)*Mathf.Rad2Deg+90;
        float lower=Mathf.Atan2(wrist.y-elbow.y,wrist.x-elbow.x)*Mathf.Rad2Deg+90;
        joint.position=shoulder;joint.rotation=Quaternion.Euler(0,0,upper);
        var root=skin.boneTransforms[0];var tip=skin.boneTransforms[1];
        float us=Vector3.Distance(shoulder,elbow)/(binding.bindUpperLength*scale);
        float ls=Vector3.Distance(elbow,wrist)/(binding.bindLowerLength*scale);
        root.position=shoulder;root.rotation=Quaternion.Euler(0,0,upper);root.localScale=Vector3.one*us;
        tip.position=elbow;tip.rotation=Quaternion.Euler(0,0,lower);tip.localScale=Vector3.one*(ls/us);
        hand.position=wrist;hand.rotation=Quaternion.Euler(0,0,lower);
    }
    public void EndReach(){reaching=false;}
    public static EndingActorView Create(string name,Transform parent,Sprite sprite,Vector3 position,float size,Color coat){
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=Vector3.one*size;
        var a=go.AddComponent<EndingActorView>();
        Part("Coat",go.transform,sprite,new Vector2(0,-.07f),new Vector2(.65f,1.1f),coat,22);
        Part("HeadSilhouette",go.transform,sprite,new Vector2(0,.78f),new Vector2(.43f,.47f),new Color(.18f,.19f,.21f),23);
        Part("LeftLeg",go.transform,sprite,new Vector2(-.17f,-.78f),new Vector2(.22f,.55f),coat*.85f,22);
        Part("RightLeg",go.transform,sprite,new Vector2(.17f,-.78f),new Vector2(.22f,.55f),coat*.85f,22);
        a.arm=Part("Arm",go.transform,sprite,new Vector2(.35f,.05f),new Vector2(.16f,.72f),coat,24).transform;
        a.hand=Part("Hand",go.transform,sprite,new Vector2(.35f,-.35f),new Vector2(.18f,.2f),new Color(.62f,.57f,.49f),25).transform;
        a.pendant=new GameObject("PlectrumNecklace").transform;a.pendant.SetParent(go.transform,false);a.pendant.localPosition=new Vector3(0,.47f,0);
        Part("Plectrum",a.pendant,sprite,Vector2.zero,new Vector2(.12f,.15f),new Color(.91f,.75f,.37f),26);
        for(int sign=-1;sign<=1;sign+=2){var cord=Part("NecklaceCord",a.pendant,sprite,new Vector2(sign*.065f,.13f),new Vector2(.024f,.24f),new Color(.76f,.67f,.45f),26);cord.transform.localRotation=Quaternion.Euler(0,0,sign*-27);}
        a.pendant.gameObject.SetActive(false);
        return a;
    }
    public void Reach(Vector3 point){if(nativePose){nativePose.ToolBlend=0;reachTarget=point;reaching=true;return;}hand.position=point;}
    public static SpriteRenderer Part(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color color,int order){
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=new Vector3(size.x,size.y,1);var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
    }
}
