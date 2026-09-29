using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class RemainingArtBuild
{
 [Serializable] public class Entry { public string key,asset; public int[] rect; }
 [Serializable] public class Entries { public Entry[] assets; }
 static Entry[] entries; public static T Ensure<T>(GameObject g) where T:Component {var c=g.GetComponent<T>(); return c?c:g.AddComponent<T>();}
 public static Transform[] All()=>RemainingArtEditor.All();
 public static string P(Transform t)=>RemainingArtEditor.PathOf(t);
 public static Transform F(string path)=>All().Single(t=>P(t)==path);
 public static Transform Child(Transform p,string name){var t=p.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(p,false);}return t;}
 public static void Ref(UnityEngine.Object o,string field,UnityEngine.Object v){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
 public static void Val(UnityEngine.Object o,string field,float v){var s=new SerializedObject(o);s.FindProperty(field).floatValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
 public static void Refs(UnityEngine.Object o,string field,IEnumerable<UnityEngine.Object> vals){var s=new SerializedObject(o);var a=s.FindProperty(field);var v=vals.ToArray();a.arraySize=v.Length;for(int i=0;i<v.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=v[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static Entry A(string key){entries??=JsonUtility.FromJson<Entries>(File.ReadAllText(RemainingArtEditor.W+"manifest.json")).assets;return entries.Single(x=>x.key==key);}
 public static void Import(){entries=null;foreach(var a in JsonUtility.FromJson<Entries>(File.ReadAllText(RemainingArtEditor.W+"manifest.json")).assets.Where(e=>!e.asset.Contains("/Scene01/")&&!e.asset.Contains("/IdentityApproved/")).GroupBy(e=>e.asset).Select(g=>g.First())){AssetDatabase.ImportAsset(a.asset);var ti=(TextureImporter)AssetImporter.GetAtPath(a.asset);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=256;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;ti.SetTextureSettings(settings);ti.SaveAndReimport();}}
 public static SpriteRenderer V(Transform parent,string name,string key,Vector2 foot,float width,float height,int order){var a=A(key);var t=Child(parent,name);var r=Ensure<SpriteRenderer>(t.gameObject);var crop=Ensure<SpriteArtRegion>(t.gameObject);var s=new SerializedObject(crop);s.FindProperty("source").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(a.asset);s.FindProperty("pixelRegion").rectValue=new Rect(a.rect[0],a.rect[1],a.rect[2],a.rect[3]);s.FindProperty("pivot").vector2Value=new Vector2(.5f,0);s.ApplyModifiedPropertiesWithoutUndo();crop.Rebuild();if(height<=0)height=width*a.rect[3]/a.rect[2];if(width<=0)width=height*a.rect[2]/a.rect[3];t.position=new Vector3(foot.x,foot.y,0);t.localScale=new Vector3(width*256/a.rect[2]/parent.lossyScale.x,height*256/a.rect[3]/parent.lossyScale.y,1);r.sortingOrder=order;r.color=Color.white;r.enabled=true;return r;}
 public static void Retire(Transform owner,IEnumerable<Renderer> renderers){var c=Ensure<RetiredArtVisibility>(owner.gameObject);Refs(c,"renderers",renderers);}
 public static void Link(SpriteRenderer src,SpriteRenderer view){var link=Ensure<SpriteVisibilityAlphaLink>(view.gameObject);Ref(link,"source",src);Ref(link,"visual",view);}
 public static void Highlight(MonoBehaviour source,SpriteRenderer[] visuals,bool dark){if(!(source is IInteractable))return;var h=Ensure<HandDrawnInteractionHighlight>(source.gameObject);Ref(h,"interactionSource",source);Refs(h,"visuals",visuals);var mat=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/正式/Effects/Interaction/Core"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).First(m=>m.name=="HandDrawnInteraction");Ref(h,"outlineMaterial",mat);Val(h,"maximumHighlightGap",.65f);Val(h,"opacityBreath",.16f);Val(h,"strokePixels",1.5f);var s=new SerializedObject(h);s.FindProperty("outerContour").arraySize=0;s.FindProperty("lineColor").colorValue=dark?new Color(.66f,.60f,.88f,.7f):new Color(1,.79f,.48f,.66f);s.ApplyModifiedPropertiesWithoutUndo();}
 public static void Region(Transform root,Vector2 min,Vector2 max){var c=Ensure<PlayerRegionVisualGroup>(root.gameObject);var s=new SerializedObject(c);s.FindProperty("showInEditMode").boolValue=true;s.FindProperty("regionMin").vector2Value=min;s.FindProperty("regionMax").vector2Value=max;var v=root.GetComponentsInChildren<SpriteRenderer>(true);var a=s.FindProperty("visuals");a.arraySize=v.Length;for(int i=0;i<v.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=v[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static void Atmosphere(Transform parent,Vector2 min,Vector2 max,float floor,bool dark,params Vector2[] lights){var c=Ensure<RoomAtmospherePresentation>(Child(parent,"NewArt_Atmosphere").gameObject);var s=new SerializedObject(c);s.FindProperty("material").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/正式/Effects/Scene01/DrawnStreetAtmosphere.mat");s.FindProperty("regionMin").vector2Value=min;s.FindProperty("regionMax").vector2Value=max;s.FindProperty("floorY").floatValue=floor;s.FindProperty("lightColor").colorValue=dark?new Color(.43f,.50f,.67f,.11f):new Color(1,.75f,.46f,.13f);s.FindProperty("mistColor").colorValue=dark?new Color(.35f,.31f,.52f,.05f):new Color(.7f,.71f,.77f,.012f);var a=s.FindProperty("lightAnchors");a.arraySize=lights.Length;for(int i=0;i<lights.Length;i++)a.GetArrayElementAtIndex(i).vector2Value=lights[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static void Mask(Transform root,Vector2 min,Vector2 max){var c=Ensure<ArtRoomVisibility>(Child(root,"NewArt_RoomVisibility").gameObject);var s=new SerializedObject(c);s.FindProperty("regionMin").vector2Value=min;s.FindProperty("regionMax").vector2Value=max;s.ApplyModifiedPropertiesWithoutUndo();Refs(c,"visuals",root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>P(r.transform).Contains("NewArt")&&r.transform.position.x>=min.x&&r.transform.position.x<=max.x&&r.transform.position.y>=min.y&&r.transform.position.y<=max.y));}
 static SpriteRenderer Replace(string path,string key,Vector2 foot,float width,float height,int order=0){var t=F(path);var old=t.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("/NewArt")).ToArray();var r=V(t,"NewArt_Visual",key,foot,width,height,order);var source=t.GetComponent<SpriteRenderer>();if(source && source.enabled)Link(source,r);else {var oldLink=r.GetComponent<SpriteVisibilityAlphaLink>();if(oldLink)UnityEngine.Object.DestroyImmediate(oldLink);r.enabled=true;}Retire(r.transform,old);var interaction=t.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m is IInteractable);if(interaction)Highlight(interaction,new[]{r},SceneManager.GetActiveScene().name.Contains("02"));return r;}
 static void Shell(Transform parent,float left,float right,float floor,bool dark,Vector2 regionMin,Vector2 regionMax){var root=Child(parent,"NewArt_RoomShell");string suffix=dark?"d":"n";float span=right-left;int n=Mathf.CeilToInt(span/8);float width=span/n;for(int i=0;i<n;i++){float x=left+(i+.5f)*width;var wall=V(root,"Wall_"+i,"wall_"+suffix,new Vector2(x,floor),width+.025f,7.5f,-35);wall.color=dark?new Color(.65f,.68f,.78f,1):new Color(.96f,.95f,.92f,1);V(root,"Floor_"+i,"floor_"+suffix,new Vector2(x,floor-4.5f),width+.025f,4.55f,-20);}Region(root,regionMin,regionMax);}
 public static void Preview02(){SceneManager.LoadScene("Scene02_Courtyard");}
 public static void Scene03Home(){
  if(SceneManager.GetActiveScene().name!="Scene03_Childhood"||Application.isPlaying)throw new Exception("Scene03 edit only");
  string home="Environment/Home_Floor01_Normal",living="Environment/LivingRoomInterior";
  var old=All().Where(t=>(P(t).StartsWith(home+"/")||P(t).StartsWith(living+"/"))&&!P(t).Contains("NewArt")&&!P(t).Contains("/母亲")&&!P(t).Contains("/父亲")&&!P(t).Contains("S03_Camera")&&!P(t).Contains("PlacedFamilyCamera")).SelectMany(t=>t.GetComponents<SpriteRenderer>()).ToArray();
  Retire(Child(F(home),"NewArt_RetiredViews"),old);
  Shell(F(home),-26,26,.5f,false,new Vector2(-40,-4),new Vector2(45,5.8f));
  Shell(F(home+"/Floor02_Normal"),-24,30,6.53f,false,new Vector2(-40,5.8f),new Vector2(45,20));
  Shell(F(home+"/MasterBedroomInterior_Normal"),58.5f,81,.5f,false,new Vector2(45,-4),new Vector2(100,15));
  Shell(F(home+"/SecondBedroomInterior_Normal"),118.5f,145.5f,.5f,false,new Vector2(105,-4),new Vector2(160,15));
  Shell(F(living),186,214,.5f,false,new Vector2(170,-4),new Vector2(235,15));
  Replace(home+"/家门","door_n",new Vector2(-23,.5f),1.75f,3.55f,-6);
  var entry=Replace(home+"/客厅门框","door_n",new Vector2(-2,.5f),1.75f,3.55f,-6);Highlight(F(home+"/S03_LivingRoomDoor").GetComponent<RoomTransitionInteractable>(),new[]{entry},false);
  var stairs=V(F(home+"/NewArt_RoomShell"),"Stairs","stairs_n",new Vector2(5.9f,.5f),4.4f,5.05f,-7);Highlight(F(home+"/S03_Stairs").GetComponent<Scene03Interactable>(),new[]{stairs},false);
  Replace(home+"/照片","photo",new Vector2(-15,1.65f),1.1f,0,-5);
  Replace(home+"/台灯","lamp",new Vector2(-10,1.37f),.65f,.95f,-2);
  V(F(home+"/NewArt_RoomShell"),"LampTable","cabinet",new Vector2(-10,.5f),1.3f,.88f,-4);
  var phone=Replace(home+"/电话小柜","phone",new Vector2(-21,.5f),1.35f,1.45f,-3);Highlight(F(home+"/S03_Phone").GetComponent<Scene03Interactable>(),new[]{phone},false);
  V(F(home+"/NewArt_RoomShell"),"ShoeRack","shoes",new Vector2(-18.8f,.5f),1.8f,0,-4);
  Replace(living+"/LivingRoomExitVisual","door_n",new Vector2(188,.5f),1.75f,3.55f,-6);
  Replace(living+"/LivingRoomFurniture/Sofa_Back","sofa_n",new Vector2(191.5f,.5f),3.7f,0,-4);
  var table=Replace(living+"/LivingRoomFurniture/客厅餐桌","table_n",new Vector2(200,.5f),4,1.5f,-3);Highlight(F(living+"/LivingRoomFurniture/S03_Table").GetComponent<DialogueInteractable>(),new[]{table},false);
  Replace(living+"/LivingRoomFurniture/TV_Cabinet","tv_n",new Vector2(209,.5f),3.2f,0,-4);
  Replace(living+"/LivingRoomFurniture/S03_Camera/相机","camera",new Vector2(201.2f,1.64f),.52f,0,3);
  V(F(living+"/NewArt_RoomShell"),"LivingWindow","window_n",new Vector2(202,4),3.7f,2.5f,-15);
  V(F(living+"/NewArt_RoomShell"),"PottedPlant","plant",new Vector2(212,.5f),1.2f,1.9f,-5);
  foreach(var room in new[]{home+"/MasterBedroomInterior_Normal",home+"/SecondBedroomInterior_Normal"}){
   var rt=F(room);float cx=rt.position.x;
   Replace(room+"/Bed","bed_n",new Vector2(cx+1.5f,.5f),4.9f,0,-3);
   Replace(room+"/Wardrobe","wardrobe_n",new Vector2(cx-4.6f,.5f),2.75f,3.25f,-4);
   V(F(room+"/NewArt_RoomShell"),"Window","window_n",new Vector2(cx+2,3.8f),3.5f,2.45f,-15);
  }
  Replace(home+"/MasterBedroomInterior_Normal/Cabinet","cabinet",new Vector2(77,.5f),1.9f,1.18f,-4);
  Replace(home+"/SecondBedroomInterior_Normal/Desk","desk_n",new Vector2(139,.5f),3.5f,1.8f,-3);
  foreach(var c in All().Select(t=>t.GetComponent<PlayerRegionVisualGroup>()).Where(c=>c&&P(c.transform).Contains("NewArt_RoomShell")))Refs(c,"visuals",c.GetComponentsInChildren<SpriteRenderer>(true));
  Mask(F(home),new Vector2(-40,-4),new Vector2(45,5.8f));Mask(F(home+"/Floor02_Normal"),new Vector2(-40,5.8f),new Vector2(45,20));
  Atmosphere(F(home),new Vector2(-26,-4),new Vector2(26,5.8f),.5f,false,new Vector2(-18,2),new Vector2(-10,1.8f));Atmosphere(F(living),new Vector2(186,-4),new Vector2(214,15),.5f,false,new Vector2(202,2));
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());RemainingArtEditor.Dump();
 }
 public static void Scene02(){
  if(SceneManager.GetActiveScene().name!="Scene02_Courtyard"||Application.isPlaying)throw new Exception("Scene02 edit only");
  var original=All().Where(t=>P(t).StartsWith("Environment/")||P(t).StartsWith("LivingRoomInterior_Return/")).SelectMany(t=>t.GetComponents<SpriteRenderer>()).Where(r=>!P(r.transform).Contains("NewArt")&&!P(r.transform).Contains("ShadowEvent")&&!r.name.Contains("DarkFog")&&!r.GetComponentsInParent<MonoBehaviour>(true).Any(m=>m is IInteractable && !m.transform.Find("NewArt_Visual"))).ToArray();
  Retire(Child(F("Environment"),"NewArt_RetiredViews"),original);
  Shell(F("Environment/Floor01"),-26,26,.5f,true,new Vector2(-40,-4),new Vector2(45,5.8f));
  Shell(F("Environment/Floor02"),-24,30,6.53f,true,new Vector2(-40,5.8f),new Vector2(45,20));
  Shell(F("Environment/MasterBedroomInterior"),58.5f,81,.5f,true,new Vector2(45,-4),new Vector2(100,15));
  Shell(F("Environment/HiddenBedroomArea"),118.5f,145.5f,.5f,true,new Vector2(105,-4),new Vector2(160,15));
  Shell(F("LivingRoomInterior_Return"),336,364,.5f,true,new Vector2(320,-4),new Vector2(380,15));
  foreach(var pair in new[]{("Environment/Floor01/BackGate_Placeholder",-23f,.5f),("Environment/Floor01/LockedLivingRoom_Placeholder",-2f,.5f),("Environment/Floor02/MasterBedroom_Placeholder",-3.62f,6.53f),("Environment/Floor02/HiddenBedroom_RevealDoor",-13.58f,6.53f),("Environment/MasterBedroomInterior/ExitDoor_Point",59.59f,.5f),("Environment/HiddenBedroomArea/HiddenBedroom_Exit",120.5f,.5f),("LivingRoomInterior_Return/LivingRoomExitVisual",338f,.5f)})Replace(pair.Item1,"door_d",new Vector2(pair.Item2,pair.Item3),1.75f,3.55f,-6);
  var stairs=Replace("Environment/Floor01/Stairs_Placeholder","stairs_d",new Vector2(5.9f,.5f),4.4f,5.05f,-7);Region(stairs.transform,new Vector2(-40,-4),new Vector2(45,5.8f));
  Replace("Environment/Floor02/StairExit_Point","stairs_d",new Vector2(7.6f,6.53f),3.7f,4.35f,-7);
  Replace("Environment/MasterBedroomInterior/Wardrobe_Placeholder","wardrobe_d",new Vector2(65.35f,.5f),2.75f,3.25f,-4);
  Replace("Environment/MasterBedroomInterior/Bed_Placeholder","bed_d",new Vector2(71.56f,.5f),4.9f,0,-3);
  Replace("Environment/HiddenBedroomArea/HiddenBedroom_Wardrobe","wardrobe_d",new Vector2(126,.5f),3,3.5f,-4);
  Replace("Environment/HiddenBedroomArea/HiddenBedroom_Bed","bed_d",new Vector2(132,.5f),4.5f,0,-3);
  Replace("Environment/HiddenBedroomArea/HiddenBedroom_Desk","desk_d",new Vector2(139,.5f),3.5f,1.8f,-3);
  Replace("Environment/HiddenBedroomArea/Window_Background","window_d",new Vector2(143.2f,2.4f),2.6f,2.4f,-15);
  Replace("LivingRoomInterior_Return/LivingRoomFurniture/Sofa_Back","sofa_d",new Vector2(341.5f,.5f),3.7f,0,-4);
  Replace("LivingRoomInterior_Return/LivingRoomFurniture/客厅餐桌","table_d",new Vector2(350,.5f),4,1.5f,-3);
  Replace("LivingRoomInterior_Return/LivingRoomFurniture/TV_Cabinet","tv_d",new Vector2(359,.5f),3.2f,0,-4);
  Replace("Environment/MasterBedroomInterior/Cabinet_Placeholder","cabinet",new Vector2(77.39f,.5f),1.9f,1.18f,-4).color=new Color(.65f,.68f,.76f,1);
  var lamp=Replace("Environment/MasterBedroomInterior/Lamp_Placeholder","lamp",new Vector2(76.69f,1.64f),.72f,.94f,-2);var bright=Ensure<SpriteBrightnessLink>(lamp.gameObject);Ref(bright,"source",F("Environment/MasterBedroomInterior/Lamp_Placeholder").GetComponent<SpriteRenderer>());Ref(bright,"visual",lamp);Val(bright,"referenceBrightness",1);
  Replace("Environment/MasterBedroomInterior/CameraItem_Placeholder","camera",new Vector2(78.06f,1.73f),.52f,0,3);
  Replace("Environment/Floor01/DirtyPhoto_Placeholder","photo",new Vector2(-15,1.65f),1.1f,0,-5).color=new Color(.6f,.62f,.72f,1);
  Replace("Environment/Floor01/BrokenLamp_Placeholder","lamp",new Vector2(-10,1.37f),.65f,.95f,-2).color=new Color(.60f,.60f,.72f,1);
  V(F("Environment/Floor01/NewArt_RoomShell"),"LampTable","cabinet",new Vector2(-10,.5f),1.3f,.88f,-4).color=new Color(.66f,.67f,.76f,1);
  V(F("Environment/Floor01/NewArt_RoomShell"),"ShoeRack","shoes",new Vector2(-18.8f,.5f),1.8f,0,-4).color=new Color(.65f,.67f,.75f,1);
  Replace("Environment/HiddenBedroomArea/Computer_Background","computer",new Vector2(139.5f,1.91f),1.2f,0,1).color=new Color(.7f,.76f,.88f,1);
  V(F("Environment/MasterBedroomInterior/NewArt_RoomShell"),"BedroomWindow","window_d",new Vector2(72,3.8f),3.5f,2.45f,-15);
  V(F("LivingRoomInterior_Return/NewArt_RoomShell"),"LivingWindow","window_d",new Vector2(352,4),3.7f,2.5f,-15);
  V(F("Environment/Floor01/NewArt_RoomShell"),"UpperWindow","window_d",new Vector2(-18,3.8f),3.4f,2.4f,-15);
  V(F("Environment/Floor02/NewArt_RoomShell"),"UpperWindow","window_d",new Vector2(17,9.4f),3.4f,2.4f,-15);
  foreach(var pair in new[]{("SilentShadow_Tall","shadow_father",2.08f),("SilentShadow_Short","shadow_mother",1.92f)}){
   var p="Environment/MasterBedroomInterior/MasterBedroom_ShadowEvent/"+pair.Item1;var t=F(p);var view=Replace(p,pair.Item2,new Vector2(t.position.x,.5f),0,pair.Item3,7);Link(t.GetComponent<SpriteRenderer>(),view);
  }
  var child=F("Gameplay/Scene02_Midgame/ChildShadow_Placeholder");var childView=Replace(P(child),"shadow_child",new Vector2(child.position.x,child.position.y-.6f),0,1.3f,8);Link(child.GetComponent<SpriteRenderer>(),childView);
  string corridor="Gameplay/Scene02_Midgame/DistortedCorridorArea";
  var oldCorridor=F(corridor).GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")&&!r.name.StartsWith("ThreeDoor_Anomaly")&&!r.name.StartsWith("CorridorWarp")).ToArray();Retire(Child(F(corridor),"NewArt_RetiredViews"),oldCorridor);
  Shell(F(corridor),176.4f,294,30.53f,true,new Vector2(165,22),new Vector2(310,48));
  foreach(var pair in new[]{("ThreeDoor_Left",257f),("ThreeDoor_Middle",269f),("ThreeDoor_Right",281f),("MasterBedroom_Reference",196.38f)})Replace(corridor+"/"+pair.Item1,"door_d",new Vector2(pair.Item2,30.53f),2.3f,4,-6);
  V(F(corridor+"/NewArt_RoomShell"),"CorridorStairwell","stairs_d",new Vector2(207.8f,30.53f),3.7f,4.35f,-7);
  var corridorShell=F(corridor+"/NewArt_RoomShell");Refs(corridorShell.GetComponent<PlayerRegionVisualGroup>(),"visuals",corridorShell.GetComponentsInChildren<SpriteRenderer>(true));
  Atmosphere(F("Environment/Floor01"),new Vector2(-26,-4),new Vector2(26,5.8f),.5f,true,new Vector2(-18,2),new Vector2(-10,1.8f));
  Atmosphere(F("Environment/MasterBedroomInterior"),new Vector2(58.5f,-4),new Vector2(81,15),.5f,true,new Vector2(72,2.2f));
  Atmosphere(F("Environment/HiddenBedroomArea"),new Vector2(118.5f,-4),new Vector2(145.5f,15),.5f,true,new Vector2(143,2));
  Atmosphere(F("LivingRoomInterior_Return"),new Vector2(336,-4),new Vector2(364,15),.5f,true,new Vector2(352,2));
  foreach(var path in new[]{"Environment/Floor01","Environment/Floor02","Environment/MasterBedroomInterior","Environment/HiddenBedroomArea","LivingRoomInterior_Return"}){var shell=F(path+"/NewArt_RoomShell");var c=shell.GetComponent<PlayerRegionVisualGroup>();Refs(c,"visuals",shell.GetComponentsInChildren<SpriteRenderer>(true));}
  Mask(F("Environment/Floor01"),new Vector2(-40,-4),new Vector2(45,5.8f));Mask(F("Environment/Floor02"),new Vector2(-40,5.8f),new Vector2(45,20));
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());RemainingArtEditor.Dump();
 }
}
