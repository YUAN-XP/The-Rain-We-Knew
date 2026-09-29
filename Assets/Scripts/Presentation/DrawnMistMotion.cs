using UnityEngine;

/// <summary>Small visual drift only; the owning fog trigger and blockers never move.</summary>
[DefaultExecutionOrder(350)]
public sealed class DrawnMistMotion : MonoBehaviour
{
    public Vector2 drift=new Vector2(.10f,.055f);
    public float phase;
    public SpriteRenderer opacitySource;
    [Range(0,1)] public float opacity=.35f;
    Vector3 origin;bool initialized;
    void Awake(){origin=transform.localPosition;initialized=true;}
    void LateUpdate(){transform.localPosition=origin+new Vector3(Mathf.Sin(Time.time*.27f+phase)*drift.x,Mathf.Sin(Time.time*.19f+phase)*drift.y,0);if(opacitySource){var r=GetComponent<SpriteRenderer>();var c=r.color;c.a=opacitySource.color.a*opacity;r.color=c;r.enabled=opacitySource.enabled&&opacitySource.gameObject.activeInHierarchy;}}
    void OnDisable(){if(initialized)transform.localPosition=origin;}
}
