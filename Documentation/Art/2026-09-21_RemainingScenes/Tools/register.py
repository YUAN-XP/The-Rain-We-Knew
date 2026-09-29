from pathlib import Path
from PIL import Image
import json,shutil,sys
sys.stdout.reconfigure(encoding='utf-8')
w=Path(__file__).parent;p=Path(r'D:\GitHub Desktop\ProjectMystery\ProjectMystery');s=Path(r'C:\Users\31975\OneDrive\Desktop\游戏设计\美术新');g=Path(r'C:\Users\31975\.codex\generated_images\01a0721c-6a48-7793-9b57-5dfc41b9da2c')
if len(sys.argv)>2:
 key,ident,category,subfolder=sys.argv[1:5]
 source=g/('exec-'+ident+'.png');im=Image.open(source)
 target=s/subfolder/(key+'.png');target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
 asset='Assets/Art/正式/'+category+'/'+key+'.png';(p/asset).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,p/asset)
 data=json.loads((w/'catalog.json').read_text(encoding='utf-8')) if (w/'catalog.json').exists() else []
 a=im.getchannel('A') if im.mode=='RGBA' else None
 record={'key':key,'source':str(target),'asset':asset,'pixels':im.size,'alpha_bbox':a.getbbox() if a else None,'alpha_zero':a.histogram()[0]/(im.width*im.height) if a else 0,'generated':str(source)}
 data=[d for d in data if d['key']!=key]+[record];(w/'catalog.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(record,ensure_ascii=False))
 if a:print('alpha samples', [a.getpixel(q) for q in [(0,0),(im.width//3,im.height//2),(im.width//2,im.height-1)]] )
else:
 for f in sorted(w.glob('*_live.json')):
  r=json.loads(f.read_text(encoding='utf-8-sig'))['rows'];print('\n'+f.stem)
  for x in r:
   path=x['path']
   if f.stem.startswith('Scene02'):
    show=any(z in path for z in ['Camera_ViewBounds','AP1B_Walls/Wall_0','Stairs_Visual','Wardrobe_Placeholder/Furniture','Bed_Placeholder/Furniture'])
   else:show=path.count('/')<=2 or (len(x['scripts']) and not any(z in path for z in ['Canvas','Art_','AP1B']))
   if show: print(path,'pos',x['pos'],'size',x['size'],'scripts',x['scripts'])
