using System;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Serialization;

// This helper class matches the JSON schema we put in the System Prompt
[System.Serializable]
public class RobotResponse
{
    public string thought;
    public string hint;
    public string emotion;
}

public class RobotHintAI : MonoBehaviour, IRobotBrain
{
    [Header("Connections")]
    public LLMCharacter robotBrain; // Drag your 'MainHub/LLM Manager' here
    public TextMeshProUGUI outputText; // The main speech text box
    public TextMeshProUGUI headerText; // Dynamic header (e.g., "Consulting Zaldric...")
    [FormerlySerializedAs("piperSpeaker")]
    public PuterSpeaker puterSpeaker; // Drag Puter script here
    public PiperSpeakerComponent piperSpeaker; // Piper voice
    public Button helpButton; // The "Help" button (optional)

    [Header("Dialogue Options")]
    public GameObject dialogueUI;      // The "Full Screen" panel

    private string _fullResponseBuffer = "";
    public Transform buttonContainer;  // Where the 4 choice buttons live
    public GameObject buttonPrefab;    // A UI Button (TMP) prefab
    private string _hintPrompt = "I am stuck. Please give me a hint.";

    void Awake()
    {
        // Auto-find references if missing
        if (robotBrain == null)
        {
            robotBrain = GetComponentInParent<LLMCharacter>();
            if (robotBrain != null) Debug.Log($"[RobotHintAI] Auto-linked RobotBrain to {robotBrain.name}");
        }

        if (puterSpeaker == null && robotBrain != null)
        {
            puterSpeaker = robotBrain.GetComponent<PuterSpeaker>();
            if (puterSpeaker != null) Debug.Log($"[RobotHintAI] Auto-linked PuterSpeaker on {robotBrain.name}");
            
            ResolvePiperSpeaker();
        }

        if (helpButton == null)
        {
            helpButton = GetComponent<Button>();
            if (helpButton != null) Debug.Log("[RobotHintAI] Auto-linked HelpButton");
        }

        // Register with Player interaction system if available
        var playerInteract = FindObjectOfType<RobotInteract>();
        if (playerInteract != null)
        {
            playerInteract.RegisterBrain(this);
            Debug.Log("[RobotHintAI] Registered as active brain with RobotInteract");
        }
    }
    private string _npcPersonalityContext = "";
    private string[] _thinkingMessages;
    private bool _isGenerating = false;
    private bool _isPlayerNear = false;

    private void ResolvePiperSpeaker()
    {
        if (piperSpeaker != null) return;

        // 1. Try to find on the robot brain GameObject or its children
        if (robotBrain != null)
        {
            piperSpeaker = robotBrain.GetComponentInChildren<PiperSpeakerComponent>();
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
            Debug.Log($"[RobotHintAI] Auto-found PiperSpeaker: {piperSpeaker.name}");
            return;
        }

        // Final fallback if everything else fails (just pick the first one)
        piperSpeaker = all[0];
    }

    void Start()
    {
        if (helpButton != null) helpButton.onClick.AddListener(RequestHint);
    }

    // --- IRobotBrain Implementation ---

    public bool IsActive()
    {
        return enabled && !_isGenerating;
    }

    public void OnRobotInteracted(RobotInteract interact)
    {
        if (_isGenerating) return;
        RequestHint();
    }

    public void SetPrompt(string prompt)
    {
        if (robotBrain != null) robotBrain.SetPrompt(prompt);
    }

