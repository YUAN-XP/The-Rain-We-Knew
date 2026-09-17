using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A scene installs its authored recipes onto the existing persistent inventory.
// Runtime state/presentation changes only; permanent removal stays protected.
[DisallowMultipleComponent]
public sealed class CombinationManager : MonoBehaviour
{
    [SerializeField] private CombinationRecipe[] recipes=Array.Empty<CombinationRecipe>();
    public IReadOnlyList<CombinationRecipe> Recipes=>recipes;
    public event Action<CombinationRecipe> Completed;
    InventoryManager inventory;FileArchiveManager files;StoryStateManager story;
    bool busy;
    public void Configure(CombinationRecipe[] value){recipes=value??Array.Empty<CombinationRecipe>();Resolve();}
    // Add chapter recipes without replacing recipes already installed by earlier chapters.
    public void Register(CombinationRecipe value) { if(value!=null&&!recipes.Contains(value)) recipes=recipes.Concat(new[]{value}).ToArray(); Resolve(); }
    void Resolve(){inventory=GetComponent<InventoryManager>();files=GetComponent<FileArchiveManager>();story=GetComponent<StoryStateManager>();}
    bool Available(CombinationRecipe r)=>r!=null&&r.materials!=null&&r.materials.Length>0&&story!=null&&!story.Has(r.completedFlag)
        &&(r.prerequisites==null||r.prerequisites.All(story.Has));
    bool MaterialAllowed(CombinationRecipe.Material m)=>m!=null&&m.item!=null&&m.count>0
        &&(!m.consume||!m.item.isPermanent)&&(m.item.canCombine||!m.consume&&m.retainedTool);
    public IEnumerable<CombinationRecipe> Candidates(IReadOnlyCollection<ItemData> selected)
    {
        if(story==null)Resolve();
        return recipes.Where(r=>Available(r)&&r.materials.All(MaterialAllowed)&&selected.All(i=>r.materials.Any(m=>m.item==i)));
    }
    public bool CanSelect(ItemData item,IReadOnlyCollection<ItemData> selected)
        =>item!=null&&inventory!=null&&inventory.HasItem(item)&&Candidates(selected).Any(r=>r.materials.Any(m=>m.item==item));
    public bool TryCombine(IReadOnlyCollection<ItemData> selected,out string feedback)
    {
        feedback="搭配不符。";Resolve();if(busy||inventory==null||files==null||story==null)return false;
        if(selected==null||selected.Count==0){feedback="仍缺少物品/文件";return false;}
        var unique=selected.Where(i=>i!=null).Distinct().ToArray();
        if(unique.Length!=selected.Count){feedback="同一物品不能重复选择。";return false;}
        var matches=Candidates(unique).ToArray();if(matches.Length==0)return false;
        var r=matches.FirstOrDefault(x=>x.materials.Length==unique.Length&&x.materials.All(m=>unique.Contains(m.item)));
        if(r==null){feedback="仍缺少物品/文件";return false;}
        if(r.materials.Select(m=>m.item.itemId).Distinct().Count()!=r.materials.Length){feedback="配方材料配置无效。";return false;}
        // Inventory V1 is unique-item based. Verify identity and actual count,
        // never assume a void Add/Remove succeeded or count one entry twice.
        if(r.materials.Any(m=>inventory.Items.Count(i=>i==m.item)!=m.count||inventory.Items.Count(i=>i!=null&&i.itemId==m.item.itemId)!=m.count))
        {feedback="仍缺少物品/文件";return false;}
        if(r.resultFile==null&&r.resultItem==null&&r.retainedResult==null){feedback="组合结果尚未配置。";return false;}
        if(r.retainedResult!=null&&!r.materials.Any(m=>m.item==r.retainedResult&&!m.consume))return false;
        if(r.resultFile!=null&&(string.IsNullOrWhiteSpace(r.resultFile.fileId)||files.HasFile(r.resultFile)))return false;
        if(r.resultItem!=null&&inventory.HasItemById(r.resultItem.itemId))return false;
        var lamp=GetComponent<OilLampController>();if(r.restoreLamp&&(lamp==null||!lamp.Owned||r.retainedResult==null))return false;
        var before=inventory.CaptureSession();var beforeFiles=files.Files.ToArray();
        var flags=new Dictionary<StoryFlag,bool>{{r.completedFlag,story.Has(r.completedFlag)}};
        if(r.restoreLamp)flags[StoryFlag.OilLampExhausted]=story.Has(StoryFlag.OilLampExhausted);
        busy=true;
        try {
            foreach(var m in r.materials.Where(m=>m.consume)){inventory.RemoveItem(m.item);if(inventory.HasItem(m.item))throw new InvalidOperationException("Material removal rejected");}
            if(r.resultItem!=null){inventory.AddItem(r.resultItem);if(!inventory.HasItem(r.resultItem))throw new InvalidOperationException("Result item rejected");}
            if(r.resultFile!=null){files.AddFile(r.resultFile);if(!files.HasFile(r.resultFile))throw new InvalidOperationException("Result file rejected");}
            if(r.retainedResult!=null)inventory.SetRuntimePresentation(r.retainedResult,r.resultName,r.resultDescription);
            var values=new Dictionary<StoryFlag,bool>{{r.completedFlag,true}};
            if(r.restoreLamp)values[StoryFlag.OilLampExhausted]=false;
            // One notification, after all material/result verification. Lamp
            // usability is derived from this same StoryState flag.
            story.RestoreSubset(values);
            if(r.restoreLamp&&!lamp.StoryUsable)throw new InvalidOperationException("Lamp restoration rejected");
            feedback=r.resultFile!=null?r.resultFile.fileName:r.resultName;
        } catch(Exception e) {
            inventory.RestoreSession(before);files.RestoreSession(beforeFiles);story.RestoreSubset(flags);
            Debug.LogWarning("Combination rolled back: "+e.Message,this);feedback="组合未完成，材料已保留。";return false;
        } finally {busy=false;}
        Completed?.Invoke(r);return true;
    }
}
