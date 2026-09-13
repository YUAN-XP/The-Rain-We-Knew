using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-local desktop close-up. Coverage survives Esc/reopening while this component lives.
// Story facts, inventory ownership, white flash and actor handover belong to the chapter.
public class PencilDeskCloseup : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] TMP_FontAsset font;
    [SerializeField] GameControlManager controls;
    [Header("Alpha pencil tuning; pixels in the fixed 600 x 240 writing surface")]
    [SerializeField, Range(4, 36)] float brushRadius = 15;
    [SerializeField, Range(.45f, .95f)] float perCharacterCoverage = .72f;
    [SerializeField, Min(.1f)] float readableNameHold = 1.15f;
    [SerializeField, Min(.1f)] float grabbingDuration = .55f;
    [SerializeField, Min(.1f)] float grabbedHold = .4f;

    const int Width = 600, Height = 240;
    RectTransform root, desktop, writingSurface;
    RawImage revealedName;
    TMP_Text ownName, investigation, instruction, glyphTemplate;
    PencilDeskHandsGraphic hands;
    Button closeButton;
    Texture2D revealTexture;
    Color32[] inkPixels;
    sbyte[] pixelOwner;
    bool[] covered;
    readonly int[] totals = new int[2];
    readonly int[] counts = new int[2];
    bool ownDesk, hasPencil, ownsLock, inputArmed, drawing, completing, dispatched, warned;
    bool dirty, maskReady;
    Vector2 previousPixel;
    Coroutine sequence;

    public event Action Completed;
    public event Action Closed;
    public bool IsOpen => root != null && root.gameObject.activeSelf;
    public bool IsPlayingGrab => completing;
    public bool HasCompleted => dispatched;
    public bool CanRub => IsOpen && !ownDesk && hasPencil && maskReady && !completing && !dispatched;
    public float FirstCharacterCoverage => totals[0] == 0 ? 0 : (float)counts[0] / totals[0];
    public float SecondCharacterCoverage => totals[1] == 0 ? 0 : (float)counts[1] / totals[1];
    public Vector2 WristScreenPosition => hands == null ? Vector2.zero : hands.WristScreenPosition;

    public void Initialize(TMP_FontAsset chineseFont, GameControlManager controlManager)
    {
        font = chineseFont;
        controls = controlManager;
    }

    public bool Open(bool ownDesk, bool hasPencil)
    {
        if (IsOpen || (!ownDesk && dispatched)) return false;
        if (controls == null)
            controls = FindAnyObjectByType<GameControlManager>(FindObjectsInactive.Include);
        if (font == null || controls == null)
        {
            WarnOnce("PencilDeskCloseup needs its Chinese TMP font and GameControlManager.");
            return false;
        }
        // Do not steal a lock belonging to dialogue, inventory, tools or another close-up.
        if (controls.IsLocked()) return false;
        if (root == null) Build();
        this.ownDesk = ownDesk;
        this.hasPencil = hasPencil;
        root.gameObject.SetActive(true);
        ownName.gameObject.SetActive(ownDesk);
        revealedName.gameObject.SetActive(!ownDesk);
        hands.gameObject.SetActive(!ownDesk && hasPencil);
        hands.GrabProgress = 0;
        hands.TipPosition = new Vector2(520, 235);
        investigation.text = ownDesk ? "这是我的座位？" : hasPencil
            ? "桌面有淡淡的划痕，应该可以用刚刚这支铅笔摹出来"
            : "桌面有淡淡的划痕，应该可以用什么东西摹出来";
        instruction.text = !ownDesk && hasPencil ? "按住鼠标左键，在刻痕上移动铅笔" : "";
        closeButton.interactable = true;
        Canvas.ForceUpdateCanvases();
        if (!ownDesk && hasPencil && !maskReady && !BuildGlyphMask())
        {
            root.gameObject.SetActive(false);
            return false;
        }
        controls.AcquireLock(ControlLockReason.CloseUp);
        ownsLock = true;
        inputArmed = !Input.GetMouseButton(0);
        drawing = false;
        if (!ownDesk && hasPencil && CoverageReached()) StartCompletion();
        return true;
    }

    void Build()
    {
        root = RuntimePanelUI.Canvas("PencilDeskCloseupCanvas", transform, 145);
        RuntimePanelUI.Panel("DimClassroom", root, Vector2.zero, Vector2.one, new Color(.045f, .047f, .049f, .98f));
        RuntimePanelUI.Panel("DeskShadow", root, new Vector2(.117f, .205f), new Vector2(.897f, .812f), new Color(.025f, .022f, .02f));
        desktop = RuntimePanelUI.Panel("WornWoodDesktop", root, new Vector2(.11f, .22f), new Vector2(.89f, .83f), new Color(.48f, .34f, .215f)).rectTransform;
        desktop.gameObject.AddComponent<RectMask2D>();
        var art = RuntimePanelUI.Rect("WoodgrainAndFaintMarks", desktop, Vector2.zero, Vector2.one).gameObject.AddComponent<PencilDeskSurfaceGraphic>();
        art.raycastTarget = false;
        writingSurface = RuntimePanelUI.Rect("EngravingSurface", desktop, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        writingSurface.sizeDelta = new Vector2(Width, Height);
        writingSurface.anchoredPosition = new Vector2(0, 28);
        revealedName = RuntimePanelUI.Rect("GraphiteOnlyWhereRubbed", writingSurface, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        revealedName.raycastTarget = false;
        ownName = RuntimePanelUI.Text("肖鹏", writingSurface, font, Vector2.zero, Vector2.one, 150);
        ownName.color = new Color(.19f, .12f, .074f);
        glyphTemplate = RuntimePanelUI.Text("郑涛", writingSurface, font, Vector2.zero, Vector2.one, 150);
        glyphTemplate.characterSpacing = 15;
        // Transparent vertex colors survive CanvasRenderer rebuild/clear, including TMP submeshes.
        // The mask reads positions, UVs and atlas alpha, never these visible vertex colors.
        glyphTemplate.color = new Color(1, 1, 1, 0);
        // This TMP object is the glyph/layout source, never a visible answer label.
        glyphTemplate.canvasRenderer.SetAlpha(0);
        var handRect = RuntimePanelUI.Rect("PencilAndGrabbingHand", desktop, Vector2.zero, Vector2.one);
        hands = handRect.gameObject.AddComponent<PencilDeskHandsGraphic>();
        hands.raycastTarget = false;
        investigation = RuntimePanelUI.Text("", root, font, new Vector2(.12f, .12f), new Vector2(.88f, .20f), 29);
        instruction = RuntimePanelUI.Text("", root, font, new Vector2(.24f, .055f), new Vector2(.76f, .12f), 23);
        instruction.color = new Color(.67f, .66f, .60f);
        closeButton = RuntimePanelUI.Button("[Esc] 退出", root, font, new Vector2(.78f, .055f), new Vector2(.91f, .12f), Close);
        root.gameObject.SetActive(false);
    }

    bool BuildGlyphMask()
    {
        glyphTemplate.gameObject.SetActive(true);
        glyphTemplate.ForceMeshUpdate(true, true);
        glyphTemplate.canvasRenderer.SetAlpha(0);
        var info = glyphTemplate.textInfo;
        if (info.characterCount != 2)
        {
            WarnOnce("PencilDeskCloseup could not build the two Chinese glyphs.");
            return false;
        }
        pixelOwner = new sbyte[Width * Height];
        for (int i = 0; i < pixelOwner.Length; i++) pixelOwner[i] = -1;
        covered = new bool[pixelOwner.Length];
        inkPixels = new Color32[pixelOwner.Length];
        totals[0] = totals[1] = counts[0] = counts[1] = 0;
        for (int c = 0; c < 2; c++)
        {
            var character = info.characterInfo[c];
            if (!character.isVisible || character.textElement == null || character.textElement.unicode != "郑涛"[c])
            {
                WarnOnce("PencilDeskCloseup font is missing a required Chinese glyph; no completion fallback was used.");
                return false;
            }
            var mesh = info.meshInfo[character.materialReferenceIndex];
            int v = character.vertexIndex;
            Vector2 lo = mesh.vertices[v], hi = mesh.vertices[v + 2];
            var uvLo = mesh.uvs0[v];
            var uvHi = mesh.uvs0[v + 2];
            var atlas = character.fontAsset.atlasTextures[character.textElement.glyph.atlasIndex];
            Color32[] pixels = ReadAtlas(atlas);
            if (pixels == null) return false;
            float rw = hi.x - lo.x, rh = hi.y - lo.y;
            if (rw <= 0 || rh <= 0) return false;
            // TMP uses a center-origin RectTransform. Raster positions map one-to-one to this surface.
            int x0 = Mathf.Clamp(Mathf.FloorToInt(lo.x + Width * .5f), 0, Width - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(hi.x + Width * .5f), 0, Width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(lo.y + Height * .5f), 0, Height - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(hi.y + Height * .5f), 0, Height - 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = Mathf.Lerp(uvLo.x, uvHi.x, (x + .5f - Width * .5f - lo.x) / rw);
                float vv = Mathf.Lerp(uvLo.y, uvHi.y, (y + .5f - Height * .5f - lo.y) / rh);
                int ax = Mathf.Clamp(Mathf.FloorToInt(u * atlas.width), 0, atlas.width - 1);
                int ay = Mathf.Clamp(Mathf.FloorToInt(vv * atlas.height), 0, atlas.height - 1);
                // TMP SDF's 0.5 contour is the actual glyph stroke, not its rectangular bounds.
                if (pixels[ay * atlas.width + ax].a < 128) continue;
                int index = y * Width + x;
                pixelOwner[index] = (sbyte)c;
                totals[c]++;
            }
        }
        glyphTemplate.gameObject.SetActive(false);
        if (totals[0] < 30 || totals[1] < 30)
        {
            WarnOnce("PencilDeskCloseup glyph mask was empty; check TMP atlas format/font wiring.");
            return false;
        }
        revealTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        revealTexture.name = "PencilDesk_RuntimeCoverage";
        revealTexture.filterMode = FilterMode.Bilinear;
        revealTexture.wrapMode = TextureWrapMode.Clamp;
        revealedName.texture = revealTexture;
        revealTexture.SetPixels32(inkPixels);
        revealTexture.Apply(false);
        maskReady = true;
        return true;
    }

    Color32[] ReadAtlas(Texture2D atlas)
    {
        if (atlas == null) return null;
        if (atlas.isReadable) return atlas.GetPixels32();
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D copy = null;
        try
        {
            Graphics.Blit(atlas, target);
            RenderTexture.active = target;
            copy = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
            copy.Apply(false);
            return copy.GetPixels32();
        }
        catch (Exception exception)
        {
            WarnOnce("PencilDeskCloseup could not read its TMP glyph atlas: " + exception.Message);
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (copy != null) Destroy(copy);
        }
    }

    void Update()
    {
        if (!IsOpen || completing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (!CanRub) return;
        if (!Input.GetMouseButton(0)) { inputArmed = true; drawing = false; }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop, Input.mousePosition, null, out var deskPoint)) return;
        var deskRect = desktop.rect;
        hands.TipPosition = new Vector2(
            Mathf.Clamp01((deskPoint.x - deskRect.xMin) / deskRect.width) * 1000,
            Mathf.Clamp01((deskPoint.y - deskRect.yMin) / deskRect.height) * 500);
        if (!inputArmed || !Input.GetMouseButton(0)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(writingSurface, Input.mousePosition, null, out var local)) return;
        var point = local + new Vector2(Width * .5f, Height * .5f);
        if (Input.GetMouseButtonDown(0)) { drawing = true; previousPixel = point; }
        if (!drawing) return;
        // Movement is sampled between frames. Holding still can reveal one brush footprint only.
        float distance = Vector2.Distance(previousPixel, point);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(1, brushRadius * .35f)));
        for (int i = 0; i <= steps; i++) Stamp(Vector2.Lerp(previousPixel, point, (float)i / steps));
        previousPixel = point;
        if (dirty)
        {
            revealTexture.SetPixels32(inkPixels);
            revealTexture.Apply(false);
            dirty = false;
        }
        if (CoverageReached()) StartCompletion();
    }

    void Stamp(Vector2 point)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(point.x - brushRadius));
        int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(point.x + brushRadius));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(point.y - brushRadius));
        int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(point.y + brushRadius));
        float radiusSquared = brushRadius * brushRadius;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int index = y * Width + x;
            int owner = pixelOwner[index];
            if (owner < 0 || covered[index]) continue;
            if ((new Vector2(x + .5f, y + .5f) - point).sqrMagnitude > radiusSquared) continue;
            covered[index] = true;
            counts[owner]++;
            inkPixels[index] = new Color32(42, 31, 23, 242);
            dirty = true;
        }
    }

    bool CoverageReached() => maskReady && FirstCharacterCoverage >= perCharacterCoverage && SecondCharacterCoverage >= perCharacterCoverage;

    void StartCompletion()
    {
        if (completing || dispatched) return;
        completing = true;
        drawing = false;
        closeButton.interactable = false;
        instruction.text = "";
        investigation.text = "";
        for (int i = 0; i < pixelOwner.Length; i++)
            if (pixelOwner[i] >= 0) inkPixels[i] = new Color32(42, 31, 23, 255);
        revealTexture.SetPixels32(inkPixels);
        revealTexture.Apply(false);
        sequence = StartCoroutine(ShowNameThenGrab());
    }

    IEnumerator ShowNameThenGrab()
    {
        yield return new WaitForSecondsRealtime(readableNameHold);
        for (float time = 0; time < grabbingDuration; time += Time.unscaledDeltaTime)
        {
            hands.GrabProgress = Mathf.SmoothStep(0, 1, time / grabbingDuration);
            yield return null;
        }
        hands.GrabProgress = 1;
        yield return new WaitForSecondsRealtime(grabbedHold);
        sequence = null;
        dispatched = true;
        // Keep the close-up and its lock until the chapter has acquired its own transition lock.
        if (Completed != null) Completed.Invoke();
        else
        {
            WarnOnce("PencilDeskCloseup completion has no chapter handover listener.");
            Close();
        }
    }

    public void Close()
    {
        if (sequence != null) { StopCoroutine(sequence); sequence = null; }
        bool wasOpen = IsOpen;
        completing = false;
        drawing = false;
        if (root != null) root.gameObject.SetActive(false);
        if (ownsLock && controls != null) controls.ReleaseLock(ControlLockReason.CloseUp);
        ownsLock = false;
        if (wasOpen) Closed?.Invoke();
    }

    void WarnOnce(string message)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning(message, this);
    }

    void OnDisable() => Close();
    void OnDestroy()
    {
        Close();
        if (revealTexture != null) Destroy(revealTexture);
    }
}
