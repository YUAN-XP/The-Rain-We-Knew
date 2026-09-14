using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Runtime Inventory")]
    [SerializeField]
    private List<ItemData> items =
        new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;
    private readonly Dictionary<ItemData, (string name, string description)> presentation
        = new Dictionary<ItemData, (string name, string description)>();
    // Session presentation only; the authored ItemData and its identity never change.
    public void SetRuntimePresentation(ItemData item, string name, string description)
    {
        if (item != null && HasItem(item)) presentation[item] = (name, description);
    }
    public string DisplayName(ItemData item) => item != null && presentation.TryGetValue(item, out var value) ? value.name : item != null ? item.itemName : "";
    public string DisplayDescription(ItemData item) => item != null && presentation.TryGetValue(item, out var value) ? value.description : item != null ? item.description : "";

    public void AddItem(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        if (HasItem(item))
        {
            Debug.Log(
                "Inventory: Item already exists - "
                + item.itemName
            );

            return;
        }

        items.Add(item);

        Debug.Log(
            "Inventory: Added item - "
            + item.itemName
        );
    }

    public bool HasItem(ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        return items.Contains(item);
    }

    public bool HasItemById(string itemId)
    {
        foreach (ItemData item in items)
        {
            if (item != null &&
                item.itemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        if (item.isPermanent)
        {
            Debug.Log(
                "Inventory: Permanent item cannot be removed - "
                + item.itemName
            );

            return;
        }

        if (items.Remove(item))
        {
            Debug.Log(
                "Inventory: Removed item - "
                + item.itemName
            );
        }
    }
}