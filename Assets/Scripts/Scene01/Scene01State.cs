using UnityEngine;

public class Scene01State : MonoBehaviour
{
    public bool hasGateKey = false;
    public bool gateOpened = false;

    public void GetGateKey()
    {
        hasGateKey = true;
        Debug.Log("Scene01: Gate key obtained.");
    }

    public void OpenGate()
    {
        gateOpened = true;
        Debug.Log("Scene01: Gate opened.");
    }
}