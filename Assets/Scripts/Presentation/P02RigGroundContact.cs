using UnityEngine;

/// <summary>Presentation-only contact marks at the measured ground. Never moves or conceals joints.</summary>
[DefaultExecutionOrder(1350)]
public sealed class P02RigGroundContact : MonoBehaviour
{
    P02CharacterRig rig;
    Mesh mesh;
    MeshRenderer left,right;
    MaterialPropertyBlock properties;
    Color ink;
    int contactOrder;
    void Start()
    {
        rig=GetComponent<P02CharacterRig>();
        var material=Resources.Load<Material>("DemoContactShadow");
        if(!rig||!material){enabled=false;return;}
        properties=new MaterialPropertyBlock();ink=material.GetColor("_Color");
        contactOrder=int.MaxValue;
        foreach(var sprite in rig.GetComponentsInChildren<SpriteRenderer>())contactOrder=Mathf.Min(contactOrder,sprite.sortingOrder-2);
        mesh=new Mesh{name="P02 measured foot contact",hideFlags=HideFlags.DontSave};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
        left=CreateMark("Left foot ground contact",material);
        right=CreateMark("Right foot ground contact",material);
    }
    MeshRenderer CreateMark(string label,Material material)
    {
        var g=new GameObject(label);g.hideFlags=HideFlags.DontSave;g.transform.SetParent(transform,false);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        return renderer;
    }
    void LateUpdate()
    {
        if(!left||!rig)return;
        Draw(left,rig.leftSole,rig.leftGround,rig.leftContact,rig.FootLeft);
        Draw(right,rig.rightSole,rig.rightGround,rig.rightContact,rig.FootRight);
    }
    void Draw(MeshRenderer mark,Vector3 support,Vector3 ground,bool contact,Transform foot)
    {
        var shoe=foot.GetComponentInChildren<SpriteRenderer>();
        mark.enabled=rig.enabled&&shoe&&shoe.enabled&&!shoe.forceRenderingOff;
        if(!mark.enabled)return;
        float h=rig.targetHeight/rig.definition.referenceHeight;
        // The contact point, unlike the sprite's transparent bounds, is a painted edge.
        // Airborne feet keep a subdued projection; the source shoe never gets lowered.
        float lift=Mathf.Max(0,support.y-ground.y)/h;
        float strength=contact?.6f:Mathf.Lerp(.35f,.1f,Mathf.Clamp01(lift/.25f));
        var color=ink;color.a*=strength;properties.SetColor("_Color",color);mark.SetPropertyBlock(properties);
        mark.transform.position=new Vector3(support.x,ground.y-.007f*h,rig.transform.position.z);
        var scale=transform.lossyScale;
        mark.transform.localScale=new Vector3(.20f*h/Mathf.Abs(scale.x),.055f*h/Mathf.Abs(scale.y),1);
        mark.sortingLayerID=shoe.sortingLayerID;mark.sortingOrder=contactOrder;
    }
    void OnDisable(){if(left)left.enabled=false;if(right)right.enabled=false;}
    void OnDestroy(){if(mesh)Destroy(mesh);}
}
