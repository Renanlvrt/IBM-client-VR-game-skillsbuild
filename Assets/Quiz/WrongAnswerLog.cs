using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class WrongQuestionEntry
{
    public string worldId;
    public string question;
    public string[] options;
    public string correctAnswer;
}

[Serializable]
public class WrongAnswerData
{
    public List<WrongQuestionEntry> entries = new List<WrongQuestionEntry>();
}

public static class WrongAnswerLog
{
    private static string FilePath =>
        Path.Combine(Application.streamingAssetsPath, "Data", "Worlds", "wrong_questions.json");

    public static void LogWrongAnswer(string worldId, QuestionData q)
    {
        var data = LoadAll();

        var entry = new WrongQuestionEntry
        {
            worldId = worldId,
            question = q.question,
            options = q.options,
            correctAnswer = q.options[q.correctIndex]
        };

        data.entries.Add(entry);
        Save(data);
        Debug.Log($"[WrongAnswerLog] Saved wrong question. Total: {data.entries.Count}. File: {FilePath}");
    }

    public static WrongAnswerData LoadAll()
    {
        if (!File.Exists(FilePath))
            return new WrongAnswerData();

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<WrongAnswerData>(json);
            return data ?? new WrongAnswerData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WrongAnswerLog] Could not read log: {e.Message}");
            return new WrongAnswerData();
        }
    }

    public static void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);

        Debug.Log("[WrongAnswerLog] Cleared all wrong questions.");
    }

    private static void Save(WrongAnswerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }
}
