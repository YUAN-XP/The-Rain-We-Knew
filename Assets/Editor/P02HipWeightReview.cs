using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Targeted, reversible weight edit. Source pixels, sprite IDs and bones stay intact.
public static class P02HipWeightReview
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        const string work="D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
        var manifest=JsonUtility.FromJson<P02ProductionFactory.Manifest>(File.ReadAllText(work+"production_skin_manifest.json"));
        foreach(var skin in manifest.skins.Where(x=>x.actor=="child"&&(x.direction=="up_left"||x.direction=="up_right")))
        foreach(var source in skin.sources.Where(x=>x.parts.Any(p=>p.name.EndsWith("_leg"))))
        {
            string backup=Path.GetFullPath("Documentation/FirstLargeTest_20261001/P02ProductionVersions/PV17_Before_HipWeights/"+Path.GetFileName(source.path)+".meta");
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(source.path+".meta",backup);
            var importer=AssetImporter.GetAtPath(source.path) as TextureImporter;
            var factory=new SpriteDataProviderFactories();factory.Init();
            var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var mesh=provider.GetDataProvider<ISpriteMeshDataProvider>();
            foreach(var part in source.parts.Where(x=>x.name.EndsWith("_leg")))
            {
                var rect=provider.GetSpriteRects().Single(x=>x.name==part.name);
                var vertices=mesh.GetVertices(rect.spriteID);
                var indices=mesh.GetIndices(rect.spriteID);
                var edges=mesh.GetEdges(rect.spriteID);
                for(int i=0;i<vertices.Length;i++)
                {
                    float yy=vertices[i].position.y/part.height;
                    float knee=Mathf.SmoothStep(0,1,Mathf.Clamp01((part.tipY+.07f-yy)/.14f));
                    // Distribute bending across the painted hip rather than
                    // pinning the entire upper seat and pinching below it.
                    float waist=Mathf.SmoothStep(0,1,Mathf.InverseLerp(part.hipY-.10f,part.pivotY,yy));
                    vertices[i].boneWeight=new BoneWeight{boneIndex0=0,weight0=waist,boneIndex1=1,weight1=(1-waist)*(1-knee),boneIndex2=2,weight2=(1-waist)*knee};
                }
                mesh.SetVertices(rect.spriteID,vertices);
                mesh.SetIndices(rect.spriteID,indices);
                mesh.SetEdges(rect.spriteID,edges);
            }
            provider.Apply();importer.SaveAndReimport();
        }
    }
    public static void ApplyPinnedSeat() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode only");
        const string work="D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
        var manifest=JsonUtility.FromJson<P02ProductionFactory.Manifest>(File.ReadAllText(work+"production_skin_manifest.json"));
        foreach(var skin in manifest.skins.Where(x=>x.actor=="child"&&(x.direction=="up_left"||x.direction=="up_right")))
        foreach(var source in skin.sources.Where(x=>x.parts.Any(p=>p.name.EndsWith("_leg")))) {
            string backup="D:/游戏设计/Codex工作区/维护记录/PV18_Before_Seat/"+Path.GetFileName(source.path)+".meta";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(source.path+".meta",backup);
            var importer=AssetImporter.GetAtPath(source.path) as TextureImporter;
            var factory=new SpriteDataProviderFactories();factory.Init();
            var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var mesh=provider.GetDataProvider<ISpriteMeshDataProvider>();
            foreach(var part in source.parts.Where(x=>x.name.EndsWith("_leg"))) {
                var rect=provider.GetSpriteRects().Single(x=>x.name==part.name);
                var vertices=mesh.GetVertices(rect.spriteID);var indices=mesh.GetIndices(rect.spriteID);var edges=mesh.GetEdges(rect.spriteID);
                for(int i=0;i<vertices.Length;i++) {
                    float yy=vertices[i].position.y/part.height;
                    float knee=Mathf.SmoothStep(0,1,Mathf.Clamp01((part.tipY+.07f-yy)/.14f));
                    // Shorts pocket/seat belongs to the pelvis. Only the lower
                    // hem blends into the thigh; otherwise depth compression
                    // rotates the painted seat into a protruding bulge.
                    float waist=Mathf.SmoothStep(0,1,Mathf.InverseLerp(part.tipY+.02f,part.tipY+.22f,yy));
                    vertices[i].boneWeight=new BoneWeight{boneIndex0=0,weight0=waist,boneIndex1=1,weight1=(1-waist)*(1-knee),boneIndex2=2,weight2=(1-waist)*knee};
                }
                mesh.SetVertices(rect.spriteID,vertices);mesh.SetIndices(rect.spriteID,indices);mesh.SetEdges(rect.spriteID,edges);
            }
            provider.Apply();importer.SaveAndReimport();
        }
    }

}
