using UnityEngine;

// Scene-owned, reversible decoration of the existing lamp body and glow.
public sealed class RuntimeToolArtwork : MonoBehaviour
{
 [SerializeField] PresentationArtLibrary art;
 SpriteRenderer original,view;OilLampController lamp;bool previous;
 void LateUpdate(){
  if(!art||!PersistentRuntime.Instance)return;
  if(!original){
   var root=PersistentRuntime.Instance.transform.Find("AdultOilLampVisual/LampBody");if(!root)return;
   original=root.GetComponent<SpriteRenderer>();if(!original)return;previous=original.forceRenderingOff;
   lamp=PersistentRuntime.Instance.GetComponent<OilLampController>();
   view=art.Add(root,"NewArt_Lamp","oil_lamp",new Vector2(0,-.5f),1.15f,1.25f,45);
  }
  if(!view)return;original.forceRenderingOff=true;view.enabled=original.enabled;
  view.color=lamp&&lamp.IsIlluminating?Color.white:new Color(.48f,.49f,.54f,1);
 }
 void OnDestroy(){if(original)original.forceRenderingOff=previous;if(view)Destroy(view.gameObject);}
}
