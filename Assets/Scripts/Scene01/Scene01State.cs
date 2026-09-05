using UnityEngine;

public class Scene01State : MonoBehaviour
{
    public bool gateOpened = false;
    public bool newspaperFragmentTaken = false;

    private StoryStateManager story;
    private void Start()
    {
        story=PersistentRuntime.Instance!=null?PersistentRuntime.Instance.GetComponent<StoryStateManager>():null;
        if(story!=null){gateOpened|=story.Has(StoryFlag.Scene01GateOpened);newspaperFragmentTaken|=story.Has(StoryFlag.Scene01NewspaperTaken);}
    }
    public void OpenGate()
    {
        gateOpened = true;
        if(story!=null)story.Set(StoryFlag.Scene01GateOpened);

        Debug.Log(
            "Scene01: Gate opened."
        );
    }

    public void TakeNewspaperFragment()
    {
        newspaperFragmentTaken = true;
        if(story!=null)story.Set(StoryFlag.Scene01NewspaperTaken);

        Debug.Log(
            "Scene01: Newspaper fragment taken."
        );
    }
}