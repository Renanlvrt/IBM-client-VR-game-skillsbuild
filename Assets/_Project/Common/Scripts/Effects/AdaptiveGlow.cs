using UnityEngine;

public class AdaptiveGlow : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light targetLight;
    [SerializeField] private string colorPropertyName = "_EmissionColor";

    [Header("Distance Settings")]
    [Tooltip("Distance at which the lighting/glow is at MIN intensity (fade out complete).")]
    [SerializeField] private float minDistance = 10f; // Increased for better visibility while approaching
    [Tooltip("Distance at which the lighting/glow is at MAX intensity (full brightness).")]
    [SerializeField] private float maxDistance = 40f; // Increased to be visible from much further

    [Header("Visual Settings")]
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField] private float maxEmissionIntensity = 0.5f; // Lowered to keep book shape visible
    [SerializeField] private float maxLightIntensity = 5f;    // Use real light for surrounding illumination

    private Material targetMaterial;
    private Transform playerTransform;

    private void Awake()
    {
        // 1. Setup Renderer & Material
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // Use material (instanced) to avoid affecting all objects sharing the same material
            targetMaterial = targetRenderer.material;
            targetMaterial.EnableKeyword("_EMISSION");
        }

        // 2. Setup Light
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight != null)
        {
            targetLight.color = glowColor;
        }

        // 3. Find Player
        if (Camera.main != null)
            playerTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Normalize distance to a 0-1 range based on our increased min/max
        // 0 = player is close (subtle/none)
        // 1 = player is far (full beacon)
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        
        // Smooth the curve (fade is nicer when squared or smoothstepped)
        t = Mathf.SmoothStep(0f, 1f, t);

        // --- Apply Emission (Subtle, so shape stays visible) ---
        if (targetMaterial != null)
        {
            Color finalEmission = glowColor * (t * maxEmissionIntensity);
            targetMaterial.SetColor(colorPropertyName, finalEmission);
        }

        // --- Apply Actual Lighting (To surrounding) ---
        if (targetLight != null)
        {
            targetLight.intensity = t * maxLightIntensity;
            // Optionally disable light when intensity is basically 0 to save performance
            targetLight.enabled = (targetLight.intensity > 0.01f);
        }
    }
}
