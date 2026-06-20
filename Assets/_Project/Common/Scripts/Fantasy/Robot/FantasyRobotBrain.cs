using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

public class FantasyRobotBrain : MonoBehaviour, IRobotBrain
{
    [Header("LLM")]
    [SerializeField] private LLMCharacter byteBrain;

    [Header("Timing")]
    [SerializeField] private int llmTimeoutMs = 5000;

    private bool isGenerating = false;
    private Queue<string> interactionHistory = new Queue<string>(3);
    private int cauldronFailCount = 0;

    private const string BYTE_SYSTEM_PROMPT =
        "You are Byte, a small robot companion in a fantasy world. " +
        "You help a player answer quiz questions by giving short hints. " +
        "Never reveal the answer directly. Never say the name of the correct answer. " +
        "Keep responses under 20 words. " +
        "Be encouraging and friendly. Reply with ONLY the hint sentence. " +
        "No JSON. No formatting. No quotes.";

    private RobotInteract robotInteract;
    private PiperSpeakerComponent piperTTS;

    private void Start()
    {
        if (byteBrain != null)
        {
            byteBrain.SetPrompt(BYTE_SYSTEM_PROMPT);
            Debug.Log("[FantasyBrain] LLM connected and prompt set");
        }
        else
        {
            Debug.Log("[FantasyBrain] No byteBrain assigned, using fallback lines only");
        }
    }

    private void OnEnable()
    {
        robotInteract = FindObjectOfType<RobotInteract>();
        if (robotInteract != null)
        {
            robotInteract.RegisterBrain(this);
            ResolvePiperTTS(robotInteract);
        }
    }

    private void ResolvePiperTTS(RobotInteract interact)
    {
        if (piperTTS != null) return;
        if (interact == null) return;

        if (interact.RobotTransform != null)
        {
            piperTTS = interact.RobotTransform.GetComponentInChildren<PiperSpeakerComponent>();
            if (piperTTS != null) return;
        }

        piperTTS = interact.GetComponentInChildren<PiperSpeakerComponent>();
        if (piperTTS != null) return;

        var all = FindObjectsOfType<PiperSpeakerComponent>();
        if (all.Length == 0) return;

        foreach (var p in all)
        {
            if (p.gameObject.name.ToLower().Contains("wizard")) continue;
            piperTTS = p;
            return;
        }

        piperTTS = all[0];
    }

    private void OnDisable()
    {
        if (robotInteract != null)
        {
            robotInteract.UnregisterBrain();
        }
    }

    public bool IsActive()
    {
        return enabled && !isGenerating;
    }

    public void OnRobotInteracted(RobotInteract interact)
    {
        TriggerContextualResponse(interact);
    }

    public void OnCauldronFailed(CauldronResult result)
    {
        cauldronFailCount++;

        string hint;

        if (cauldronFailCount == 1)
        {
            hint = $"{result.total - result.correct} ingredients were wrong. Try again!";
        }
        else if (cauldronFailCount == 2)
        {
            hint = "Check your answers more carefully. Re-read the books.";
        }
        else
        {
            hint = "Take your time. The answers are all in the books.";
        }

        if (robotInteract != null && robotInteract.SpeechBubble != null)
        {
            ResolvePiperTTS(robotInteract);
            if (piperTTS != null) piperTTS.Speak(hint);
            robotInteract.SpeechBubble.Say(new string[] { hint });
        }
    }

    // ─── Main Response Logic ─────────────────────────────
    private async void TriggerContextualResponse(RobotInteract interact)
    {
        isGenerating = true;

        string context = DetermineContext();
        string fallbackLine = GetFallbackLine(context);

        string finalHint;

        if (context == "STATION_HINT")
        {
            finalHint = await TryStationHint(fallbackLine);
        }
        else
        {
            finalHint = await TryLLMPolish(fallbackLine);
        }

        if (finalHint.Contains("{") || finalHint.Contains("}"))
        {
            finalHint = fallbackLine;
        }

        ResolvePiperTTS(interact);
        if (piperTTS != null) piperTTS.Speak(finalHint);

        if (interact?.SpeechBubble != null)
        {
            interact.SpeechBubble.Say(new string[] { finalHint });
        }

        if (interactionHistory.Count >= 3) interactionHistory.Dequeue();
        interactionHistory.Enqueue(finalHint);

        isGenerating = false;
    }

