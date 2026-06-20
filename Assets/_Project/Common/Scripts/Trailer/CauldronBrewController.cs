using System.Collections;
using UnityEngine;

/// <summary>
/// Triggered by a Timeline Signal at the BOOM moment.
/// Fires all one-shot particle systems and lights simultaneously for the cauldron explosion.
/// </summary>
public class CauldronBrewController : MonoBehaviour
{
    [Header("Explosion Particle Systems")]
    [Tooltip("PS4 - The main vertical burst")]
    [SerializeField] private ParticleSystem brewExplosion;
    
    [Tooltip("PS6 - The horizontal flash ring")]
    [SerializeField] private ParticleSystem shockwaveRing;
    
    [Tooltip("PS5 - The lingering smoke cloud")]
    [SerializeField] private ParticleSystem magicCloud;

    [Header("Explosion Lighting")]
    [Tooltip("L2 - The pure white/purple flash that blinds the scene")]
    [SerializeField] private Light brewFlash;

    // NOTE: You can uncomment the Cinemachine logic below if you have Cinemachine installed in your project!
    // [Header("Camera Shake")]
    // [SerializeField] private Cinemachine.CinemachineImpulseSource shake;

    /// <summary>
    /// Call this exact function from your Timeline Signal Emitter
    /// </summary>
    public void TriggerBoom()
    {
        Debug.Log("[Trailer] Cauldron BOOM Triggered!");

        // 1. Play instant particle bursts
        if (brewExplosion != null) brewExplosion.Play();
        if (shockwaveRing != null) shockwaveRing.Play();

        // 2. Play secondary cloud on a slight delay
        if (magicCloud != null)
        {
            StartCoroutine(DelayedPlay(magicCloud, 0.3f));
        }

        // 3. Flash the scene with blinding light and fade it over 1.5 seconds
        if (brewFlash != null)
        {
            StartCoroutine(LightFlash(brewFlash, 250f, 1.5f));
        }

        // 4. Trigger screen shake
        // if (shake != null) shake.GenerateImpulse();
    }

    private IEnumerator DelayedPlay(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.Play();
    }

    private IEnumerator LightFlash(Light flashLight, float peakIntensity, float fadeTime)
    {
        flashLight.intensity = peakIntensity;
        float t = 0;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(peakIntensity, 0, t / fadeTime);
            yield return null;
        }

        flashLight.intensity = 0;
    }
}
