using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class WorldScore
{
    public string worldId;
    public int bestScore;
    public int totalQuestions;
}

[Serializable]
public class ScoreData
{
    public List<WorldScore> scores = new List<WorldScore>();
}

public static class ScoreLog
{
    private static string FilePath =>
        Path.Combine(Application.streamingAssetsPath, "Data", "Worlds", "scores.json");

    /// <summary>
    /// Records a score for a world. Returns true if it's a new best.
    /// </summary>
    public static bool RecordScore(string worldId, int score, int totalQuestions)
    {
        var data = LoadAll();
        var entry = data.scores.Find(s => s.worldId == worldId);

        bool isNewBest;

        if (entry == null)
        {
            data.scores.Add(new WorldScore
            {
                worldId = worldId,
                bestScore = score,
                totalQuestions = totalQuestions
            });
            isNewBest = true;
        }
        else
        {
            isNewBest = score > entry.bestScore;
            if (isNewBest)
                entry.bestScore = score;
            entry.totalQuestions = totalQuestions;
        }

        Save(data);
        Debug.Log($"[ScoreLog] {worldId}: {score}/{totalQuestions} (best: {(entry != null ? entry.bestScore : score)}). New best: {isNewBest}");
        return isNewBest;
    }

    public static int GetBestScore(string worldId)
    {
        var data = LoadAll();
        var entry = data.scores.Find(s => s.worldId == worldId);
        return entry?.bestScore ?? 0;
    }

    public static ScoreData LoadAll()
    {
        if (!File.Exists(FilePath))
            return new ScoreData();

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<ScoreData>(json);
            return data ?? new ScoreData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ScoreLog] Could not read scores: {e.Message}");
            return new ScoreData();
        }
    }

    private static void Save(ScoreData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }
}
