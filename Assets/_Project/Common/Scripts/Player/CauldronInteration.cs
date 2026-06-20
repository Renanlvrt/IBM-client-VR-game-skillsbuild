using UnityEngine;
using UnityEngine.InputSystem;

public class CauldronInteraction : MonoBehaviour
{
    [SerializeField] private FantasyCauldron cauldron;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform cauldronTransform;
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private float interactAngle = 30f;

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (!enabled) return;
        if (cauldron == null || playerCamera == null || cauldronTransform == null) return;

        Vector3 dirToCauldron = (cauldronTransform.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, dirToCauldron);
        float distance = Vector3.Distance(playerCamera.position, cauldronTransform.position);

        if (angle < interactAngle && distance < interactRange)
        {
            cauldron.StartBrewing();
        }
    }
}