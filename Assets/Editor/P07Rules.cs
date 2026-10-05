using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class P07Rules {
 public static void Run(){
 var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
 var g=new GameObject("P07_InactiveRuleFixture"){hideFlags=HideFlags.DontSave};g.SetActive(false);
 var a=ScriptableObject.CreateInstance<ItemData>();var b=ScriptableObject.CreateInstance<ItemData>();var c=ScriptableObject.CreateInstance<ItemData>();var f=ScriptableObject.CreateInstance<FileData>();var r=ScriptableObject.CreateInstance<CombinationRecipe>();
 try{var history=g.AddComponent<AcquisitionHistory>();var inv=g.AddComponent<InventoryManager>();var files=g.AddComponent<FileArchiveManager>();var story=g.AddComponent<StoryStateManager>();var cm=g.AddComponent<CombinationManager>();var gain=g.AddComponent<P07AcquisitionPresentation>();
 a.itemId="P07_test_a";b.itemId="P07_test_b";c.itemId="P07_test_c";a.canCombine=b.canCombine=true;f.fileId="P07_test_file";
 r.materials=new[]{new CombinationRecipe.Material{item=a},new CombinationRecipe.Material{item=b}};r.prerequisites=new[]{StoryFlag.Scene01GateKeyCollected};r.completedFlag=StoryFlag.CameraDoorRevealed;r.resultItem=c;cm.Configure(new[]{r});
 var method=typeof(P07AcquisitionPresentation).GetMethod("Combinable",BindingFlags.Instance|BindingFlags.NonPublic);Func<bool,bool> query=isFile=>(bool)method.Invoke(gain,new object[]{isFile});
 inv.AddItem(a);check(!query(false),"missing material has no item-combination hint");inv.AddItem(b);check(!query(false),"unmet original prerequisite has no hint");story.Set(StoryFlag.Scene01GateKeyCollected);check(query(false)&&!query(true),"eligible item recipe yields item-only hint");
 var seen=history.Capture();check(query(false)&&history.Capture().SequenceEqual(seen)&&inv.HasItem(a)&&inv.HasItem(b),"hint query does not consume or notify");
 r.resultFile=f;r.resultItem=null;check(query(true)&&!query(false),"eligible file result yields file-only hint");files.AddFile(f);check(!query(true),"already-owned result does not hint");typeof(FileArchiveManager).GetMethod("RestoreSession",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(files,new object[]{Array.Empty<FileData>()});
 r.materials[1].count=2;check(!query(true),"actual unique inventory count enforced");r.materials[1].count=1;
 a.isPermanent=true;check(!query(true),"permanent material cannot be consumed");a.isPermanent=false;
 r.resultItem=c;r.resultFile=null;int notifications=0;history.Obtained+=(id,first)=>{if(id=="item:"+c.itemId)notifications++;};check(cm.TryCombine(new[]{a,b},out _)&&notifications==1&&inv.HasItem(c)&&!inv.HasItem(a)&&!inv.HasItem(b),"original successful combination notifies exactly once after commit");check(!query(false),"completed recipe has no duplicate hint");
 history.BeginTransaction();history.Record("file:P07_rollback");history.RollbackTransaction();check(!history.Capture().Contains("file:P07_rollback"),"transaction rollback clears phantom first-acquisition entry");
 }catch(Exception e){check(false,"EXCEPTION "+e);}finally{UnityEngine.Object.DestroyImmediate(g);foreach(var o in new UnityEngine.Object[]{a,b,c,f,r})UnityEngine.Object.DestroyImmediate(o);}
 Directory.CreateDirectory(P07Regression.Work+"checks");File.WriteAllLines(P07Regression.Work+"checks/acquisition-rules.txt",lines);
 }

 public static void Settings(){
 var checks=new List<string>();Action<bool,string> check=(ok,s)=>checks.Add((ok?"PASS ":"FAIL ")+s);
 float before=AudioListener.volume;bool had=PlayerPrefs.HasKey("P07.MasterVolume");float saved=PlayerPrefs.GetFloat("P07.MasterVolume",1);
 var g=new GameObject("P07_InactiveRuleFixture"){hideFlags=HideFlags.DontSave};g.SetActive(false);
 try{
 var panel=g.AddComponent<P07SettingsPanel>();panel.Build(()=>{});var slider=g.GetComponentInChildren<UnityEngine.UI.Slider>(true);
 check(panel.Tab==0&&slider,"production settings defaults to audio with actual slider");
 slider.onValueChanged.Invoke(.37f);check(Mathf.Abs(AudioListener.volume-.37f)<.00001f&&Mathf.Abs(PlayerPrefs.GetFloat("P07.MasterVolume")-.37f)<.00001f,"actual slider updates global AudioListener and persistent volume");
 slider.onValueChanged.Invoke(0);check(AudioListener.volume==0,"zero volume reaches the actual audio listener");
 P07SettingsPanel.ApplySaved();check(AudioListener.volume==0,"saved volume restored by production startup");
 slider.onValueChanged.Invoke(1);check(AudioListener.volume==1,"global volume restores normally without deleting guitar clips");
 var modes=P07SettingsPanel.Resolutions();check(modes.Length==modes.Distinct().Count()&&modes.Zip(modes.Skip(1),(a,b)=>a.x*a.y>=b.x*b.y).All(x=>x),"device resolution options unique and descending");
 foreach(var pair in new[]{("[E] Talk","[E] 对话"),("[E] Enter","[E] 进入"),("Gate Key","铁门钥匙"),("ID Holder","破旧证件套"),("Newspaper Fragment 01","报纸碎片一")})check(P07Language.Display(pair.Item1)==pair.Item2,"legacy display translated: "+pair.Item1);
 check(P07Language.Display("ProjectMystery")=="ProjectMystery"&&P07Language.Display("Bang！")=="Bang！"&&P07Language.Display("[Space] 拍照")=="[Space] 拍照","proper title, Bang and specified key labels preserved");
 var item=ScriptableObject.CreateInstance<ItemData>();try{item.itemId="gate_key";item.itemName="Gate Key";item.description="An old key found beneath a newspaper fragment.";var inv=g.AddComponent<InventoryManager>();inv.AddItem(item);inv.SetRuntimePresentation(item,"Gate Key",item.description);check(inv.DisplayName(item)=="铁门钥匙"&&inv.DisplayDescription(item).Contains("旧钥匙"),"old-save runtime presentation translated without changing item identity");check(item.itemName=="Gate Key"&&item.itemId=="gate_key","translation does not rewrite authored IDs or saved content");}finally{UnityEngine.Object.DestroyImmediate(item);}
 }catch(Exception e){check(false,"EXCEPTION "+e);}finally{AudioListener.volume=before;if(had)PlayerPrefs.SetFloat("P07.MasterVolume",saved);else PlayerPrefs.DeleteKey("P07.MasterVolume");PlayerPrefs.Save();UnityEngine.Object.DestroyImmediate(g);}
 File.WriteAllLines(P07Regression.Work+"checks/settings-and-language-rules.txt",checks);
 }
}
