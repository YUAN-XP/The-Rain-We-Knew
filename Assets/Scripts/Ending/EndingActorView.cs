using UnityEngine;

// Non-controllable silhouette: never carries player input, outfit logic, physics or a listener.
public sealed class EndingActorView:MonoBehaviour
{
    public Transform hand,arm,pendant;
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
    public void Reach(Vector3 point){hand.position=point;Vector3 shoulder=transform.TransformPoint(new Vector3(.29f,.31f,0));Vector3 localA=transform.InverseTransformPoint(shoulder),localB=transform.InverseTransformPoint(point);var v=localB-localA;arm.localPosition=(localA+localB)*.5f;arm.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg-90);arm.localScale=new Vector3(.16f,v.magnitude,1);}
    public static SpriteRenderer Part(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color color,int order){
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=new Vector3(size.x,size.y,1);var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
    }
}
