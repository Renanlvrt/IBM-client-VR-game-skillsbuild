using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class IngredientTracker : MonoBehaviour
{
    public static IngredientTracker Instance { get; private set; }

    private Dictionary<string, List<ChoiceRecord>> stationChoices
        = new Dictionary<string, List<ChoiceRecord>>();
    private List<IngredientData> collectedInOrder = new List<IngredientData>();
    private GrabbableObject lastSwappedGrabbable;

    public int TotalCollected => collectedInOrder.Count;

    public static event Action<IngredientData> OnIngredientCollected;
    public static event Action<IngredientData, IngredientData> OnIngredientSwapped;

    private void Awake()
    {
        Instance = this;
    }

    public void CollectFromStation(string stationID, IngredientData data,
        GrabbableObject obj, int maxPerStation = 1)
    {
        if (!stationChoices.ContainsKey(stationID))
        {
            stationChoices[stationID] = new List<ChoiceRecord>();
        }

        List<ChoiceRecord> choices = stationChoices[stationID];

        ChoiceRecord existing = choices.Find(c => c.data.ingredientID == data.ingredientID);
        if (existing != null) return;

        if (choices.Count >= maxPerStation)
        {
            ChoiceRecord old = choices[0];
            lastSwappedGrabbable = old.grabbable;
            collectedInOrder.Remove(old.data);
            choices.RemoveAt(0);

            choices.Add(new ChoiceRecord(data, obj));
            collectedInOrder.Add(data);
            OnIngredientSwapped?.Invoke(old.data, data);
        }
        else
        {
            choices.Add(new ChoiceRecord(data, obj));
            collectedInOrder.Add(data);
            OnIngredientCollected?.Invoke(data);
        }
    }

    public GrabbableObject GetGrabbableForLastSwap()
    {
        return lastSwappedGrabbable;
    }

    public bool HasChosenFromStation(string stationID)
    {
        return stationChoices.ContainsKey(stationID)
            && stationChoices[stationID].Count > 0;
    }

    public IngredientData GetChoiceForStation(string stationID)
    {
        if (stationChoices.ContainsKey(stationID)
            && stationChoices[stationID].Count > 0)
            return stationChoices[stationID][0].data;
        return null;
    }

    public List<ChoiceRecord> GetAllChoicesForStation(string stationID)
    {
        if (stationChoices.ContainsKey(stationID))
            return new List<ChoiceRecord>(stationChoices[stationID]);
        return new List<ChoiceRecord>();
    }

    public ChoiceRecord GetChoiceRecordForStation(string stationID)
    {
        if (stationChoices.ContainsKey(stationID)
            && stationChoices[stationID].Count > 0)
            return stationChoices[stationID][0];
        return null;
    }

    public List<ChoiceRecord> GetAllChoiceRecordsForStation(string stationID)
    {
        if (stationChoices.ContainsKey(stationID))
            return new List<ChoiceRecord>(stationChoices[stationID]);
        return new List<ChoiceRecord>();
    }

    public List<ChoiceRecord> GetAllChoices()
    {
        List<ChoiceRecord> all = new List<ChoiceRecord>();
        foreach (var kvp in stationChoices)
        {
            all.AddRange(kvp.Value);
        }
        return all;
    }

    public void ClearStation(string stationID)
    {
        if (stationChoices.ContainsKey(stationID))
        {
            foreach (ChoiceRecord record in stationChoices[stationID])
            {
                collectedInOrder.Remove(record.data);
            }
            stationChoices.Remove(stationID);
        }
    }

    public CauldronResult EvaluateResults(FantasyStationConfig[] allStations)
    {
        int correct = 0;
        int total = 0;
        List<string> wrongStationIDs = new List<string>();

        foreach (FantasyStationConfig station in allStations)
        {
            total += station.correctIngredientIDs.Length;

            if (stationChoices.ContainsKey(station.stationID))
            {
                List<ChoiceRecord> choices = stationChoices[station.stationID];
                bool allCorrectForStation = true;

                foreach (ChoiceRecord choice in choices)
                {
                    if (station.correctIngredientIDs.Contains(choice.data.ingredientID))
                    {
                        correct++;
                    }
                    else
                    {
                        allCorrectForStation = false;
                    }
                }

                if (choices.Count < station.correctIngredientIDs.Length)
                {
                    allCorrectForStation = false;
                }

                if (!allCorrectForStation)
                {
                    wrongStationIDs.Add(station.stationID);
                }
            }
            else
            {
                wrongStationIDs.Add(station.stationID);
            }
        }

        return new CauldronResult(correct, total, wrongStationIDs);
    }
}

[System.Serializable]
public class ChoiceRecord
{
    public IngredientData data;
    public GrabbableObject grabbable;

    public ChoiceRecord(IngredientData data, GrabbableObject grabbable)
    {
        this.data = data;
        this.grabbable = grabbable;
    }
}

public class CauldronResult
{
    public int correct;
    public int total;
    public float percentage;
    public List<string> wrongStationIDs;

    public CauldronResult(int correct, int total, List<string> wrongStationIDs)
    {
        this.correct = correct;
        this.total = total;
        this.percentage = total > 0 ? (float)correct / total : 0f;
        this.wrongStationIDs = wrongStationIDs;
    }
}