using System;
using UnityEngine;

// Serialized values are append-only. Display names never determine anonymous identities.
public enum DialogueCharacterId
{
    Unassigned = 0, Narrator = 1, XiaoPeng = 2, QiuTian = 3, ChildhoodBoy = 4,
    ZhengTao = 5, ZhangRuoYuan = 6, Director = 7, XiangYan = 8, Mother = 9,
    Father = 10, LiMei = 11, Storyteller = 12, ZhaoBingCheng = 13,
    ZhangChen = 14, SunFu = 15, WangKun = 16, WangPing = 17,
    RingVendor = 18, RestaurantOwner = 19, ShopOwner = 20, YangJunWang = 21,
    Doctor = 22, Unknown = 23
}
public enum DialoguePortraitMode { Character = 0, None = 1, Silhouette = 2 }
public enum DialogueAge { Context = 0, Adult = 1, Child = 2, Youth = 3 }

public static class DialogueIdentity
{
    public static DialogueCharacterId Resolve(DialogueLine line)
    {
        if (line == null) return DialogueCharacterId.Narrator;
        return line.characterId != DialogueCharacterId.Unassigned ? line.characterId : LegacyName(line.speaker);
    }

    // Compatibility for un-migrated third-party DialogueData only. Exact aliases, never Contains.
    public static DialogueCharacterId LegacyName(string name)
    {
        switch ((name ?? string.Empty).Trim())
        {
            case "": case "旁白": case "内心": return DialogueCharacterId.Narrator;
            case "肖鹏": case "小鹏": case "Xiao Peng": return DialogueCharacterId.XiaoPeng;
            case "邱天": case "Qiu Tian": return DialogueCharacterId.QiuTian;
            case "男孩": return DialogueCharacterId.ChildhoodBoy;
            case "郑涛": return DialogueCharacterId.ZhengTao;
            case "张若媛": case "张若源": return DialogueCharacterId.ZhangRuoYuan;
            case "主任": case "教导主任": return DialogueCharacterId.Director;
            case "向焱": return DialogueCharacterId.XiangYan;
            case "母亲": case "妈妈": return DialogueCharacterId.Mother;
            case "父亲": case "爸爸": return DialogueCharacterId.Father;
            case "李梅": return DialogueCharacterId.LiMei;
            case "说书人": return DialogueCharacterId.Storyteller;
            case "赵炳程": case "赵秉承": return DialogueCharacterId.ZhaoBingCheng;
            case "张辰": case "张晨": return DialogueCharacterId.ZhangChen;
            case "孙付": case "孙福": return DialogueCharacterId.SunFu;
            case "王坤": return DialogueCharacterId.WangKun;
            case "王平": case "王萍": return DialogueCharacterId.WangPing;
            case "套圈老板": return DialogueCharacterId.RingVendor;
            case "饭店老板": return DialogueCharacterId.RestaurantOwner;
            case "便利店老板": return DialogueCharacterId.ShopOwner;
            case "杨骏汪": return DialogueCharacterId.YangJunWang;
            case "医生": return DialogueCharacterId.Doctor;
            default: return DialogueCharacterId.Unknown;
        }
    }

    public static string PortraitKey(DialogueCharacterId id, DialogueAge age)
    {
        switch (id)
        {
            case DialogueCharacterId.XiaoPeng:
                return age == DialogueAge.Child ? "CHR_XP_CHILD_BASE:0" : age == DialogueAge.Youth ? "CHR_XP_YOUTH_BASE:0" : "CHR_XP_ADULT_DEFAULT_CLOSEUP";
            case DialogueCharacterId.QiuTian: return "CHR_GUIDE_CLOSEUP";
            case DialogueCharacterId.ChildhoodBoy: return "CHR_BOY_CHILD_BASE:0";
            case DialogueCharacterId.ZhengTao: return "CHR_ZHENGTAO_YOUTH_BASE:0";
            case DialogueCharacterId.ZhangRuoYuan: return "CHR_SCHOOL_SUPPORT_BASE:CH_ZHANGRUOYUAN";
            case DialogueCharacterId.Director: return "CHR_SCHOOL_SUPPORT_BASE:CH_DIRECTOR";
            case DialogueCharacterId.XiangYan: return "CHR_SCHOOL_SUPPORT_BASE:CH_XIANGYAN_normal_reference";
            case DialogueCharacterId.Mother: return "CHR_FAMILY_NEIGHBOR_BASE:CH_XP_MOTHER";
            case DialogueCharacterId.Father: return "CHR_FAMILY_NEIGHBOR_BASE:CH_XP_FATHER";
            case DialogueCharacterId.LiMei: return "CHR_FAMILY_NEIGHBOR_BASE:CH_LIMEI";
            case DialogueCharacterId.Storyteller: return "CHR_STORY_ZHAO_ZHANG_BASE:CH_STORYTELLER";
            case DialogueCharacterId.ZhaoBingCheng: return "CHR_STORY_ZHAO_ZHANG_BASE:CH_ZHAOBINGCHENG";
            case DialogueCharacterId.ZhangChen: return "CHR_STORY_ZHAO_ZHANG_BASE:CH_ZHANGCHEN";
            case DialogueCharacterId.SunFu: return "CHR_CHESS_NEIGHBORS_BASE:CH_SUNFU";
            case DialogueCharacterId.WangKun: return "CHR_CHESS_NEIGHBORS_BASE:CH_WANGKUN";
            case DialogueCharacterId.WangPing: return "CHR_CHESS_NEIGHBORS_BASE:CH_WANGPING";
            case DialogueCharacterId.RingVendor: return "CHR_TOWN_TRADERS_BASE:CH_RING_VENDOR";
            // Missing approved portraits remain absent; never substitute a different person.
            default: return null;
        }
    }
}
