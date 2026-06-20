using UnityEngine;

[CreateAssetMenu(fileName = "FantasyStationConfig", menuName = "Game/Fantasy Station Config")]
public class FantasyStationConfig : ScriptableObject
{
    public string stationID;

    [TextArea(3, 5)]
    public string question;

    public AnswerMapping[] answers;

    [Tooltip("One ID for single-answer stations. Multiple IDs for multi-pick stations.")]
    public string[] correctIngredientIDs;
}

[System.Serializable]
public class AnswerMapping
{
    public string answerText;
    public string ingredientID;
}