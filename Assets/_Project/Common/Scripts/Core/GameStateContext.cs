// ============================================================
//  GameStateContext.cs
//  PURPOSE: Singleton that tracks the current state of the game
//           and provides a "Global Context Block" injected at the
//           top of EVERY LLM prompt. This ensures Byte (the robot)
//           always knows where the player is in the story.
//
//  HOW TO USE (for AI testing):
//    - Adjust the public fields in the Inspector to simulate
//      different game states before pressing a keyboard key.
//
//  TODO (Transfer — for partner dev):
//    - Connect each field to the real game event that changes it.
//    - See individual TODO comments on each field below.
//
//  BIOME DETECTOR:
//    - GetBiome(itemName) returns which world an item belongs to.
//    - Based on the full 28-item database from the design doc.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class GameStateContext : MonoBehaviour
{
    // ── SINGLETON SETUP ──────────────────────────────────────────────────────
    public static GameStateContext Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameStateContext] Duplicate detected — destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persists across scenes
        Debug.Log("[GameStateContext] ✅ Singleton initialized. Context system ready.");
    }

    // ── PUBLIC STATE FIELDS ──────────────────────────────────────────────────
    // All fields are public so they can be adjusted in the Inspector for testing.
    // Each has a TODO comment showing which game system should update it.

    [Header("=== WORLD STATE (Set in Inspector for testing) ===")]

    [Tooltip("The world the player is currently in.")]
    // TODO (Transfer): Set this when a portal/door takes the player to a new world.
    //                  e.g. GameStateContext.Instance.currentWorld = "Fantasy World";
    public string currentWorld = "Fantasy World";

    [Tooltip("The current location within the world (e.g. 'Near Volcano Station').")]
    // TODO (Transfer): Update based on StationZone proximity trigger.
    public string currentLocation = "Main Area";

    [Header("=== ITEM PROGRESS ===")]

    [Tooltip("How many of the 7 required ingredients the player has collected.")]
    // TODO (Transfer): Increment in InventoryManager.AddItem() for correct items.
    //                  e.g. GameStateContext.Instance.itemsCollected++;
    [Range(0, 7)]
    public int itemsCollected = 0;

    [Header("=== CAULDRON ===")]

    [Tooltip("Number of failed cauldron attempts this session.")]
    // TODO (Transfer): Increment in Cauldron.FailPuzzle().
    //                  e.g. GameStateContext.Instance.cauldronFailures++;
    public int cauldronFailures = 0;

    [Tooltip("True when the player has successfully completed the cauldron ritual.")]
    // TODO (Transfer): Set in Cauldron.WinPuzzle().
    public bool cauldronComplete = false;

    [Header("=== STORY FLAGS ===")]

    [Tooltip("True when the dragon has been healed and the amulet received.")]
    // TODO (Transfer): Set when the winning cutscene plays.
    public bool dragonHealed = false;

    [Header("=== INTERACTION COUNTER ===")]

    [Tooltip("Total number of LLM interactions (any key press) this session.")]
    // Updated automatically every time an event fires in LLMEventTester.
    // TODO (Transfer): This will auto-update — no manual connection needed.
    public int totalInteractions = 0;

    // ── BUILD CONTEXT BLOCK ──────────────────────────────────────────────────
    /// <summary>
    /// Returns a formatted string that is prepended to EVERY LLM prompt.
    /// This ensures Byte always knows the current game state.
    /// The block is designed to be read by Llama 3.2 efficiently.
    /// </summary>
    public string BuildContextBlock()
    {
        // ── Derive story progress line ──────────────────────────────────────
        string progressLine;
        if (dragonHealed)
        {
            progressLine = "The dragon has been healed. The player has received the amulet.";
        }
        else if (cauldronComplete)
        {
            progressLine = "The cauldron ritual is complete. The healing potion has been brewed.";
        }
        else
        {
            progressLine = $"The player has collected {itemsCollected} of 7 ingredients. "
                         + $"The cauldron has been attempted {cauldronFailures} time(s).";
        }

        // ── Interaction note (for counter-awareness prompt in Byte) ─────────
        string interactionNote = totalInteractions >= 8
            ? $"[INTERACTION NOTE] Byte and the player have exchanged {totalInteractions} messages. "
            + "Byte may briefly and warmly acknowledge this if it feels natural."
            : "";

        // ── Assemble full context block ─────────────────────────────────────
        string block = $@"[GLOBAL GAME STATE]
World: {currentWorld}
Location: {currentLocation}
Story Progress: {progressLine}
Cauldron Failures (this session): {cauldronFailures}
Total Byte Interactions: {totalInteractions}
{interactionNote}[END GLOBAL STATE]";

        return block;
    }

    // ── INTERACTION TRACKER ──────────────────────────────────────────────────
    /// <summary>
    /// Call this every time ANY LLM event fires.
    /// Automatically increments the interaction counter.
    /// </summary>
    public void RegisterInteraction()
    {
        totalInteractions++;
        Debug.Log($"[GameStateContext] Interaction #{totalInteractions} registered.");

        if (totalInteractions == 8)
        {
            Debug.Log("[GameStateContext] ⚠️ Interaction threshold reached (8). " +
                      "Byte will acknowledge this on the next message.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  BIOME DETECTOR
    //  Maps each of the 28 game items to their world region (biome).
    //  Used by LLMEventTester to build prompts with correct biome references.
    //
    //  TODO (Transfer): Move this to ItemData.cs as a serialized field,
    //                   so each item knows its own biome at edit-time.
    //                   For now, this static dictionary is used for reliability.
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly Dictionary<string, string> _itemBiomeMap = new Dictionary<string, string>()
    {
        // ── VOLCANO ──────────────────────────────────────────────────────────
        // ✅ Correct ingredients from this world:
        { "Heart of the Volcano",       "Volcano" },
        { "Molten Basilisk Scale",       "Volcano" },
        // ❌ Wrong options (appear in quiz as distractors):
        { "Emberstone Shard",            "Volcano" },
        { "Cinder Ash Vial",             "Volcano" },
        { "Lava Serpent Fang",           "Volcano" },
        { "Sulfur Bloom",                "Volcano" },
        { "Charred Obsidian Fragment",   "Volcano" },
        { "Ashen Phoenix Feather",       "Volcano" },

        // ── MAGIC FOREST ─────────────────────────────────────────────────────
        // ✅ Correct:
        { "Whispering Moss",             "Magic Forest" },
        { "Moonlit Spider Silk",         "Magic Forest" },
        // ❌ Wrong:
        { "Glowcap Mushroom",            "Magic Forest" },
        { "Hollow Dryad Bark",           "Magic Forest" },
        { "River Spirit Pebble",         "Magic Forest" },
        { "Shadow Fern Sprig",           "Magic Forest" },
        { "Night Owl Talon",             "Magic Forest" },
        { "Spiritwood Acorn",            "Magic Forest" },

        // ── MAGIC GARDEN LABYRINTH ───────────────────────────────────────────
        // ✅ Correct:
        { "Stardust Petal",              "Magic Garden" },
        { "Golden Nectar Orb",           "Magic Garden" },
        // ❌ Wrong:
        { "Prism Lily Stem",             "Magic Garden" },
        { "Crystal Dew Drop",            "Magic Garden" },
        { "Thornvine Loop",              "Magic Garden" },
        { "Timeblossom Seed",            "Magic Garden" },
        { "Enchanted Rose Thorn",        "Magic Garden" },
        { "Glasswing Butterfly Wing",    "Magic Garden" },

        // ── FARM ─────────────────────────────────────────────────────────────
        // ✅ Correct:
        { "Runic Pumpkin Core",          "Farm" },
        // ❌ Wrong:
        { "Enchanted Wheat Bundle",      "Farm" },
        { "Silver Hen Egg",              "Farm" },
        { "Sapphire Milk Bottle",        "Farm" },
    };

    /// <summary>
    /// Returns the world/biome name for a given item name.
    /// Returns "Unknown Region" if the item is not in the database.
    /// </summary>
    /// <param name="itemName">Exact item name as it appears in the game.</param>
    public static string GetBiome(string itemName)
    {
        if (_itemBiomeMap.TryGetValue(itemName, out string biome))
        {
            return biome;
        }

        Debug.LogWarning($"[GameStateContext] ⚠️ GetBiome: Item '{itemName}' not found in biome map!");
        return "Unknown Region";
    }

    /// <summary>
    /// Returns true if the item is one of the 7 correct ingredients for the cauldron.
    /// </summary>
    public static bool IsCorrectIngredient(string itemName)
    {
        // The 7 correct ingredient names — hardcoded here for reference
        // TODO (Transfer): Compare against Cauldron.winningItems list instead
        return itemName == "Heart of the Volcano"
            || itemName == "Molten Basilisk Scale"
            || itemName == "Whispering Moss"
            || itemName == "Moonlit Spider Silk"
            || itemName == "Stardust Petal"
            || itemName == "Golden Nectar Orb"
            || itemName == "Runic Pumpkin Core";
    }
}
