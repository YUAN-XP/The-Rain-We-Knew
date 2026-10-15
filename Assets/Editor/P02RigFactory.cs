using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

/// <summary>Imports only P02 candidates, preserving all approved source GUIDs and scene layouts.</summary>
public static class P02RigFactory
{
    const string Root = "Assets/Art/P02Motion";
    const string Work = "D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
    [Serializable] public sealed class Source { public string path; public int width, height; public Part[] parts; }
    [Serializable] public sealed class Part
    {
        public string name;
        public int x, y, width, height;
        public float pivotX, pivotY, paintedHeight, tipY, bend, artworkAngle, waistToHip = .115f;
        public int order;
        public bool limb;
        public bool calibratedLimb;
        public bool garment,nearOnLeft;
        public string skinDirection;
        public float hipX, hipY, jointX, jointY, endX, endY;
        public float bindUpperLength, bindLowerLength;
        // Sprite-local pixel rectangles, measured from the bottom left.
        // Import topology excludes neighbouring atlas islands without editing
        // source pixels or changing the retained artwork's bind coordinates.
        public MeshCutout[] meshCutouts;
    }
    [Serializable] public sealed class MeshCutout {
        public float x,y,width,height;
        public Rect Bounds=>new Rect(x,y,width,height);
    }
    [Serializable] sealed class Manifest { public string identity; public Source[] sources; }

