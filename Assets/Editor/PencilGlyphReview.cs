using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

// Focused regression of the actual close-up, using its original font and mesh.
public static class PencilGlyphReview
{
    static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var checks=new List<string>();
        bool had=PlayerPrefs.HasKey(GameLocalization.PreferenceKey);
        string saved=PlayerPrefs.GetString(GameLocalization.PreferenceKey,"");
        var prior=GameLocalization.Current;
        int[] firstTotals=null;
        GameObject root=null;
        Action<bool,string> check=(ok,name)=>{checks.Add((ok?"PASS ":"FAIL ")+name);};
        try {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ChineseMain SDF.asset");
            if(!font)throw new Exception("Missing existing font");
            foreach(var language in new[]{GameLanguage.English,GameLanguage.SimplifiedChinese}) {
                GameLocalization.SetLanguage(language);
                root=new GameObject("Unsaved original pencil localization check");
                var controls=root.AddComponent<GameControlManager>();
                var desk=root.AddComponent<PencilDeskCloseup>();desk.Initialize(font,controls);
                check(desk.Open(true,false),language+" own desk opens");
                var own=(TMP_Text)typeof(PencilDeskCloseup).GetField("ownName",Hidden).GetValue(desk);
                Canvas.ForceUpdateCanvases();own.ForceMeshUpdate(true,true);
                check(own.textInfo.lineCount==1,language+" own name fits on one line");
                desk.Close();
                check(desk.Open(false,false)&&!desk.CanRub,language+" no-pencil desk stays inspectable without revealing name");
                desk.Close();GameLocalization.BindLoadedText();
                check(desk.Open(false,true)&&desk.CanRub,language+" original pencil opens writable close-up");
                var glyph=(TMP_Text)typeof(PencilDeskCloseup).GetField("glyphTemplate",Hidden).GetValue(desk);
                check(glyph.textInfo.characterCount==2&&glyph.textInfo.characterInfo[0].textElement.unicode=='郑'&&glyph.textInfo.characterInfo[1].textElement.unicode=='涛',language+" mask uses authored engraving glyphs");
                var totals=((int[])typeof(PencilDeskCloseup).GetField("totals",Hidden).GetValue(desk)).ToArray();
                check(totals.Length==2&&totals.All(t=>t>0),language+" both original coverage regions contain ink pixels");
                if(firstTotals==null)firstTotals=totals;else check(firstTotals.SequenceEqual(totals),"language change preserves exact two coverage regions");
                GameLocalization.SetLanguage(language==GameLanguage.English?GameLanguage.SimplifiedChinese:GameLanguage.English);
                check(desk.CanRub&&desk.FirstCharacterCoverage==0&&desk.SecondCharacterCoverage==0,"switching language does not complete or reset the puzzle");
                desk.Close();UnityEngine.Object.DestroyImmediate(root);root=null;
            }
        } finally {
            if(root)UnityEngine.Object.DestroyImmediate(root);
            GameLocalization.SetLanguage(prior);
            if(had)PlayerPrefs.SetString(GameLocalization.PreferenceKey,saved);else PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);
            PlayerPrefs.Save();
            File.WriteAllLines(ProgressionReview.Work+"Results/pencil-language-checks.txt",checks);
        }
        if(checks.Any(c=>c.StartsWith("FAIL")))throw new Exception("Pencil language regression failed");
    }
}
