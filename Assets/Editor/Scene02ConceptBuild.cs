using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using K=ContinuousRoomArtwork.Knot;

public static class Scene02ConceptBuild
{
 const string A="Assets/Art/正式/Backgrounds/Scene02/ConceptMatched_R2/";
 const string W=ArtRebuildEditor.W;
 static Transform[] All()=>ArtRebuildEditor.All();
 static string P(Transform t)=>ArtRebuildEditor.PathOf(t);
 static Transform F(string path)=>All().Single(t=>P(t)==path);
 static T Ensure<T>(GameObject g)where T:Component { var component=g.GetComponent<T>(); return component ? component : g.AddComponent<T>(); }
 static Transform Child(Transform p,string name){var t=p.Find(name);if(t)return t;t=new GameObject(name).transform;t.SetParent(p,false);return t;}
 static void Ref(UnityEngine.Object o,string n,UnityEngine.Object v){var s=new SerializedObject(o);s.FindProperty(n).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Refs(UnityEngine.Object o,string n,IEnumerable<UnityEngine.Object> values){var s=new SerializedObject(o);var p=s.FindProperty(n);var a=values.ToArray();p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(A+"PM_S02_"+name+"_Concept_R2.png");
 static Material Mat;
 public static void Import()
 {
  AssetDatabase.Refresh();
  foreach(var file in Directory.GetFiles(A,"*.png"))
  {
   var path=file.Replace('\\','/');var ti=(TextureImporter)AssetImporter.GetAtPath(path);
   ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=256;
   ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;
   ti.filterMode=FilterMode.Bilinear;ti.wrapMode=TextureWrapMode.Clamp;
   var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;ti.SetTextureSettings(settings);ti.SaveAndReimport();
  }
  Mat=AssetDatabase.LoadAssetAtPath<Material>(A+"ContinuousPainting.mat");
  if(!Mat){Mat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(Mat,A+"ContinuousPainting.mat");}
  AssetDatabase.SaveAssets();
 }
 static K[] Knots(float[] pairs,float factor)
 {var a=new K[pairs.Length/2];for(int i=0;i<a.Length;i++)a[i]=new K(pairs[i*2]*factor,pairs[i*2+1]);return a;}
 static ContinuousRoomArtwork Room(Transform parent,string name,string image,float[] x,float[] y,Rect region)
 {
  var t=Child(parent,"ConceptR2_"+name);t.position=Vector3.zero;t.rotation=Quaternion.identity;
  t.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1);
  var c=Ensure<ContinuousRoomArtwork>(t.gameObject);c.painting=Sprite(image).texture;c.material=Mat;
  c.horizontal=Knots(x,c.painting.width/2048f);c.vertical=Knots(y,c.painting.height/683f);c.actorRegion=region;c.Rebuild();return c;
 }
 static SpriteRenderer Cut(Transform parent,string name,Sprite source,Rect pixel,float x,float y,float width,float height,int order)
 {
  var t=Child(parent,name);var r=Ensure<SpriteRenderer>(t.gameObject);var crop=Ensure<SpriteArtRegion>(t.gameObject);
  var s=new SerializedObject(crop);s.FindProperty("source").objectReferenceValue=source;s.FindProperty("pixelRegion").rectValue=pixel;
  s.FindProperty("pivot").vector2Value=new Vector2(.5f,0);s.ApplyModifiedPropertiesWithoutUndo();crop.Rebuild();
  t.position=new Vector3(x,y,0);t.rotation=Quaternion.identity;
  t.localScale=new Vector3(width*256/pixel.width/parent.lossyScale.x,height*256/pixel.height/parent.lossyScale.y,1);
  r.sortingOrder=order;r.color=Color.white;r.enabled=true;return r;
 }
 static SpriteRenderer Surface(ContinuousRoomArtwork room,string path,float left,float top,float right,float bottom)
 {
  float sx=room.painting.width/2048f,sy=room.painting.height/683f;
  var rect=new Rect(left*sx,room.painting.height-bottom*sy,(right-left)*sx,(bottom-top)*sy);
  float l=ContinuousRoomArtwork.Map(room.horizontal,left*sx),r=ContinuousRoomArtwork.Map(room.horizontal,right*sx);
  float b=ContinuousRoomArtwork.Map(room.vertical,bottom*sy),t=ContinuousRoomArtwork.Map(room.vertical,top*sy);
  var output=Cut(F(path),"ConceptR2_Surface",AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(room.painting)),rect,(l+r)/2,b,r-l,t-b,-28);
  // Only the calibrated panorama is painted. This invisible surface supplies the outline contour.
  var retired=Ensure<RetiredArtVisibility>(output.gameObject);Refs(retired,"renderers",new[]{output});output.forceRenderingOff=true;
  var interaction=F(path).GetComponents<MonoBehaviour>().FirstOrDefault(m=>m is IInteractable);
  if(interaction)Highlight(interaction,output,true);
  return output;
 }
 static void Highlight(MonoBehaviour source,SpriteRenderer visual,bool rectangle)
 {
  var h=Ensure<HandDrawnInteractionHighlight>(source.gameObject);Ref(h,"interactionSource",source);Refs(h,"visuals",new[]{visual});
  var s=new SerializedObject(h);s.FindProperty("maximumHighlightGap").floatValue=.65f;s.FindProperty("lineColor").colorValue=new Color(.8f,.76f,.92f,.42f);
  var contour=s.FindProperty("outerContour");contour.arraySize=rectangle?8:0;
  var pts=new[]{new Vector2(.02f,.03f),new Vector2(.02f,.5f),new Vector2(.025f,.97f),new Vector2(.5f,.98f),new Vector2(.97f,.96f),new Vector2(.98f,.5f),new Vector2(.97f,.03f),new Vector2(.5f,.02f)};
  if(rectangle && source.name.Contains("Bed_") && !source.name.Contains("Door"))pts=new[]{new Vector2(0,.1f),new Vector2(.03f,0),new Vector2(.97f,0),new Vector2(1,.1f),new Vector2(.93f,.7f),new Vector2(.86f,.78f),new Vector2(.86f,.99f),new Vector2(.14f,.99f),new Vector2(.14f,.78f),new Vector2(.05f,.7f)}; if(rectangle && source.name.Contains("Desk"))pts=new[]{new Vector2(.02f,.02f),new Vector2(.02f,.61f),new Vector2(.13f,.61f),new Vector2(.13f,.98f),new Vector2(.68f,.98f),new Vector2(.68f,.61f),new Vector2(.98f,.61f),new Vector2(.98f,.02f)}; contour.arraySize=rectangle?pts.Length:0; if(rectangle)for(int i=0;i<pts.Length;i++)contour.GetArrayElementAtIndex(i).vector2Value=pts[i]; s.FindProperty("opacityBreath").floatValue=.2f;
  s.FindProperty("contourMaterial").objectReferenceValue=Mat;
  if(!s.FindProperty("outlineMaterial").objectReferenceValue)
   s.FindProperty("outlineMaterial").objectReferenceValue=AssetDatabase.FindAssets("HandDrawnInteraction t:Material").Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).First();
  s.ApplyModifiedPropertiesWithoutUndo();
 }
 static SpriteRenderer Prop(string path,string name,Rect pixels,float x,float y,float width,float height,bool alpha=false,bool brightness=false)
 {
  var root=F(path);var r=Cut(root,name,Sprite("Props"),pixels,x,y,width,height,-2);
  var source=root.GetComponent<SpriteRenderer>();
  if(alpha){var link=Ensure<SpriteVisibilityAlphaLink>(r.gameObject);Ref(link,"source",source);Ref(link,"visual",r);}
  else if(brightness){var link=Ensure<SpriteBrightnessLink>(r.gameObject);Ref(link,"source",source);Ref(link,"visual",r);}
  else {var link=Ensure<RendererVisibilityLink>(r.gameObject);Ref(link,"source",source);Ref(link,"visual",r);}
  var interaction=root.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m is IInteractable);if(interaction)Highlight(interaction,r,false);
  return r;
 }
 static void RetireOld(Transform root)
 {
  foreach(var mask in root.GetComponentsInChildren<ArtRoomVisibility>(true))mask.enabled=false;
  var old=root.GetComponentsInChildren<Renderer>(true).Where(r=>!P(r.transform).Contains("ConceptR2")&&!P(r.transform).Contains("ShadowEvent")&&!P(r.transform).Contains("ReturnFileFragment2")&&!P(r.transform).Contains("ReturnCameraGentlemanEcho")).ToArray();
  var owner=Child(root,"ConceptR2_RetiredVisuals");var c=Ensure<RetiredArtVisibility>(owner.gameObject);Refs(c,"renderers",old);
 }
 public static void Contract()
 {
  var rows=All().SelectMany(t=>t.GetComponents<Component>()).Where(c=>c&&!P(c.transform).Contains("ConceptR2")).Where(c=>
  {
   if(c is Collider2D||c is Rigidbody2D)return true;
   if(!(c is MonoBehaviour m))return false;
   var script=MonoScript.FromMonoBehaviour(m);var path=script?AssetDatabase.GetAssetPath(script):"";
   return path.StartsWith("Assets/Scripts/")&&!path.Contains("/Presentation/");
  }).Select(c=>P(c.transform)+"|"+c.GetType().FullName+"|"+EditorJsonUtility.ToJson(c)).OrderBy(x=>x).ToArray();
  File.WriteAllLines(W+"contract_current.txt",rows);
 }
 public static void Build()
 {
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene02_Courtyard")throw new Exception("Scene02 edit mode only");
  if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene; do not overwrite user changes");
  Import();
  const string f1="Environment/Floor01",f2="Environment/Floor02",master="Environment/MasterBedroomInterior",second="Environment/HiddenBedroomArea",living="LivingRoomInterior_Return",corr="Gameplay/Scene02_Midgame/DistortedCorridorArea";
  Contract();if(!File.Exists(W+"contract_before.txt"))File.Copy(W+"contract_current.txt",W+"contract_before.txt");
  foreach(var root in new[]{f1,f2,master,second,living,corr})RetireOld(F(root));
  var one=Room(F(f1),"WholeFirstFloor","Floor01",
   new float[]{0,-26,70,-24,125,-23,182,-22,344,-15.65f,386,-15,424,-14.35f,559,-10.8f,600,-10,641,-9.2f,792,-3,844,-2,897,-1,1058,4,1167,7,1278,10,2048,26},
   new float[]{218,4.5f,245,3.1f,273,2.72f,332,1.8f,410,.66f,419,.5f,428,-1},new Rect(-40,-4,85,9.8f));
  var two=Room(F(f2),"WholeSecondFloor","Floor02",
   new float[]{0,-24,810,-4.72f,870,-3.62f,930,-2.52f,1180,3.8f,1260,5.3f,1330,7.8f,1390,10.1f,1753,15.55f,1820,17,1887,18.45f,2048,22},
   new float[]{214,10.6f,241,9.23f,350,7.83f,437,6.73f,446,6.53f,458,5.1f},new Rect(-40,5.8f,85,15));
  var bed=Room(F(master),"WholeMasterBedroom","Master",
   new float[]{0,58.5f,43,58.64f,128,59.59f,212,60.54f,430,63.8f,567,65.35f,704,66.9f,855,69.11f,1130,71.56f,1407,74.01f,1536,75.81f,1720,77.39f,1904,78.97f,2048,81.5f},
   new float[]{0,5f,186,3f,214,2.65f,357,1.65f,389,1.45f,540,.68f,564,.5f,683,-1.1f},new Rect(45,-4,55,19));
  var bedroom=Room(F(second),"WholeSecondBedroom","Second",
   new float[]{0,118.5f,52,119.6f,126,120.5f,200,121.4f,335,124.5f,458,126,580,127.5f,735,129.75f,963,132,1191,134.25f,1294,137,1468,139,1640,141,1730,142.05f,1860,143.5f,2008,144.95f,2048,145.5f},
   new float[]{0,5,119,3f,212,2.65f,269,2.05f,335,1.58f,363,1.35f,503,.68f,526,.5f,683,-1.1f},new Rect(105,-4,55,19));
  var sitting=Room(F(living),"WholeReturnLivingRoom","Living",
   new float[]{0,336,70,337.1f,154,338,239,338.9f,310,339.65f,582,341.5f,860,343.35f,980,346.5f,1035,348,1185,350,1338,352,1370,354.3f,1455,355.6f,1485,357.4f,1682,359,1884,360.6f,2048,364},
   new float[]{0,5,138,3f,334,1.6f,501,.72f,520,.5f,683,-1.1f},new Rect(320,-4,65,19));
  var longRoom=Room(F(corr),"WholeDistortedCorridor","Corridor",
   new float[]{0,176.4f,335,195.23f,380,196.38f,426,197.53f,513,203.5f,571,207.8f,650,211.5f,760,218,920,224,1352,255.85f,1392,257,1434,258.15f,1550,267.85f,1591,269,1630,270.15f,1745,279.85f,1792,281,1834,282.15f,2048,294},
   new float[]{217,34.6f,281,33.23f,358,31.83f,432,30.73f,438,30.53f,466,29.1f},new Rect(165,22,150,26));
  longRoom.fadeLeft=new Vector2(218,224);longRoom.recoverySource=F(corr+"/ThreeDoor_NormalWall").GetComponent<SpriteRenderer>();longRoom.Rebuild();
  // The join must use the identical upstairs painting, translated by the existing scene offset.
  var continuity=Room(F(corr),"ContinuousUpstairsEcho","Floor02",new float[]{0,176,810,195.28f,870,196.38f,930,197.48f,1180,203.8f,1260,205.3f,1330,207.8f,1390,210.1f,1753,215.55f,1820,217,1887,218.45f,2048,226},new float[]{214,34.6f,241,33.23f,350,31.83f,437,30.73f,446,30.53f,458,29.1f},new Rect(165,22,150,26));
  continuity.sortingOrder=-31;continuity.Rebuild();
  Surface(one,f1+"/BackGate_Placeholder",70,254,182,419);
  Surface(one,f1+"/DirtyPhoto_Placeholder",344,273,424,325);
  Surface(one,f1+"/LockedLivingRoom_Placeholder",792,245,897,419);
  Surface(two,f2+"/MasterBedroom_Placeholder",810,241,930,446);
  foreach(var v in new[]{(master+"/ExitDoor_Point",43f,186f,212f,540f),(master+"/Wardrobe_Placeholder",430f,214f,704f,545f),(master+"/Bed_Placeholder",855f,357f,1407f,564f),(master+"/Cabinet_Placeholder",1536f,389f,1904f,545f)})Surface(bed,v.Item1,v.Item2,v.Item3,v.Item4,v.Item5);
  foreach(var v in new[]{(second+"/HiddenBedroom_Exit",52f,119f,200f,503f),(second+"/HiddenBedroom_Wardrobe",335f,212f,580f,505f),(second+"/HiddenBedroom_Bed",735f,344f,1191f,526f),(second+"/HiddenBedroom_Desk",1294f,269f,1640f,506f)})Surface(bedroom,v.Item1,v.Item2,v.Item3,v.Item4,v.Item5);
  Surface(sitting,living+"/ReturnLivingRoomDoor",70,138,239,510);
  foreach(var v in new[]{("ThreeDoor_Left",1352f,1434f),("ThreeDoor_Middle",1550f,1630f),("ThreeDoor_Right",1745f,1834f)})Surface(longRoom,corr+"/"+v.Item1,v.Item2,281,v.Item3,438);
  // Independent story visuals; positions are on their newly painted supporting surfaces.
  Prop(f2+"/HiddenBedroom_RevealDoor","ConceptR2_RevealDoor",new Rect(307,445,306,550),-13.58f,6.53f,1.80f,2.70f,true);
  var floorLamp=Prop(f1+"/BrokenLamp_Placeholder","ConceptR2_Lamp",new Rect(929,448,289,455),-10,1.23f,.56f,.88f); Ref(floorLamp.GetComponent<RendererVisibilityLink>(),"source",F(f1+"/BrokenLamp_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>());
  Prop(master+"/Lamp_Placeholder","ConceptR2_Lamp",new Rect(305,32,320,390),76.69f,1.44f,.55f,.67f,false,true);
  Prop(master+"/CameraItem_Placeholder","ConceptR2_Camera",new Rect(860,60,390,250),78.06f,1.43f,.5f,.32f);
  foreach(var paper in new[]{("ReturnFileFragment2",new Rect(75,275,765,340),349.8f,.70f),("ReturnCameraGentlemanEcho",new Rect(930,260,540,360),351.7f,.50f)})
  {
   var root=F(living+"/"+paper.Item1);var original=root.GetComponent<SpriteRenderer>();
   var output=Cut(root,"ConceptR2_TablePaper",Sprite("Papers"),paper.Item2,paper.Item3,1.14f,paper.Item4,.16f,-2);
   var link=Ensure<RendererVisibilityLink>(output.gameObject);Ref(link,"source",original);Ref(link,"visual",output);
   var retired=Ensure<RetiredArtVisibility>(output.gameObject);Refs(retired,"renderers",new[]{original});
   Highlight(root.GetComponent<Scene02ReturnInteractable>(),output,false);
  }
  var fogRoot=F(f1+"/DarkFog_Placeholder");
  for(int i=0;i<2;i++)
  {
   var fog=Cut(fogRoot,"ConceptR2_RightMist_"+i,Sprite("Fog"),new Rect(0,0,1536,1024),24.7f+i*.4f,-.25f+i*.15f,10.0f,6.8f,-8+i);
   fog.color=new Color(.27f,.25f,.34f,i==0?.98f:.6f);
   fog.flipY=i==1;var motion=Ensure<DrawnMistMotion>(fog.gameObject);motion.phase=i*2;motion.drift=new Vector2(.11f,.05f);
  }
  // Respect every original anomaly's visibility and recovery alpha, without showing old rectangles.
  foreach(var t in F(corr).GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("CorridorWarp_")||t.name.StartsWith("ThreeDoor_Anomaly_")).ToArray())
  {
   var original=t.GetComponent<SpriteRenderer>();if(!original)continue;
   var r=Cut(t,"ConceptR2_InkAnomaly",Sprite("Fog"),new Rect(0,0,1536,1024),t.position.x,30.45f,4.4f,4.1f,-20);
   r.flipY=true;r.color=new Color(.24f,.16f,.31f,.42f);var link=Ensure<SpriteVisibilityAlphaLink>(r.gameObject);Ref(link,"source",original);Ref(link,"visual",r);
   var motion=Ensure<DrawnMistMotion>(r.gameObject);motion.opacitySource=original;motion.opacity=.33f;motion.phase=t.position.x;motion.drift=new Vector2(.05f,.05f);
  }
  var camera=All().Select(t=>t.GetComponent<Camera>()).First(c=>c&&c.CompareTag("MainCamera"));
  var frame=Ensure<RoomPresentationFraming>(camera.gameObject);frame.letterbox=.1f;frame.centreAboveFloor=1.55f;
  frame.rooms=new[]{new RoomPresentationFraming.Room{area=new Rect(-40,-4,85,9.8f),floorY=.5f,size=2.8f},new RoomPresentationFraming.Room{area=new Rect(-40,5.8f,85,15),floorY=6.53f,size=2.8f},new RoomPresentationFraming.Room{area=new Rect(45,-4,55,19),floorY=.5f,size=2.8f},new RoomPresentationFraming.Room{area=new Rect(105,-4,55,19),floorY=.5f,size=2.8f},new RoomPresentationFraming.Room{area=new Rect(320,-4,65,19),floorY=.5f,size=2.8f},new RoomPresentationFraming.Room{area=new Rect(165,22,150,26),floorY=30.53f,size=2.8f}};
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());ArtRebuildEditor.Dump();Contract();
 }
}
