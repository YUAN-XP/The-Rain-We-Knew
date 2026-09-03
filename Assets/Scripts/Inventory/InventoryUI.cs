using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private enum InventoryTab
    {
        Items,
        Files
    }

    [Header("Main")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Managers")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private FileArchiveManager fileArchiveManager;

    [Header("Game Control")]
    [SerializeField] private GameControlManager gameControlManager;

    [Header("Tabs")]
    [SerializeField] private Button itemsTabButton;
    [SerializeField] private Button filesTabButton;

    [Header("Pages")]
    [SerializeField] private GameObject itemsPage;
    [SerializeField] private GameObject filesPage;

    [Header("Items UI")]
    [SerializeField] private Transform itemsListContainer;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image itemPreviewImage;

    [Header("Files UI")]
    [SerializeField] private Transform filesListContainer;
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private TMP_Text fileContentText;
    [SerializeField] private Image filePreviewImage;

    [Header("Template")]
    [SerializeField] private Button entryButtonTemplate;

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
        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.AddListener(
                ShowItems
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
        if (Input.GetKeyDown(KeyCode.Tab))
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

        if (gameControlManager != null &&
            gameControlManager.IsLockedBy(
                ControlLockReason.Dialogue
            ))
        {
            return;
        }

        inventoryPanel.SetActive(true);

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Inventory
            );
        }

        if (currentTab ==
            InventoryTab.Items)
        {
            ShowItems();
        }
        else
        {
            ShowFiles();
        }
    }

    private void CloseInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        inventoryPanel.SetActive(false);

        if (gameControlManager != null)
        {
            gameControlManager.ReleaseLock(
                ControlLockReason.Inventory
            );
        }
    }

    private void ShowItems()
    {
        currentTab =
            InventoryTab.Items;

        if (itemsPage != null)
        {
            itemsPage.SetActive(true);
        }

        if (filesPage != null)
        {
            filesPage.SetActive(false);
        }

        RefreshItems();
        ClearItemDetails();
    }

    private void ShowFiles()
    {
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

            CreateItemEntry(item);
        }
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
            newButton.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text =
                item.itemName;
        }

        newButton.onClick.AddListener(
            () => ShowItemDetails(item)
        );

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
            newButton.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text =
                file.fileName;
        }

        newButton.onClick.AddListener(
            () => ShowFileDetails(file)
        );

        spawnedFileEntries.Add(
            newButton.gameObject
        );
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
                item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                item.description;
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
            filePreviewImage.sprite =
                file.icon;

            filePreviewImage.enabled =
                file.icon != null;
        }
    }

    private void ClearItemEntries()
    {
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
                "Select an item";
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
                "Select a file";
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