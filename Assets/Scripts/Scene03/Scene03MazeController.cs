using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Scene03MazeController : CameraCaptureContext
{
    public Scene03Chapter chapter;
    public MazeLayoutData[] layouts = new MazeLayoutData[2];
    public Transform mazeSpace, mazeStart, mazeGoal, cameraBounds, riverSpawn;
    public GameObject riverRoot;
    public GameObject[] visuals = new GameObject[2], collisions = new GameObject[2];
    [Min(2)] public float cellSize = 3.2f;
    [Range(.1f,.4f)] public float wallThickness = .2f;
    [Min(6)] public float walkingViewSize = 9;
    [Min(0)] public float previewMargin = 2;
    public UnityEvent onRiverReached = new UnityEvent();
    public bool IsInMaze { get; private set; }
    public bool IsAtRiver { get; private set; }
    public bool OwnsView => IsInMaze || IsAtRiver;
    public override bool CanRaise => IsInMaze && !transitioning;
    public int CurrentLayout { get; private set; }
    public int PreviewLayout => 1 - CurrentLayout;
    public Bounds MazeBounds => new Bounds(mazeSpace.position + new Vector3(MazeLayoutData.Columns * cellSize / 2, MazeLayoutData.Rows * cellSize / 2), new Vector3(MazeLayoutData.Columns * cellSize, MazeLayoutData.Rows * cellSize, 1));
    [HideInInspector, SerializeField] private string bakedDataSignature = "";
    [HideInInspector, SerializeField] private float bakedCellSize = 3.2f;
    string DataSignature => cellSize.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" + wallThickness.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" + string.Join("", layouts[0].horizontalRows) + string.Join("", layouts[0].verticalRows) + string.Join("", layouts[1].horizontalRows) + string.Join("", layouts[1].verticalRows);
    [ContextMenu("Rebuild Cached Maze Walls")]
    public void RebuildCachedWalls()
    {
        if (IsInMaze || layouts.Length != 2 || !layouts[0].IsValid() || !layouts[1].IsValid()) return;
        float ratio = cellSize / bakedCellSize;
        foreach (Transform child in mazeSpace)
            if (child.gameObject != visuals[0] && child.gameObject != visuals[1] && child.gameObject != collisions[0] && child.gameObject != collisions[1])
            { child.localPosition *= ratio; var scale = child.localScale; child.localScale = new Vector3(scale.x * ratio, scale.y * ratio, scale.z); }
        MazeLayoutGeometry.Rebuild(this);
        bakedCellSize = cellSize; bakedDataSignature = DataSignature;
        #if UNITY_EDITOR
        if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(this); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene); }
        #endif
    }
    readonly List<Rect>[] wallRects = { new List<Rect>(), new List<Rect>() };
    Rigidbody2D body; Collider2D bodyCollider; CameraModeController cameraMode;
    bool ready, transitioning, ownsLock;
    float previousSize, previousOffset; bool previousFollowY; Vector2 previousX, previousY;

    IEnumerator Start()
    {
        if (chapter == null || mazeSpace == null || riverRoot == null || riverSpawn == null || layouts.Length != 2 ||
            layouts[0] == null || layouts[1] == null || !layouts[0].IsValid() || !layouts[1].IsValid() ||
            visuals.Length != 2 || collisions.Length != 2 || visuals[0] == null || visuals[1] == null || collisions[0] == null || collisions[1] == null)
        { Debug.LogWarning("Scene03 maze needs two valid layouts and its baked scene references.", this); yield break; }
        mazeSpace.gameObject.SetActive(false); riverRoot.SetActive(false);
        if (bakedDataSignature != DataSignature) RebuildCachedWalls();
        while (!chapter.Ready) yield return null;
        body = chapter.child.GetComponent<Rigidbody2D>(); bodyCollider = chapter.child.GetComponent<Collider2D>();
        cameraMode = PersistentRuntime.Instance.GetComponent<CameraModeController>();
        for (int i = 0; i < 2; i++) wallRects[i].AddRange(layouts[i].WallRects(cellSize, wallThickness));
        ready = true;
        if (chapter.Story.Has(StoryFlag.Scene03MazeEntered) && !chapter.Story.Has(StoryFlag.Scene03ReturnedHome))
        {
            RememberCamera();
            if (chapter.Story.Has(StoryFlag.Scene03MazeCompleted)) ArriveAtRiver();
            else ActivateMaze(true);
        }
    }
    public void Enter()
    {
        if (!ready || transitioning || IsInMaze || chapter.Controls.IsLocked()) return;
        StartCoroutine(EnterRoutine());
    }
    IEnumerator EnterRoutine()
    {
        transitioning = true; Lock(); RememberCamera();
        yield return chapter.Fade(1, .3f);
        bool resume = chapter.Story.Has(StoryFlag.Scene03MazeEntered);
        chapter.Story.Set(StoryFlag.Scene03MazeEntered);
        if (chapter.Story.Has(StoryFlag.Scene03MazeCompleted)) ArriveAtRiver(); else ActivateMaze(resume);
        yield return new WaitForFixedUpdate();
        chapter.follow.SnapToTarget();
        yield return chapter.Fade(0, .4f);
        Unlock(); transitioning = false;
        if (IsInMaze && !chapter.Story.Has(StoryFlag.Scene03MazeFirstSwitch))
            PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("[W/A/S/D] 或方向键移动    [Q] 举起相机", 5);
    }
    void RememberCamera()
    {
        previousSize = Camera.main.orthographicSize;
        previousX = chapter.follow.HorizontalLimits; previousY = chapter.follow.VerticalLimits;
        previousOffset = chapter.follow.VerticalOffset; previousFollowY = chapter.follow.FollowsY;
    }
    void ActivateMaze(bool resume)
    {
        IsInMaze = true; IsAtRiver = false;
        chapter.homeRoot.SetActive(false); chapter.livingRoomRoot.SetActive(false); chapter.streetRoot.SetActive(false);
        riverRoot.SetActive(false); mazeSpace.gameObject.SetActive(true);
        CurrentLayout = chapter.Story.Has(StoryFlag.Scene03MazeCurrentLayout) ? 1 : 0;
        ApplyLayout();
        chapter.child.SetPlanarMovement(true);
        SetPosition(CellCenter(resume ? chapter.Story.Scene03MazeCell : new Vector2Int(14, 0)));
        cameraMode.SetCaptureContext(this); cameraMode.ContextAvailable = true;
        var bounds = MazeBounds;
        chapter.follow.SetLimits(bounds.min.x + walkingViewSize * Camera.main.aspect, bounds.max.x - walkingViewSize * Camera.main.aspect);
        chapter.follow.SetVerticalLimits(true, 0, bounds.min.y + walkingViewSize, bounds.max.y - walkingViewSize);
        Camera.main.orthographicSize = walkingViewSize; chapter.follow.SnapToTarget();
        chapter.neighbours.volume = .012f; chapter.ambience.volume = .055f; chapter.water.volume = .02f;
    }
    public Vector2Int CellAt(Vector2 world)
    {
        Vector2 local = world - (Vector2)mazeSpace.position;
        return new Vector2Int(Mathf.Clamp(MazeLayoutData.Rows - 1 - Mathf.FloorToInt(local.y / cellSize), 0, MazeLayoutData.Rows - 1), Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize), 0, MazeLayoutData.Columns - 1));
    }
    public Vector2 CellCenter(Vector2Int cell) => (Vector2)mazeSpace.position + new Vector2((Mathf.Clamp(cell.y, 0, 19) + .5f) * cellSize, (14 - Mathf.Clamp(cell.x, 0, 14) + .5f) * cellSize);
    public bool WouldOverlap(int layout, Vector2 world, Vector2 halfSize)
    {
        Vector2 local = world - (Vector2)mazeSpace.position;
        var rect = new Rect(local - halfSize, halfSize * 2);
        foreach (var wall in wallRects[layout]) if (wall.Overlaps(rect)) return true;
        return false;
    }
    public Vector2 SafeSwitchPosition(int layout, Vector2 world, Vector2 halfSize)
    {
        if (!WouldOverlap(layout, world, halfSize)) return world;
        Vector2 center = CellCenter(CellAt(world));
        Vector2 reach = Vector2.one * (cellSize / 2 - wallThickness / 2 - .035f) - halfSize;
        return new Vector2(Mathf.Clamp(world.x, center.x - reach.x, center.x + reach.x), Mathf.Clamp(world.y, center.y - reach.y, center.y + reach.y));
    }
    public override void Preview(Camera camera)
    {
        if (!chapter.Story.Has(StoryFlag.Scene03MazeFirstSwitch)) PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("", 0);
        visuals[CurrentLayout].SetActive(false); visuals[PreviewLayout].SetActive(true);
        var bounds = MazeBounds;
        camera.orthographicSize = Mathf.Max(bounds.extents.y + previewMargin, (bounds.extents.x + previewMargin) / Mathf.Max(.1f, camera.aspect));
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, camera.transform.position.z);
    }
    public override void CancelPreview(Camera camera)
    {
        if (!IsInMaze || camera == null) return;
        visuals[CurrentLayout].SetActive(true); visuals[PreviewLayout].SetActive(false);
        camera.orthographicSize = walkingViewSize; chapter.follow.SnapToTarget();
    }
    public override void CommitCapture(Camera camera)
    {
        if (!IsInMaze) return;
        body.linearVelocity = Vector2.zero;
        int next = PreviewLayout;
        Vector2 safe = SafeSwitchPosition(next, body.position, bodyCollider.bounds.extents);
        collisions[CurrentLayout].SetActive(false);
        if ((safe - body.position).sqrMagnitude > .000001f) SetPosition(safe);
        CurrentLayout = next; ApplyLayout(); Physics2D.SyncTransforms();
        chapter.Story.Set(StoryFlag.Scene03MazeCurrentLayout, CurrentLayout == 1);
        chapter.Story.Set(StoryFlag.Scene03MazeFirstSwitch);
        chapter.Story.SetScene03MazeCell(CellAt(body.position));
    }
    void ApplyLayout()
    {
        for (int i = 0; i < 2; i++) { visuals[i].SetActive(i == CurrentLayout); collisions[i].SetActive(i == CurrentLayout); }
    }
    void Update()
    {
        if (!ready || !IsInMaze || transitioning || chapter.Controls.IsLocked()) return;
        Vector2Int cell = CellAt(body.position); chapter.Story.SetScene03MazeCell(cell);
        if (cell == new Vector2Int(0, 19) && Vector2.Distance(body.position, (Vector2)mazeGoal.position) < cellSize * .36f)
            StartCoroutine(ExitRoutine());
    }
    IEnumerator ExitRoutine()
    {
        transitioning = true; Lock(); body.linearVelocity = Vector2.zero;
        chapter.Story.Set(StoryFlag.Scene03MazeCompleted);
        yield return chapter.Fade(1, .3f); ArriveAtRiver();
        yield return new WaitForFixedUpdate(); chapter.follow.SnapToTarget();
        yield return chapter.Fade(0, .4f); Unlock(); transitioning = false; onRiverReached.Invoke();
    }
    void ArriveAtRiver()
    {
        cameraMode.SetCaptureContext(null); IsInMaze = false; IsAtRiver = true;
        chapter.child.SetPlanarMovement(false); mazeSpace.gameObject.SetActive(false);
        chapter.homeRoot.SetActive(false); chapter.livingRoomRoot.SetActive(false); chapter.streetRoot.SetActive(false);
        riverRoot.SetActive(true); SetPosition(riverSpawn.position);
        Camera.main.orthographicSize = previousSize;
        chapter.follow.SetLimits(riverSpawn.position.x + 4, riverSpawn.position.x + 22);
        chapter.follow.SetVerticalLimits(previousFollowY, previousOffset, riverSpawn.position.y - 5, riverSpawn.position.y + 5);
        chapter.follow.SnapToTarget(); chapter.neighbours.volume = 0; chapter.ambience.volume = .09f; chapter.water.volume = .18f;
    }
    public void ReleaseToHome()
    {
        cameraMode.SetCaptureContext(null); IsInMaze=false; IsAtRiver=false;
        chapter.child.SetPlanarMovement(false); mazeSpace.gameObject.SetActive(false); riverRoot.SetActive(false);
        chapter.follow.SetVerticalLimits(previousFollowY,previousOffset,previousY.x,previousY.y);
    }
    void SetPosition(Vector2 value) { body.linearVelocity = Vector2.zero; body.position = value; chapter.child.transform.position = value; }
    void Lock() { chapter.Controls.AcquireLock(ControlLockReason.Interaction); ownsLock = true; }
    void Unlock() { if (ownsLock && chapter != null && chapter.Controls != null) chapter.Controls.ReleaseLock(ControlLockReason.Interaction); ownsLock = false; }
    void OnDisable()
    {
        StopAllCoroutines(); Unlock();
        if (IsInMaze && chapter != null && chapter.child != null)
        {
            if (cameraMode != null) cameraMode.SetCaptureContext(null);
            chapter.child.SetPlanarMovement(false);
            if (chapter.follow != null) { chapter.follow.SetLimits(previousX.x, previousX.y); chapter.follow.SetVerticalLimits(previousFollowY, previousOffset, previousY.x, previousY.y); }
        }
        IsInMaze = false;
    }
}
