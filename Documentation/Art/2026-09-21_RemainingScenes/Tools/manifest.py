from pathlib import Path
from PIL import Image
import json
w=Path(__file__).parent
catalog={a['key']:a for a in json.loads((w/'catalog.json').read_text(encoding='utf-8'))}
assets=[]
def rects(file,items):
 a=catalog[file];height=a['pixels'][1]
 for key,r in items.items():
  x,y,right,bottom=r;assets.append(dict(key=key,asset=a['asset'],rect=[x,height-bottom,right-x,bottom-y]))
rects('PM_Home_Architecture_N02',{'door_n':[126,29,332,467],'window_n':[455,58,990,438],'stairs_n':[1068,28,1441,460],'door_d':[115,513,335,967],'window_d':[452,536,994,953],'stairs_d':[1058,511,1462,967]})
rects('PM_Home_Surfaces_N01',{'wall_n':[0,0,768,532],'wall_d':[768,0,1536,532],'floor_n':[0,534,768,1024],'floor_d':[768,534,1536,1024]})
rects('PM_Home_Furniture_Normal_N01',{'sofa_n':[17,181,571,420],'table_n':[591,139,1044,420],'tv_n':[1082,113,1517,420],'bed_n':[33,623,608,894],'wardrobe_n':[661,505,1001,904],'desk_n':[1110,625,1503,904]})
rects('PM_Home_Furniture_Otherworld_N01',{'sofa_d':[10,173,572,443],'table_d':[577,125,1057,444],'tv_d':[1070,111,1529,444],'bed_d':[20,601,634,906],'wardrobe_d':[654,496,1004,914],'desk_d':[1098,610,1522,922]})
rects('PM_Home_Details_N01',{'phone':[45,62,405,418],'camera':[468,151,817,422],'lamp':[920,41,1184,426],'photo':[44,491,387,781],'plant':[453,428,795,784],'shoes':[832,500,1232,796],'computer':[36,861,478,1158],'guitar':[532,783,754,1220],'cabinet':[882,829,1197,1200]})
rects('PM_S02_ShadowFamily_N01',{'shadow_father':[175,0,532,1016],'shadow_mother':[603,96,900,1014],'shadow_child':[913,353,1420,994]})
rects('PM_S03_StreetFacades_N01',{'facade_home':[18,8,494,504],'facade_shop':[521,7,1017,504],'facade_chess':[1026,8,1518,504],'facade_food':[17,516,494,996],'facade_zhao':[521,514,1011,996],'facade_alley':[1036,516,1523,996]})
rects('PM_S03_MazeModules_N01',{'maze_ground_n':[34,127,329,416],'maze_ground_d':[34,581,329,873],'maze_wall_n':[373,206,789,244],'maze_wall_d':[373,665,789,704],'maze_shed_n':[833,92,1135,436],'maze_shed_d':[833,543,1135,904],'maze_corner_n':[1178,89,1532,438],'maze_corner_d':[1178,532,1532,906]})
rects('PM_S04_SchoolArchitecture_N01',{'school_door_n':[67,72,438,467],'school_window_n':[525,125,975,364],'school_stairs_n':[1051,63,1484,475],'school_door_d':[67,543,438,937],'school_window_d':[525,596,975,834],'school_stairs_d':[1051,532,1484,945]})
rects('PM_CH_NeighborhoodCast_N01',{'father':[160,4,363,512],'mother':[480,20,667,512],'limei':[827,25,1040,512],'storyteller':[1143,6,1378,513],'wangping':[136,513,360,1014],'sunfu':[500,513,708,1014],'zhaobingcheng':[824,553,1038,1013],'rings_vendor':[1137,513,1425,1014]})
for suffix,file in [('n','PM_S04_SchoolProps_Normal_N01'),('d','PM_S04_SchoolProps_Otherworld_N01')]:
 rects(file,{'school_desks_'+suffix:[23,184,510,479],'school_board_'+suffix:[532,99,1037,395],'school_office_'+suffix:[1058,95,1519,489],'school_cctv_'+suffix:[24,573,521,930],'school_sports_'+suffix:[565,540,1000,953],'school_toilet_'+suffix:[1038,570,1523,909]})