    // ─── Find Nearest Station ────────────────────────────
    private FantasyStationConfig FindNearestStationConfig()
    {
        FantasyQuizStation[] stations = FindObjectsOfType<FantasyQuizStation>();
        Transform playerTransform = Camera.main?.transform;

        if (playerTransform == null) return null;

        float closestDist = float.MaxValue;
        FantasyStationConfig closest = null;

        foreach (FantasyQuizStation station in stations)
        {
            float dist = Vector3.Distance(playerTransform.position, station.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = station.Config;
            }
        }

        if (closestDist < 15f && closest != null)
        {
            return closest;
        }

        return null;
    }

    // ─── Resolve Correct Answer Names ────────────────────
    private string GetCorrectAnswerText(FantasyStationConfig config)
    {
        List<string> correctNames = new List<string>();

        foreach (string correctID in config.correctIngredientIDs)
        {
            foreach (AnswerMapping answer in config.answers)
            {
                if (answer.ingredientID == correctID)
                {
                    correctNames.Add(answer.answerText);
                    break;
                }
            }
        }

        return string.Join(", ", correctNames);
    }

    // ─── Resolve Player's Current Pick ───────────────────
    private string GetPlayerPickText(FantasyStationConfig config)
    {
        if (IngredientTracker.Instance == null) return null;
        if (!IngredientTracker.Instance.HasChosenFromStation(config.stationID)) return null;

        List<ChoiceRecord> choices = IngredientTracker.Instance.GetAllChoiceRecordsForStation(config.stationID);
        if (choices.Count == 0) return null;

        List<string> pickNames = new List<string>();
        foreach (ChoiceRecord choice in choices)
        {
            // Find the answer text for this ingredient
            foreach (AnswerMapping answer in config.answers)
            {
                if (answer.ingredientID == choice.data.ingredientID)
                {
                    pickNames.Add(answer.answerText);
                    break;
                }
            }
        }

        return pickNames.Count > 0 ? string.Join(", ", pickNames) : null;
    }

