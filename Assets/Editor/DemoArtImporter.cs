using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DemoArtImporter
{
    const string Work="C:/Users/31975/Documents/Codex/2026-09-22/demo-demo-gdd-gdd-demo/work/2026-09-23-integration/";
    [Serializable] public class Region { public string key,alias;public int x,y,w,h; }
    [Serializable] public class Source { public string id,path; public int width,height; public Region[] regions; }
    [Serializable] public class Binding { public string path,key,domain,state,field; }
    [Serializable] public class Alias { public string key,target; }
    [Serializable] public class Plan { public Source[] sources; public Binding[] bindings; public Alias[] aliases; }
    public static void Run()
    { Import(true); }
    public static void Import(bool exit)
    {
        try{
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Work+"import_plan.json"));
            Directory.CreateDirectory("Assets/Resources");
            const string catalogPath="Assets/Resources/DemoArtCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>(catalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<DemoArtCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            var existing=new Dictionary<string,Sprite>();foreach(var o in AssetDatabase.LoadAllAssetsAtPath(catalogPath))if(o is Sprite s)existing[s.name]=s;
            var entries=new List<DemoArtCatalog.Entry>();var sprites=new Dictionary<string,Sprite>();
            foreach(var source in plan.sources){
                var importer=(TextureImporter)AssetImporter.GetAtPath(source.path);
                if(!importer){AssetDatabase.ImportAsset(source.path,ImportAssetOptions.ForceSynchronousImport);importer=(TextureImporter)AssetImporter.GetAtPath(source.path);}
                var before=new TextureImporterSettings();importer.ReadTextureSettings(before);
                bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||importer.maxTextureSize!=8192||importer.spritePixelsPerUnit!=256||importer.mipmapEnabled||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.filterMode!=FilterMode.Bilinear||importer.wrapMode!=TextureWrapMode.Clamp||before.spriteMeshType!=SpriteMeshType.FullRect||before.spriteGenerateFallbackPhysicsShape;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.maxTextureSize=8192;importer.spritePixelsPerUnit=256;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);if(changed)importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source.path);
                if(texture.width!=source.width||texture.height!=source.height)throw new Exception("Unexpected texture resize: "+source.id);
                foreach(var r in source.regions){
                    var rect=P01SpriteRegionCorrections.Correct(r.key,new Rect(r.x,r.y,r.w,r.h));
                    if(r.w<=0||r.h<=0||r.x<0||r.y<0||rect.xMax>texture.width||rect.yMax>texture.height)throw new Exception("Invalid sprite region: "+r.key);
                    if(!existing.TryGetValue(r.key,out var sprite)||sprite.texture!=texture||sprite.rect!=rect){
                        if(sprite)UnityEngine.Object.DestroyImmediate(sprite,true);
                        sprite=Sprite.Create(texture,rect,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect,Vector4.zero,false);sprite.name=r.key;AssetDatabase.AddObjectToAsset(sprite,catalog);
                    }
                    entries.Add(new DemoArtCatalog.Entry{key=r.key,sprite=sprite});sprites[r.key]=sprite;
                    if(!string.IsNullOrEmpty(r.alias)){entries.Add(new DemoArtCatalog.Entry{key=r.alias,sprite=sprite});sprites[r.alias]=sprite;}
                }
            }
            if(plan.aliases!=null)foreach(var a in plan.aliases){if(!sprites.TryGetValue(a.target,out var sprite))throw new Exception("Missing alias target "+a.target);entries.Add(new DemoArtCatalog.Entry{key=a.key,sprite=sprite});}
            catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);
            foreach(var binding in plan.bindings){
                if(binding.state!="default"&&!binding.domain.StartsWith("Outfit"))continue;
                var data=AssetDatabase.LoadMainAssetAtPath(binding.path);if(!data||!sprites.TryGetValue(binding.key,out var sprite))continue;
                var so=new SerializedObject(data);var field=so.FindProperty(!string.IsNullOrEmpty(binding.field)?binding.field:binding.domain.StartsWith("Outfit")?"preview":"icon");if(field==null)continue;
                field.objectReferenceValue=sprite;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Work+"import_result.json","{\"sources\":"+plan.sources.Length+",\"regions\":"+entries.Count+",\"completed\":true}");
            Debug.Log("DEMO_ART_IMPORT_COMPLETE "+entries.Count);if(exit&&Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);File.WriteAllText(Work+"import_error.txt",e.ToString());if(!exit)throw;if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
}
