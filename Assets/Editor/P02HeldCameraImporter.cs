using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Imports only the approved handheld-camera variant. Inventory and tabletop art stay separate.
public static class P02HeldCameraImporter
{
    const string Path="Assets/Resources/P02HeldCamera/hand_camera_PV18.png";
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Path);
        if(!importer)throw new FileNotFoundException(Path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=100;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        var factories=new SpriteDataProviderFactories();factories.Init();
        var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var existing=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
        var rects=new[]{
            new SpriteRect{name="camera_left",rect=new Rect(108,233,666,420),pivot=new Vector2(.76f,.50f),alignment=SpriteAlignment.Custom},
            new SpriteRect{name="camera_right",rect=new Rect(1001,235,665,418),pivot=new Vector2(.24f,.50f),alignment=SpriteAlignment.Custom}
        };
        foreach(var rect in rects)rect.spriteID=existing.TryGetValue(rect.name,out var id)?id:GUID.Generate();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Sprite>().ToArray();
        if(sprites.Length!=2)throw new InvalidOperationException("Both handheld views required");
        File.WriteAllText(P02ProductionInputReview.Work+"held_camera_import.txt",DateTime.Now.ToString("O")+" Two native sprites; hand-grip pivots; no inventory/tabletop reference replaced.");
    }
}
