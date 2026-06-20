using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Cauldron Puzzle — The Perfect Alchemist
///
/// Behaviour:
///   - Liquid is hidden until the first ingredient is added.
///   - Each ingredient blends the liquid color (using ItemData.itemColor).
///   - At 7 ingredients, the recipe is checked:
///       WIN  → gold glow on exterior + liquid, 4-second shake, Byte speaks success.
///       FAIL → red glow on exterior, 4-second shake, Byte speaks Tier 1 hint,
///              items returned to inventory, cauldron resets.
///   - Once won, the cauldron is locked (_isSolved) and accepts no more items.
/// </summary>
public class Cauldron : MonoBehaviour
{
    // ── LIQUID ────────────────────────────────────────────────────────────────
    [Header("Liquid Setup")]
    [Tooltip("Renderer of the liquid mesh (child of cauldron).")]
    public Renderer liquidRenderer;
    public string colorPropertyName = "_BaseColor";
    public Color defaultColor = new Color(0.2f, 0.2f, 0.2f);

    // ── PUZZLE ────────────────────────────────────────────────────────────────
    [Header("Puzzle Settings")]
    [Tooltip("Optional: drag the 7 correct ItemData assets here for Inspector reference. Not used for matching.")]
    public List<ItemData> winningItems = new List<ItemData>();

    /// <summary>
    /// The single source of truth for the winning recipe.
    /// These are the itemID values read directly from each ScriptableObject asset.
    /// This bypasses any Inspector/reference issues completely.
    /// </summary>
    private static readonly HashSet<string> _winningIDs = new HashSet<string>
    {
        "heart_volcano",    // Heart of the Volcano
        "basilisk_scale",   // Molten Basilisk Scale
        "whispering_moss",  // Whispering Moss
        "moonlit_silk",     // Moonlit Spider Silk
        "stardust_petal",   // Stardust Petal
        "golden_nectar",    // Golden Nectar Orb
        "runic_pumpkin",    // Runic Pumpkin Core
    };

    // ── VISUAL FEEDBACK ───────────────────────────────────────────────────────
    [Header("Visual Feedback")]
    [Tooltip("The outer cauldron body renderer (for glow effects).")]
    public Renderer cauldronExteriorRenderer;
    public Color winGlowColor = new Color(1f, 0.84f, 0f, 1f);   // gold

    [Header("Cinematic Success Animation")]
    [Tooltip("Reference to the Trailer script that handles explosions and light flashes")]
    public CauldronBrewController brewController;
    [Tooltip("Reference to the Trailer script that handles the potion floating and spinning")]
    public CinematicPotion cinematicPotion;

    // ── LLM ───────────────────────────────────────────────────────────────────
    [Header("LLM Integration")]
    [Tooltip("Drag the LLMEventTester GameObject from the scene.")]
    public LLMEventTester llmEventTester;

    // ── INTERACTION ───────────────────────────────────────────────────────────
    [Header("Interaction Settings")]
    public float maxFocusDistance = 5f;

    // ── UI ────────────────────────────────────────────────────────────────────
    [Header("UI Settings")]
    public float textHeightOffset = 2.0f;

    // ── RUNTIME STATE (read-only in Inspector for debugging) ──────────────────
    [Header("State (Debug)")]
    public List<ItemData> currentIngredients = new List<ItemData>();

    // ── PRIVATE ───────────────────────────────────────────────────────────────
    private int failureCount = 0;
    private bool _isSolved = false;
    private bool isPlayerNearby = false;

    private Color targetColor;
    private Color currentColor;
    private Quaternion _originalRotation;
    private Vector3 _originalPosition;

    private TextMeshPro floatingText;
    private Transform playerCamera;

    // --- Physical Tracking ---
    private List<GameObject> _activeIngredientObjects = new List<GameObject>();
    private Dictionary<ItemData, List<GameObject>> _allWorldItems = new Dictionary<ItemData, List<GameObject>>();

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Store the exterior mesh's original rotation (we only shake that, not the whole cauldron)
        if (cauldronExteriorRenderer != null)
            _originalRotation = cauldronExteriorRenderer.transform.localRotation;
            