    /// <summary>
    /// Called by QuizManager when the user answers incorrectly, so the hint
    /// includes the specific question, topic, learning goal, and wrong answer.
    /// </summary>
    public void SetCurrentQuestion(QuestionData q, string wrongAnswer)
    {
        if (q == null) return;

        string wrongLine = !string.IsNullOrEmpty(wrongAnswer)
            ? $"\nMy answer was: {wrongAnswer}" : "";
        string topicLine = !string.IsNullOrEmpty(q.topic)
            ? $"\nTopic: {q.topic}" : "";
        string goalLine = !string.IsNullOrEmpty(q.learning_goal)
            ? $"\nLearning goal: {q.learning_goal}" : "";
        string correctLine = (q.options != null && q.correctIndex >= 0 && q.correctIndex < q.options.Length)
            ? $"\nThe correct answer is: {q.options[q.correctIndex]}" : "";

        /* Original long prompt:
        _hintPrompt = $"I answered a quiz question incorrectly and need a hint."
            + topicLine + goalLine
            + $"\n\nQuestion: {q.question}"
            + wrongLine
            + correctLine
            + "\n\nWithout giving the answer away, please give me a hint to help me understand the concept.";
        */

        _hintPrompt = $"[SYSTEM COMMAND: ANSWER WITH EXACTLY ONE WORD]"
            + topicLine + goalLine
            + $"\n\nQuestion: {q.question}"
            + wrongLine
            + correctLine
            + "\n\nWithout giving the answer away, give me a ONE WORD hint.";
    }

    // Called by QuizManager to configure this NPC's personality from world JSON
    public void SetNpcInfo(NpcData npc)
    {
        if (npc == null) return;

        // Build personality context for LLM requests
        string rules = npc.rules != null ? string.Join(". ", npc.rules) : "";
        _npcPersonalityContext = $"You are {npc.name}, a {npc.role}. " +
                                $"Personality: {npc.personality} " +
                                $"Rules: {rules}\n\n";

        // Apply LLM system prompt if provided in JSON
        if (robotBrain != null && !string.IsNullOrEmpty(npc.systemPrompt))
            robotBrain.SetPrompt(npc.systemPrompt);

        // Apply voice if provided in JSON
        if (headerText != null)
            headerText.text = $"Consulting... {npc.name}";

        // Apply voice if provided in JSON
        if (puterSpeaker != null && !string.IsNullOrEmpty(npc.voiceId))
        {
            puterSpeaker.SetVoice("elevenlabs", "eleven_multilingual_v2", npc.voiceId);
            puterSpeaker.SetFallbackVoice(npc.voiceId);
        }

        // Store thinking messages
        _thinkingMessages = npc.thinkingMessages;

        // Spawn dialogue option buttons if provided
        if (npc.dialogueOptions != null && npc.dialogueOptions.Length > 0
            && buttonContainer != null && buttonPrefab != null)
        {
            SpawnDialogueButtons(npc.dialogueOptions);
        }

        Debug.Log($"[RobotHintAI] Configured as '{npc.name}' ({npc.role})");
    }

