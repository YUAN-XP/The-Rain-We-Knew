using UnityEngine;
/// <summary>Only the painted fragments move; the continuous walking surface and doors are unchanged.</summary>
public sealed class P03CorridorCollapse:MonoBehaviour
{
 public DistortedCorridorController corridor;
 public Transform[] fragments;
 Vector3[] origins;float[] rotation;
 void Awake(){origins=new Vector3[fragments.Length];rotation=new float[fragments.Length];for(int i=0;i<fragments.Length;i++){origins[i]=fragments[i].localPosition;rotation[i]=fragments[i].localEulerAngles.z;}}
 void LateUpdate()
 {
  if(!corridor)return;float amount=corridor.LampStage*.5f;
  for(int i=0;i<fragments.Length;i++)
  {
   float sign=i%2==0?1:-1;var p=origins[i];p.y+=amount*amount*(.025f+.018f*(i%3));p.x+=sign*amount*.032f;
   fragments[i].localPosition=p;fragments[i].localRotation=Quaternion.Euler(0,0,rotation[i]+sign*amount*.85f);
  }
 }
}
