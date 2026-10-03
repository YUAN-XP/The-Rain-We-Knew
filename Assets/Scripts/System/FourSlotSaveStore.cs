using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Validation completes before replacing a slot. A damaged slot never resets another.
public sealed class FourSlotSaveStore {
    public readonly string DirectoryPath;
    public FourSlotSaveStore(string directory){DirectoryPath=Path.GetFullPath(directory);}
    public string SlotPath(int slot){if(slot<1||slot>4)throw new ArgumentOutOfRangeException(nameof(slot));return Path.Combine(DirectoryPath,"slot-"+slot.ToString("00")+".json");}
    static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
    public bool Exists(int slot)=>File.Exists(SlotPath(slot));
    public bool TryRead(int slot,out GameSaveData data,out string error){
        data=null;error=null;try{
            string path=SlotPath(slot);if(!File.Exists(path)){error="空存档";return false;}
            if(new FileInfo(path).Length>4*1024*1024)throw new InvalidDataException("存档大小异常");
            var envelope=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path,Encoding.UTF8));
            if(envelope==null||envelope.format!="ProjectMystery.Save.1"||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)throw new InvalidDataException("存档校验失败");
            var candidate=JsonUtility.FromJson<GameSaveData>(envelope.payload);
            Validate(candidate,slot);data=candidate;return true;
        }catch(Exception){error="无法读取存档：文件损坏、版本不兼容或无法访问。请尝试其他存档。";return false;}
    }
    public void Write(int slot,GameSaveData data,bool overwrite){
        Validate(data,slot);string target=SlotPath(slot);Directory.CreateDirectory(DirectoryPath);
        if(File.Exists(target)&&!overwrite)throw new InvalidOperationException("该存档槽已有内容，请先确认覆盖");
        string payload=JsonUtility.ToJson(data);string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{
            byte[] bytes=new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=Hash(payload)}));
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public int LatestValidSlot(){int result=0;long newest=0;for(int i=1;i<=4;i++)if(TryRead(i,out var d,out _)&&d.savedUtcTicks>newest){newest=d.savedUtcTicks;result=i;}return result;}
    public static void Validate(GameSaveData d,int slot){
        if(d==null||d.version!=1||d.slot!=slot||slot<1||slot>4)throw new InvalidDataException("存档版本或槽位不兼容");
        if(d.scene!="Scene01_Rain"&&d.scene!="Scene02_Courtyard"&&d.scene!="Scene03_Childhood"&&d.scene!="Scene04_School")throw new InvalidDataException("不支持的场景");
        if(d.story==null||d.story.flags==null||d.items==null||d.files==null||d.school==null||d.firstObtained==null||d.companions==null||!QuestProgress.Valid(d.quests))throw new InvalidDataException("存档状态缺失");
        if(!Enum.IsDefined(typeof(AdultOutfit),d.story.outfit)||d.threeDoorProgress<0||d.threeDoorProgress>3)throw new InvalidDataException("存档状态越界");
        if(d.story.flags.Distinct().Count()!=d.story.flags.Length)throw new InvalidDataException("重复剧情状态");
        foreach(var f in d.story.flags)if(!Enum.TryParse<StoryFlag>(f,out var value)||!Enum.IsDefined(typeof(StoryFlag),value))throw new InvalidDataException("未知剧情状态："+f);
        foreach(var v in new[]{d.position.x,d.position.y,d.position.z,d.cameraSize,d.scale.x,d.scale.y,d.scale.z,d.cameraPosition.x,d.cameraPosition.y,d.cameraPosition.z,d.cameraOffset,d.facing,d.cameraX.x,d.cameraX.y,d.cameraY.x,d.cameraY.y})if(float.IsNaN(v)||float.IsInfinity(v))throw new InvalidDataException("无效坐标");
        if(d.cameraSize<=0||d.playSeconds<0||double.IsNaN(d.playSeconds)||double.IsInfinity(d.playSeconds)||d.savedUtcTicks<=0||d.savedUtcTicks>DateTime.MaxValue.Ticks||Mathf.Abs(d.scale.x)<.001f||d.scale.y<=0||d.cameraX.x>d.cameraX.y||d.cameraY.x>d.cameraY.y)throw new InvalidDataException("无效时间或镜头");
        if(d.actor!="adult"&&d.actor!="child"&&d.actor!="youth")throw new InvalidDataException("无效角色");
        if(d.school.present&&(!Enum.IsDefined(typeof(SchoolWorldVariant),d.school.variant)||!Enum.IsDefined(typeof(SchoolActor),d.school.actor)||!Enum.IsDefined(typeof(SchoolPhase),d.school.phase)||string.IsNullOrEmpty(d.school.room)))throw new InvalidDataException("无效学校状态");
        if(d.items.Any(x=>x==null||string.IsNullOrEmpty(x.key)||string.IsNullOrEmpty(x.id))||d.files.Any(x=>x==null||string.IsNullOrEmpty(x.key)||string.IsNullOrEmpty(x.id)))throw new InvalidDataException("资源身份缺失");
        var roles=new System.Collections.Generic.HashSet<string>();
        foreach(var c in d.companions){
            if(c==null||!roles.Add(c.role)||
                !(c.role=="childhood-boy"&&d.scene=="Scene03_Childhood"&&(c.parent=="home"||c.parent=="living"||c.parent=="river")||c.role=="home-guide"&&d.scene=="Scene02_Courtyard"&&c.parent=="authored"||c.role=="street-guide"&&d.scene=="Scene01_Rain"&&c.parent=="authored"))throw new InvalidDataException("同伴身份不兼容");
            foreach(float v in new[]{c.position.x,c.position.y,c.position.z,c.scale.x,c.scale.y,c.scale.z,c.direction})if(float.IsNaN(v)||float.IsInfinity(v))throw new InvalidDataException("同伴坐标无效");
        }
        if(d.items.Select(x=>x.key).Distinct().Count()!=d.items.Length||d.files.Select(x=>x.key).Distinct().Count()!=d.files.Length)throw new InvalidDataException("重复库存");
    }
}
