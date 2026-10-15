using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Reviewed corrections are shared with the production importer so a future
// rebuild cannot restore the old near/far or sleeve-direction mistakes.
public static class P02VisualAssetRules
{
    public const float CourtyardStairHeight = 3.1f;
    public static void Apply(P02RigDefinition rig)
    {
        if(rig.viewKind=="profile" && rig.direction=="left")
        {
            var near=Array.Find(rig.parts,p=>p.joint=="near_hand");
            var far=Array.Find(rig.parts,p=>p.joint=="far_hand");
            // All seven authored left profile atlases labelled the palm as
            // near_hand. The anatomical left hand is the visible outer/back
            // hand in this view. Keep each sprite's own wrist pivot and art angle.
            if(near!=null && far!=null && near.sprite.name=="near_hand" && far.sprite.name=="far_hand")
            {
                var sprite=near.sprite;near.sprite=far.sprite;far.sprite=sprite;
                sprite=near.runSprite;near.runSprite=far.runSprite;far.runSprite=sprite;
                float value=near.paintedHeight;near.paintedHeight=far.paintedHeight;far.paintedHeight=value;
                value=near.artworkAngle;near.artworkAngle=far.artworkAngle;far.artworkAngle=value;
                value=near.runPaintedHeight;near.runPaintedHeight=far.runPaintedHeight;far.runPaintedHeight=value;
                value=near.runArtworkAngle;near.runArtworkAngle=far.runArtworkAngle;far.runArtworkAngle=value;
            }
            if(rig.actorKey=="adult_default" || rig.actorKey=="qt_adult")
                foreach(var part in rig.parts.Where(p=>p.joint=="near_arm"||p.joint=="far_arm"))part.mirrorLimbArtwork=true;
            // These two back-hand drawings retained the right-hand thumb.
            // Reflect only the rigid hand around its retained wrist pivot.
            if(rig.actorKey=="child"||rig.actorKey=="boy_child")near.mirrorArtworkX=true;
        }
        if(rig.actorKey=="qt_adult")
        {
            // The imported sleeve cap extended above the painted shoulder.
            // Lower the joint into the existing jacket; retain the coat, face,
            // limb lengths, walking speed, and all authored story transforms.
            rig.shoulderOffset=new Vector2(rig.shoulderOffset.x,Mathf.Min(rig.shoulderOffset.y,.43f));
            rig.idlePelvisHeight=.932f;
        }
    }
    public static void ApplyInstalled()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode is required.");
        TrimInstalledHead();
        foreach(var entry in P02MotionLibrary.Current.entries.Where(e=>e.rig.actorKey=="qt_adult"||e.direction=="left"&&e.rig.viewKind=="profile"))
        {Apply(entry.rig);EditorUtility.SetDirty(entry.rig);}
        AssetDatabase.SaveAssets();
        const string scenePath="Assets/Scenes/Scene02_Courtyard.unity";
        var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        var stairs=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Single(r=>r.name=="P03_CompactStairs");
        float bottom=stairs.bounds.min.y;
        float scale=CourtyardStairHeight/stairs.bounds.size.y;
        stairs.transform.localScale=new Vector3(stairs.transform.localScale.x*scale,stairs.transform.localScale.y*scale,stairs.transform.localScale.z);
        stairs.transform.position+=Vector3.up*(bottom-stairs.bounds.min.y);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(VisualFeedbackReview.Work+"asset-corrections.txt","Reviewed profile hand mapping; measured sleeve orientation; Qiu Tian shoulder/standing pose; native-aspect stairs 3.1 world units with original ground contact and interaction zone retained.");
    }
    const string HeadSource="Assets/Art/P02Motion/Production/qt_adult_right_head_source_v01.png";
    static void TrimInstalledHead()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(HeadSource);
        var factory=new SpriteDataProviderFactories();factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var rect=provider.GetSpriteRects().Single(r=>r.name=="head");
        var mesh=provider.GetDataProvider<ISpriteMeshDataProvider>();
        var vertices=mesh.GetVertices(rect.spriteID);var indices=mesh.GetIndices(rect.spriteID).ToList();
        FilterHeadMesh(HeadSource,"head",ref vertices,ref indices);
        mesh.SetVertices(rect.spriteID,vertices);mesh.SetIndices(rect.spriteID,indices.ToArray());
        var edges=new System.Collections.Generic.HashSet<Vector2Int>();
        for(int i=0;i<indices.Count;i+=3)for(int k=0;k<3;k++){int a=indices[i+k],b=indices[i+(k+1)%3];edges.Add(new Vector2Int(Math.Min(a,b),Math.Max(a,b)));}
        mesh.SetEdges(rect.spriteID,edges.ToArray());provider.Apply();importer.SaveAndReimport();
    }
    public static void FilterHeadMesh(string source,string name,ref Vertex2DMetaData[] vertices,ref System.Collections.Generic.List<int> indices)
    {
        if(source!=HeadSource||name!="head")return;
        // The head photograph retained an old coat collar. Clip only that
        // disconnected clothing along its actual neckline; preserve all source
        // pixels, face/hair geometry, pivots, bone identities, and the skin neck.
        float Side(Vertex2DMetaData v)=>v.position.y+.75f*v.position.x-85.5f;
        var input=vertices;
        if(!indices.Any(i=>Side(input[i])<-.001f))return;
        var output=new System.Collections.Generic.List<Vertex2DMetaData>();
        var triangles=new System.Collections.Generic.List<int>();
        for(int t=0;t<indices.Count;t+=3){
            var polygon=new System.Collections.Generic.List<Vertex2DMetaData>();
            for(int k=0;k<3;k++){
                var a=input[indices[t+k]];var b=input[indices[t+(k+1)%3]];float sa=Side(a),sb=Side(b);
                if(sa>=0)polygon.Add(a);
                if((sa>=0)!=(sb>=0)){
                    float u=sa/(sa-sb);float w=Mathf.Lerp(a.boneWeight.weight1,b.boneWeight.weight1,u);
                    polygon.Add(new Vertex2DMetaData{position=Vector2.Lerp(a.position,b.position,u),boneWeight=new BoneWeight{boneIndex0=0,weight0=1-w,boneIndex1=1,weight1=w}});
                }
            }
            int first=output.Count;output.AddRange(polygon);
            for(int k=1;k<polygon.Count-1;k++)triangles.AddRange(new[]{first,first+k,first+k+1});
        }
        vertices=output.ToArray();indices=triangles;
    }
}
