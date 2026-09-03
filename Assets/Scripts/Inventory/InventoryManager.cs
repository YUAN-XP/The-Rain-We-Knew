using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Runtime Inventory")]
    [SerializeField]
    private List<ItemData> items =
        new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;

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

        if (items.Remove(item))
        {
            Debug.Log(
                "Inventory: Removed item - "
                + item.itemName
            );
        }
    }
}