using UnityEngine;

/// <summary>Optional authored reach override for high fixtures or oversized interaction sensors.</summary>
[DisallowMultipleComponent]
public sealed class InteractionReach : MonoBehaviour
{
    public Transform anchor;
    public Renderer visualSurface;
    [Min(.05f)] public float horizontalReach = .55f;
    [Min(.05f)] public float verticalReach = 1.8f;
}
