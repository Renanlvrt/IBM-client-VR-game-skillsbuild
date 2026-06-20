using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton that manages the player's inventory of collected items.
/// Add this to your Player or XR Origin GameObject.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find it in the scene
                _instance = FindObjectOfType<InventoryManager>();

                // If still null, create it!
                if (_instance == null)
                {
                    GameObject go = new GameObject("[InventoryManager]");
                    _instance = go.AddComponent<InventoryManager>();
                    // DontDestroyOnLoad(go); // Optional global persistence
                    Debug.Log("<color=green>[Inventory]</color> Auto-created missing InventoryManager instance.");
                }
            }
            return _instance;
        }
    }

    [Header("Inventory State")]
    public List<ItemData> items = new List<ItemData>();

    [Header("Puzzle Settings")]
    public int requiredItemCount = 7; 

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.Log("<color=yellow>[Inventory]</color> Duplicate InventoryManager found and destroyed.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    public void AddItem(ItemData item, Vector3? spawnPos = null)
    {
        if (item == null) return;
        
        items.Add(item);
        Debug.Log($"<color=green>[Inventory]</color> Added item: {item.itemName} (Total: {items.Count})");

        if (items.Count >= requiredItemCount)
        {
            Debug.Log("<color=green>[Inventory]</color> All items collected! Firing OnAllItemsCollected.");
        }
    }

    /// <summary>
    /// Resets any flags or state related to item collection.
    /// </summary>
    public void ResetCollectedFlag()
    {
        items.Clear();
    }

    /// <summary>
    /// Checks if a specific item (by ID) is in the inventory.
    /// </summary>
    public bool HasItem(string itemID)
    {
        return items.Exists(i => i.itemID == itemID);
    }

    /// <summary>
    /// Returns the last item added to the inventory and removes it.
    /// Returns null if empty.
    /// </summary>
    public ItemData PopItem()
    {
        if (items.Count == 0) return null;
        
        int lastIndex = items.Count - 1;
        ItemData item = items[lastIndex];
        items.RemoveAt(lastIndex);
        return item;
    }
}
