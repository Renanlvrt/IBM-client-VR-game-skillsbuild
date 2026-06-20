using UnityEngine;
using System;

public class FantasyPhaseManager : MonoBehaviour
{
    public static FantasyPhaseManager Instance { get; private set; }

    public enum WorldPhase
    {
        Ingredients,
        Phase2,
        Phase3,
        Complete
    }

    public static WorldPhase currentPhase { get; private set; } = WorldPhase.Ingredients;
    public static event Action<WorldPhase> OnPhaseChanged;

    private void Awake()
    {
        Instance = this;
        currentPhase = WorldPhase.Ingredients;
    }

    public void AdvancePhase()
    {
        switch (currentPhase)
        {
            case WorldPhase.Ingredients:
                currentPhase = WorldPhase.Phase2;
                break;
            case WorldPhase.Phase2:
                currentPhase = WorldPhase.Phase3;
                break;
            case WorldPhase.Phase3:
                currentPhase = WorldPhase.Complete;
                break;
        }

        OnPhaseChanged?.Invoke(currentPhase);
    }
}