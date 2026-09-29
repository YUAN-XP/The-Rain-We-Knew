from pathlib import Path
import re,json,subprocess
p=Path(r'D:\GitHub Desktop\ProjectMystery\ProjectMystery');w=Path(__file__).parent
def blocks(text):
 return {int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
def field(s,key):
 m=re.search(r'^  '+re.escape(key)+r': (.*)',s,re.M);return m[1] if m else ''
def normalized(s):return '\n'.join(line.rstrip() for line in s.splitlines())
report={}
for scene in ['Scene02_Courtyard','Scene03_Childhood','Scene04_School','EndingSequence']:
 before=blocks((w/(scene+'_before.unity')).read_text(encoding='utf-8-sig'));after=blocks((p/'Assets/Scenes'/(scene+'.unity')).read_text(encoding='utf-8-sig'))
 findings=[];counts={'original_objects':0,'transforms':0,'physics':0,'logic':0}
 for k,(kind,body) in before.items():
  if k not in after:findings.append({'id':k,'kind':kind,'issue':'removed original'});continue
  target=after[k][1]
  if kind==1:
   counts['original_objects']+=1
   for name in ['m_Name','m_IsActive','m_Layer','m_TagString']:
    if field(body,name)!=field(target,name):findings.append({'id':k,'issue':'game object field changed '+name})
  elif kind in [4,224]:
   counts['transforms']+=1
   for name in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
    if field(body,name)!=field(target,name):findings.append({'id':k,'issue':'original transform changed '+name})
  elif kind in [50,58,60,61,68,70]:
   counts['physics']+=1
   if normalized(body)!=normalized(target):findings.append({'id':k,'issue':'original physics serialization changed'})
  elif kind==114:
   counts['logic']+=1
   # Existing presentation outline bindings may intentionally point to new views.
   if normalized(body)!=normalized(target):
    findings.append({'id':k,'issue':'original component serialization changed','script':field(body,'m_Script')})
 report[scene]={'counts':counts,'findings':findings,'added_serialized_objects':len(set(after)-set(before))}
(w/'contract_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
if __name__=='__main__':print(json.dumps(report,ensure_ascii=False,indent=2))
