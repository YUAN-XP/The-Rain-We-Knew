using System.Collections.Generic;
// Exact legacy display strings only. Item IDs, save data and story conditions stay unchanged.
public static class P07Language {
 static readonly Dictionary<string,string> legacy=new Dictionary<string,string>{
 {"[E] Talk","[E] 对话"},{"[E] Exit","[E] 离开"},{"[E] Enter","[E] 进入"},
 {"[E] Inspect","[E] 调查"},{"[E] Pick Up","[E] 拾取"},{"[E] Collect","[E] 收集"},
 {"Gate Key","铁门钥匙"},{"ID Holder","破旧证件套"},{"Newspaper Fragment 01","报纸碎片一"},
 {"An old key found beneath a newspaper fragment.","在报纸碎片下面找到的一把旧钥匙。"},
 {"Xiao Peng's damaged ID holder.","肖鹏破损的证件套。"},
 {"A torn piece of an old newspaper.","从旧报纸上撕下的一块碎片。"}};
 public static string Display(string value)=>value!=null&&legacy.TryGetValue(value,out var translated)?translated:value;
}
