using UnityEngine;

public sealed partial class EndingDirector
{
    float foregroundY,segmentTriggerX,bedAnchorX,stageCenter;
    Transform walkStage;
    Transform[] laneTiles;
    SpriteRenderer[] wardCoverage;
    const float LaneTop=-1.1f;
    const float TileWidth=32f;

    void PrepareWalkLane()
    {
        var bodyCollider=player.GetComponent<Collider2D>();
        foregroundY=LaneTop+(bodyCollider!=null?player.transform.position.y-bodyCollider.bounds.min.y:1.1f);
        laneTiles=new Transform[3];
        for(int n=0;n<3;n++){
            var tile=new GameObject("EndingWalkGround");tile.transform.SetParent(presentation,false);laneTiles[n]=tile.transform;
            var ground=Shape("Ground",tile.transform,new Vector2(0,LaneTop-.2f),new Vector2(TileWidth,.4f),new Color(.15f,.2f,.22f),6);
            ground.gameObject.AddComponent<BoxCollider2D>();
            // World-aligned seams provide actual scrolling while the current
            // memory stays readable, even when its text is left open for minutes.
            for(int k=0;k<8;k++)Shape("GroundSeam",tile.transform,new Vector2(-14+k*4,LaneTop-.15f),new Vector2(.045f,.3f),new Color(.25f,.3f,.3f),7);
        }
        wardCoverage=new[]{wardSet.transform.Find("HospitalWall").GetComponent<SpriteRenderer>(),wardSet.transform.Find("WallRail").GetComponent<SpriteRenderer>()};
    }

    void EnterWalkStage(EndingStep next)
    {
        // Each memory gets a fresh local advance reference. Distance accrued
        // while reading the previous cue can never trigger several memories.
        Step=next;float x=player.transform.position.x;
        segmentTriggerX=x+Mathf.Min(5f,Mathf.Max(1f,childWatchX));
        var set=next==EndingStep.WalkChild?childSet:next==EndingStep.WalkYouth?youthSet:next==EndingStep.WalkHospital?hospitalSet:wardSet;
        stageCenter=set==childSet?9:set==youthSet?23:set==hospitalSet?38:49;
        walkStage=set.transform;
        walkStage.position=new Vector3(x+3.2f-stageCenter,0,0);SetSets(set);
        if(next==EndingStep.WalkBed){
            // The bed is a stationary, reachable object, never a moving goal.
            walkStage.position+=Vector3.right*3f;
            bedAnchorX=wardSet.transform.Find("WardBed").position.x-1.5f;bedArmed=false;
        }
        UpdateWalkStage();
    }

    void UpdateWalkStage()
    {
        float x=player.transform.position.x;
        if(walkStage!=null&&Step!=EndingStep.WalkBed)
            walkStage.position=new Vector3(x+3.2f-stageCenter,0,0);
        if(laneTiles!=null){
            int center=Mathf.FloorToInt(x/TileWidth);
            for(int n=0;n<laneTiles.Length;n++)laneTiles[n].position=new Vector3((center+n-1)*TileWidth,0,0);
        }
        // Extend the ward wall only; its bed and other interaction anchors stay
        // fixed. Walking past the bed remains reversible with left input.
        if(Step==EndingStep.WalkBed&&wardCoverage!=null)
            foreach(var panel in wardCoverage){var p=panel.transform.position;p.x=x+3.2f;panel.transform.position=p;}
    }
}
