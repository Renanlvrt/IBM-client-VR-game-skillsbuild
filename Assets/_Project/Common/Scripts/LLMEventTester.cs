// ============================================================
//  LLMEventTester.cs
//
//  PURPOSE:
//    A template-based testing tool for LLM-driven robot events.
//    Uses 15+ pre-defined 'Styles' per event from ByteVoiceLines.cs.
//    The LLM selects and refines the best style for the current context.
//
//  SCHEMA:
//    { "Hint": "...", "Emotion": "...", "Additional": "..." }
//
//  USAGE (for AI developer - Renan):
//    1. Attach this script to any GameObject in the scene (e.g. "LLMTester").
//    2. Also attach: GameStateContext.cs on the same or a persistent GameObject.
//    3. In the Inspector, assign: RobotHintAI, PuterSpeaker.
//    4. Press Play, then use keyboard keys to trigger events.
//    5. All output appears in the Unity Console.
//
//  TRANSFER GUIDE (for partner dev):
//    Every method marked with "TODO (Transfer)" shows exactly which game event
//    should call it. Replace the keyboard check with the real trigger.
//    The method signatures and prompt logic stay unchanged.
//
//  KEY MAP SUMMARY:
//    R = Inspect: Whispering Moss        (✅ correct item)
//    T = Inspect: Heart of the Volcano   (✅ correct item)
//    Y = Inspect: Emberstone Shard       (❌ wrong item)
//    U = Cauldron Hint — Tier 1 (count)
//    I = Cauldron Hint — Tier 2 (biome)
//    O = Cauldron Hint — Tier 3 (wrong item named)
//    P = Cauldron Success — 1st try
//    F = Cauldron Success — 3rd try (perseverance win)
//    G = Narrator / Story Summary
//    H = Inventory Check — 5 items (4 correct + 1 wrong)
//    J = Inventory Check — 5 items (all correct)
//    K = Inventory Check — 7/7 complete (go to cauldron!)
//    L = World Entry — Volcano
//    Q = World Entry — Magic Forest
//    Z = Stuck / Inactivity Warning
//    C = Encouragement (after repeated failures)
//    V = Wizard Intro (Fantasy World entry)
//    B = Dragon Healed — End of world celebration
//    N = Free Question ("What should I do next?")
//    M = Interaction Counter Test (simulate 8 interactions)
// ============================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using LLMUnity;

public class LLMEventTester : MonoBehaviour
{
    // ── INSPECTOR REFERENCES ─────────────────────────────────────────────────
    [Header("=== REQUIRED CONNECTIONS ===")]
    [Tooltip("The LLMCharacter component on the Robot (Byte). Drag from scene hierarchy.")]
    public LLMCharacter byteBrain;

    [Tooltip("The PuterSpeaker component for voice output. Drag from Robot.")]
    public PuterSpeaker puterSpeaker;

    [Header("=== VISUAL FEEDBACK ===")]
    [Tooltip("Fires when LLM starts polishing (e.g. blink yellow eyes).")]
    public UnityEngine.Events.UnityEvent onThinkingStart;
    [Tooltip("Fires when speech starts or LLM finishes.")]
    public UnityEngine.Events.UnityEvent onThinkingEnd;

    // ── PRIVATE STATE ─────────────────────────────────────────────────────────
    private bool _isGenerating = false; // Prevent overlapping LLM calls
    private string _rawResponseBuffer = "";

    // ── SYSTEM PROMPT ─────────────────────────────────────────────────────────
    private const string BYTE_SYSTEM_PROMPT =
        "You are Byte, an efficient robotic companion. " +
        "You ALWAYS output ONLY a JSON object. No text before or after it.\n\n" +
        "JSON Schema:\n" +
        "{\"Hint\": \"<your response here>\", \"Emotion\": \"<one word>\", \"Additional\": \"None\"}\n\n" +
        "Valid emotions: Helpful | Pleased | Concerned | Proud | Encouraging | Curious | Neutral\n\n" +
        "Your Task: Rewrite the provided input sentence to be DIRECT and SIMPLE. " +
        "Be efficient. No poetic fluff. Use max 10 words.";

