using UnityEngine;

// Music-only authoring. Scene topology and gameplay triggers are not owned here.
public enum MusicSceneRole { Rain, Home, Childhood, School, Ending }
public sealed class SceneMusicProfile : MonoBehaviour
{
    public MusicSceneRole role;
    public MusicState defaultState;
}
