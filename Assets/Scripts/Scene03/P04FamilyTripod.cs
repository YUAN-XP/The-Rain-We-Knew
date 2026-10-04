using UnityEngine;
public sealed class P04FamilyTripod:MonoBehaviour {
 public Scene03FamilySequence family;SpriteRenderer ink;
 void Awake(){ink=GetComponent<SpriteRenderer>();}
 void LateUpdate(){if(ink&&family)ink.enabled=!family.placedCamera.activeSelf;}
}