    // ── STATE ─────────────────────────────────────────────────────────────────
    private Queue<string> _interactionHistory = new Queue<string>(3); // Memory buffer

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Apply Byte's system prompt to the LLM character
        if (byteBrain != null)
        {
            byteBrain.SetPrompt(BYTE_SYSTEM_PROMPT);
            Debug.Log("[LLMEventTester] ✅ System prompt applied to Byte.");
        }
        else
        {
            Debug.LogError("[LLMEventTester] ❌ byteBrain (LLMCharacter) is not assigned! " +
                           "Please drag the Robot's LLMCharacter into the Inspector.");
        }

        if (puterSpeaker == null)
            Debug.LogWarning("[LLMEventTester] ⚠️ PuterSpeaker not assigned. Voice output disabled.");

        Debug.Log("[LLMEventTester] ✅ Ready. Press R/T/Y/U/I/O/P/F/G/H/J/K/L/Q/Z/C/V/B/N/M to test events.");
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // ── INSPECT EVENTS ──────────────────────────────────────────────────
        if (Keyboard.current.rKey.wasPressedThisFrame)
            TriggerInspect("Whispering Moss",
                "A clump of ancient moss that hums softly in the dark forest. Associated with forgotten forest prayers.",
                isCorrect: true);

        if (Keyboard.current.tKey.wasPressedThisFrame)
            TriggerInspect("Heart of the Volcano",
                "A glowing crystal formed deep within the volcano, pulsing with intense geothermal energy.",
                isCorrect: true);

        if (Keyboard.current.yKey.wasPressedThisFrame)
            TriggerInspect("Emberstone Shard",
                "A jagged fragment of volcanic rock that radiates dry heat. Common near lava flows.",
                isCorrect: false);

        // ── CAULDRON HINT EVENTS ────────────────────────────────────────────
        if (Keyboard.current.uKey.wasPressedThisFrame)
            TriggerCauldronHintTier1(wrongCount: 3);

        if (Keyboard.current.iKey.wasPressedThisFrame)
            TriggerCauldronHintTier2(wrongItemName: "Emberstone Shard");  // biome auto-detected

        if (Keyboard.current.oKey.wasPressedThisFrame)
            TriggerCauldronHintTier3(wrongItemName: "Emberstone Shard");

        // ── CAULDRON SUCCESS EVENTS ─────────────────────────────────────────
        if (Keyboard.current.pKey.wasPressedThisFrame)
            TriggerCauldronSuccess(attemptNumber: 1);

        if (Keyboard.current.fKey.wasPressedThisFrame)
            TriggerCauldronSuccess(attemptNumber: 3);

        // ── NARRATIVE EVENTS ────────────────────────────────────────────────
        if (Keyboard.current.gKey.wasPressedThisFrame)
            TriggerNarrator();

