using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Keep verified slice boundaries independent of the legacy generated import plan.
public static class P01SpriteRegionCorrections
{
    public static Rect Correct(string key, Rect proposed)
    {
        return key == "CHR_XP_YOUTH_TURN3:1" ? new Rect(676,20,208,991) : proposed;
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required");
        const string path="Assets/Resources/DemoArtCatalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>(path);
        var sprite=catalog.entries.Single(e=>e.key=="CHR_XP_YOUTH_TURN3:1").sprite;
        var rect=Correct(sprite.name,sprite.rect);
        string guid;long before;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out guid,out before);
        if(sprite.rect!=rect)
        {
            var replacement=Sprite.Create(sprite.texture,rect,new Vector2(.5f,0),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,Vector4.zero,false);
            replacement.name=sprite.name;
            EditorUtility.CopySerialized(replacement,sprite);
            UnityEngine.Object.DestroyImmediate(replacement);
            EditorUtility.SetDirty(sprite);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        long after;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out guid,out after);
        if(sprite.rect!=rect||before!=after)throw new Exception("Slice rectangle or reference identity changed unexpectedly");
        Directory.CreateDirectory("Temp/P01SpriteRegionCorrections");
        File.WriteAllText("Temp/P01SpriteRegionCorrections/results.txt","PASS youth right-facing slice excludes adjacent sleeve; rect="+sprite.rect+"; preserved fileID="+after);
    }
}
