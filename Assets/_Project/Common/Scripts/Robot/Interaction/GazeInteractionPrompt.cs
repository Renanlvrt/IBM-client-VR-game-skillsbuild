using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GazeInteractionPrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform robot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private Transform promptTransform;

    [Header("UI Elements")]
    [SerializeField] private Image buttonIconImage;
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Data")]
    [SerializeField] private InputIconMap iconMap;

    [Header("Position Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0.4f, -0.3f, 0f);

    [Header("Gaze Settings")]
    [SerializeField] private float gazeAngleThreshold = 25f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float scaleSpeed = 8f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Vector3 visibleScale = Vector3.one;

    [Header("Dynamic Scaling")]
    [SerializeField] private float referenceDistance = 8f; // Distance where scale = 1
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.0f;

    private float currentAlpha = 0f;
    private bool isActive = true;

    /// <summary>
    /// Change what action the prompt displays.
    /// Automatically picks VR or KBM icon based on GameSettings.
    /// </summary>
    public void ShowAction(string actionName)
    {
        var action = iconMap.GetAction(actionName);
        if (action == null) return;

        if (GameSettings.isVR)
        {
            buttonIconImage.sprite = action.vrIcon;
            labelText.text = action.vrLabel;
        }
        else
        {
            buttonIconImage.sprite = action.kbmIcon;
            labelText.text = action.kbmLabel;
        }

        buttonIconImage.SetNativeSize();
        isActive = true;
    }

    public void Hide()
    {
        isActive = false;
    }

    private void Update()
    {
        UpdatePosition();
        UpdateBillboard();
        UpdateGazeFade();
        UpdateScale();
    }

    private void UpdateScale()
    {
        float distance = Vector3.Distance(playerCamera.position, robot.position);
        float scale = distance / referenceDistance;
        scale = Mathf.Clamp(scale, minScale, maxScale);

        promptTransform.localScale = Vector3.Lerp(
            promptTransform.localScale,
            Vector3.one * scale,
            Time.deltaTime * scaleSpeed
        );
    }

    private void UpdatePosition()
    {
        // Direction from robot to player (horizontal only)
        Vector3 dirToPlayer = playerCamera.position - robot.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        dirToPlayer.Normalize();

        // Place prompt between robot and player, offset to the right and below
        Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer); // Perpendicular to facing direction

        Vector3 promptPos = robot.position
            + dirToPlayer * offset.z      // Forward toward player
            + right * offset.x            // Right side
            + Vector3.up * offset.y;      // Below

        promptTransform.position = promptPos;
    }

    private void UpdateBillboard()
    {
        Vector3 directionToCamera = playerCamera.position - promptTransform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            promptTransform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }

    private void UpdateGazeFade()
    {
        bool shouldShow = false;

        if (isActive)
        {
            Vector3 dirToRobot = (robot.position - playerCamera.position).normalized;
            float dot = Vector3.Dot(playerCamera.forward, dirToRobot);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            float distance = Vector3.Distance(playerCamera.position, robot.position);
            shouldShow = angle < gazeAngleThreshold && distance < maxDistance;
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        promptCanvasGroup.alpha = currentAlpha;

        Vector3 targetScale = shouldShow ? visibleScale : hiddenScale;
        promptTransform.localScale = Vector3.Lerp(
            promptTransform.localScale, targetScale, Time.deltaTime * scaleSpeed
        );
    }
}