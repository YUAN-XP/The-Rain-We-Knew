using UnityEngine;
using UnityEngine.UI;
// Crossfades painted motion samples; fingers and wrist remain within the approved art.
public sealed class P04GuitarHands:MonoBehaviour {
 public GuitarRhythmGame rhythm;Image a,b;Sprite[] frames;int last=-1;float changed;
 void Start(){frames=new[]{DemoArtCatalog.Find("P04_GuitarHands:0"),DemoArtCatalog.Find("P04_GuitarHands:1"),DemoArtCatalog.Find("P04_GuitarHands:2"),DemoArtCatalog.Find("P04_GuitarHands:3")};a=Make("Painting");b=Make("Motion");a.sprite=b.sprite=frames[0];a.color=Color.white;b.color=new Color(1,1,1,0);}
 Image Make(string n){var t=RuntimePanelUI.Rect(n,transform,Vector2.zero,Vector2.one);var i=t.gameObject.AddComponent<Image>();i.preserveAspect=true;i.raycastTarget=false;return i;}
 void Update(){if(!a||frames==null)return;int index=0;if(rhythm&&!rhythm.WaitingForStart&&rhythm.IsRunning){float t=rhythm.PhraseTime;var phrase=rhythm.phrases[Mathf.Clamp(rhythm.PhraseIndex,0,3)];index=phrase.heldChord==KeyCode.LeftArrow?0:2;if(t>=phrase.notes[0].time-.5f)index=(Mathf.Sin(t*7)>0)?index:1;foreach(var note in phrase.notes)if(t>=note.time-.06f&&t<note.time+.23f)index=note.key==KeyCode.Space?3:((int)(t*4)%2==0?1:2);}
  if(index!=last){a.sprite=b.sprite;b.sprite=frames[index];last=index;changed=GameplayClock.Now;}float blend=Mathf.Clamp01((GameplayClock.Now-changed)/.18f);b.color=new Color(1,1,1,blend);}
}

