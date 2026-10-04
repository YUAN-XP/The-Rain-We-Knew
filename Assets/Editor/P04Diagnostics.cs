using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class P04Diagnostics {
 public static void LiveProbe(){var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();var player=controls.ActivePlayer;var interaction=player.GetComponent<PlayerInteraction>();var rows=new System.Collections.Generic.List<string>();rows.Add("player="+player.transform.position+" scale="+player.transform.lossyScale+" speed="+player.WalkSpeed+" feet="+interaction.Feet+" enabled="+interaction.enabled+" target="+interaction.CurrentVisualTarget+" blocked="+GameplayInput.Blocked+" lock="+controls.IsLocked());foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>Mathf.Abs(c.transform.position.x-player.transform.position.x)<5)){rows.Add(PathOf(c.transform)+" bounds="+c.bounds+" enabled="+c.enabled+" layer="+c.gameObject.layer+" trigger="+c.isTrigger);var reach=c.GetComponent<InteractionReach>();if(reach)rows.Add("reach="+(reach.anchor?reach.anchor.position.ToString():"null")+" h="+reach.horizontalReach+" v="+reach.verticalReach);}File.WriteAllLines(P04Regression.Work+"runtime_final/gate_probe.txt",rows);}
 public static void RuntimePaintings(string label){
  var rows=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy).Select(r=>PathOf(r.transform)+" | "+(r is SpriteRenderer sr&&sr.sprite?sr.sprite.name:r.GetType().Name)+" | foot="+r.bounds.min.y.ToString("F3")+" height="+r.bounds.size.y.ToString("F3")+" x="+r.bounds.center.x.ToString("F3")+" | "+string.Join(",",r.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))).ToArray();
  File.WriteAllLines(P04Regression.Work+"runtime_final/"+label+"_renderers.txt",rows);
 }
 public static void Renderers(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
  var ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();ch.homeRoot.SetActive(true);ch.livingRoomRoot.SetActive(true);ch.streetRoot.SetActive(true);
  var rows=Resources.FindObjectsOfTypeAll<Renderer>().Where(r=>r.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()).Where(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy).Select(r=>PathOf(r.transform)+" | "+r.GetType().Name+" | "+r.bounds+" | order="+r.sortingOrder+" | material="+(r.sharedMaterial?r.sharedMaterial.name:"")+" tex="+(r.sharedMaterial&&r.sharedMaterial.mainTexture?r.sharedMaterial.mainTexture.name:"")+" | sprite="+(r is SpriteRenderer s&&s.sprite?s.sprite.name:"")).ToArray();
  File.WriteAllLines(P04Audit.Work+"visible_renderers.txt",rows);
  File.WriteAllLines(P04Audit.Work+"catalog_probe.txt",new[]{"P04_QuietMemory","MOD_HOME_WALL_CLOSE_MEMORY_V03","MOD_STREET_RESIDENTIAL_CHILD:0","SPR_HOME_SOFA_TABLE_MEMORY:1","SPR_STREET_POND_SIGN:1"}.Select(k=>{var s=DemoArtCatalog.Find(k);return k+" rect="+s.rect+" pivot="+s.pivot+" ppu="+s.pixelsPerUnit;}));
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
}

