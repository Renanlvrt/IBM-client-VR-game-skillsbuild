using UnityEngine;
using System;

public class FantasyWorldManager : MonoBehaviour
{
    public static FantasyWorldManager Instance { get; private set; }

    [Header("Phase 1 - Ingredients")]
    [SerializeField] private CauldronInteraction cauldronInteraction;
    [SerializeField] private IngredientOrbit ingredientOrbit;
    [SerializeField] private RobotAbsorb robotAbsorb;
    [SerializeField] private FantasyRobotBrain fantasyBrain;

    [Header("Phase 2 (Future)")]
    [SerializeField] private MonoBehaviour[] phase2Scripts;
    [SerializeField] private GameObject[] phase2Objects;

    public static bool worldActive { get; private set; } = false;
    public static bool worldComplete { get; private set; } = false;
    public static event Action OnWorldActivated;
    public static event Action OnWorldCompleted;

    private void Awake()
    {
        Instance = this;
        worldActive = false;
        worldComplete = false;
    }

    private void OnEnable()
    {
        FantasyPhaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        FantasyPhaseManager.OnPhaseChanged -= OnPhaseChanged;
    }

    public void ActivateWorld()
    {
        worldActive = true;
        cauldronInteraction.enabled = true;
        ingredientOrbit.enabled = true;
        robotAbsorb.enabled = true;
        fantasyBrain.enabled = true;
        OnWorldActivated?.Invoke();
    }

    private void OnPhaseChanged(FantasyPhaseManager.WorldPhase newPhase)
    {
        if (newPhase == FantasyPhaseManager.WorldPhase.Phase2)
        {
            cauldronInteraction.enabled = false;
            robotAbsorb.enabled = false;

            foreach (MonoBehaviour script in phase2Scripts)
            {
                if (script != null) script.enabled = true;
            }
            foreach (GameObject obj in phase2Objects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
        else if (newPhase == FantasyPhaseManager.WorldPhase.Complete)
        {
            CompleteWorld();
        }
    }

    public void CompleteWorld()
    {
        worldComplete = true;
        robotAbsorb.enabled = false;
        cauldronInteraction.enabled = false;
        OnWorldCompleted?.Invoke();
    }
}