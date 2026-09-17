using UnityEngine;

public sealed partial class EndingDirector
{
    GameObject Set(string name){var go=new GameObject(name);go.transform.SetParent(presentation,false);return go;}
    SpriteRenderer Shape(string name,Transform parent,Vector2 pos,Vector2 size,Color color,int order=0)=>EndingActorView.Part(name,parent,data.bodySprite,pos,size,color,order);
    void BuildPresentation(){
        presentation=new GameObject("EndingPresentation").transform;presentation.SetParent(transform,false);
        carSet=Set("CarRearSeat");childSet=Set("ChildhoodFarewell");youthSet=Set("YouthFarewell");hospitalSet=Set("HospitalCorridor");wardSet=Set("HospitalWard");
        Shape("CarInterior",carSet.transform,new Vector2(0,2),new Vector2(20,11),new Color(.08f,.12f,.15f),-10);
        Shape("RearWindow",carSet.transform,new Vector2(0,3.7f),new Vector2(10,2.8f),new Color(.22f,.3f,.35f),-8);
        Shape("RearSeat",carSet.transform,new Vector2(0,-.8f),new Vector2(8,1.5f),new Color(.18f,.2f,.24f),-2);
        Shape("SeatBack",carSet.transform,new Vector2(0,.65f),new Vector2(8,1.7f),new Color(.24f,.27f,.29f),-3);
        for(int n=0;n<16;n++){var line=Shape("RainOnGlass",carSet.transform,new Vector2(-4.6f+n*.6f,3.8f),new Vector2(.025f,1.3f),new Color(.48f,.56f,.61f,.4f),-5);line.transform.localRotation=Quaternion.Euler(0,0,-15);}
        carGuide=EndingActorView.Create("GuideRearSeatView",carSet.transform,data.bodySprite,new Vector3(1.7f,.2f,0),1,new Color(.38f,.4f,.39f));
        Shape("ForegroundLane",presentation,new Vector2(26,-1.25f),new Vector2(90,.4f),new Color(.15f,.2f,.22f),5);
        RiverBackdrop(childSet.transform,9,false);RiverBackdrop(youthSet.transform,23,true);
        if(riverLandmarksTemplate!=null){var view=Instantiate(riverLandmarksTemplate,childSet.transform);view.name="Scene03RiverVisualsOnly";view.SetActive(true);}
        child=EndingActorView.Create("ChildMemoryView",childSet.transform,data.bodySprite,new Vector3(8.8f,1.35f+.72f*1.055f,0),.72f,new Color(.39f,.44f,.48f));
        boy=EndingActorView.Create("BoyMemoryView",childSet.transform,data.bodySprite,new Vector3(10.5f,1.35f+.74f*1.055f,0),.74f,new Color(.36f,.36f,.39f));
        youth=EndingActorView.Create("YouthMemoryView",youthSet.transform,data.bodySprite,new Vector3(22.7f,1.35f+.92f*1.055f,0),.92f,new Color(.36f,.4f,.45f));
        friend=EndingActorView.Create("FriendMemoryView",youthSet.transform,data.bodySprite,new Vector3(24.5f,1.35f+.94f*1.055f,0),.94f,new Color(.3f,.34f,.37f));
        Shape("CoveredArmWound",friend.transform,new Vector2(-.34f,.17f),new Vector2(.13f,.34f),new Color(.36f,.22f,.23f),25);
        foreach(var room in new[]{hospitalSet,wardSet}){
            float x=room==hospitalSet?38:49;Shape("HospitalWall",room.transform,new Vector2(x,3),new Vector2(28,9),new Color(.37f,.45f,.46f),-15);
            Shape("WallRail",room.transform,new Vector2(x,1.6f),new Vector2(28,.16f),new Color(.23f,.34f,.34f),-10);
            for(int n=0;n<5;n++){Shape("CeilingLight",room.transform,new Vector2(x-10+n*5,5.4f),new Vector2(2,.13f),new Color(.84f,.86f,.76f),-8);}
        }
        stretchers=new Transform[2];for(int i=0;i<2;i++){
            var bed=new GameObject("AnonymousStretcher_"+(i+1));bed.transform.SetParent(hospitalSet.transform);bed.transform.position=new Vector3(46+i*5,1.05f+i*.5f,0);stretchers[i]=bed.transform;
            Shape("Frame",bed.transform,Vector2.zero,new Vector2(2.8f,.18f),new Color(.58f,.63f,.6f),20);
            Shape("Blanket",bed.transform,new Vector2(0,.38f),new Vector2(2.2f,.65f),new Color(.6f,.65f,.6f),21);
            Shape("FeaturelessCoveredHead",bed.transform,new Vector2(-.9f,.58f),new Vector2(.4f,.36f),new Color(.2f,.23f,.24f),22);
            Shape("Wheel1",bed.transform,new Vector2(-.9f,-.65f),new Vector2(.32f,.32f),new Color(.12f,.17f,.18f),21);Shape("Wheel2",bed.transform,new Vector2(.9f,-.65f),new Vector2(.32f,.32f),new Color(.12f,.17f,.18f),21);
            EndingActorView.Create("MedicalStaff",bed.transform,data.bodySprite,bed.transform.position+new Vector3(1.8f,.35f,0),.92f,new Color(.7f,.75f,.7f));
        }
        Shape("WardBed",wardSet.transform,new Vector2(51,.0f),new Vector2(3.6f,.3f),new Color(.64f,.69f,.65f),15);
        Shape("WardBlanket",wardSet.transform,new Vector2(51,.65f),new Vector2(3.3f,.85f),new Color(.63f,.71f,.69f),16);
        Shape("WardPillowFeatureless",wardSet.transform,new Vector2(52.2f,.98f),new Vector2(.6f,.4f),new Color(.17f,.2f,.22f),17);
        Shape("WardLeg1",wardSet.transform,new Vector2(49.6f,-.6f),new Vector2(.15f,1.2f),Color.gray,15);Shape("WardLeg2",wardSet.transform,new Vector2(52.3f,-.6f),new Vector2(.15f,1.2f),Color.gray,15);
        painHand=new GameObject("HeadachePose");painHand.transform.SetParent(player.transform,false);
        Shape("BentForearm",painHand.transform,new Vector2(.32f,.28f),new Vector2(.16f,.48f),new Color(.53f,.5f,.46f),35);
        Shape("HandAtTemple",painHand.transform,new Vector2(.23f,.48f),new Vector2(.25f,.14f),new Color(.69f,.63f,.54f),36);painHand.SetActive(false);
        SetSets(null);
    }
    void RiverBackdrop(Transform parent,float x,bool rainy){
        Shape("MemorySky",parent,new Vector2(x,3),new Vector2(29,10),rainy?new Color(.20f,.27f,.31f):new Color(.42f,.46f,.43f),-20);
        Shape("River",parent,new Vector2(x,.7f),new Vector2(29,1.3f),rainy?new Color(.22f,.34f,.37f):new Color(.35f,.45f,.44f),-12);
        Shape("RiverbankStage",parent,new Vector2(x,1.2f),new Vector2(14,.3f),new Color(.31f,.32f,.28f),2);
        if(rainy)for(int n=0;n<34;n++){var r=Shape("MemoryRain",parent,new Vector2(x-12+n*.7f,3.4f+(n%3)*.4f),new Vector2(.018f,2.3f),new Color(.55f,.64f,.67f,.55f),5);r.transform.localRotation=Quaternion.Euler(0,0,-18);}
    }
}
