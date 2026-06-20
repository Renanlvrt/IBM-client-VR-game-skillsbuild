using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuizManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizUI quizUI;
    [SerializeField] private IntroUI introUI;
    [SerializeField] private GameObject chatPanel; // drag ChatPanel here to enable "Ask AI"

    [Header("Which JSON to load")]
    [SerializeField] private string worldId = "cyberpunk"; 
    // Loads: Assets/StreamingAssets/Data/Worlds/<worldId>.json

    private const int MaxRetries = 3;
    private const int MaxAIUses = 3;

    [Header("Hint AI (optional)")]
    [SerializeField] private RobotHintAI robotHintAI;

    private QuizData quizData;
    private NpcData currentNpc;
    private int currentIndex = 0;
    private int score = 0;
    private int retriesUsed = 0;
    private int aiUsesLeft = MaxAIUses;
    private string lastWrongAnswer = "";

    private IEnumerator Start()
    {
        if (quizUI == null)
        {
            Debug.LogError("[QuizManager] quizUI is not assigned. Drag QuizPanel (QuizUI component) into QuizManager.", this);
            yield break;
        }

        quizUI.BindOptionHandler(SubmitAnswer);
        quizUI.BindWrongAnswerHandlers(OnRetry, OnAskAI);

        yield return LoadQuiz(worldId);

        if (quizData == null || quizData.questions == null || quizData.questions.Length == 0)
        {
            Debug.LogError("[QuizManager] Loaded quizData is empty or invalid.", this);
            yield break;
        }

        currentIndex = 0;
        score = 0;
        retriesUsed = 0;

        // Configure RobotHintAI with NPC personality
        if (currentNpc != null)
        {
            var hintAI = FindObjectOfType<RobotHintAI>();
            if (hintAI != null) hintAI.SetNpcInfo(currentNpc);
        }

        // Hide quiz and chat panels while intro is showing
        quizUI.gameObject.SetActive(false);
        if (chatPanel != null) chatPanel.SetActive(false);

        // Show intro page first, then proceed to NPC intro / quiz
        if (introUI != null)
        {
            string desc = quizData.description ?? "";
            introUI.Show(desc, currentNpc, OnIntroComplete);
        }
        else
        {
            OnIntroComplete();
        }
    }

    private void OnIntroComplete()
    {
        // Let the robot greet the player before the quiz NPC intro
        var hintAI = robotHintAI != null ? robotHintAI : FindObjectOfType<RobotHintAI>();
        if (hintAI != null)
            hintAI.SendGreeting(ProceedToNpcIntro);
        else
            ProceedToNpcIntro();
    }

    private void ProceedToNpcIntro()
    {
        // Re-show quiz panel now that greeting is done
        quizUI.gameObject.SetActive(true);

        if (currentNpc != null)
            quizUI.ShowNpcIntro(currentNpc, ShowCurrentQuestion);
        else
            ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (quizData == null || quizData.questions == null) return;

        if (currentIndex < 0 || currentIndex >= quizData.questions.Length)
        {
            int total = quizData.questions.Length;
            bool isNewBest = ScoreLog.RecordScore(worldId, score, total);
            int best = ScoreLog.GetBestScore(worldId);
            quizUI.ShowQuizComplete(score, total, best, isNewBest);
            return;
        }

        quizUI.DisplayQuestion(quizData.questions[currentIndex], currentIndex + 1, quizData.questions.Length);
    }

    private void SubmitAnswer(int selectedIndex)
    {
        var q = quizData.questions[currentIndex];

        bool correct = (selectedIndex == q.correctIndex);
        if (correct)
        {
            score++;
            quizUI.ShowFeedback(true);
            StartCoroutine(NextQuestionAfterDelay(0.75f));
        }
        else
        {
            retriesUsed++;
            lastWrongAnswer = (q.options != null && selectedIndex >= 0 && selectedIndex < q.options.Length)
                ? q.options[selectedIndex]
                : "";

            if (retriesUsed >= MaxRetries)
            {
                string correctText = q.options[q.correctIndex];
                quizUI.ShowCorrectAnswer(correctText);
                WrongAnswerLog.LogWrongAnswer(worldId, q);
                StartCoroutine(NextQuestionAfterDelay(2f));
            }
            else
            {
                quizUI.ShowFeedback(false);
                quizUI.ShowWrongAnswerButtons(aiUsesLeft > 0);

                // Give the hint AI full context about the current question
                if (robotHintAI != null)
                    robotHintAI.SetCurrentQuestion(q, lastWrongAnswer);
            }
        }
    }

    private void OnRetry()
    {
        ShowCurrentQuestion();
    }

    private void OnAskAI()
    {
        if (chatPanel == null || aiUsesLeft <= 0) return;

        aiUsesLeft--;
        chatPanel.SetActive(true);

        // Log the question as wrong before advancing
        if (quizData != null && currentIndex < quizData.questions.Length)
            WrongAnswerLog.LogWrongAnswer(worldId, quizData.questions[currentIndex]);

        var chatUI = chatPanel.GetComponent<ChatUIController>();
        if (chatUI != null && quizData != null && currentIndex < quizData.questions.Length)
        {
            if (currentNpc != null)
                chatUI.SetNpcInfo(currentNpc);

            var q = quizData.questions[currentIndex];
            string wrongAnswerLine = !string.IsNullOrEmpty(lastWrongAnswer)
                ? $"\nMy wrong answer was: {lastWrongAnswer}" : "";
            string topicLine = !string.IsNullOrEmpty(q.topic)
                ? $"\nTopic: {q.topic}" : "";
            string goalLine = !string.IsNullOrEmpty(q.learning_goal)
                ? $"\nLearning goal: {q.learning_goal}" : "";
            string helpMessage = $"I got this quiz question wrong, can you help me understand it?"
                + topicLine + goalLine
                + $"\n\nQuestion: {q.question}"
                + wrongAnswerLine
                + $"\nThe correct answer is: {q.options[q.correctIndex]}"
                + "\n\nPlease explain why the correct answer is right and why my answer was wrong.";
            chatUI.SendAutoMessage(helpMessage, "I got this question wrong, can you help me understand it?");
        }

        currentIndex++;
        retriesUsed = 0;
        ShowCurrentQuestion();
    }

    private IEnumerator NextQuestionAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        currentIndex++;
        retriesUsed = 0;
        ShowCurrentQuestion();
    }

    private IEnumerator LoadQuiz(string id)
    {
        string fileName = $"{id}.json";
        string relativePath = $"Data/Worlds/{fileName}";
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);

        // UnityWebRequest needs file:// on some platforms
        string url = fullPath;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            url = "file://" + url;
        }

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[QuizManager] Failed to load quiz JSON: {relativePath}\nPath: {fullPath}\nError: {req.error}", this);
                yield break;
            }

            string json = req.downloadHandler.text;

            try
            {
                quizData = JsonUtility.FromJson<QuizData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuizManager] JSON parse error for {fileName}: {e.Message}", this);
                yield break;
            }

            // Extract NPC data (JsonUtility can't handle dictionary keys)
            currentNpc = ExtractNpc(json);
            if (currentNpc != null)
                Debug.Log($"[QuizManager] Loaded NPC: {currentNpc.name} ({currentNpc.role})", this);

            // Validate basic structure
            if (quizData?.questions == null)
            {
                Debug.LogError($"[QuizManager] JSON parsed but 'questions' is missing/null in {fileName}.", this);
                yield break;
            }

            // Validate and resolve correct_answer → correctIndex
            for (int i = 0; i < quizData.questions.Length; i++)
            {
                var q = quizData.questions[i];
                if (q == null || string.IsNullOrWhiteSpace(q.question))
                    Debug.LogWarning($"[QuizManager] Question {i} is missing 'question' text.", this);

                if (q.options == null || q.options.Length != 4)
                    Debug.LogWarning($"[QuizManager] Question {i} should have exactly 4 options.", this);

                // If correctIndex wasn't set in JSON, derive it from correct_answer
                if (q.correctIndex < 0 && !string.IsNullOrEmpty(q.correct_answer) && q.options != null)
                {
                    q.correctIndex = System.Array.IndexOf(q.options, q.correct_answer);
                    if (q.correctIndex < 0)
                        Debug.LogWarning($"[QuizManager] Question {i}: correct_answer does not match any option.", this);
                }

                if (q.correctIndex < 0 || q.correctIndex > 3)
                    Debug.LogWarning($"[QuizManager] Question {i} has correctIndex out of range (0..3).", this);
            }
        }
    }

    /// <summary>
    /// Extracts the first NPC from the "npcs" dictionary in the raw JSON.
    /// JsonUtility cannot deserialize dictionary keys, so we parse manually.
    /// </summary>
    private NpcData ExtractNpc(string json)
    {
        int npcsIdx = json.IndexOf("\"npcs\"", StringComparison.Ordinal);
        if (npcsIdx < 0) return null;

        // Find the opening brace of the npcs object
        int outerBrace = json.IndexOf('{', npcsIdx);
        if (outerBrace < 0) return null;

        // First quoted string after the brace is the NPC name
        int nameStart = json.IndexOf('"', outerBrace + 1);
        if (nameStart < 0) return null;
        int nameEnd = json.IndexOf('"', nameStart + 1);
        if (nameEnd < 0) return null;
        string npcName = json.Substring(nameStart + 1, nameEnd - nameStart - 1);

        // Find the NPC's own object { ... }
        int objStart = json.IndexOf('{', nameEnd);
        if (objStart < 0) return null;

        // Match the closing brace
        int depth = 0;
        int objEnd = -1;
        for (int i = objStart; i < json.Length; i++)
        {
            if (json[i] == '{') depth++;
            else if (json[i] == '}') { depth--; if (depth == 0) { objEnd = i; break; } }
        }
        if (objEnd < 0) return null;

        string npcJson = json.Substring(objStart, objEnd - objStart + 1);

        try
        {
            NpcData npc = JsonUtility.FromJson<NpcData>(npcJson);
            // Capitalize the key as the display name
            npc.name = char.ToUpper(npcName[0]) + npcName.Substring(1);
            return npc;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QuizManager] Failed to parse NPC data: {e.Message}", this);
            return null;
        }
    }
}