    // Create clickable dialogue option buttons
    void SpawnDialogueButtons(string[] options)
    {
        // Clear old buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        foreach (string option in options)
        {
            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);

            TMP_Text label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = option;

            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                string captured = option; // capture for closure
                btn.onClick.AddListener(() => OnDialogueOptionClicked(captured));
            }
        }
    }

    // Player clicked one of the 4 dialogue option buttons
    async void OnDialogueOptionClicked(string selectedOption)
    {
        if (_isGenerating) return;
        _isGenerating = true;

        // Hide dialogue buttons while generating
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);

        // Show random thinking message
        ShowThinkingMessage();

        _fullResponseBuffer = "";

        if (robotBrain != null)
        {
            string message = _npcPersonalityContext + selectedOption;
            await robotBrain.Chat(message, HandleReply, HandleComplete);
        }
        else
        {
            _isGenerating = false;
            if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);
        }
    }

    async void RequestHint()
    {
        _isGenerating = false; // Safety reset
        if (helpButton != null) helpButton.interactable = false;

        if (outputText != null) outputText.text = "Wait.";
        _fullResponseBuffer = "";

        if (robotBrain != null)
        {
            await robotBrain.Chat(_hintPrompt, HandleReply, HandleComplete);
        }
        else
        {
            if (helpButton != null) helpButton.interactable = true;
        }
    }

    void HandleReply(string response)
    {
        _fullResponseBuffer = response;
    }

    void HandleComplete()
    {
        _isGenerating = false;
        if (helpButton != null) helpButton.interactable = true;

        // 1. Parse JSON from buffer
        string cleanedHint = "";
        
        if (string.IsNullOrEmpty(_fullResponseBuffer))
        {
            cleanedHint = "Failed";
        }
        else
        {
            try
            {
                RobotResponse data = JsonUtility.FromJson<RobotResponse>(_fullResponseBuffer);
                cleanedHint = (data != null && !string.IsNullOrEmpty(data.hint)) ? data.hint : _fullResponseBuffer;
            }
            catch
            {
                cleanedHint = _fullResponseBuffer;
            }
        }

        // Final safety check
        if (string.IsNullOrWhiteSpace(cleanedHint)) cleanedHint = "Failed";

        Debug.Log($"<color=cyan>Robot Answered:</color> {cleanedHint}");
        
        // 2. Show in UI
        if (outputText != null) outputText.text = cleanedHint;

        // 3. Speak
        ResolvePiperSpeaker();
        if (piperSpeaker != null && !string.IsNullOrEmpty(cleanedHint))
        {
            piperSpeaker.Speak(cleanedHint);
        }
        else if (puterSpeaker != null && !string.IsNullOrEmpty(cleanedHint))
        {
            puterSpeaker.Speak(cleanedHint);
        }

        // 4. Re-show dialogue buttons after a delay
        if (buttonContainer != null)
        {
            float delay = Mathf.Max(3f, cleanedHint.Length * 0.08f);
            Invoke(nameof(ShowDialogueButtons), delay);
        }
    }

    public void ShowDialogueButtons()
    {
        // Only show if player is actually near (if proximity is used)
        if (_isPlayerNear)
        {
            if (dialogueUI != null) dialogueUI.SetActive(true);
            if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);
        }
    }

    public void SetPlayerNear(bool isNear)
    {
        _isPlayerNear = isNear;
        if (isNear)
        {
            if (!_isGenerating) ShowDialogueButtons();
        }
        else
        {
            if (dialogueUI != null) dialogueUI.SetActive(false);
            if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
        }
    }

    void ShowThinkingMessage()
    {
        /* Original thinking logic preserved
        if (_thinkingMessages != null && _thinkingMessages.Length > 0)
        {
            string msg = _thinkingMessages[UnityEngine.Random.Range(0, _thinkingMessages.Length)];
            if (outputText != null) outputText.text = msg;
        }
        else
        {
            if (outputText != null) outputText.text = "Wait.";
        }
        */
        if (outputText != null) outputText.text = "Thinking";
    }

    // ── Robot Greeting ──────────────────────────────────────────────────────

    private Action _greetingCallback;

    public async void AskRobotAboutObject(string objectName, string objectLore)
    {
        if (_isGenerating) return;
        _isGenerating = true;
        // 1. Show thinking message and clear buffer
        ShowThinkingMessage();
        _fullResponseBuffer = "";
        // 2. Disable UI buttons while thinking
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
        // 3. Construct the character-aware prompt
        // We include the personality context so the robot stays in character!
        string prompt = _npcPersonalityContext +
                        $"[System Command: The player is pointing at '{objectName}'. Lore: '{objectLore}'. " +
                        "Explain this object in 1-2 short sentences in your character voice.]";
        Debug.Log($"<color=cyan>Robot is thinking about:</color> {objectName}");
        if (robotBrain != null)
        {
            // 4. Send to LLM
            await robotBrain.Chat(prompt, HandleReply, HandleComplete);
        }
        else
        {
            _isGenerating = false;
            Debug.LogError("RobotBrain (LLMCharacter) is not assigned on RobotHintAI!");
        }
    }

    public void SendGreeting(Action onDone)
    {
        if (robotBrain == null)
        {
            Debug.LogWarning("[RobotHintAI] No robotBrain assigned, skipping greeting.");
            onDone?.Invoke();
            return;
        }

        _greetingCallback = onDone;
        _isGenerating = true;
        _fullResponseBuffer = "";

        if (dialogueUI != null) dialogueUI.SetActive(true);
        if (headerText != null) headerText.text = "Incoming message...";
        ShowThinkingMessage();

        // string greetingPrompt =
        //     "You are Granite, a friendly robot assistant in the Main Hub of a VR quiz game. " +
        //     "Greet the player warmly. Introduce yourself and tell them you are " +
        //     "excited to help them on their adventure across four worlds. Keep it to 2-3 sentences.";
        string greetingPrompt = "Hello.";

        _ = robotBrain.Chat(greetingPrompt, HandleReply, HandleGreetingComplete);
    }

    private void HandleGreetingComplete()
    {
        _isGenerating = false;

        string text = "";
        try
        {
            RobotResponse data = JsonUtility.FromJson<RobotResponse>(_fullResponseBuffer);
            text = data.hint;
        }
        catch
        {
            text = _fullResponseBuffer;
        }

        if (string.IsNullOrEmpty(text)) text = "Hello.";

        if (outputText != null) outputText.text = text;
        
        ResolvePiperSpeaker();
        if (piperSpeaker != null)
        {
            piperSpeaker.Speak(text);
        }
        else if (puterSpeaker != null)
        {
            puterSpeaker.Speak(text);
        }

        float delay = Mathf.Max(3f, text.Length * 0.05f);
        Invoke(nameof(FireGreetingCallback), delay);
    }

    private void FireGreetingCallback()
    {
        if (dialogueUI != null) dialogueUI.SetActive(false);
        _greetingCallback?.Invoke();
        _greetingCallback = null;
    }

    /// <summary>
    /// Called by Cauldron when the player wins the puzzle.
    /// </summary>
    public async void GiveCauldronVictory()
    {
        if (_isGenerating) return;
        _isGenerating = true;

        string prompt = _npcPersonalityContext +
                        "[System Command: The player has successfully combined the 8 items in the cauldron! " +
                        "The potion is glowing yellow. In your character voice, give a SHORT (1-2 sentences) " +
                        "congratulatory message celebrating their victory and the completion of the ritual.]";

        ShowThinkingMessage();
        _fullResponseBuffer = "";

        if (robotBrain != null)
        {
            await robotBrain.Chat(prompt, HandleReply, HandleComplete);
        }
        else
        {
            _isGenerating = false;
        }
    }

    /// <summary>
    /// Provides a structured, restrictive hint based on the failure count.
    /// </summary>
    public async void GiveAdaptiveHint(int failCount, ItemData wrong, ItemData correct, string biome, int wrongCount)
    {
        if (_isGenerating) return;
        _isGenerating = true;

        string factToRewrite = "";
        
        if (failCount == 1)
        {
            factToRewrite = $"You have {wrongCount} items wrong in the cauldron. Try searching the worlds again!";
        }
        else if (failCount == 2)
        {
            factToRewrite = $"One of the items you added from the {biome} station is incorrect. You should double check that area!";
        }
        else
        {
            string wrongName = wrong != null ? wrong.itemName : "one of those";
            string correctName = correct != null ? correct.itemName : "something else";
            factToRewrite = $"The {wrongName} you added is actually incorrect. You were supposed to use the {correctName} instead!";
        }

        string prompt = _npcPersonalityContext + 
                        $"[System Command: Strictly rewrite the following fact in your character voice (Granite). " +
                        $"Do not add extra hints or reveals beyond this fact. Keep it to 1-2 short sentences. " +
                        $"Fact: {factToRewrite}]";

        ShowThinkingMessage();
        _fullResponseBuffer = "";

        if (robotBrain != null)
        {
            await robotBrain.Chat(prompt, HandleReply, HandleComplete);
        }
        else
        {
            _isGenerating = false;
        }
    }
}
