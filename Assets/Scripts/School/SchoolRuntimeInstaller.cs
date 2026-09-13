using UnityEngine;

// Extends the surviving runtime when entering a chapter. Scene01's serialized
// runtime remains intact; direct chapter starts use the same configured prefab.
public class SchoolRuntimeInstaller : MonoBehaviour
{
    public ItemData oilLamp;
    [Header("Optional on the older lamp-only installer; bind both at the school")]
    public OutfitData schoolUniform;
    public GameObject schoolUniformVisualPrefab;
    const string AdultVisualName = "SchoolUniformRuntimeVisual";
    bool warned;

    void Start() { Install(); }

    // Idempotent installation only: neither ownership nor selected outfit is
    // changed here. Rewards remain SchoolSceneController's responsibility.
    public void Install()
    {
        var runtime = PersistentRuntime.Instance;
        if (runtime == null) { Warn("School tools require the existing PersistentRuntime."); return; }
        var lamp = runtime.GetComponent<OilLampController>();
        if (lamp == null) lamp = runtime.gameObject.AddComponent<OilLampController>();
        lamp.ConfigureItem(oilLamp);

        // Existing Scene02 installers can keep their original lamp-only setup.
        if (schoolUniform == null && schoolUniformVisualPrefab == null) return;
        if (schoolUniform == null || schoolUniformVisualPrefab == null
            || schoolUniform.outfit != AdultOutfit.SchoolUniform || schoolUniform.unlockFlag != StoryFlag.SchoolUniformOwned)
        { Warn("SchoolRuntimeInstaller needs the adult SchoolUniform OutfitData and its visual prefab together."); return; }
        if (schoolUniformVisualPrefab.scene.IsValid()
            || schoolUniformVisualPrefab.GetComponentInChildren<PlayerController>(true) != null
            || schoolUniformVisualPrefab.GetComponentInChildren<AdultOutfitController>(true) != null)
        { Warn("School uniform visual must be an art-only prefab asset, without an actor or outfit controller."); return; }

        var page = runtime.GetComponentInChildren<OutfitInventoryPage>(true);
        if (page != null) page.RegisterOutfit(schoolUniform);
        else Warn("The surviving runtime has no OutfitInventoryPage for school uniform registration.");

        // Resolve the original adult component beneath the persistent root,
        // including when hidden. ActivePlayer may already be a child or youth.
        // Never add AdultOutfitController to the current scene actor.
        var adultControllers = runtime.GetComponentsInChildren<AdultOutfitController>(true);
        if (adultControllers.Length != 1)
        { Warn("School uniform registration needs exactly one original AdultOutfitController beneath PersistentRuntime."); return; }
        var originalAdult = adultControllers[0];
        if (originalAdult.GetComponentInParent<PlayerController>(true) == null)
        { Warn("Original adult outfit controller has no player parent; uniform visual was not installed."); return; }

        var existing = originalAdult.transform.Find(AdultVisualName);
        GameObject visual;
        if (existing != null) visual = existing.gameObject;
        else
        {
            visual = Instantiate(schoolUniformVisualPrefab, originalAdult.transform, false);
            visual.name = AdultVisualName;
        }
        visual.SetActive(false);
        originalAdult.RegisterVisual(AdultOutfit.SchoolUniform, visual);
        // The instance belongs to the persistent adult, so scene unload and
        // youth handover keep its original outfit selection and registration.
    }

    void Warn(string message)
    { if (!warned) { warned = true; Debug.LogWarning(message, this); } }
}
