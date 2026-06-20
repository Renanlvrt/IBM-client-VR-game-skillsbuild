using UnityEngine;
using UnityEngine.InputSystem;

public class RobotInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform robotTransform;
    [SerializeField] private SpeechBubble speechBubble;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private float interactAngle = 30f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 3f;

    private float lastInteractTime = -999f;
    private IRobotBrain activeBrain;

    public SpeechBubble SpeechBubble => speechBubble;
    public Transform RobotTransform => robotTransform;

    public void RegisterBrain(IRobotBrain brain)
    {
        activeBrain = brain;
    }

    public void UnregisterBrain()
    {
        activeBrain = null;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (!enabled) return;
        if (Time.time - lastInteractTime < cooldown) return;

        // Don't fire during intro
        if (IntroManager.Instance != null && !IntroManager.introComplete) return;

        // Must be looking at robot
        if (!IsLookingAtRobot()) return;

        // Must have a brain registered
        if (activeBrain == null || !activeBrain.IsActive()) return;

        lastInteractTime = Time.time;
        activeBrain.OnRobotInteracted(this);
    }

    private bool IsLookingAtRobot()
    {
        Vector3 dirToRobot = (robotTransform.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, dirToRobot);
        float distance = Vector3.Distance(playerCamera.position, robotTransform.position);

        return angle < interactAngle && distance < interactRange;
    }
}