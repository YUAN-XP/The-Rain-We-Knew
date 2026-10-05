#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class P07Audit {
 public static void Export(){
  string w=@"D:\游戏设计\Codex工作区\work\2026-10-05-P07\checks";Directory.CreateDirectory(w);
  var cat=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  File.WriteAllLines(Path.Combine(w,"art_paths.txt"),cat.entries.Where(e=>e.sprite&&(e.key.Contains("S01")||e.key.Contains("BASE")||e.key.Contains("CLOSEUP")||e.key.Contains("ZHANG")||e.key.Contains("XIANG")||e.key.Contains("GUIDE"))).Select(e=>e.key+" | "+AssetDatabase.GetAssetPath(e.sprite.texture)+" | "+e.sprite.rect));
  File.WriteAllText(Path.Combine(w,"editor_state.txt"),"Playing="+EditorApplication.isPlaying+"; scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"; screen="+Screen.width+"x"+Screen.height+"; font="+TMPro.TMP_Settings.defaultFontAsset);
  Debug.Log("P07 source audit exported");
 }
}
#endif
