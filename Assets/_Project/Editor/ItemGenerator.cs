using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemGenerator : EditorWindow
{
    [MenuItem("Tools/Cauldron/Generate Final items")]
    public static void GenerateItems()
    {
        string path = "Assets/_Project/Common/Data/Items";
        if (!AssetDatabase.IsValidFolder(path))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Common/Data/Items"));
            AssetDatabase.Refresh();
        }

        // --- VOLCANO ---
        CreateItem("Heart of the Volcano", "heart_volcano", Color.red, "A pulsating core of pure heat.");
        CreateItem("Emberstone Shard", "emberstone_shard", new Color(0.5f, 0.2f, 0.1f), "A jagged fragment of volcanic sediment.");
        CreateItem("Cinder Ash Vial", "cinder_ash", Color.gray, "Warm, grey ash collected from the crater's edge.");
        CreateItem("Lava Serpent Fang", "lava_fang", Color.white, "A sharp, blackened tooth imbued with molten energy.");
        CreateItem("Molten Basilisk Scale", "basilisk_scale", new Color(1f, 0.4f, 0f), "A heavy, glowing scale from a deep-lava predator.");
        CreateItem("Sulfur Bloom", "sulfur_bloom", Color.yellow, "A brittle, yellow crystal that smells strongly of brimstone.");
        CreateItem("Charred Obsidian Fragment", "obsidian_fragment", Color.black, "Sharp, glass-like volcanic rock.");
        CreateItem("Ashen Phoenix Feather", "phoenix_feather", new Color(1f, 0.2f, 0f), "A feather that glows with a faint, eternal ember.");

        // --- MAGIC FOREST ---
        CreateItem("Whispering Moss", "whispering_moss", new Color(0.2f, 0.8f, 0.2f), "Moss that hums softly with the forest's memory.");
        CreateItem("Glowcap Mushroom", "glowcap_mushroom", Color.cyan, "A neon-blue fungi that illuminates the dark roots.");
        CreateItem("Hollow Dryad Bark", "dryad_bark", new Color(0.4f, 0.3f, 0.1f), "Ancient bark that feels warm to the touch.");
        CreateItem("River Spirit Pebble", "river_pebble", new Color(0.5f, 0.7f, 1f), "A perfectly smooth stone from a magical spring.");
        CreateItem("Moonlit Spider Silk", "moonlit_silk", new Color(0.9f, 0.9f, 1f), "Strong, translucent silk woven under moonlight.");
        CreateItem("Shadow Fern Sprig", "shadow_fern", new Color(0.1f, 0.1f, 0.2f), "A dark frond that hides in the deepest thickets.");
        CreateItem("Night Owl Talon", "owl_talon", Color.white, "A sharp talon from a nocturnal forest guardian.");
        CreateItem("Spiritwood Acorn", "spiritwood_acorn", new Color(0.6f, 0.4f, 0.2f), "A dense, magical seed of the Great Oak.");

        // --- MAGIC GARDEN LABYRINTH ---
        CreateItem("Stardust Petal", "stardust_petal", new Color(0.8f, 0.5f, 1f), "A petal that leaves a trail of glitter in the air.");
        CreateItem("Prism Lily Stem", "prism_lily", Color.magenta, "A stem that refracts light into tiny rainbows.");
        CreateItem("Crystal Dew Drop", "dew_drop", Color.white, "A droplet of water that never evaporates.");
        CreateItem("Thornvine Loop", "thornvine_loop", new Color(0.1f, 0.4f, 0.1f), "A twisted piece of semi-sentient garden vines.");
        CreateItem("Golden Nectar Orb", "golden_nectar", new Color(1f, 0.8f, 0f), "Sweet, liquid light contained in a thin shell.");
        CreateItem("Timeblossom Seed", "timeblossom_seed", new Color(0.7f, 1f, 0.7f), "A seed that seems to blur slightly when observed.");
        CreateItem("Enchanted Rose Thorn", "enchanted_thorn", Color.red, "A sharp thorn that drips with magical essence.");
        CreateItem("Glasswing Butterfly Wing", "butterfly_wing", new Color(0.8f, 0.9f, 1f), "A fragile, transparent wing of a garden spirit.");

        // --- FARM ---
        CreateItem("Runic Pumpkin Core", "runic_pumpkin", new Color(1f, 0.5f, 0f), "A core carved with ancient agricultural wards.");
        CreateItem("Enchanted Wheat Bundle", "enchanted_wheat", new Color(0.9f, 0.8f, 0.4f), "Golden stalks that sway even without wind.");
        CreateItem("Silver Hen Egg", "silver_egg", new Color(0.8f, 0.8f, 0.8f), "A heavy egg with a metallic sheen.");
        CreateItem("Sapphire Milk Bottle", "sapphire_milk", Color.blue, "Milk that has turned a shimmering blue under enchantment.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "28 Items generated in " + path, "OK");
    }

    private static void CreateItem(string name, string id, Color color, string lore)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = name;
        item.itemID = id;
        item.itemColor = color;
        item.lore = lore;

        string assetPath = $"Assets/_Project/Common/Data/Items/{name.Replace(" ", "")}.asset";
        AssetDatabase.CreateAsset(item, assetPath);
        Debug.Log("Created Item: " + assetPath);
    }
}
