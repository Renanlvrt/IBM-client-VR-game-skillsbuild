using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Master setup script for cinematic recording.
/// Disables VR components, activates cinematic cameras and volumes.
/// </summary>
public class TrailerOrchestrator : MonoBehaviour
{
    [Header("Core References")]
    public UnityEngine.Playables.PlayableDirector timelineDirector;
    public Camera recordingCamera; // The single cinematic brain camera

    [Header("VR Rig Override")]
    public GameObject xrOrigin; // The live VR player rig to hide

    [Header("Post Processing Overrides")]
    public Volume trailerPostProcessVolume; // The high-quality cinematic volume
    public Volume[] liveGameVolumes; // Standard game volumes to disable

    [Header("Settings")]
    public bool autoPlayOnStart = true;

    private void Start()
    {
        SetupTrailerEnvironment();

        if (autoPlayOnStart && timelineDirector != null)
        {
            PlayTrailer();
        }
    }

    /// <summary>
    /// Prepares the scene for cinematic recording by disabling VR rigs and 
    /// enabling the dedicated high-fidelity recording camera and post-processing.
    /// </summary>
    [ContextMenu("Setup Trailer Environment")]
    public void SetupTrailerEnvironment()
    {
        // 1. Disable the VR Rig so it doesn't hijack the camera
        if (xrOrigin != null)
        {
            xrOrigin.SetActive(false);
            Debug.Log("[TrailerOrchestrator] Disabled XR Origin.");
        }

        // 2. Enable the dedicated recording camera (Cinemachine Brain)
        if (recordingCamera != null)
        {
            recordingCamera.gameObject.SetActive(true);
            Debug.Log("[TrailerOrchestrator] Enabled Recording Camera.");
        }

        // 3. Disable active game volumes to avoid lighting conflicts
        if (liveGameVolumes != null)
        {
            foreach (Volume v in liveGameVolumes)
            {
                if (v != null) v.enabled = false;
            }
        }

        // 4. Enable the over-the-top cinematic post processing
        if (trailerPostProcessVolume != null)
        {
            trailerPostProcessVolume.enabled = true;
            Debug.Log("[TrailerOrchestrator] Enabled Trailer Post Processing Volume.");
        }
    }

    [ContextMenu("Play Trailer Timeline")]
    public void PlayTrailer()
    {
        if (timelineDirector != null)
        {
            timelineDirector.time = 0;
            timelineDirector.Play();
            Debug.Log("[TrailerOrchestrator] Playing Timeline.");
        }
    }
}
