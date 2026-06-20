using UnityEngine;
using UnityEngine.InputSystem;

public class StationInteraction : MonoBehaviour
{
    [SerializeField] private float holdDuration = 1.5f;

    private bool exitHeld = false;
    private float holdTime = 0f;

    public void OnExitStation(InputAction.CallbackContext context)
    {
        //Debug.Log($"ExitStation callback - Phase: {context.phase}, GameState: {GameState.currentState}");

        if (GameState.currentState != GameState.PlayerState.Station) return;

        if (context.started)
        {
           // Debug.Log("ExitStation: HOLD STARTED");
            exitHeld = true;
        }
        if (context.canceled)
        {
            //Debug.Log("ExitStation: HOLD CANCELED");
            exitHeld = false;
            holdTime = 0f;
        }
    }

    void Update()
    {
        if (!exitHeld) return;

        holdTime += Time.deltaTime;

        if (holdTime >= holdDuration)
        {
            exitHeld = false;
            holdTime = 0f;
            GameState.currentStation.ExitStation(transform);
        }
    }
}