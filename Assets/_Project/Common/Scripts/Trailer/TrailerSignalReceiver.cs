using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach this to the TrailerDirector GameObject. 
/// Receives Unity Timeline signals and forwards them to game objects for the trailer recordings.
/// </summary>
[RequireComponent(typeof(UnityEngine.Playables.PlayableDirector))]
public class TrailerSignalReceiver : MonoBehaviour
{
    [Header("Hub Reveal Sequence (Shot 03)")]
    [Tooltip("The main robot object holding the NavMeshAgent")]
    public GameObject hubRobot;

    [Tooltip("Target transform where the robot should walk (station)")]
    public Transform stationTarget;

    [Tooltip("Target transform where the robot should walk (portal)")]
    public Transform portalTarget;

    [Tooltip("The Animator attached to the hub door")]
    public Animator hubDoorAnimator;

    [Tooltip("The PortalDoor script attached to the hub door")]
    public PortalDoor hubPortalDoor;

    [Tooltip("Custom UnityEvent if your door uses a specific script rather than an animator")]
    public UnityEvent OnDoorOpen;

    [Tooltip("The speech bubble component attached to the robot")]
    public SpeechBubble robotSpeechBubble;

    [Tooltip("The Piper TTS component attached to the robot")]
    public PiperSpeakerComponent piperTTS;

    [Tooltip("The portal VFX object that should glow/activate")]
    public GameObject hubPortalVfx;

    [Header("Custom Trailer Events")]
    [Tooltip("Assign custom methods here via Inspector if needed")]
    public UnityEvent OnRobotTalk;
    public UnityEvent OnPortalActivate;

    // ─── HUB REVEAL SIGNALS (Triggered dynamically by Timeline) ───

    /// <summary>
    /// Timeline Signal: t=2s 
    /// Player approaches robot. Trigger the speech bubble and TTS welcoming the player.
    /// </summary>
    public void TriggerRobotTalk()
    {
        Debug.Log("[Trailer] Signal Received: TriggerRobotTalk");

        string welcomeLine = "Hello, I am your AI companion.";

        // 1. Suppressed TTS as per trailer music setup
        // if (piperTTS != null)
        // {
        //     piperTTS.Speak(welcomeLine);
        // }

        // 2. Show speech bubble
        if (robotSpeechBubble != null)
        {
            robotSpeechBubble.Say(new string[] { welcomeLine });
        }

        // 3. Fallback for custom logic in Inspector
        OnRobotTalk?.Invoke();
    }

    /// <summary>
    /// Timeline Signal: Triggers the learning purpose text
    /// </summary>
    public void TriggerHubInfoText()
    {
        Debug.Log("[Trailer] Signal Received: TriggerHubInfoText");
        string line = "This hub was built to change how you learn.";
        if (robotSpeechBubble != null)
        {
            robotSpeechBubble.Say(new string[] { line });
        }
    }

    /// <summary>
    /// Timeline Signal: Triggers the final destination text
    /// </summary>
    public void TriggerWorldWaitingText()
    {
        Debug.Log("[Trailer] Signal Received: TriggerWorldWaitingText");
        string line = "Your first world is waiting.";
        if (robotSpeechBubble != null)
        {
            robotSpeechBubble.Say(new string[] { line });
        }
    }

    /// <summary>
    /// Timeline Signal: Command robot to walk to the station
    /// </summary>
    public void MoveRobotToStation()
    {
        Debug.Log("[Trailer] Signal Received: MoveRobotToStation");

        if (hubRobot == null || stationTarget == null) return;

        var ai = hubRobot.GetComponentInChildren<RobotAI>();
        if (ai != null)
        {
            ai.enabled = true; // Crucial: Re-enables the script that processes the hover & walk logic
            ai.SetDestinationOverride(stationTarget.position);
        }
    }

    /// <summary>
    /// Timeline Signal: Command robot to walk to the portal
    /// </summary>
    public void MoveRobotToPortal()
    {
        Debug.Log("[Trailer] Signal Received: MoveRobotToPortal");
        if (hubRobot == null || portalTarget == null) return;

        var ai = hubRobot.GetComponentInChildren<RobotAI>();
        if (ai != null)
        {
            ai.enabled = true; // Crucial: Re-enables the script that processes the hover & walk logic
            ai.SetDestinationOverride(portalTarget.position);
        }
    }

    /// <summary>
    /// Timeline Signal: Opens the main hub door
    /// </summary>
    public void OpenDoor()
    {
        Debug.Log("[Trailer] Signal Received: OpenDoor");
        if (hubDoorAnimator != null)
        {
            // Tries common animator parameters to ensure it catches standard animation logic
            hubDoorAnimator.SetTrigger("Open");
            hubDoorAnimator.SetBool("IsOpen", true);
        }

        if (hubPortalDoor != null)
        {
            hubPortalDoor.ForceOpen();
        }
    }

    /// <summary>
    /// Timeline Signal: t=4s
    /// Camera pulls back, portal VFX activates behind the robot.
    /// </summary>
    public void ActivatePortalVFX()
    {
        Debug.Log("[Trailer] Signal Received: ActivatePortalVFX");

        if (hubPortalVfx != null)
        {
            hubPortalVfx.SetActive(true);

            // If there's an animator or particle system on the portal, trigger it here.
            var particles = hubPortalVfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in particles)
            {
                p.Play();
            }
        }

        OnPortalActivate?.Invoke();
    }

    // ─── OTHER TRAILER SIGNALS (From design doc Phase 5) ───

    public void OnCauldronBrew()
    {
        Debug.Log("[Trailer] Signal Received: OnCauldronBrew");
    }

    public void OnAllOrbsSpawn()
    {
        Debug.Log("[Trailer] Signal Received: OnAllOrbsSpawn");
    }
}
