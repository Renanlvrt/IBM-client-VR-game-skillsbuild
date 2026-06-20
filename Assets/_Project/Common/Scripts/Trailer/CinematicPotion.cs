using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to the Potion or Cauldron item.
/// Triggers a magical potion sequence: Float up in small circles, shake at climax, drop down, change to gold.
/// </summary>
public class CinematicPotion : MonoBehaviour
{
    [Header("Phase Timing")]
    [Tooltip("How long it takes to float up into the air")]
    public float timeGoingUp = 2.5f;
    [Tooltip("How long it stays shaking at the very top")]
    public float timeShakingAtTop = 1.0f;
    [Tooltip("How long it takes to fall back down")]
    public float timeGoingDown = 1.0f;

    [Header("Movement Dynamics")]
    [Tooltip("Target height to float to")]
    public float floatHeight = 1.0f;
    [Tooltip("Radius of the small circles it makes while floating")]
    public float circleRadius = 0.05f;
    [Tooltip("How fast it draws those circles")]
    public float circleSpeed = 5.0f;
    [Tooltip("Gentle shake while going up")]
    public float gentleShake = 0.005f;
    [Tooltip("Violent shake at the climax")]
    public float climaxShake = 0.03f;

    [Header("Color Magic")]
    [Tooltip("Assign the visual mesh of the potion liquid/bottle here safely")]
    public Renderer potionRenderer;
    [Tooltip("The magical color it turns at the end")]
    public Color magicalColor = new Color(1f, 0.9f, 0.2f, 1f); // Golden yellow
    public string shaderColorProperty = "_BaseColor";

    [Header("Optional VFX")]
    [Tooltip("Particle system to play when brewing starts")]
    public ParticleSystem magicSparks;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Color originalColor;
    private Material potionMat;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (potionRenderer != null)
        {
            // Instantiate material safely so we don't permanently alter the project file
            potionMat = potionRenderer.material; 
            if (potionMat.HasProperty(shaderColorProperty))
            {
                originalColor = potionMat.GetColor(shaderColorProperty);
            }
        }
    }

    public void PlayBrewingAnimation()
    {
        StartCoroutine(BrewRoutine());
    }

    private IEnumerator BrewRoutine()
    {
        if (magicSparks != null) magicSparks.Play();

        Vector3 startPos = originalPosition;
        Vector3 peakPos = originalPosition + Vector3.up * floatHeight;

        // PHASE 1: Float up while making small organic circles
        float elapsed = 0f;
        while (elapsed < timeGoingUp)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / timeGoingUp;
            
            // Go up smoothly
            Vector3 currentHeightPos = Vector3.Lerp(startPos, peakPos, Mathf.SmoothStep(0, 1, t));

            // Calculate small circle based on X and Z (ignoring pivot rotation bugs)
            float circleX = Mathf.Cos(elapsed * circleSpeed) * circleRadius * t; // gets slightly wider at top
            float circleZ = Mathf.Sin(elapsed * circleSpeed) * circleRadius * t;
            
            // Add tiny, gentle shake
            Vector3 microShake = GenerateShake(gentleShake);

            transform.localPosition = currentHeightPos + new Vector3(circleX, microShake.y, circleZ) + microShake;
            yield return null;
        }

        // PHASE 2: Climax! Shaking at the absolute top while spinning faster and faster
        elapsed = 0f;
        while (elapsed < timeShakingAtTop)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / timeShakingAtTop; // 0 to 1

            // Calculate chaotic shake
            Vector3 climaxShakeVec = GenerateShake(climaxShake);
            transform.localPosition = peakPos + climaxShakeVec;

            // Spin on Y axis (slowly at first, then extremely fast at the climax)
            // Starts at maybe 30 degrees/sec, ramps smoothly up to 1500 degrees/sec
            float currentSpinSpeed = Mathf.Lerp(30f, 1500f, t * t * t);
            transform.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.Self);

            yield return null;
        }

        // PHASE 3: Drop back down, spin down to a halt, and magically change color
        elapsed = 0f;
        while (elapsed < timeGoingDown)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / timeGoingDown;

            // Smooth drop to original
            transform.localPosition = Vector3.Lerp(peakPos, originalPosition, Mathf.SmoothStep(0, 1, t));

            // Decelerating spin! Starts at 1500 (climax speed) and gracefully slows down to 0
            // Using reverse cubic curve for that satisfying brake effect
            float brakeCurve = 1f - t; 
            float currentSpinSpeed = Mathf.Lerp(0f, 1500f, brakeCurve * brakeCurve * brakeCurve);
            transform.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.Self);

            // Start glowing yellow/gold!
            if (potionMat != null && potionMat.HasProperty(shaderColorProperty))
            {
                potionMat.SetColor(shaderColorProperty, Color.Lerp(originalColor, magicalColor, t));
                
                // If it has emission, turn that on too for extra glow
                if (potionMat.HasProperty("_EmissionColor"))
                {
                    potionMat.EnableKeyword("_EMISSION");
                    potionMat.SetColor("_EmissionColor", Color.Lerp(Color.black, magicalColor * 2f, t));
                }
            }

            yield return null;
        }

        // Snap perfectly to finish
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation; 
    }

    private Vector3 GenerateShake(float intensity)
    {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * intensity;
    }

    public void ResetPotion()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        if (potionMat != null && potionMat.HasProperty(shaderColorProperty))
        {
            potionMat.SetColor(shaderColorProperty, originalColor);
            if (potionMat.HasProperty("_EmissionColor"))
            {
                potionMat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