        _originalPosition = transform.position;
    }

    private void Start()
    {
        // ── Liquid: start hidden (cauldron is empty / dry) ───────────────────
        if (liquidRenderer != null)
        {
            liquidRenderer.gameObject.SetActive(false);
            currentColor = defaultColor;
            targetColor  = defaultColor;
        }

        // --- Cache all items for physical return logic ---
        ObjectLoreInspector[] all = FindObjectsOfType<ObjectLoreInspector>(true);
        foreach (var item in all)
        {
            if (item.itemData != null)
            {
                if (!_allWorldItems.ContainsKey(item.itemData))
                    _allWorldItems[item.itemData] = new List<GameObject>();
                _allWorldItems[item.itemData].Add(item.gameObject);
            }
        }

        if (Camera.main != null) playerCamera = Camera.main.transform;
        CreateFloatingText();
    }

    private void Update()
    {
        // Debug Test: Press 'K' to force the Win Sequence animation!
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("<color=yellow>[Cauldron]</color> 'K' pressed - Forcing WinSequence for Testing.");
            StartCoroutine(WinSequence());
        }

        // 1. Smooth color blending each frame
        if (liquidRenderer != null && liquidRenderer.gameObject.activeSelf && currentColor != targetColor)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 2f);
            liquidRenderer.material.SetColor(colorPropertyName, currentColor);
        }

        // 2. Billboard floating text
        if (isPlayerNearby && playerCamera != null && floatingText != null)
        {
            floatingText.transform.rotation = Quaternion.LookRotation(
                floatingText.transform.position - playerCamera.position);
        }

        // 3. Input when player is nearby
        if (!isPlayerNearby || _isSolved) return;

        // 'E' keyboard — proximity
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("<color=yellow>[Cauldron]</color> 'E' pressed.");
            InteractWithCauldron();
        }
        // VR 'X' button + looking at cauldron
        else if (WasVRXPressed() && IsLookingAtCauldron())
        {
            Debug.Log("<color=yellow>[Cauldron]</color> VR 'X' pressed.");
            InteractWithCauldron();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROXIMITY / TRIGGER
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // Physical item dropped into the cauldron
        ObjectLoreInspector inspector = other.GetComponentInParent<ObjectLoreInspector>();
        if (inspector != null && inspector.itemData != null)
        {
            AddIngredient(inspector.itemData, inspector.gameObject);
            return;
        }

        // Player entered proximity zone
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (floatingText != null) floatingText.gameObject.SetActive(true);
            Debug.Log("<color=green>[Cauldron]</color> Player nearby.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (floatingText != null) floatingText.gameObject.SetActive(false);
            Debug.Log("<color=red>[Cauldron]</color> Player left zone.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INTERACTION
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pops one item from the inventory and adds it to the cauldron.
    /// </summary>
    private void InteractWithCauldron()
    {
        // Try to add from inventory
        if (InventoryManager.Instance != null && InventoryManager.Instance.items.Count > 0)
        {
            ItemData item = InventoryManager.Instance.PopItem();
            if (item != null)
            {
                // Find matching inactive world object
                GameObject worldObj = GetInactiveWorldObject(item);
                AddIngredient(item, worldObj);
            }
        }
        else
        {
            Debug.Log("<color=red>[Cauldron]</color> Inventory is empty.");
        }
    }

    private GameObject GetInactiveWorldObject(ItemData data)
    {
        if (_allWorldItems.TryGetValue(data, out var list))
        {
            foreach (var go in list)
            {
                if (!go.activeInHierarchy) return go;
            }
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INGREDIENT LOGIC
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds an ingredient to the cauldron.
    /// </summary>
    /// <param name="data">The ItemData of the ingredient.</param>
    /// <param name="physicalObject">The GameObject representing the physical item, if available (e.g., from world drop or inventory pop).</param>
    public void AddIngredient(ItemData data, GameObject physicalObject = null)
    {
        if (_isSolved || currentIngredients.Count >= 7) return;

        // Show liquid when first ingredient is added
        if (currentIngredients.Count == 0 && liquidRenderer != null)
        {
            liquidRenderer.gameObject.SetActive(true);
            liquidRenderer.material.SetColor(colorPropertyName, defaultColor);
            currentColor = defaultColor;
            targetColor  = defaultColor;
        }

        currentIngredients.Add(data);

        // Blend toward this item's characteristic color
        targetColor = Color.Lerp(targetColor, data.itemColor, 0.4f);

        Debug.Log($"<color=cyan>[Cauldron]</color> Added: {data.itemName} ({currentIngredients.Count}/7)");

        if (currentIngredients.Count == 7)
        {
            CheckRecipe();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RECIPE CHECK
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckRecipe()
    {
        // Use the hardcoded itemID set — 100% reliable, no Inspector dependency.
        // Print each item's ID to console for easy debugging.
        HashSet<string> remaining = new HashSet<string>(_winningIDs);
        bool isCorrect = true;

        Debug.Log("<color=white>[Cauldron] Recipe check —</color>");
        foreach (var item in currentIngredients)
        {
            bool matched = remaining.Remove(item.itemID);
            Debug.Log($"  · {item.itemName} (id='{item.itemID}') → {(matched ? "✅ CORRECT" : "❌ WRONG")}");
            if (!matched) isCorrect = false;
        }

        if (isCorrect && remaining.Count == 0) WinPuzzle();
        else                                   FailPuzzle();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WIN
    // ─────────────────────────────────────────────────────────────────────────

    private void WinPuzzle()
    {
        _isSolved = true;
        Debug.Log("<color=green>[Cauldron]</color> SUCCESS! All 7 correct items!");

        // Byte speaks immediately (LLM starts loading during shake)
        if (llmEventTester != null)
            llmEventTester.TriggerCauldronSuccess(failureCount + 1);

        if (GameStateContext.Instance != null)
            GameStateContext.Instance.dragonHealed = true;

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        // Try to automatically find the components anywhere in the scene (even if inactive)
        if (cinematicPotion == null) cinematicPotion = FindObjectOfType<CinematicPotion>(true);
        if (brewController == null) brewController = FindObjectOfType<CauldronBrewController>(true);

        // Make sure the liquid is actually visible, otherwise the potion script is disabled 
        // and Unity will refuse to run its Coroutine!
        if (liquidRenderer != null) liquidRenderer.gameObject.SetActive(true);
        if (cinematicPotion != null) cinematicPotion.gameObject.SetActive(true);

        // 1. Start the cinematic float/spin animation (if assigned)
        if (cinematicPotion != null)
        {
            cinematicPotion.PlayBrewingAnimation();
        }
        else
        {
            Debug.LogWarning("<color=orange>[Cauldron]</color> CinematicPotion is NULL! Found nowhere in the scene.");
        }

        // Wait for the potion to reach its highest point/climax
        float climaxDelay = cinematicPotion != null ? cinematicPotion.timeGoingUp : 2.5f;
        
        // 2. Shake exterior while the potion floats up
        StartCoroutine(ShakeCauldron(climaxDelay + 1f));
        
        yield return new WaitForSeconds(climaxDelay);

        // 3. BOOM! Trigger the explosion
        if (brewController != null)
        {
            brewController.gameObject.SetActive(true); // Must be active to play particles
            brewController.TriggerBoom();
        }
        else
        {
            Debug.LogWarning("<color=orange>[Cauldron]</color> CauldronBrewController is NULL! Found nowhere in the scene.");
        }

        // 4. Apply golden glow to the cauldron exterior
        targetColor = winGlowColor;

        if (liquidRenderer != null)
        {
            liquidRenderer.material.EnableKeyword("_EMISSION");
            liquidRenderer.material.SetColor("_EmissionColor", winGlowColor * 1.5f);
        }
        if (cauldronExteriorRenderer != null)
        {
            cauldronExteriorRenderer.material.EnableKeyword("_EMISSION");
            cauldronExteriorRenderer.material.SetColor("_EmissionColor", winGlowColor * 2.5f);
        }

        // The CinematicPotion script handles its own falling down and liquid color change.
        // We just hold our exterior glow for a few seconds.
        yield return new WaitForSeconds(3f);

        // 5. Fade glow off (puzzle stays solved, liquid stays visible)
        if (liquidRenderer != null)
            liquidRenderer.material.SetColor("_EmissionColor", Color.black);
        if (cauldronExteriorRenderer != null)
            cauldronExteriorRenderer.material.SetColor("_EmissionColor", Color.black);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FAIL
    // ─────────────────────────────────────────────────────────────────────────

    private void FailPuzzle()
    {
        failureCount++;
        Debug.Log($"<color=red>[Cauldron]</color> FAILURE #{failureCount}!");

        // Count wrong items using the hardcoded itemID set
        HashSet<string> remaining = new HashSet<string>(_winningIDs);
        List<ItemData> incorrectItems = new List<ItemData>();
        foreach (var item in currentIngredients)
        {
            if (!remaining.Remove(item.itemID))
                incorrectItems.Add(item);
        }

        Debug.Log($"<color=orange>[Cauldron]</color> Wrong items: {incorrectItems.Count} / {currentIngredients.Count}");
        foreach (var w in incorrectItems)
            Debug.Log($"  · Wrong: {w.itemName} (id='{w.itemID}')");

        if (llmEventTester != null)
        {
            string wrongName = incorrectItems.Count > 0 ? incorrectItems[0].itemName : "";
            if      (failureCount == 1) llmEventTester.TriggerCauldronHintTier1(incorrectItems.Count);
            else if (failureCount == 2) llmEventTester.TriggerCauldronHintTier2(wrongName);
            else                        llmEventTester.TriggerCauldronHintTier3(wrongName);
        }

        // Clear check states
        currentIngredients.Clear();
        targetColor = defaultColor;

        StartCoroutine(FailSequence(_activeIngredientObjects));
        _activeIngredientObjects = new List<GameObject>(); // Reset list for next try
    }

    private IEnumerator FailSequence(List<GameObject> objectsToReturn)
    {
        // 1. Shake exterior for 4 s
        yield return StartCoroutine(ShakeCauldron(4f));

        // 2. Soft warm-red glow AFTER shake
        Color softRed = new Color(0.8f, 0.1f, 0.05f);
        if (cauldronExteriorRenderer != null)
        {
            cauldronExteriorRenderer.material.EnableKeyword("_EMISSION");
            cauldronExteriorRenderer.material.SetColor("_EmissionColor", softRed * 1.8f);
        }

        // Hide liquid — potion discarded
        if (liquidRenderer != null) liquidRenderer.gameObject.SetActive(false);

        // 3. Hold red for 3 s
        yield return new WaitForSeconds(3f);

        // 4. Fade exterior glow off
        if (cauldronExteriorRenderer != null)
            cauldronExteriorRenderer.material.SetColor("_EmissionColor", Color.black);

        // 5. Return items to WORLD NOW (cauldron is visually reset and empty)
        // Same mechanics as collected, but in reverse (eject/pop out)
        foreach (var go in objectsToReturn)
        {
            if (go == null) continue;

            // Position it at the cauldron spawn point
            go.transform.position = transform.position + Vector3.up * 1.5f;
            go.SetActive(true);

            // Give it a little pop/eject physics if it has a Rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            
            Vector3 ejectForce = new Vector3(Random.Range(-2f, 2f), 5f, Random.Range(-2f, 2f));
            rb.AddForce(ejectForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        Debug.Log($"<color=cyan>[Cauldron]</color> {objectsToReturn.Count} items returned to the world.");
    }

    /// <summary>
    /// Shakes the cauldron left/right (Z-axis tilt) for the given duration.
    /// Called at the start of Win/Fail to mask LLM + Piper loading time.
    /// </summary>
    private void TriggerShake(float duration = 4f)
    {
        StartCoroutine(ShakeCauldron(duration));
    }

    private IEnumerator ShakeCauldron(float duration)
    {
        if (cauldronExteriorRenderer == null) yield break;

        Transform t      = cauldronExteriorRenderer.transform;
        float elapsed    = 0f;
        
        // Settings for oscillation
        float rotAmplitude = 3f;    // gentle ±3 degree tilt (Rotation Z)
        float posAmplitude = 0.05f; // small ±0.05m jitter (Position X/Y)
        float speed        = 8f;    // slightly faster for jitter feel

        Vector3 originalPos = t.localPosition;

        while (elapsed < duration)
        {
            float wave = Mathf.Sin(elapsed * speed * Mathf.PI * 2f);
            
            // 1. Rotation Shake (Z-axis rocking)
            float angle = wave * rotAmplitude;
            t.localRotation = _originalRotation * Quaternion.Euler(0f, 0f, angle);

            // 2. Position Shake (X and Y axis)
            float offsetX = wave * posAmplitude;
            float offsetY = Mathf.Cos(elapsed * speed * Mathf.PI * 2f) * posAmplitude; // Offset Y wave
            t.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        t.localRotation = _originalRotation; // snap back cleanly
        t.localPosition = originalPos;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private bool IsLookingAtCauldron()
    {
        if (playerCamera == null) return false;
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxFocusDistance))
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        return false;
    }

    private bool WasVRXPressed()
    {
        var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        if (leftHand.isValid &&
            leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xPressed))
            return xPressed;
        return false;
    }

    private void CreateFloatingText()
    {
        GameObject textObj = new GameObject("CauldronPromptText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, textHeightOffset, 0);

        floatingText = textObj.AddComponent<TextMeshPro>();
        floatingText.text = "Press 'E' to add item";
        floatingText.alignment = TextAlignmentOptions.Center;
        floatingText.fontSize = 4f;
        floatingText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC HELPERS (for external scripts)
    // ─────────────────────────────────────────────────────────────────────────

    public void FinalizeSubmission()
    {
        Debug.Log("<color=cyan>[Cauldron]</color> Robot reached station. Processing submission...");
    }

    public void TryAddSingleIngredient(ItemData data) => AddIngredient(data);
    public bool IsSolved => _isSolved;
    public int FailureCount => failureCount;
}