rects('PM_CH_LeadAges_N02',{'child_xp':[62,104,355,1004],'child_qt':[419,75,724,1004],'youth_xp':[782,2,1112,1007],'youth_qt':[1160,0,1507,1007]})
rects('PM_S03_NeighborhoodLife_N01',{'chess_group':[13,52,468,502],'diners':[476,140,1103,505],'shop_vendor':[1124,63,1525,512],'neighbours':[19,523,466,982],'cat_stand':[742,646,1003,932],'dog':[1009,741,1282,936],'puddle_child':[1282,548,1528,968]})
rects('PM_S03_NeighborhoodLife_N02',{'cat_sleep':[491,746,740,943]})
rects('PM_S03_RiverPanorama_N01',{'river_day':[0,0,1536,512],'river_rain':[0,512,1536,1024],'town_day':[0,0,1536,308]})
rects('PM_S03_StreetDetails_N01',{'noticeboard':[48,64,468,455],'ring_prizes':[477,184,1050,454],'pond':[1020,71,1536,490],'river_tree':[12,469,612,982],'pavilion':[614,582,1213,986],'signpost':[1230,512,1508,984]})
rects('PM_CH_SchoolCast_N02',{'zhangruoyuan':[127,10,321,550],'director':[454,8,683,551],'xiangyan':[839,9,1084,550],'xiangyan_shadow':[1219,7,1458,551],'students_chat':[51,553,371,1007],'student_read':[449,568,733,1007],'student_sleep':[808,599,1110,1005],'students_back':[1193,552,1492,1008]})
rects('PM_S04_SchoolSurfaces_N01',{'school_wall_n':[0,0,768,538],'school_wall_d':[768,0,1536,538],'school_floor_n':[0,544,768,1024],'school_floor_d':[768,544,1536,1024]})
rects('PM_S04_SchoolDetails_N01',{'hoop':[25,0,491,514],'basketball':[610,177,772,348],'oil_lamp':[811,106,931,349],'folded_note':[688,358,870,487],'roof_hut':[1027,48,1536,503],'water_tank':[45,538,495,997],'school_books':[529,529,997,1001],'usb':[1061,591,1236,726],'pencil':[1215,588,1495,707],'postits':[1058,751,1238,911],'folder':[1228,712,1506,983]})
rects('PM_Ending_Environment_N01',{'hospital_wall':[18,66,525,425],'hospital_bed':[545,181,1034,442],'gurney':[1042,219,1535,441],'car_interior':[10,530,551,976],'nurse':[718,442,899,998],'doctor':[1148,431,1358,1010]})
rects('PM_Ending_Gestures_N01',{'uniform_drop':[30,86,504,486],'covered_bed':[505,131,1043,470],'reaching_hand':[1065,159,1514,414],'uniform_sleeve':[13,620,481,884],'temple_hand':[506,546,760,945],'dark_sleeve':[735,536,1046,977],'pendant':[1064,542,1262,907],'arm_wound':[1258,604,1513,948]})
rects('PM_S04_BasketballStand_Side_N01',{'hoop_board':[352,74,384,524],'hoop_rim':[82,400,352,601],'hoop_support':[383,243,970,1464]})
rects('PM_S03_MazeDetails_N01',{'maze_face_n':[13,232,545,390],'maze_face_d':[14,710,545,874],'maze_chalk_n':[567,91,808,484],'maze_chalk_d':[539,569,830,968],'maze_planter_n':[838,225,1248,411],'maze_planter_d':[838,716,1248,893],'maze_meter_n':[1280,50,1525,462],'maze_meter_d':[1274,538,1525,970]})
rects('PM_CH_AdultSeated_N01',{'seated_xp':[135,8,721,1008],'seated_qt':[865,8,1433,1012]})
rects('PM_Ending_CarInterior_N02',{'car_rear':[0,0,1672,941]})
rects('PM_Ending_WardDetails_N01',{'ward_window':[70,53,713,391],'ward_panel':[776,148,1486,388],'ward_curtain':[180,415,629,991],'ward_cabinet':[936,451,1289,965]})
rects('PM_Ending_Environment_N01',{'hospital_plaster':[249,151,477,288],'hospital_dado':[240,332,478,400],'hospital_baseboard':[225,404,480,417]})
assets.append(dict(key='street_gate',asset='Assets/Art/正式/Props/Scene01/PM_S01_Prop_Gate_N01.png',rect=[78,88,1388,864]))
assets.append(dict(key='adult_qt',asset='Assets/Art/正式/Characters/IdentityApproved/PM_CH_QiuTian_Adult_Idle_A_v01.png',rect=[290,0,493,1536]))
rects('PM_S03_ActivityPaper_N01',{'puppet_princess':[44,0,387,460],'puppet_knight':[445,0,773,462],'puppet_rebel':[882,0,1236,460],'puppet_castle':[20,460,415,825],'puppet_ring':[438,503,836,815],'puppet_coin':[896,513,1220,815],'puppet_prize':[55,823,423,1244],'puppet_skittles':[511,822,744,1239],'puppet_stage':[832,825,1253,1254]})
out={'assets':assets};(w/'manifest.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
print('Regions:',len(assets))
