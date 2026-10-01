using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InventoryUI : MonoBehaviour
{
    private enum InventoryTab
    {
        Items,
        KeyItems,
        Files,
        Outfits
    }

    [Header("Main")]
    [SerializeField]
    private GameObject inventoryPanel;

    [Tooltip("Fit legacy QHD pixel offsets to smaller windows; custom CanvasScaler modes are preserved.")]
    [SerializeField] private bool fitLegacyCanvasToScreen = true;

    [Header("Managers")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private FileArchiveManager fileArchiveManager;

    [Header("Game Control")]
    [SerializeField]
    private GameControlManager gameControlManager;

    [Header("Tabs")]
    [SerializeField]
    private Button itemsTabButton;

    [SerializeField]
    private Button keyItemsTabButton;

    [SerializeField]
    private Button filesTabButton;

    [Header("Pages")]
    [SerializeField]
    private GameObject itemsPage;

    [SerializeField]
    private GameObject filesPage;

    [Header("Items UI")]
    [SerializeField]
    private Transform itemsListContainer;

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private TMP_Text itemDescriptionText;

    [SerializeField]
    private Image itemPreviewImage;

    [Header("Files UI")]
    [SerializeField]
    private Transform filesListContainer;

    [SerializeField]
    private TMP_Text fileNameText;

    [SerializeField]
    private TMP_Text fileContentText;

    [SerializeField]
    private Image filePreviewImage;

    [Header("Template")]
    [SerializeField]
    private Button entryButtonTemplate;

    [Header("Adult Outfits")]
    [SerializeField] private Button outfitsTabButton;
    [SerializeField] private GameObject outfitsPage;

    private InventoryTab currentTab =
        InventoryTab.Items;

    private readonly List<GameObject>
        spawnedItemEntries =
            new List<GameObject>();

    private readonly List<GameObject>
        spawnedFileEntries =
            new List<GameObject>();

    private void Start()
    {
        if (fitLegacyCanvasToScreen) RuntimePanelUI.FitLegacyCanvas(inventoryPanel);
        ConfigureArtLayout();
        if(outfitsTabButton!=null)outfitsTabButton.onClick.AddListener(ShowOutfits);
        if(outfitsPage!=null)outfitsPage.SetActive(false);
        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.AddListener(
                ShowItems
            );
        }

        if (keyItemsTabButton != null)
        {
            keyItemsTabButton.onClick.AddListener(
                ShowKeyItems
            );
        }

        if (filesTabButton != null)
        {
            filesTabButton.onClick.AddListener(
                ShowFiles
            );
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        currentTab =
            InventoryTab.Items;
    }

    private void Update()
    {
        if (GameplayInput.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool isCurrentlyOpen =
            inventoryPanel.activeSelf;

        if (!isCurrentlyOpen)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }

    private void OpenInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        if (!MayOpen())
        {
            return;
        }

        if(outfitsTabButton!=null)outfitsTabButton.interactable=gameControlManager==null || gameControlManager.AllowsAdultOutfits;
        if(gameControlManager!=null && !gameControlManager.AllowsAdultOutfits && currentTab==InventoryTab.Outfits)currentTab=InventoryTab.Items;
        inventoryPanel.SetActive(true);

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Inventory
            );
        }

        switch (currentTab)
        {
            case InventoryTab.Items:
                ShowItems();
                break;

            case InventoryTab.KeyItems:
                ShowKeyItems();
                break;

            case InventoryTab.Outfits:
                ShowOutfits();
                break;

            case InventoryTab.Files:
                ShowFiles();
                break;
        }
        Opened?.Invoke();
    }

    public void CloseInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool wasOpen=IsOpen;
        ClearCombinationSelection();
        inventoryPanel.SetActive(false);

        if (gameControlManager != null)
        {
            gameControlManager.ReleaseLock(
                ControlLockReason.Inventory
            );
        }
        if(wasOpen)Closed?.Invoke();
    }

    private void ShowItems()
    {
        ClearCombinationSelection();
        currentTab =
            InventoryTab.Items;

        ShowItemsPage();
        RefreshItems();
        ClearItemDetails();
    }

    private void ShowKeyItems()
    {
        ClearCombinationSelection();
        currentTab =
            InventoryTab.KeyItems;

        ShowItemsPage();
        RefreshItems();
        ClearItemDetails();
    }

    private void ShowOutfits()
    {
        ClearCombinationSelection();
        if(gameControlManager!=null && !gameControlManager.AllowsAdultOutfits)return;
        currentTab=InventoryTab.Outfits;
        if(itemsPage!=null)itemsPage.SetActive(false);
        if(filesPage!=null)filesPage.SetActive(false);
        if(outfitsPage!=null)outfitsPage.SetActive(true);
    }

    private void ShowFiles()
    {
        ClearCombinationSelection();
        if(outfitsPage!=null)outfitsPage.SetActive(false);
        currentTab =
            InventoryTab.Files;

        if (itemsPage != null)
        {
            itemsPage.SetActive(false);
        }

        if (filesPage != null)
        {
            filesPage.SetActive(true);
        }

        RefreshFiles();
        ClearFileDetails();
    }

    private void ShowItemsPage()
    {
        if(outfitsPage!=null)outfitsPage.SetActive(false);
        if (itemsPage != null)
        {
            itemsPage.SetActive(true);
        }

        if (filesPage != null)
        {
            filesPage.SetActive(false);
        }
    }

    private void RefreshItems()
    {
        ClearItemEntries();

        if (inventoryManager == null)
        {
            return;
        }

        foreach (ItemData item
                 in inventoryManager.Items)
        {
            if (item == null)
            {
                continue;
            }

            if (!ShouldShowItem(item))
            {
                continue;
            }

            CreateItemEntry(item);
        }
    }

    private bool ShouldShowItem(
        ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        if (currentTab ==
            InventoryTab.Items)
        {
            return item.category ==
                ItemCategory.Regular;
        }

        if (currentTab ==
            InventoryTab.KeyItems)
        {
            return item.category ==
                ItemCategory.KeyItem;
        }

        return false;
    }

    private void RefreshFiles()
    {
        ClearFileEntries();

        if (fileArchiveManager == null)
        {
            return;
        }

        foreach (FileData file
                 in fileArchiveManager.Files)
        {
            if (file == null)
            {
                continue;
            }

            CreateFileEntry(file);
        }
    }

    private void CreateItemEntry(
        ItemData item)
    {
        if (entryButtonTemplate == null ||
            itemsListContainer == null)
        {
            return;
        }

        Button newButton =
            Instantiate(
                entryButtonTemplate,
                itemsListContainer
            );

        newButton.gameObject.SetActive(true);

        TMP_Text buttonText =
            newButton.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
        {
            BindEntryLabel(buttonText, inventoryManager.DisplayName(item));
        }

        newButton.onClick.AddListener(
            () => ShowItemDetails(item)
        );
        BindCombination(item,newButton);
        DecorateArtEntry(newButton,item.icon);

        spawnedItemEntries.Add(
            newButton.gameObject
        );
    }

    private void CreateFileEntry(
        FileData file)
    {
        if (entryButtonTemplate == null ||
            filesListContainer == null)
        {
            return;
        }

        Button newButton =
            Instantiate(
                entryButtonTemplate,
                filesListContainer
            );

        newButton.gameObject.SetActive(true);

        TMP_Text buttonText =
            newButton.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
        {
            BindEntryLabel(buttonText, file.fileName);
        }

        newButton.onClick.AddListener(
            () => ShowFileDetails(file)
        );

        DecorateArtEntry(newButton,file.icon);
        spawnedFileEntries.Add(
            newButton.gameObject
        );
    }

    private static void BindEntryLabel(TMP_Text label, string value)
    {
        // Preserve Inspector styling; only repair a material from a different font atlas.
        if (label.font != null && (label.fontSharedMaterial == null ||
            label.fontSharedMaterial.mainTexture != label.font.atlasTexture))
            label.fontSharedMaterial = label.font.material;
        label.gameObject.SetActive(true);
        label.enabled = true;
        label.text = value;
    }

    private void ShowItemDetails(
        ItemData item)
    {
        if (item == null)
        {
            return;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                inventoryManager.DisplayName(item);
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                inventoryManager.DisplayDescription(item);
        }

        if (itemPreviewImage != null)
        {
            itemPreviewImage.sprite =
                item.icon;

            itemPreviewImage.enabled =
                item.icon != null;
        }
    }

    private void ShowFileDetails(
        FileData file)
    {
        if (file == null)
        {
            return;
        }

        if (fileNameText != null)
        {
            fileNameText.text =
                file.fileName;
        }

        if (fileContentText != null)
        {
            fileContentText.text =
                file.summary +
                "\n\n" +
                file.content;
        }

        if (filePreviewImage != null)
        {
            filePreviewImage.preserveAspect = true;
            filePreviewImage.sprite =
                file.icon;

            filePreviewImage.enabled =
                file.icon != null;
        }
        FitFileReading();
    }

    private void ClearItemEntries()
    {
        materialButtons.Clear();
        foreach (GameObject entry
                 in spawnedItemEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }

        spawnedItemEntries.Clear();
    }

    private void ClearFileEntries()
    {
        foreach (GameObject entry
                 in spawnedFileEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }

        spawnedFileEntries.Clear();
    }

    private void ClearItemDetails()
    {
        if (itemNameText != null)
        {
            itemNameText.text =
                "选择一件物品";
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                "";
        }

        if (itemPreviewImage != null)
        {
            itemPreviewImage.enabled =
                false;
        }
    }

    private void ClearFileDetails()
    {
        if (fileNameText != null)
        {
            fileNameText.text =
                "选择一份文件";
        }

        if (fileContentText != null)
        {
            fileContentText.text =
                "";
        }

        if (filePreviewImage != null)
        {
            filePreviewImage.enabled =
                false;
        }
    }
}