        // ── INVENTORY EVENTS ────────────────────────────────────────────────
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            // Simulate 4 correct + 1 wrong item
            // TODO (Transfer): Replace with InventoryManager.Instance.GetAllItems()
            TriggerInventoryCheck(new List<string> {
                "Whispering Moss",
                "Heart of the Volcano",
                "Stardust Petal",
                "Golden Nectar Orb",
                "Emberstone Shard"   // ← wrong item
            }, totalNeeded: 7, label: "PARTIAL_WRONG");
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            // Simulate 5 all correct items
            TriggerInventoryCheck(new List<string> {
                "Whispering Moss",
                "Heart of the Volcano",
                "Stardust Petal",
                "Golden Nectar Orb",
                "Moonlit Spider Silk"  // all correct
            }, totalNeeded: 7, label: "PARTIAL_OK");
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            // Simulate full 7/7 complete inventory
            TriggerInventoryCheck(new List<string> {
                "Heart of the Volcano",
                "Molten Basilisk Scale",
                "Whispering Moss",
                "Moonlit Spider Silk",
                "Stardust Petal",
                "Golden Nectar Orb",
                "Runic Pumpkin Core"
            }, totalNeeded: 7, label: "FULL");
        }

        // ── WORLD ENTRY EVENTS ──────────────────────────────────────────────
        if (Keyboard.current.lKey.wasPressedThisFrame)
            TriggerWorldEntry("Volcano",
                new List<string> { "Heart of the Volcano", "Molten Basilisk Scale" });

        if (Keyboard.current.qKey.wasPressedThisFrame)
            TriggerWorldEntry("Magic Forest",
                new List<string> { "Whispering Moss", "Moonlit Spider Silk" });

        if (Keyboard.current.vKey.wasPressedThisFrame)
            TriggerWizardIntro();

        // ── UTILITY EVENTS ──────────────────────────────────────────────────
        if (Keyboard.current.zKey.wasPressedThisFrame)
            TriggerStuckWarning();

        if (Keyboard.current.cKey.wasPressedThisFrame)
            TriggerEncouragement();

        if (Keyboard.current.bKey.wasPressedThisFrame)
            TriggerDragonHealed();

        if (Keyboard.current.nKey.wasPressedThisFrame)
            TriggerFreeQuestion();

        if (Keyboard.current.mKey.wasPressedThisFrame)
            TriggerInteractionCounterTest();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  EVENT METHODS
    //  Each method:
    //   1. Declares local variables (hardcoded for testing, clearly labeled)
    //   2. Builds and logs the full prompt to Console
    //   3. Sends to LLM
    //   4. On completion: parses JSON, logs output, triggers Piper TTS
    // ═════════════════════════════════════════════════════════════════════════

    // ─────────────────────────────────────────────────────────────────────────
    //  R / T / Y — INSPECT OBJECT
    //  TODO (Transfer): Call TriggerInspect() from ObjectLoreInspector.TriggerRobotSpeech()
    //                   Pass: objectName, objectDescription, isCorrect (from Cauldron.winningItems)
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerInspect(string itemName, string itemLore, bool isCorrect)
    {
        if (!CanFire("INSPECT")) return;

        string category = isCorrect ? "INSPECT_CORRECT" : "INSPECT_INCORRECT";
        var replacements = new Dictionary<string, string> {
            { "{itemName}", itemName },
            { "{itemLore}", itemLore },
            { "{itemBiome}", GameStateContext.GetBiome(itemName) }
        };

        string preFilledLine = ByteVoiceLines.GetRandomFilled(category, replacements);

        LogEventHeader("INSPECT", $"Item: {itemName} | Correct: {isCorrect}");
        LogContextVariables($"  preFilledLine = \"{preFilledLine}\"");

        await ProcessProfessionalBrain(preFilledLine, "INSPECT");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  U — CAULDRON HINT TIER 1 (Wrong Count Only)
    //  TODO (Transfer): Call from Cauldron.FailPuzzle() when failureCount == 1
    //                   Pass: wrongCount = incorrectItems.Count
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerCauldronHintTier1(int wrongCount)
    {
        if (!CanFire("HINT_T1")) return;

        var replacements = new Dictionary<string, string> {
            { "{wrongCount}", wrongCount.ToString() }
        };

        string preFilledLine = ByteVoiceLines.GetRandomFilled("CAULDRON_FAIL_T1", replacements);

        LogEventHeader("HINT_T1", $"Wrong Count: {wrongCount}");
        LogContextVariables($"  preFilledLine = \"{preFilledLine}\"");

        await ProcessProfessionalBrain(preFilledLine, "HINT_T1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  I — CAULDRON HINT TIER 2 (Biome Region Hint)
    //  TODO (Transfer): Call from Cauldron.FailPuzzle() when failureCount == 2
    //                   Pass: wrongItemName = incorrectItems[0].itemName
    //                   Biome is auto-detected here from GameStateContext.GetBiome()
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerCauldronHintTier2(string wrongItemName)
    {
        if (!CanFire("HINT_T2")) return;

        string wrongItemBiome = GameStateContext.GetBiome(wrongItemName);
        var replacements = new Dictionary<string, string> {
            { "{wrongBiome}", wrongItemBiome }
        };

        string preFilledLine = ByteVoiceLines.GetRandomFilled("CAULDRON_FAIL_T2", replacements);

        LogEventHeader("HINT_T2", $"Wrong Item Biome: {wrongItemBiome}");
        LogContextVariables($"  preFilledLine = \"{preFilledLine}\"");

        await ProcessProfessionalBrain(preFilledLine, "HINT_T2");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  O — CAULDRON HINT TIER 3 (Name the Wrong Item)
    //  TODO (Transfer): Call from Cauldron.FailPuzzle() when failureCount >= 3
    //                   Pass: wrongItemName = incorrectItems[0].itemName
    //  NOTE: Per spec, we ONLY reveal the wrong item, NOT the correct replacement.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerCauldronHintTier3(string wrongItemName)
    {
        if (!CanFire("HINT_T3")) return;

        string baseJson = "{\"Hint\": \"The " + wrongItemName + " is incorrect for this ritual.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "HINT_T3");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  P / F — CAULDRON SUCCESS
    //  TODO (Transfer): Call from Cauldron.WinPuzzle()
    //                   Pass: attemptNumber = cauldronFailures + 1
    //  P = 1st try success (attemptNumber: 1)
    //  F = 3rd try hard win (attemptNumber: 3) — perseverance message
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerCauldronSuccess(int attemptNumber)
    {
        if (!CanFire("SUCCESS")) return;

        var replacements = new Dictionary<string, string> {
            { "{attemptNumber}", attemptNumber.ToString() }
        };

        string preFilledLine = ByteVoiceLines.GetRandomFilled("CAULDRON_SUCCESS", replacements);

        LogEventHeader("SUCCESS", $"Attempt #{attemptNumber}");
        LogContextVariables($"  preFilledLine = \"{preFilledLine}\"");

        await ProcessProfessionalBrain(preFilledLine, "SUCCESS");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  G — NARRATOR / STORY SUMMARY
    //  TODO (Transfer): Call from hub idle state, NPC dialogue, or intro cutscene.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerNarrator()
    {
        if (!CanFire("NARRATOR")) return;

        string baseJson = "{\"Hint\": \"Recover seven ingredients to heal the dragon.\", \"Emotion\": \"Neutral\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "NARRATOR");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  H / J / K — INVENTORY CHECK
    //  TODO (Transfer): Call when player opens inventory UI or a dedicated button.
    //                   Pass: InventoryManager.Instance.GetAllItems() as List<string>
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerInventoryCheck(List<string> items, int totalNeeded, string label)
    {
        if (!CanFire($"INVENTORY_{label}")) return;

        int collected = items.Count;
        int remaining = totalNeeded - collected;
        bool isFull = remaining <= 0;
        string itemListStr = string.Join(", ", items);

        string hint = isFull 
            ? $"Collection complete. Head to the cauldron."
            : $"Collected {collected} of {totalNeeded}. {remaining} remaining: {itemListStr}.";

        string baseJson = "{\"Hint\": \"" + hint + "\", \"Emotion\": \"" + (isFull ? "Pleased" : "Helpful") + "\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, $"INVENTORY_{label}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  L / Q — WORLD ENTRY
    //  TODO (Transfer): Call from StationZone.OnTriggerEnter or portal activation.
    //                   Pass: worldName, knownCorrectItems (from game data).
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerWorldEntry(string worldName, List<string> correctItemsInWorld)
    {
        if (!CanFire("WORLD_ENTRY")) return;

        string worldDescription = worldName switch {
            "Volcano"       => "a scorching landscape of molten rock, erupting geysers, and ancient fire spirits",
            "Magic Forest"  => "a dense, enchanted forest glowing with bioluminescent flora and ancient magical creatures",
            "Magic Garden"  => "a labyrinthine garden overflowing with rare magical flora and mysterious pathways",
            "Farm"          => "a surprisingly peaceful pastoral farm with an air of enchanted simplicity",
            _               => "a newly discovered region filled with unknown wonders"
        };

        string hint = $"Arrived in {worldName}. Search for materials.";
        string baseJson = "{\"Hint\": \"" + hint + "\", \"Emotion\": \"Curious\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "WORLD_ENTRY");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  V — WIZARD INTRO (Fantasy World Entry)
    //  TODO (Transfer): Call when the player enters the Fantasy World for the first time.
    //                   Ideally triggered once (use a bool flag to prevent repeat).
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerWizardIntro()
    {
        if (!CanFire("WIZARD_INTRO")) return;

        string baseJson = "{\"Hint\": \"The dragon requires seven elements. We must help.\", \"Emotion\": \"Neutral\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "WIZARD_INTRO");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  B — DRAGON HEALED (World Complete)
    //  TODO (Transfer): Call from the world completion cutscene / victory trigger.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerDragonHealed()
    {
        if (!CanFire("DRAGON_HEALED")) return;

        string baseJson = "{\"Hint\": \"Amulet restored. The dragon is healed!\", \"Emotion\": \"Proud\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "DRAGON_HEALED");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Z — STUCK / INACTIVITY WARNING
    //  TODO (Transfer): Trigger from an InactivityTimer that fires after X seconds with no input.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerStuckWarning()
    {
        if (!CanFire("STUCK")) return;

        string baseJson = "{\"Hint\": \"Progress is stalled. Check your inventory or stations.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "STUCK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  C — ENCOURAGEMENT
    //  TODO (Transfer): Trigger after cauldronFailures >= 3, or after quiz wrong streak.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerEncouragement()
    {
        if (!CanFire("ENCOURAGE")) return;

        string baseJson = "{\"Hint\": \"Stay focused. The cure is near.\", \"Emotion\": \"Encouraging\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "ENCOURAGE");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  N — FREE QUESTION ("What should I do next?")
    //  TODO (Transfer): Trigger from a physical "Ask Byte" button or gesture.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerFreeQuestion()
    {
        if (!CanFire("FREE_QUESTION")) return;

        int collected = GameStateContext.Instance != null ? GameStateContext.Instance.itemsCollected : 0;
        string hint = collected < 7 
            ? $"Collected {collected} of 7. Continue exploring stations."
            : "7 of 7 collected. Start the ritual.";

        string baseJson = "{\"Hint\": \"" + hint + "\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "FREE_QUESTION");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  M — INTERACTION COUNTER TEST
    //  Simulates having 8 interactions to test Byte's counter acknowledgement.
    //  TODO (Transfer): Not needed for transfer — counter auto-increments.
    // ─────────────────────────────────────────────────────────────────────────
    public async void TriggerInteractionCounterTest()
    {
        if (!CanFire("COUNTER_TEST")) return;

        if (GameStateContext.Instance != null) GameStateContext.Instance.totalInteractions = 8;

        string baseJson = "{\"Hint\": \"Eight conversations completed. Our partnership is efficient.\", \"Emotion\": \"Pleased\", \"Additional\": \"None\"}";
        await ProcessProfessionalBrain(baseJson, "COUNTER_TEST");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LLM PIPELINE
    //  Everything below is shared infrastructure used by ALL events above.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The "Professional Brain" pipeline: Speculative Execution.
    /// 1. Instantly prepares a fallback line.
    /// 2. Starts the LLM "Polishing" task.
    /// 3. If LLM is slow (>1.5s), speaks fallback immediately.
    /// </summary>
    private async Task ProcessProfessionalBrain(string rawTemplateJson, string debugTag)
    {
        try 
        {
            onThinkingStart?.Invoke();

            // Step 1: Parse the C# pre-filled template as our fallback
            ByteResponse fallback = JsonUtility.FromJson<ByteResponse>(rawTemplateJson);
            string fallbackText = fallback?.Hint ?? "";

            // Step 2: Build LLM Prompt
            string history = string.Join(" | ", _interactionHistory);
            string polishPrompt =
                $"[INPUT SENTENCE]\n{fallbackText}\n\n" +
                $"[HISTORY - DO NOT REPEAT]\n{history}\n\n" +
                $"[TASK] Rewrite the input sentence for robotic variety. Keep names and meaning.";

            // Step 3: Start LLM Task
            Task<string> llmTask = SendToLLMDirect(polishPrompt, debugTag);

            // Step 4: Race! LLM vs 1500ms Timeout
            Task delayTask = Task.Delay(1500);
            Task completedTask = await Task.WhenAny(llmTask, delayTask);

            string finalHint = fallbackText;
            string finalEmotion = fallback?.Emotion ?? "Neutral";

            if (completedTask == llmTask)
            {
                string rawLLMResponse = await llmTask;
                try 
                {
                    ByteResponse polished = JsonUtility.FromJson<ByteResponse>(rawLLMResponse);
                    if (!string.IsNullOrEmpty(polished?.Hint))
                    {
                        finalHint = polished.Hint;
                        finalEmotion = polished.Emotion;
                        Debug.Log($"<color=green>[{debugTag}] ✅ LLM Polished in time: \"{finalHint}\"</color>");
                    }
                } 
                catch 
                {
                    string extHint = ExtractJsonField(rawLLMResponse, "Hint");
                    if (!string.IsNullOrEmpty(extHint))
                    {
                        finalHint = extHint;
                        finalEmotion = ExtractJsonField(rawLLMResponse, "Emotion");
                        Debug.Log($"<color=yellow>[{debugTag}] ✅ LLM Polished (Regex Fallback): \"{finalHint}\"</color>");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"<color=orange>[{debugTag}] ⏳ LLM Timeout (1.5s). Using C# Fallback.</color>");
            }

            // Step 5: Update History
            if (_interactionHistory.Count >= 3) _interactionHistory.Dequeue();
            _interactionHistory.Enqueue(finalHint);

            // Step 6: Speak
            if (puterSpeaker != null)
            {
                Debug.Log($"<color=magenta>[{debugTag}] 🔊 SPEAKING: \"{finalHint}\" [{finalEmotion}]</color>");
                puterSpeaker.Speak(finalHint);
            }
        }
        finally
        {
            onThinkingEnd?.Invoke();
            _isGenerating = false;
        }
    }

    private async Task<string> SendToLLMDirect(string prompt, string debugTag)
    {
        if (byteBrain == null) return null;

        string globalContext = GameStateContext.Instance != null ? GameStateContext.Instance.BuildContextBlock() : "";
        string fullPrompt = globalContext + "\n\n" + prompt;

        _rawResponseBuffer = "";
        await byteBrain.Chat(fullPrompt, (txt) => _rawResponseBuffer = txt, () => { });
        return _rawResponseBuffer;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  JSON FIELD EXTRACTOR (Regex Fallback)
    //  Extracts a named field value from a JSON-like string.
    //  Works even if Llama 3.2 outputs slightly malformed JSON.
    //  e.g. {"hint": "hello world", "emotion": "Pleased}  ← missing closing quote
    // ═════════════════════════════════════════════════════════════════════════
    private string ExtractJsonField(string raw, string fieldName)
    {
        // Pattern: "fieldName": "<value>" or "fieldName": "<value> (with or without closing quote)
        var pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"}}]+)";
        var match = System.Text.RegularExpressions.Regex.Match(raw, pattern);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }
        return "";
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LOGGING HELPERS
    //  Standardized console output for every event.
    // ═════════════════════════════════════════════════════════════════════════

    private void LogEventHeader(string tag, string message)
    {
        Debug.Log(
            $"<color=white>══════════════════════════════════════════════</color>\n" +
            $"<color=yellow><b>[KEY EVENT: {tag}]</b> {message}</color>"
        );
    }

    private void LogContextVariables(string variables)
    {
        Debug.Log($"<color=cyan>── CONTEXT VARIABLES ──────────────────────────</color>\n{variables}");
    }

    private void LogFullPrompt(string prompt)
    {
        Debug.Log(
            $"<color=cyan>── USER PROMPT (FULL) ──────────────────────────</color>\n" +
            $"{prompt}\n" +
            $"<color=cyan>── END PROMPT ──────────────────────────────────</color>"
        );
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  GUARD: Prevent overlapping LLM calls
    // ═════════════════════════════════════════════════════════════════════════

    private bool CanFire(string tag)
    {
        if (_isGenerating)
        {
            Debug.LogWarning($"[{tag}] ⏳ Still generating previous response. Key press ignored.");
            return false;
        }
        _isGenerating = true;
        return true;
    }
}

// ── DATA STRUCTURES ──────────────────────────────────────────────────────────

[System.Serializable]
public class ByteResponse
{
    public string Hint;
    public string Emotion;
    public string Additional;
}
