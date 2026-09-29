using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Explicit, Inspector-authored art regions for runtime-created presentation only.</summary>
[CreateAssetMenu(menuName="Presentation/Art Library")]
public sealed class PresentationArtLibrary : ScriptableObject
{
 [Serializable] public class Region { public string key; public Sprite source; public Rect pixels; }
 [SerializeField] Region[] regions=Array.Empty<Region>();
 readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
 public Sprite Get(string key){
  var current=DemoArtCatalog.Find("LEGACY:"+key);if(current)return current;
  if(cache.TryGetValue(key,out var existing)&&existing)return existing;
  var entry=Array.Find(regions,r=>r.key==key);
  if(entry==null||!entry.source)return null;
  var texture=entry.source.texture;var rect=entry.pixels;
  if(rect.width<=0||rect.height<=0||rect.xMin<0||rect.yMin<0||rect.xMax>texture.width||rect.yMax>texture.height)return null;
  var sprite=Sprite.Create(texture,rect,new Vector2(.5f,0),entry.source.pixelsPerUnit,0,SpriteMeshType.FullRect,Vector4.zero,false);
  sprite.name=key; sprite.hideFlags=HideFlags.DontSave;cache[key]=sprite;return sprite;
 }
 void OnDisable(){foreach(var s in cache.Values)if(s){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}cache.Clear();}
 public SpriteRenderer Add(Transform parent,string name,string key,Vector2 localFoot,float width,float height,int order){
  var sprite=Get(key);if(!sprite)return null;
  var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=localFoot;
  if(width<=0)width=height*sprite.rect.width/sprite.rect.height;
  if(height<=0)height=width*sprite.rect.height/sprite.rect.width;
  g.transform.localScale=new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
  var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;
 }
}