    [MenuItem("ProjectMystery/P02/Build default right sample")]
    public static void BuildSample()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Sample build requires the saved editor scene in Edit Mode.");
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Work + "sample_source_manifest.json"));
        var bindings = new List<P02RigDefinition.P02PartBinding>();
        var sprites=new Dictionary<string,Sprite>();
        foreach(var source in manifest.sources){Import(source);foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(source.path).OfType<Sprite>())sprites.Add(sprite.name,sprite);}
        foreach (var source in manifest.sources)
        {
            foreach (var p in source.parts)
            {
                if(p.name.EndsWith("_run"))continue;
                if (!sprites.TryGetValue(p.name, out var sprite))
                    throw new InvalidOperationException("Missing imported part " + p.name);
                int expectedBones=p.name.EndsWith("_leg")?3:2;
                if (sprite.GetBones().Length != expectedBones || sprite.GetBindPoses().Length != expectedBones ||
                    sprite.GetVertexAttribute<BoneWeight>(UnityEngine.Rendering.VertexAttribute.BlendWeight).Length < 20)
                    throw new InvalidOperationException("Native skin data missing " + p.name);
                var variant=manifest.sources.SelectMany(s=>s.parts).FirstOrDefault(v=>v.name==p.name+"_run");
                bindings.Add(new P02RigDefinition.P02PartBinding { joint = p.name, sprite = sprite,
                    runSprite=variant!=null?sprites[variant.name]:null,runPaintedHeight=variant!=null?variant.paintedHeight:0,
                    paintedHeight = p.paintedHeight, sortingOffset = p.order, bendInfluence = p.bend,
                    articulatedLimb = p.limb,stabilizedWaist=p.name.EndsWith("_leg"),
                    paintedSole = p.name.EndsWith("_shoe") ? ReadPaintedSole(sprite,p.paintedHeight) : null });
            }
        }
        string definitionPath = Root + "/XP_Default_Right.asset";
        var definition = AssetDatabase.LoadAssetAtPath<P02RigDefinition>(definitionPath);
        if (!definition)
        {
            definition = ScriptableObject.CreateInstance<P02RigDefinition>();
            AssetDatabase.CreateAsset(definition, definitionPath);
        }
        definition.identity = manifest.identity;
        definition.walkingStride=1.2f;definition.runningStride=1.35f;
        definition.sampleWalkSpeed=1.35f;definition.sampleRunSpeed=1.35f*1.55f;
        definition.walkingLean=1.8f;definition.runningLean=13f;
        definition.waistToHip=.115f;definition.thighLength=.29f;definition.shinLength=.405f;
        definition.parts = bindings.ToArray();
        EditorUtility.SetDirty(definition);
        var actor = new GameObject("P02_XP_Default_Right");
        try
        {
            var rig = actor.AddComponent<P02CharacterRig>();
            rig.definition = definition;
            rig.sampleMode = true;
            rig.Build();
            PrefabUtility.SaveAsPrefabAsset(actor, Root + "/P02_XP_Default_Right.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(actor); }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Work + "native_skin_import.json", JsonUtility.ToJson(new ImportReport {
            timestamp = DateTime.Now.ToString("O"), identity = manifest.identity, partCount = bindings.Count,
            verticesPerPart = 297, nativeBonesPerPart = 0,nativeBonesTotal=bindings.Sum(p=>p.stabilizedWaist?3:2),
            scope = "isolated right-side sample; 2 bones per rigid part/arm, 3 per continuous leg with pelvis-stabilized waist; no production scene changed"
        }, true));
    }

    [Serializable] sealed class ImportReport
    {
        public string timestamp, identity, scope;
        public int partCount, verticesPerPart, nativeBonesPerPart,nativeBonesTotal;
    }

    public static Vector2[] ReadPaintedSole(Sprite sprite,float height)
    {
        var texture=sprite.texture;
        var pixels=texture.GetPixels32();
        var rect=sprite.rect;
        int ox=Mathf.RoundToInt(rect.x),oy=Mathf.RoundToInt(rect.y);
        int width=Mathf.RoundToInt(rect.width),rows=Mathf.RoundToInt(rect.height);
        float unitsPerPixel=height/rows;
        var outline=new List<Vector2>();
        // Lower pixel boundary of the first opaque pixel in each column.
        // Reading the source alpha does not alter or resample its artwork.
        for(int x=0;x<width;x++)for(int y=0;y<rows;y++)
        {
            if(pixels[(oy+y)*texture.width+ox+x].a<128)continue;
            outline.Add(new Vector2(x+.5f-sprite.pivot.x,y-sprite.pivot.y)*unitsPerPixel);
            break;
        }
        if(outline.Count<20)throw new InvalidOperationException("Painted shoe contour missing "+sprite.name);
        return outline.ToArray();
    }

    public static void Import(Source source)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(source.path);
        if (!importer) throw new FileNotFoundException(source.path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = true;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects().ToDictionary(x => x.name, x => x.spriteID);
        var rects = source.parts.Select(p => new SpriteRect {
            name = p.name, spriteID = old.TryGetValue(p.name, out var id) ? id : GUID.Generate(),
            rect = new Rect(p.x, source.height - p.y - p.height, p.width, p.height),
            pivot = new Vector2(p.pivotX, p.pivotY), alignment = SpriteAlignment.Custom
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
        var mesh = provider.GetDataProvider<ISpriteMeshDataProvider>();
        var bones = provider.GetDataProvider<ISpriteBoneDataProvider>();
        foreach (var rect in rects)
        {
            var part = source.parts.First(x => x.name == rect.name);
            var origin = Vector2.Scale(rect.rect.size, rect.pivot);
            var end = part.calibratedLimb ? new Vector2(part.jointX*part.width,part.jointY*part.height) : new Vector2(origin.x, part.tipY * part.height);
            bool waist=part.name.EndsWith("_leg");
            // The anatomical hip is below the trouser waistband. Preserve the
            // painted waist/knee/ankle positions; relocate only the thigh axis.
            float hipY=part.pivotY-part.waistToHip/part.paintedHeight;
            var hip=part.calibratedLimb || part.garment ? new Vector2(part.hipX*part.width,part.hipY*part.height) : new Vector2(origin.x,hipY*part.height);
            var distal = new Vector2(part.endX*part.width,part.endY*part.height);
            float upperAngle=part.calibratedLimb?DownAngle(end-(waist?hip:origin)):0;
            float lowerAngle=part.calibratedLimb?DownAngle(distal-end):0;
            var upperRotation=Quaternion.Euler(0,0,upperAngle);
            bones.SetBones(rect.spriteID, part.garment?new List<SpriteBone>{
                new SpriteBone{name=part.name+"_waist",guid=GUID.Generate().ToString(),parentId=-1,position=origin,rotation=Quaternion.identity,length=part.height*.1f},
                new SpriteBone{name=part.name+"_near_thigh",guid=GUID.Generate().ToString(),parentId=0,position=hip-origin,rotation=Quaternion.identity,length=part.height*.7f},
                new SpriteBone{name=part.name+"_far_thigh",guid=GUID.Generate().ToString(),parentId=0,position=distal-origin,rotation=Quaternion.identity,length=part.height*.7f}
            }:waist?new List<SpriteBone>{
                new SpriteBone{name=part.name+"_waist",guid=GUID.Generate().ToString(),parentId=-1,
                    position=origin,rotation=Quaternion.identity,length=part.height*.08f},
                new SpriteBone{name=part.name+"_thigh",guid=GUID.Generate().ToString(),parentId=0,
                    position=hip-origin,rotation=upperRotation,length=(end-hip).magnitude},
                new SpriteBone{name=part.name+"_flex",guid=GUID.Generate().ToString(),parentId=1,
                    position=Quaternion.Inverse(upperRotation)*(end-hip),rotation=Quaternion.Euler(0,0,lowerAngle-upperAngle),length=part.calibratedLimb?(distal-end).magnitude:part.height*.15f}
            }:new List<SpriteBone> {
                new SpriteBone { name = part.name + "_root", guid = GUID.Generate().ToString(), parentId = -1,
                    position = origin, rotation = upperRotation, length = (end-origin).magnitude },
                new SpriteBone { name = part.name + "_flex", guid = GUID.Generate().ToString(), parentId = 0,
                    position = Quaternion.Inverse(upperRotation)*(end-origin), rotation = Quaternion.Euler(0,0,lowerAngle-upperAngle), length = part.calibratedLimb?(distal-end).magnitude:part.height * .15f }
            });
            var xs=Enumerable.Range(0,part.garment?25:9).Select(v=>v/(part.garment?24f:8f)*part.width).ToList();
            var ys=Enumerable.Range(0,part.garment?41:33).Select(v=>v/(part.garment?40f:32f)*part.height).ToList();
            var cutouts=(part.meshCutouts??Array.Empty<MeshCutout>()).Select(v=>v.Bounds).ToArray();
            if(cutouts.Any(c=>c.width<=0||c.height<=0||c.xMin<0||c.yMin<0||c.xMax>part.width||c.yMax>part.height))
                throw new InvalidOperationException("Invalid measured mesh exclusion: "+source.path+"/"+part.name);
            foreach(var cut in cutouts){
                xs.Add(Mathf.Clamp(cut.xMin,0,part.width));xs.Add(Mathf.Clamp(cut.xMax,0,part.width));
                ys.Add(Mathf.Clamp(cut.yMin,0,part.height));ys.Add(Mathf.Clamp(cut.yMax,0,part.height));
            }
            xs=xs.Distinct().OrderBy(v=>v).ToList();ys=ys.Distinct().OrderBy(v=>v).ToList();
            int cols=xs.Count-1,rows=ys.Count-1;
            var vertices = new Vertex2DMetaData[(cols+1)*(rows+1)];
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= cols; x++)
                {
                    float yy = ys[y]/part.height;
                    // Smooth two-bone transition; no rigid whole-person bitmap deformation.
                    float influence = part.limb ? Mathf.Clamp01((part.tipY + .07f - yy) / .14f) :
                        Mathf.Clamp01((yy - part.pivotY) / (part.tipY-part.pivotY));
                    influence = Mathf.SmoothStep(0, 1, influence);
                    float measuredHipY=part.calibratedLimb?part.hipY:hipY;
                    float waistWeight=waist?Mathf.SmoothStep(0,1,Mathf.Clamp01((yy-(measuredHipY-.10f))/.10f)):0;
                    var weight=waist?new BoneWeight{boneIndex0=0,weight0=waistWeight,
                        boneIndex1=1,weight1=(1-waistWeight)*(1-influence),boneIndex2=2,weight2=(1-waistWeight)*influence}:
                        new BoneWeight{boneIndex0=0,weight0=1-influence,boneIndex1=1,weight1=influence};
                    if(part.garment){
                        float waistBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.82f,yy));
                        float right=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.43f,.57f,xs[x]/part.width));
                        float near=part.nearOnLeft?1-right:right;
                        weight=new BoneWeight{boneIndex0=0,weight0=waistBlend,
                            boneIndex1=1,weight1=(1-waistBlend)*near,boneIndex2=2,weight2=(1-waistBlend)*(1-near)};
                    }
                    vertices[y*(cols+1)+x] = new Vertex2DMetaData {
                        position = new Vector2(xs[x], ys[y]),
                        boneWeight = weight
                    };
                }
            var indices = new List<int>();
            for (int y=0; y<rows; y++) for (int x=0; x<cols; x++)
            {
                var center=new Vector2((xs[x]+xs[x+1])*.5f,(ys[y]+ys[y+1])*.5f);
                if(cutouts.Any(c=>c.Contains(center)))continue;
                int a=y*(cols+1)+x, b=a+1, c=a+cols+1, d=c+1;
                indices.AddRange(new[] { a,c,b,b,c,d });
            }
            P02VisualAssetRules.FilterHeadMesh(source.path,rect.name,ref vertices,ref indices);
            mesh.SetVertices(rect.spriteID, vertices);
            mesh.SetIndices(rect.spriteID, indices.ToArray());
            var edges = new HashSet<Vector2Int>();
            for(int i=0;i<indices.Count;i+=3)for(int e=0;e<3;e++){
                int a=indices[i+e],b=indices[i+(e+1)%3];edges.Add(new Vector2Int(Math.Min(a,b),Math.Max(a,b)));
            }
            mesh.SetEdges(rect.spriteID, edges.ToArray());
        }
        provider.Apply();
        importer.SaveAndReimport();
    }
    static float DownAngle(Vector2 axis)=>Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg+90;

    [MenuItem("ProjectMystery/P02/Open motion sample")]
    public static void OpenSample()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Opening sample requires saved scene in Edit Mode.");
        string path = "Assets/Scenes/Validation/P02MotionSample.unity";
        if(!AssetDatabase.IsValidFolder("Assets/Scenes/Validation"))
            AssetDatabase.CreateFolder("Assets/Scenes","Validation");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 1.55f;
        camera.transform.position = new Vector3(.1f,.9f,-10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.30f,.31f,.32f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/P02_XP_Default_Right.prefab");
        if (!prefab) throw new InvalidOperationException("Build sample first");
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        actor.GetComponent<P02CharacterRig>().sampleMode = true;
        actor.AddComponent<P02MotionSampleProbe>();
        var ground = new GameObject("Sample ground contact line").AddComponent<LineRenderer>();
        ground.positionCount=2;ground.SetPosition(0,new Vector3(-100,0,0));ground.SetPosition(1,new Vector3(100,0,0));
        ground.startWidth=.008f;ground.endWidth=.008f;
        ground.material=new Material(Shader.Find("Sprites/Default"));
        ground.startColor=new Color(.65f,.64f,.62f);ground.endColor=ground.startColor;
        ground.sortingOrder=-10;
        for(int i=-20;i<120;i++)
        {
            var tick=new GameObject("Ground distance reference "+i).AddComponent<LineRenderer>();
            tick.positionCount=2;tick.SetPosition(0,new Vector3(i*.5f,0,0));tick.SetPosition(1,new Vector3(i*.5f,.025f,0));
            tick.startWidth=.006f;tick.endWidth=.006f;tick.material=ground.material;
            tick.startColor=ground.startColor;tick.endColor=ground.endColor;tick.sortingOrder=-9;
        }
        if(!EditorSceneManager.SaveScene(scene,path))
            throw new InvalidOperationException("Could not save isolated motion sample");
        Selection.activeGameObject = actor;
    }

    public static void SaveOpenSample()
    {
        if(EditorApplication.isPlaying || !UnityEngine.Object.FindAnyObjectByType<P02MotionSampleProbe>())
            throw new InvalidOperationException("Only the isolated motion sample may be saved by this command");
        if(!AssetDatabase.IsValidFolder("Assets/Scenes/Validation"))
            AssetDatabase.CreateFolder("Assets/Scenes","Validation");
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Validation/P02MotionSample.unity"))
            throw new InvalidOperationException("Sample scene save failed");
    }

    public static void RunChecks()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Requires isolated sample Play Mode");
        var probe = UnityEngine.Object.FindAnyObjectByType<P02MotionSampleProbe>();
        if(!probe)throw new InvalidOperationException("Motion sample not open");
        string runDirectory = probe.PrepareRun();
        string moviePath = runDirectory + "continuous_motion.mp4";
        string slowPath = runDirectory + "continuous_motion_slow.mp4";
        var encoder = new MediaEncoder(moviePath, new VideoTrackAttributes {
            width=1920, height=1080, frameRate=new MediaRational(60), includeAlpha=false,bitRateMode=VideoBitrateMode.High
        });
        var slowEncoder = new MediaEncoder(slowPath, new VideoTrackAttributes {
            width=1920,height=1080,frameRate=new MediaRational(60),includeAlpha=false,bitRateMode=VideoBitrateMode.High
        });
        probe.StartChecks((texture, seconds) => {
            bool normal=encoder.AddFrame(texture,new MediaTime((long)(seconds*1000000),1000000,1));
            bool slow=slowEncoder.AddFrame(texture,new MediaTime((long)(seconds*4000000),1000000,1));
            return normal&&slow;
        }, () => {encoder.Dispose();slowEncoder.Dispose();}, moviePath, slowPath);
    }
}

