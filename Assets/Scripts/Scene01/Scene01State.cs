using UnityEngine;

public class Scene01State : MonoBehaviour
{
    public bool gateOpened = false;
    public bool newspaperFragmentTaken = false;

    public void OpenGate()
    {
        gateOpened = true;

        Debug.Log(
            "Scene01: Gate opened."
        );
    }

    public void TakeNewspaperFragment()
    {
        newspaperFragmentTaken = true;

        Debug.Log(
            "Scene01: Newspaper fragment taken."
        );
    }
}