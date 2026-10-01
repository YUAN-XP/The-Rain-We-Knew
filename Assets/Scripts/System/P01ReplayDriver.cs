#if UNITY_EDITOR
using System;
using UnityEngine;
// Editor diagnostic driver is absent from player builds.
[DefaultExecutionOrder(-32500)]
public sealed class P01ReplayDriver : MonoBehaviour {
    public Action Tick, Cleanup;
    void Update()=>Tick?.Invoke();
    void OnDestroy()=>Cleanup?.Invoke();
}
#endif