    // ─── Station-Specific LLM Hint ───────────────────────
    private async Task<string> TryStationHint(string fallbackLine)
    {
        FantasyStationConfig config = FindNearestStationConfig();

        if (config == null || byteBrain == null)
        {
            return fallbackLine;
        }

        // Build answer list (without marking which is correct)
        string answerList = "";
        foreach (AnswerMapping answer in config.answers)
        {
            answerList += $"- {answer.answerText}\n";
        }

        // Get the correct answer text for the LLM to hint toward
        string correctAnswer = GetCorrectAnswerText(config);

        // Check what the player already picked
        string playerPickInfo = "";
        string playerPick = GetPlayerPickText(config);
        if (playerPick != null)
        {
            // Check if their pick is correct
            bool isCorrect = false;
            List<ChoiceRecord> choices = IngredientTracker.Instance.GetAllChoiceRecordsForStation(config.stationID);
            foreach (ChoiceRecord choice in choices)
            {
                foreach (string correctID in config.correctIngredientIDs)
                {
                    if (choice.data.ingredientID == correctID)
                    {
                        isCorrect = true;
                        break;
                    }
                }
            }

            if (isCorrect)
            {
                playerPickInfo = $"\nThe player already chose \"{playerPick}\" which is correct, " +
                    "but do NOT confirm this. Just encourage them to keep going.";
            }
            else
            {
                playerPickInfo = $"\nThe player already chose \"{playerPick}\" which is wrong. " +
                    "Gently hint that they might want to reconsider, without saying " +
                    "which answer is correct.";
            }
        }

        string history = string.Join(" | ", interactionHistory);

        string prompt =
            $"The player is at a quiz station in a fantasy world.\n\n" +
            $"Question: \"{config.question}\"\n\n" +
            $"Answer options:\n{answerList}\n" +
            $"The correct answer is: \"{correctAnswer}\"\n" +
            $"{playerPickInfo}\n\n" +
            $"Give a short hint (under 20 words) that helps the player think " +
            $"about the topic of the question and nudges them toward the correct " +
            $"answer WITHOUT saying the answer name directly. " +
            $"Do not say \"{correctAnswer}\" in your hint. " +
            $"Do not repeat these previous hints: {history}\n\n" +
            $"Hint:";

        try
        {
            string rawResponse = "";
            Task llmTask = byteBrain.Chat(prompt,
                (response) => rawResponse = response,
                () => { }
            );

            Task delayTask = Task.Delay(llmTimeoutMs);
            Task completed = await Task.WhenAny(llmTask, delayTask);

            if (completed == llmTask && !string.IsNullOrEmpty(rawResponse))
            {
                string cleaned = CleanLLMResponse(rawResponse);

                if (!string.IsNullOrEmpty(cleaned) && cleaned.Length < 150)
                {
                    // Safety check: make sure the hint doesn't contain the answer
                    string lowerCleaned = cleaned.ToLower();
                    string lowerCorrect = correctAnswer.ToLower();

                    if (lowerCleaned.Contains(lowerCorrect))
                    {
                        Debug.LogWarning($"[FantasyBrain] LLM leaked answer '{correctAnswer}', using fallback");
                        return fallbackLine;
                    }

                    return cleaned;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FantasyBrain] Station hint LLM error: {e.Message}");
        }

        return fallbackLine;
    }

    // ─── Generic LLM Polish ──────────────────────────────
    private async Task<string> TryLLMPolish(string fallbackLine)
    {
        if (byteBrain == null)
        {
            return fallbackLine;
        }

        try
        {
            string history = string.Join(" | ", interactionHistory);
            string polishPrompt =
                $"Rewrite this sentence with different wording. Keep under 15 words. " +
                $"Do not repeat these previous lines: {history}\n\n" +
                $"Sentence: {fallbackLine}\n\n" +
                $"Rewritten:";

            string rawResponse = "";
            Task llmTask = byteBrain.Chat(polishPrompt,
                (response) => rawResponse = response,
                () => { }
            );

            Task delayTask = Task.Delay(llmTimeoutMs);
            Task completed = await Task.WhenAny(llmTask, delayTask);

            if (completed == llmTask && !string.IsNullOrEmpty(rawResponse))
            {
                string cleaned = CleanLLMResponse(rawResponse);

                if (!string.IsNullOrEmpty(cleaned) && cleaned.Length < 100)
                {
                    return cleaned;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FantasyBrain] LLM error: {e.Message}");
        }

        return fallbackLine;
    }

    // ─── Context Detection ───────────────────────────────
    private string DetermineContext()
    {
        int collected = IngredientTracker.Instance != null
            ? IngredientTracker.Instance.TotalCollected
            : 0;

        var phase = FantasyPhaseManager.currentPhase;

        if (phase == FantasyPhaseManager.WorldPhase.Phase2)
        {
            if (GameState.currentState == GameState.PlayerState.Station)
                return "PHASE2_STATION";
            return "PHASE2_EXPLORE";
        }

        if (phase == FantasyPhaseManager.WorldPhase.Phase3)
        {
            if (GameState.currentState == GameState.PlayerState.Station)
                return "PHASE3_STATION";
            return "PHASE3_EXPLORE";
        }

        if (phase == FantasyPhaseManager.WorldPhase.Complete)
        {
            return "WORLD_COMPLETE";
        }

        if (GameState.currentState == GameState.PlayerState.Station)
        {
            return "STATION_HINT";
        }

        if (collected >= 8)
        {
            return "GO_CAULDRON";
        }

        if (collected > 0)
        {
            return "PROGRESS";
        }

        return "EXPLORE";
    }

    // ─── Fallback Lines ──────────────────────────────────
    private string GetFallbackLine(string context)
    {
        int collected = IngredientTracker.Instance != null
            ? IngredientTracker.Instance.TotalCollected
            : 0;

        switch (context)
        {
            case "STATION_HINT":
                string[] stationHints = {
                    "Read the book carefully. The answer points to an ingredient.",
                    "Take your time. Think about what the question is really asking.",
                    "Look around the station. One of these ingredients is the answer.",
                    "The book holds the clue. Match the answer to an ingredient.",
                    "Trust your knowledge. Pick the ingredient that fits best."
                };
                return stationHints[Random.Range(0, stationHints.Length)];

            case "GO_CAULDRON":
                string[] cauldronLines = {
                    "All ingredients collected! Head to the cauldron.",
                    "We have everything. Time for the ritual!",
                    "The cauldron awaits. Let's see if our choices were right."
                };
                return cauldronLines[Random.Range(0, cauldronLines.Length)];

            case "PROGRESS":
                string[] progressLines = {
                    $"We have {collected} ingredients so far. Keep exploring!",
                    $"{collected} down, more to find. Check the other stations.",
                    $"Good progress! {collected} ingredients collected."
                };
                return progressLines[Random.Range(0, progressLines.Length)];

            case "EXPLORE":
                string[] exploreLines = {
                    "Look for stations around the world. Each has a question.",
                    "The wizard needs our help. Let's find those ingredients!",
                    "Explore the area. Stations are where you'll find ingredients."
                };
                return exploreLines[Random.Range(0, exploreLines.Length)];

            case "PHASE2_STATION":
                string[] p2Station = {
                    "Focus on the question. You've got this.",
                    "Take your time and think it through.",
                    "The answer is in the material. Trust what you learned."
                };
                return p2Station[Random.Range(0, p2Station.Length)];

            case "PHASE2_EXPLORE":
                string[] p2Explore = {
                    "The next challenge awaits. Keep going!",
                    "One step closer to healing the dragon.",
                    "Look for the next station. We are almost done."
                };
                return p2Explore[Random.Range(0, p2Explore.Length)];

            case "PHASE3_STATION":
                string[] p3Station = {
                    "Almost there. Think carefully about this one.",
                    "You have come so far. Trust your knowledge.",
                    "Read closely. The answer will come to you."
                };
                return p3Station[Random.Range(0, p3Station.Length)];

            case "PHASE3_EXPLORE":
                string[] p3Explore = {
                    "The final challenges lie ahead.",
                    "We are in the home stretch. Keep going!",
                    "One last push. The dragon is counting on us."
                };
                return p3Explore[Random.Range(0, p3Explore.Length)];

            case "WORLD_COMPLETE":
                string[] complete = {
                    "We did it! The dragon is saved!",
                    "All challenges complete. What an adventure.",
                    "The amulet is ours. Well done!"
                };
                return complete[Random.Range(0, complete.Length)];

            default:
                return "Let me know if you need help.";
        }
    }

    // ─── Response Cleanup ────────────────────────────────
    private string CleanLLMResponse(string raw)
    {
        string cleaned = raw.Trim();

        if (cleaned.StartsWith("{"))
        {
            string hint = ExtractJsonField(cleaned, "Hint");
            if (!string.IsNullOrEmpty(hint)) return hint;

            hint = ExtractJsonField(cleaned, "hint");
            if (!string.IsNullOrEmpty(hint)) return hint;

            string sentence = ExtractJsonField(cleaned, "sentence");
            if (!string.IsNullOrEmpty(sentence)) return sentence;

            return null;
        }

        string[] prefixes = { "Rewritten:", "Here:", "Response:",
            "Output:", "Sentence:", "Hint:" };
        foreach (string prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
            }
        }

        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2);
        }

        while (cleaned.EndsWith(".."))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 1);
        }

        return cleaned;
    }

    private string ExtractJsonField(string raw, string fieldName)
    {
        var pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"}}]+)";
        var match = System.Text.RegularExpressions.Regex.Match(raw, pattern);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }
        return "";
    }
}