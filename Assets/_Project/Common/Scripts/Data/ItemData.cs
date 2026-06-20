using UnityEngine;

/// <summary>
/// Persistent data for a collectible item.
/// Create instances of this using 'Create/Items/ItemData' in the Project view.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Item Identity")]
    public string itemName = "New Item";
    public string itemID = "item_001";
    
    [Header("Visuals & Lore")]
    public Sprite icon;
    public Color itemColor = Color.white;
    public string itemBiome = "General"; // Volcano, Forest, Garden, Farm, Labyrinth
    [TextArea(3, 10)]
    public string lore = "Lore description of the item goes here.";
}
