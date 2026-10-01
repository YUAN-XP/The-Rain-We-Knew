using UnityEngine;

// The pause UI reads Unity Input itself; gameplay never sees its opening/closing frame.
public static class GameplayInput
{
#if UNITY_EDITOR
    // Explicitly installed only by the editor replay harness. Null keeps native input.
    public interface IReplaySource {
        bool Key(KeyCode key); bool KeyDown(KeyCode key); float Axis(string axis);
        bool Mouse(int button); bool MouseDown(int button); bool MouseUp(int button); Vector3 Pointer { get; }
    }
    public static IReplaySource Replay;
#endif
    static bool RawKey(KeyCode key) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.Key(key);
#endif
        return Input.GetKey(key);
    }
    public static bool RawKeyDown(KeyCode key) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.KeyDown(key);
#endif
        return Input.GetKeyDown(key);
    }
    static float RawAxis(string axis) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.Axis(axis);
#endif
        return Input.GetAxisRaw(axis);
    }
    static bool RawMouse(int button) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.Mouse(button);
#endif
        return Input.GetMouseButton(button);
    }
    static bool RawMouseDown(int button) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.MouseDown(button);
#endif
        return Input.GetMouseButtonDown(button);
    }
    static bool RawMouseUp(int button) {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.MouseUp(button);
#endif
        return Input.GetMouseButtonUp(button);
    }
    static Vector3 RawPointer { get {
#if UNITY_EDITOR
        if(Replay!=null)return Replay.Pointer;
#endif
        return Input.mousePosition;
    }}
    static int consumedFrame=-1, escapeFrame=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){consumedFrame=-1;escapeFrame=-1;
#if UNITY_EDITOR
        Replay=null;
#endif
    }
    public static bool Blocked => GamePause.IsPaused || consumedFrame==Time.frameCount;
    public static void ConsumeFrame() { consumedFrame=Time.frameCount; }
    public static bool GetKey(KeyCode key) => !Blocked && RawKey(key);
    public static bool GetKeyDown(KeyCode key) {
        if(Blocked || !RawKeyDown(key))return false;
        if(key!=KeyCode.Escape)return true;
        if(escapeFrame==Time.frameCount)return false;
        escapeFrame=Time.frameCount;return true;
    }
    public static float GetAxisRaw(string axis)=>Blocked?0:RawAxis(axis);
    public static bool GetMouseButton(int button)=>!Blocked&&RawMouse(button);
    public static bool GetMouseButtonDown(int button)=>!Blocked&&RawMouseDown(button);
    public static bool GetMouseButtonUp(int button)=>!Blocked&&RawMouseUp(button);
    public static Vector3 mousePosition=>RawPointer;
}
