using UnityEngine;

public class PlayerModeController : MonoBehaviour
{
    private PlayerMovementCC movement;
    private HoverboardInteraction hoverboardInteraction;
    private StationInteraction stationInteraction;

    void Awake()
    {
        movement = GetComponent<PlayerMovementCC>();
        hoverboardInteraction = GetComponent<HoverboardInteraction>();
        stationInteraction = GetComponent<StationInteraction>();
    }

    void OnEnable()
    {
        GameState.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        GameState.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState.PlayerState newState)
    {
        switch (newState)
        {
            case GameState.PlayerState.Hoverboard:
                EnterHoverboardMode();
                break;
            case GameState.PlayerState.Station:
                EnterStationMode();
                break;
        }
    }

    void EnterStationMode()
    {
        if (GameSettings.isVR)
        {
            movement.enabled = false;
        }
        else
        {
            movement.SetStationMode();
        }
        hoverboardInteraction.enabled = false;
        stationInteraction.enabled = true;
    }

    void EnterHoverboardMode()
    {
        if (GameSettings.isVR)
        {
            movement.enabled = true;
        }
        else
        {
            movement.SetHoverboardMode();
        }
        hoverboardInteraction.enabled = true;
        stationInteraction.enabled = false;
    }
}