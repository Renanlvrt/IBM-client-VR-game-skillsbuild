using UnityEngine;
using TMPro;

public class QuizPanel : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private FantasyStationConfig config;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI[] answerTexts;

    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI stationLabel;

    [Header("Billboard")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private Transform player;

    private void Start()
    {
        if (config != null)
        {
            LoadQuestion(config);
        }
    }

    private void Update()
    {
        if (facePlayer && player != null)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    public void LoadQuestion(FantasyStationConfig stationConfig)
    {
        config = stationConfig;

        if (stationLabel != null)
        {
            stationLabel.text = stationConfig.stationID;
        }

        questionText.text = stationConfig.question;

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < stationConfig.answers.Length)
            {
                answerTexts[i].text = stationConfig.answers[i].answerText;
                answerTexts[i].gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].gameObject.SetActive(false);
            }
        }
    }

    public FantasyStationConfig Config => config;
}
