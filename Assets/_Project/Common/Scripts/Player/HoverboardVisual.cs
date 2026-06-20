using System.Collections;
using UnityEngine;

public class HoverboardVisual : MonoBehaviour
{
    [SerializeField] private Transform board; // The hoverboard model
    [SerializeField] private Transform playerRoot; // XR Origin or player root

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.05f, 0f); // Just above ground

    [Header("Pop Animation")]
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float popOvershoot = 0.2f;

    [Header("Hover Effect")]
    [SerializeField] private float hoverAmplitude = 0.02f;
    [SerializeField] private float hoverFrequency = 2f;
    [SerializeField] private float tiltAmount = 3f;
    [SerializeField] private float tiltSpeed = 5f;

    private Vector3 originalScale;
    private bool visible = false;
    private Vector3 lastPos;
    private Coroutine popRoutine;

    private void Start()
    {
        originalScale = board.localScale;
        lastPos = playerRoot.position;

        // Listen for state changes
        GameState.OnStateChanged += OnStateChanged;

        // Start visible if in hoverboard mode
        if (GameState.currentState == GameState.PlayerState.Hoverboard)
        {
            board.localScale = originalScale;
            visible = true;
        }
        else
        {
            board.localScale = Vector3.zero;
            visible = false;
        }
    }

    private void OnDestroy()
    {
        GameState.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState.PlayerState newState)
    {
        if (newState == GameState.PlayerState.Hoverboard)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void Show()
    {
        if (visible) return;
        visible = true;
        board.gameObject.SetActive(true);

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopScale(Vector3.zero, originalScale));
    }

    public void Hide()
    {
        if (!visible) return;
        visible = false;

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopScale(originalScale, Vector3.zero, () =>
        {
            board.gameObject.SetActive(false);
        }));
    }

    private void Update()
    {
        if (!visible || Time.deltaTime <= 0) return;

        // Tilt based on movement direction relative to player facing
        Vector3 velocity = (playerRoot.position - lastPos) / Time.deltaTime;
        lastPos = playerRoot.position;

        // Convert velocity to local space relative to player facing
        Vector3 localVelocity = Quaternion.Inverse(Quaternion.Euler(0, playerRoot.eulerAngles.y, 0)) * velocity;

        // Forward/back movement tilts nose down/up, left/right tilts sideways
        float tiltX = Mathf.Clamp(localVelocity.z * tiltAmount, -tiltAmount, tiltAmount);
        float tiltZ = Mathf.Clamp(-localVelocity.x * tiltAmount, -tiltAmount, tiltAmount);

        // Ensure eulerAngles.y is also valid
        float yaw = playerRoot.eulerAngles.y;
        if (float.IsNaN(yaw)) yaw = 0f;

        Quaternion targetRot = Quaternion.Euler(tiltX, yaw, tiltZ);
        board.rotation = Quaternion.Lerp(board.rotation, targetRot, Time.deltaTime * tiltSpeed);
    }

    private IEnumerator PopScale(Vector3 from, Vector3 to, System.Action onComplete = null)
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            float bounce = 1f + popOvershoot * Mathf.Sin(t * Mathf.PI);
            float ease = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            board.localScale = Vector3.Lerp(from, to, ease * bounce);

            yield return null;
        }

        board.localScale = to;
        onComplete?.Invoke();
    }
}
