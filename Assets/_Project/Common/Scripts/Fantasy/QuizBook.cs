using UnityEngine;
using TMPro;

public class QuizBook : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI[] answerTexts;

    [Header("Optional")]
    [SerializeField] private GameObject bookVisual;

    public void LoadQuestion(FantasyStationConfig config)
    {
        if (questionText != null)
            questionText.text = config.question;

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < config.answers.Length)
            {
                answerTexts[i].text = config.answers[i].answerText;
                answerTexts[i].gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].gameObject.SetActive(false);
            }
        }
    }

    public void Show()
    {
        if (bookVisual != null)
            bookVisual.SetActive(true);
    }

    public void Hide()
    {
        if (bookVisual != null)
            bookVisual.SetActive(false);
    }
}