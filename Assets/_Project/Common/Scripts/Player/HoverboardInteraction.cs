using UnityEngine;
using UnityEngine.InputSystem;

public class HoverboardInteraction : MonoBehaviour
{
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed
            && GameState.currentState == GameState.PlayerState.Hoverboard
            && GameState.nearbyStation != null
            && GameState.interactionInputEnabled)
        {
            GameState.nearbyStation.EnterStation(transform);
        }
    }
}