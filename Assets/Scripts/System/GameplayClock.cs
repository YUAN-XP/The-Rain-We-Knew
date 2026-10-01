using System.Collections;
using UnityEngine;

// All narrative and rhythm time uses this pause-aware, unscaled clock.
[DefaultExecutionOrder(-32000), DisallowMultipleComponent]
public sealed class GameplayClock : MonoBehaviour
{
    public static double Elapsed { get; private set; }
    public static float Now => (float)Elapsed;
    public static float Delta => GamePause.IsPaused ? 0 : Time.unscaledDeltaTime;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetClock() { Elapsed=0; }
    void Update() { Elapsed += Delta; }
    public static void Restore(double seconds) { Elapsed=System.Math.Max(0,seconds); }
    public static CustomYieldInstruction Wait(float seconds) => new PauseAwareWait(seconds);
    sealed class PauseAwareWait : CustomYieldInstruction {
        readonly double until;
        public PauseAwareWait(float seconds){until=Elapsed+Mathf.Max(0,seconds);}
        public override bool keepWaiting => GamePause.IsPaused || Elapsed<until;
    }
}
