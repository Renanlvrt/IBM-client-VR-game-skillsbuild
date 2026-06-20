using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // <-- Added this to use TextMeshPro!

public class ObjectLoreInspector : MonoBehaviour
{
    [Header("Lore Settings")]
    public string objectName = "Mystery Object";
    [TextArea(3, 5)]
    public string objectDescription = "Provide the lore or usage for this object here.";

    [Header("Inventory Settings")]
    [Tooltip("If checked, this object can be picked up with the 'X' key.")]
    public bool isPickable = true;
    [Tooltip("Persistent data to add to inventory when picked up.")]
    public ItemData itemData;

    [Header("UI Settings")]
    [Tooltip("How high above the object the text should float.")]
    public float textHeightOffset = 1.0f;

    private bool isPlayerNearby = false;
    private RobotHintAI robotAI;

    // Variables for the floating text
    private TextMeshPro floatingText;
    private Transform playerCamera;

    void Start()
    {
        robotAI = FindObjectOfType<RobotHintAI>();

        // Grab the main camera so the text knows what to look at
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // Automatically generate the 3D text object
        CreateFloatingText();
    }

    void Update()
    {
        if (isPlayerNearby)
        {
            // --- BILLBOARDING LOGIC ---
            // Make the text face the camera. We subtract the camera position 
            // from the text position so the text doesn't render backwards!
            if (playerCamera != null)
            {
                floatingText.transform.rotation = Quaternion.LookRotation(floatingText.transform.position - playerCamera.position);
            }

            // --- INPUT LOGIC ---

            // 1. Robot Logic (Y key or E key)
            if (Keyboard.current != null && (Keyboard.current.yKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                TriggerRobotSpeech();
            }

            // 2. Pickup Logic (X key / VR X button)
            if (isPickable)
            {
                if (WasXKeyPressed())
                {
                    TriggerPickup();
                }
            }
        }
    }

    private bool WasXKeyPressed()
    {
        // Keyboard 'X'
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
            return true;

        // VR 'X' button (Left Hand Primary)
        var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        if (leftHand.isValid && leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xPressed))
        {
            return xPressed;
        }

        return false;
    }

    private void CreateFloatingText()
    {
        // 1. Create a new empty GameObject child
        GameObject textObj = new GameObject("HoverPromptText");
        textObj.transform.SetParent(this.transform);

        // 2. Position it directly above the object using your height offset
        textObj.transform.localPosition = new Vector3(0, textHeightOffset, 0);

        // 3. Add the TextMeshPro component and style it
        floatingText = textObj.AddComponent<TextMeshPro>();
        
        string promptText = "Press 'E' or 'Y' to inspect";
        if (isPickable)
            promptText += "\nPress 'X' to pickup";
            
        floatingText.text = promptText;
        floatingText.alignment = TextAlignmentOptions.Center;
        floatingText.fontSize = 3f; // Adjust this if the text is too big/small

        // 4. Hide it until the player walks close
        floatingText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            floatingText.gameObject.SetActive(true); // Show the text!
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            floatingText.gameObject.SetActive(false); // Hide the text!
        }
    }

    private void TriggerRobotSpeech()
    {
        if (robotAI != null)
        {
            string prompt = $@"
            [System Command: You are a robotic game guide. 
            Object: '{objectName}'
            Game Hint: '{objectDescription}'
            
            You MUST reply with EXACTLY three sentences following this strict structure. Do not add any outside lore.
            
            Sentence 1 - Identification: Acknowledge the item. 
            Sentence 2 - The Hint: State the 'Game Hint' clearly. 
            Sentence 3 - The Action: Instruct the player to check their book to see if it belongs in the cauldron.
            
            Speak in your robotic personality, but stick strictly to this 3-sentence blueprint.]";

            robotAI.AskRobotAboutObject(objectName, prompt);
        }
    }

    private void TriggerPickup()
    {
        Debug.Log($"<color=yellow>[Interaction]</color> Picking up: {objectName}");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemData);
        }
        else
        {
            Debug.LogWarning("InventoryManager instance not found! Item was not added.");
        }

        // Disable the object
        gameObject.SetActive(false);
    }
}