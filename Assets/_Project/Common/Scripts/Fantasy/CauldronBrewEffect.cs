using UnityEngine;
using System.Collections;

public class CauldronBrewEffect : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeMagnitude = 0.05f;
    public float shakeSpeed = 25f;

    [Header("Pulse Settings")]
    public float pulseScale = 1.08f;
    public float pulseSpeed = 8f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isBrewing = false;

    void Awake()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    public void StartBrewing()
    {
        if (!isBrewing)
            StartCoroutine(BrewRoutine());
    }

    public void StopBrewing()
    {
        isBrewing = false;
    }

    private IEnumerator BrewRoutine()
    {
        isBrewing = true;
        float elapsed = 0f;

        while (isBrewing)
        {
            elapsed += Time.deltaTime;

            // Shake: random offset that gets stronger over time (caps at 3x)
            float intensity = Mathf.Min(elapsed / 3f, 1f);
            Vector3 shakeOffset = new Vector3(
                Mathf.Sin(Time.time * shakeSpeed + 1f) * shakeMagnitude * intensity,
                Mathf.Sin(Time.time * shakeSpeed * 0.8f) * shakeMagnitude * 0.5f * intensity,
                Mathf.Sin(Time.time * shakeSpeed + 2f) * shakeMagnitude * intensity
            );
            transform.localPosition = originalPosition + shakeOffset;

            // Pulse: gentle breathing scale
            float pulse = 1f + Mathf.Sin(elapsed * pulseSpeed) * 0.04f * intensity;
            transform.localScale = originalScale * pulse;

            yield return null;
        }

        // Snap back cleanly
        yield return StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        float duration = 0.3f;
        float t = 0f;

        Vector3 currentPos = transform.localPosition;
        Vector3 currentScale = transform.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(currentPos, originalPosition, t);
            transform.localScale = Vector3.Lerp(currentScale, originalScale, t);
            yield return null;
        }

        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
    }
}