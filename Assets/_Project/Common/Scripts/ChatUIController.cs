using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using LLMUnity;
// using UnityPiper; // Uncomment if PiperSpeaker is defined in a namespace

public class ChatUIController : MonoBehaviour
{
    [Header("AI & Voice Components")]
    [Tooltip("The GameObject with the LLMCharacter script")]
    public LLMCharacter aiCharacter;
    [Tooltip("The GameObject with the PuterSpeaker script")]
    public PuterSpeaker puterSpeaker;
    public PiperSpeakerComponent piperSpeaker;


    [Header("Context & Situations")]
    public AntiGravity.AI.SituationManager situationManager;

    [Header("UI References")]
    public GameObject chatPanel;
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform messageContainer;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    // NPC identity (set per-world by QuizManager)
    private string _npcName = "Elara";
    private string _npcPersonalityContext = "";

    // Internal state
    private string _fullResponseBuffer = "";
    private bool _isGenerating = false;

    // JSON Structure matching your System Prompt
    [System.Serializable]
    private struct RobotResponse
    {
        public string thought;
        public string hint;
        public string emotion;
    }

    private void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener((msg) => OnSendClicked());

        // Auto-find speaker if not assigned
        if (aiCharacter != null) 
        {
            if (puterSpeaker == null) puterSpeaker = aiCharacter.GetComponent<PuterSpeaker>();
            ResolvePiperSpeaker();
        }
    }

    private void ResolvePiperSpeaker()
    {
        if (piperSpeaker != null) return;

        // 1. Try to find on the AI character GameObject or its children
        if (aiCharacter != null)
        {
            piperSpeaker = aiCharacter.GetComponentInChildren<PiperSpeakerComponent>();
            if (piperSpeaker != null) return;
        }

        // 2. Try to find on the same object as this script
        piperSpeaker = GetComponentInChildren<PiperSpeakerComponent>();
        if (piperSpeaker != null) return;

        // 3. Fallback: Search globally but pick the best one
        var all = FindObjectsOfType<PiperSpeakerComponent>();
        if (all.Length == 0) return;
        
        foreach (var p in all)
        {
            // Ignore the wizard speaker if we are looking for the robot speaker
            if (p.gameObject.name.ToLower().Contains("wizard")) continue;
            
            piperSpeaker = p;
            Debug.Log($"[ChatUIController] Auto-found PiperSpeaker: {piperSpeaker.name}");
            return;
        }

        // Final fallback if everything else fails (just pick the first one)
        piperSpeaker = all[0];
    }

    private void OnSendClicked()
    {
        if (_isGenerating || string.IsNullOrWhiteSpace(inputField.text)) return;

        string originalUserMessage = inputField.text;
        inputField.text = ""; // Clear input

        // Inject NPC personality and situation context
        string processedMessage = _npcPersonalityContext + originalUserMessage;
        if (situationManager != null)
        {
            processedMessage = situationManager.GetSituationContext() + "\n\n" + processedMessage;
        }

        // 1. Show Player Message
        SpawnMessage("You", originalUserMessage, Color.green);

        // 2. Show "Thinking..." Placeholder for NPC
        SpawnMessage(_npcName, "<i>Thinking...</i>", Color.cyan);

        _isGenerating = true;
        _fullResponseBuffer = ""; // Reset buffer

        // 3. Send to LLM
        _ = aiCharacter.Chat(processedMessage,
            OnPartialResponse,
            OnCompleteResponse
        );
    }

    private void OnPartialResponse(string response)
    {
        _fullResponseBuffer = response;
        UpdateLastMessage(_npcName, "<i>Thinking...</i>");
    }

    private void OnCompleteResponse()
    {
        _isGenerating = false;

        // 4. Parse the JSON Buffer
        string finalHintText = "";

        try
        {
            // Attempt to parse the strictly formatted JSON
            RobotResponse response = JsonUtility.FromJson<RobotResponse>(_fullResponseBuffer);
            finalHintText = response.hint;
        }
        catch
        {
            // Fallback: If the LLM messed up the JSON, just show the raw text
            // or use simple string manipulation to find the "hint"
            Debug.LogWarning("JSON Parse Failed, showing raw output.");
            finalHintText = _fullResponseBuffer;
        }

        // 5. Update the UI (Replace "Thinking..." with actual text)
        // (Simplification: We just spawn a new correct message for now)
        UpdateLastMessage(_npcName, finalHintText);

        // 6. Trigger Text-to-Speech
        ResolvePiperSpeaker();
        if (piperSpeaker != null && !string.IsNullOrEmpty(finalHintText))
        {
            piperSpeaker.Speak(finalHintText);
        }
        else if (puterSpeaker != null && !string.IsNullOrEmpty(finalHintText))
        {
            puterSpeaker.Speak(finalHintText);
        }
    }

    // --- Helper Methods ---

    private void SpawnMessage(string sender, string text, Color color)
    {
        GameObject bubble = Instantiate(messagePrefab, messageContainer);
        bubble.GetComponentInChildren<TMP_Text>().text = $"<b>{sender}:</b> {text}";
        bubble.GetComponent<Image>().color = color;

        // Ensure the bubble auto-sizes to fit its text content
        if (bubble.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = bubble.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        ForceScroll();
    }

    private void UpdateLastMessage(string sender, string newText)
    {
        if (messageContainer.childCount > 0)
        {
            Transform lastBubble = messageContainer.GetChild(messageContainer.childCount - 1);
            lastBubble.GetComponentInChildren<TMP_Text>().text = $"<b>{sender}:</b> {newText}";

            // Force the bubble and container to recalculate size for the new text
            LayoutRebuilder.ForceRebuildLayoutImmediate(lastBubble as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);

            ForceScroll();
        }
    }

    /// <summary>
    /// Sets the NPC identity for this chat session based on the current world.
    /// </summary>
    public void SetNpcInfo(NpcData npc)
    {
        if (npc == null) return;
        _npcName = npc.name;

        string rules = npc.rules != null ? string.Join(". ", npc.rules) : "";
        _npcPersonalityContext = $"You are {npc.name}, a {npc.role}. " +
                                $"Personality: {npc.personality} " +
                                $"Rules: {rules}\n\n";

        // Apply voice if provided in JSON
        if (puterSpeaker != null && !string.IsNullOrEmpty(npc.voiceId))
        {
            puterSpeaker.SetVoice("elevenlabs", "eleven_multilingual_v2", npc.voiceId);
            Debug.Log($"[ChatUIController] Applied voice '{npc.voiceId}' for {npc.name}");
        }
    }

    /// <summary>
    /// Called externally (e.g. from QuizManager) to auto-send a message on behalf of the user.
    /// <param name="messageToLLM">The full context-rich message sent to the LLM.</param>
    /// <param name="displayMessage">What is shown to the user in the chat UI. Defaults to messageToLLM if null.</param>
    /// </summary>
    public void SendAutoMessage(string messageToLLM, string displayMessage = null)
    {
        if (_isGenerating || string.IsNullOrEmpty(messageToLLM)) return;

        string processedMessage = _npcPersonalityContext + messageToLLM;
        if (situationManager != null)
        {
            processedMessage = situationManager.GetSituationContext() + "\n\n" + processedMessage;
        }

        SpawnMessage("You", displayMessage ?? messageToLLM, Color.green);
        SpawnMessage(_npcName, "<i>Thinking...</i>", Color.cyan);

        _isGenerating = true;
        _fullResponseBuffer = "";

        _ = aiCharacter.Chat(processedMessage,
            OnPartialResponse,
            OnCompleteResponse
        );
    }

    private void ForceScroll()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}