using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ItemPrefabBatchGenerator : EditorWindow
{
    private struct ItemConfig
    {
        public string name;
        public string id;
        public Color color;
        public string biome;
        public string lore;

        public ItemConfig(string name, string id, Color color, string biome, string lore)
        {
            this.name = name;
            this.id = id;
            this.color = color;
            this.biome = biome;
            this.lore = lore;
        }
    }

    [MenuItem("Tools/Cauldron/Update Data and Generate Prefabs")]
    public static void Execute()
    {
        List<ItemConfig> items = GetItemConfigs();
        
        string dataPath = "Assets/_Project/Common/Data/Items";
        string prefabPath = "Assets/_Project/Common/Prefabs/CauldronItems";

        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder(dataPath)) Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Common/Data/Items"));
        if (!AssetDatabase.IsValidFolder(prefabPath)) Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Common/Prefabs/CauldronItems"));

        AssetDatabase.Refresh();

        foreach (var cfg in items)
        {
            // 1. Create or Update ItemData Asset
            string assetPath = $"{dataPath}/{cfg.name.Replace(" ", "")}.asset";
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.itemName = cfg.name;
            data.itemID = cfg.id;
            data.itemColor = cfg.color;
            data.itemBiome = cfg.biome;
            data.lore = cfg.lore;

            EditorUtility.SetDirty(data);

            // 2. Create Prefab
            string prefabFilePath = $"{prefabPath}/{cfg.name.Replace(" ", "")}.prefab";
            
            // Create a temporary GameObject in the scene
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = cfg.name;
            go.transform.localScale = Vector3.one * 0.3f; // Set a reasonable scale for items

            // Configure Renderer
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = cfg.color;
            // Save material to a subfolder to avoid cluttering or sharing
            string matFolder = prefabPath + "/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder)) Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Common/Prefabs/CauldronItems/Materials"));
            AssetDatabase.CreateAsset(mat, $"{matFolder}/{cfg.name.Replace(" ", "")}_Mat.mat");
            mr.material = mat;

            // Configure Collider
            SphereCollider sc = go.GetComponent<SphereCollider>();
            sc.isTrigger = true;

            // Add Rigidbody
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add GrabbableObject (from project)
            // Note: If GrabbableObject is in a specific namespace, add it here.
            // Based on previous views, it seems to be in global namespace.
            var grabbable = go.AddComponent<GrabbableObject>();
            // Using SerializedObject to set private/protected fields if necessary, 
            // but for now we'll just add the component as it appeared in Sphere.prefab.

            // Add ObjectLoreInspector
            ObjectLoreInspector loreInspector = go.AddComponent<ObjectLoreInspector>();
            loreInspector.objectName = cfg.name;
            loreInspector.objectDescription = $"Context: Cauldron item ; Description: {cfg.lore}";
            loreInspector.itemData = data;
            loreInspector.isPickable = true;
            loreInspector.textHeightOffset = 2.0f;

            // Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(go, prefabFilePath);
            
            // Cleanup
            DestroyImmediate(go);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Successfully updated {items.Count} ItemData assets and generated corresponding prefabs!", "Great!");
    }

    private static List<ItemConfig> GetItemConfigs()
    {
        return new List<ItemConfig>
        {
            // VOLCANO
            new ItemConfig("Heart of the Volcano", "heart_volcano", Color.red, "Volcano", "A crystallized ember-core pulsing with ancient magma."),
            new ItemConfig("Emberstone Shard", "emberstone_shard", new Color(0.8f, 0.2f, 0.1f), "Volcano", "A jagged coal-red shard that never fully cools."),
            new ItemConfig("Cinder Ash Vial", "cinder_ash", Color.gray, "Volcano", "A glass vial of ash that smolders without flame."),
            new ItemConfig("Lava Serpent Fang", "lava_fang", Color.white, "Volcano", "A curved, obsidian fang still warm to touch."),
            new ItemConfig("Molten Basilisk Scale", "basilisk_scale", new Color(1f, 0.5f, 0f), "Volcano", "A heavy scale that sweats glowing lava tears."),
            new ItemConfig("Sulfur Bloom", "sulfur_bloom", Color.yellow, "Volcano", "A brittle yellow blossom that reeks of storms."),
            new ItemConfig("Charred Obsidian Fragment", "obsidian_fragment", Color.black, "Volcano", "A black glass splinter veined with ember cracks."),
            new ItemConfig("Ashen Phoenix Feather", "phoenix_feather", Color.white, "Volcano", "A pale feather that reignites when whispered to."),

            // MAGIC FOREST
            new ItemConfig("Whispering Moss", "whispering_moss", new Color(0.2f, 0.6f, 0.2f), "Forest", "Soft moss that murmurs secrets when stepped on."),
            new ItemConfig("Glowcap Mushroom", "glowcap_mushroom", Color.cyan, "Forest", "A small cap that glows brighter near danger."),
            new ItemConfig("Hollow Dryad Bark", "dryad_bark", new Color(0.4f, 0.3f, 0.1f), "Forest", "A bark shell echoing faint heartbeats of the forest."),
            new ItemConfig("River Spirit Pebble", "river_pebble", new Color(0.5f, 0.7f, 1f), "Forest", "A smooth stone that drips water in dry hands."),
            new ItemConfig("Moonlit Spider Silk", "moonlit_silk", new Color(0.9f, 0.9f, 1f), "Forest", "Thread that shimmers like moonlight on water."),
            new ItemConfig("Shadow Fern Sprig", "shadow_fern", new Color(0.1f, 0.1f, 0.2f), "Forest", "A fern cutting that darkens light around it."),
            new ItemConfig("Night Owl Talon", "owl_talon", Color.white, "Forest", "A hooked talon that hums under starlight."),
            new ItemConfig("Spiritwood Acorn", "spiritwood_acorn", new Color(0.6f, 0.4f, 0.2f), "Forest", "A tiny acorn that rattles with trapped whispers."),

            // MAGIC GARDEN LABYRINTH
            new ItemConfig("Stardust Petal", "stardust_petal", new Color(0.7f, 0.4f, 1f), "Garden", "A fragile petal trailing glittering constellations."),
            new ItemConfig("Prism Lily Stem", "prism_lily", Color.magenta, "Garden", "A glassy stem bending light into shifting rainbows."),
            new ItemConfig("Crystal Dew Drop", "dew_drop", Color.white, "Garden", "A frozen droplet that echoes distant chimes."),
            new ItemConfig("Thornvine Loop", "thornvine_loop", new Color(0.1f, 0.4f, 0.1f), "Garden", "A living ring of thorns that tightens when gripped."),
            new ItemConfig("Golden Nectar Orb", "golden_nectar", new Color(1f, 0.8f, 0.2f), "Garden", "A warm orb sloshing with honey-thick radiance."),
            new ItemConfig("Timeblossom Seed", "timeblossom_seed", new Color(0.6f, 1f, 0.6f), "Garden", "A seed that ticks softly like a clock."),
            new ItemConfig("Enchanted Rose Thorn", "enchanted_thorn", Color.red, "Garden", "A blood-red thorn that pricks only the unworthy."),
            new ItemConfig("Glasswing Butterfly Wing", "butterfly_wing", new Color(0.8f, 0.9f, 1f), "Garden", "A translucent wing that refracts memories, not light."),

            // FARM
            new ItemConfig("Runic Pumpkin Core", "runic_pumpkin", new Color(1f, 0.5f, 0f), "Farm", "A carved pumpkin heart glowing with shifting runes."),
            new ItemConfig("Enchanted Wheat Bundle", "enchanted_wheat", new Color(0.9f, 0.8f, 0.4f), "Farm", "A bound sheaf that rustles in unheard winds."),
            new ItemConfig("Silver Hen Egg", "silver_egg", new Color(0.8f, 0.8f, 0.8f), "Farm", "A metallic egg that rings when shaken."),
            new ItemConfig("Sapphire Milk Bottle", "sapphire_milk", Color.blue, "Farm", "A blue-tinted bottle brimming with star-flecked milk.")
        };
    }
}
