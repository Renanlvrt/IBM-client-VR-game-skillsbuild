using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class QuizUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons; // size 4
    [SerializeField] private TMP_Text feedbackText;

    [Header("Wrong Answer Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button askAIButton;

    private Action<int> onOptionSelected;
    private Action onRetry;
    private Action onAskAI;

    private void Awake()
    {
        if (questionText == null || feedbackText == null ||
            optionButtons == null || optionButtons.Length != 4)
        {
            Debug.LogWarning(
                $"[QuizUI] UI references not assigned on '{gameObject.name}'. " +
                "Disabling this QuizUI instance — it may be a duplicate.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null)
            {
                Debug.LogWarning(
                    $"[QuizUI] OptionButtons[{i}] is NULL on '{gameObject.name}'. " +
                    "Disabling this QuizUI instance.", this);
                enabled = false;
                return;
            }
        }

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);
        if (askAIButton != null)
            askAIButton.gameObject.SetActive(false);

        Debug.Log("[QuizUI] Awake - All UI references valid.", this);
    }

    public void BindOptionHandler(Action<int> handler)
    {
        onOptionSelected = handler;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() =>
            {
                onOptionSelected?.Invoke(index);
            });
        }
    }

    public void BindWrongAnswerHandlers(Action retryHandler, Action askAIHandler)
    {
        onRetry = retryHandler;
        onAskAI = askAIHandler;

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => onRetry?.Invoke());
        }

        if (askAIButton != null)
        {
            askAIButton.onClick.RemoveAllListeners();
            askAIButton.onClick.AddListener(() => onAskAI?.Invoke());
        }
    }

    public void ShowNpcIntro(NpcData npc, Action onStart)
    {
        questionText.text = $"<size=130%><b>Meet {npc.name}</b></size>\n" +
                            $"<size=85%><color=#AAAAAA>{npc.role}</color></size>";

        feedbackText.text = $"<i>\"{npc.personality}\"</i>\n\n" +
                            "<color=#666666>━━━━━━━━━━━━━━━━━━━━━━</color>\n\n" +
                            $"<size=85%>{npc.name} will be your guide for this quiz.\n" +
                            "You have <b>3 retries</b> per question and <b>3 AI assists</b> total.</size>";

        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].gameObject.SetActive(false);

        if (askAIButton != null)
            askAIButton.gameObject.SetActive(false);

        // Reuse the retry button as "Start Quiz"
        if (retryButton != null)
        {
            TMP_Text btnLabel = retryButton.GetComponentInChildren<TMP_Text>();
            if (btnLabel != null)
                btnLabel.text = "Begin Quiz";

            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>
            {
                // Restore original retry label and handler
                if (btnLabel != null)
                    btnLabel.text = "Retry";
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => onRetry?.Invoke());
                retryButton.gameObject.SetActive(false);

                onStart?.Invoke();
            });
            retryButton.gameObject.SetActive(true);
        }
    }

    public void DisplayQuestion(QuestionData question, int current = 0, int total = 0)
    {
        if (question == null) return;

        string progress = total > 0
            ? $"<size=70%><color=#AAAAAA>Question {current} / {total}</color></size>\n"
            : "";
        questionText.text = progress + question.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            TMP_Text label = optionButtons[i].GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = question.options[i];
        }

        feedbackText.text = "";
        HideWrongAnswerButtons();
    }

    public void ShowFeedback(bool correct)
    {
        feedbackText.text = correct ? "✅ Correct!" : "❌ Incorrect!";
    }

    public void ShowWrongAnswerButtons(bool showAskAI = true)
    {
        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].gameObject.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
        if (askAIButton != null)
            askAIButton.gameObject.SetActive(showAskAI);
    }

    public void HideWrongAnswerButtons()
    {
        if (retryButton != null)
            retryButton.gameObject.SetActive(false);
        if (askAIButton != null)
            askAIButton.gameObject.SetActive(false);
    }

    public void ShowCorrectAnswer(string correctOption)
    {
        feedbackText.text = $"The correct answer was:\n{correctOption}";

        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].gameObject.SetActive(false);

        HideWrongAnswerButtons();
    }

    public void ShowQuizComplete(int score, int total, int bestScore = 0, bool isNewBest = false)
    {
        questionText.text = "<size=130%><b>Quiz Complete!</b></size>";

        string scoreText = $"Score: <b>{score} / {total}</b>";

        if (isNewBest)
            scoreText += "\n\n<color=#FFD700><b>New Best Score!</b></color>";
        else if (bestScore > 0)
            scoreText += $"\n\n<size=85%><color=#AAAAAA>Best: {bestScore} / {total}</color></size>";

        feedbackText.text = scoreText;

        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].gameObject.SetActive(false);

        HideWrongAnswerButtons();
    }
}
