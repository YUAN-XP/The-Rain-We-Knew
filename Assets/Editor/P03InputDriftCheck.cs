using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Editor-only observer: no movement, saves, scene or test-input changes.
public static class P03InputDriftCheck
{
    public const string Folder="D:/游戏设计/Codex工作区/work/2026-10-04-P03/input_drift_20261004";
    [Serializable] public sealed class Sample
    {
        public string time, scene, replay, devices;
        public bool playing, focused, paused, d, a, right, left, newD, newA, newRight, newLeft;
        public float rawHorizontal, rawVertical, gameplayHorizontal, legacyGamepadHorizontal, legacyGamepadVertical;
        public string[] joysticks;
        public Vector2 gamepadLeft, gamepadRight, velocity;
        public Vector3 playerPosition;
    }
    [Serializable] public sealed class Report { public Sample[] samples; }
    static readonly List<Sample> samples=new List<Sample>();
    static double next, until;
    public static Sample Read()
    {
        var p=PersistentRuntime.Instance ? PersistentRuntime.Instance.GetComponent<GameControlManager>()?.ActivePlayer : null;
        var body=p ? p.GetComponent<Rigidbody2D>() : null;
        var keyboard=Keyboard.current;var pad=Gamepad.current;
        return new Sample {
            time=DateTime.Now.ToString("O"),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            playing=EditorApplication.isPlaying,focused=Application.isFocused,paused=GamePause.IsPaused,
            replay=GameplayInput.Replay?.GetType().FullName??"none",
            devices=string.Join(";",InputSystem.devices.Select(x=>x.displayName+" ["+x.layout+"]")),
            d=Input.GetKey(KeyCode.D),a=Input.GetKey(KeyCode.A),right=Input.GetKey(KeyCode.RightArrow),left=Input.GetKey(KeyCode.LeftArrow),
            newD=keyboard!=null&&keyboard.dKey.isPressed,newA=keyboard!=null&&keyboard.aKey.isPressed,
            newRight=keyboard!=null&&keyboard.rightArrowKey.isPressed,newLeft=keyboard!=null&&keyboard.leftArrowKey.isPressed,
            rawHorizontal=Input.GetAxisRaw("Horizontal"),rawVertical=Input.GetAxisRaw("Vertical"),
            gameplayHorizontal=GameplayInput.GetAxisRaw("Horizontal"),joysticks=Input.GetJoystickNames(),legacyGamepadHorizontal=Input.GetAxisRaw("GamepadHorizontal"),legacyGamepadVertical=Input.GetAxisRaw("GamepadVertical"),
            gamepadLeft=pad!=null?pad.leftStick.ReadValue():Vector2.zero,gamepadRight=pad!=null?pad.rightStick.ReadValue():Vector2.zero,
            velocity=body?body.linearVelocity:Vector2.zero,playerPosition=p?p.transform.position:Vector3.zero
        };
    }
    public static void Capture()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required for input diagnostics");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/latest.json",JsonUtility.ToJson(Read(),true));
    }
    public static void Watch()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required for input diagnostics");
        Directory.CreateDirectory(Folder);samples.Clear();next=0;until=EditorApplication.timeSinceStartup+12;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>=until){EditorApplication.update-=Tick;Save();return;}
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.2;samples.Add(Read());Save();
    }
    static void Save()=>File.WriteAllText(Folder+"/observer.json",JsonUtility.ToJson(new Report{samples=samples.ToArray()},true));
}