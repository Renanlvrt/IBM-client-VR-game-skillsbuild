using UnityEngine;

public class FantasyQuizStation : MonoBehaviour
{
    [SerializeField] private FantasyStationConfig config;
    [SerializeField] private QuizBook book;

    private void Start()
    {
        if (book != null && config != null)
        {
            book.LoadQuestion(config);
        }
    }

    public FantasyStationConfig Config => config;
}