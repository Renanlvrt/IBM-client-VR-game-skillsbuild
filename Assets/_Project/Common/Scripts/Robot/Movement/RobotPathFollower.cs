using UnityEngine;
using UnityEngine.AI;

public class RobotAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Follow Distances")]
    [SerializeField] private float arrivalRadius = 10f;
    [SerializeField] private float slowingRadius = 15f;
    [SerializeField] private float triggerRadius = 20f;

    [Header("Positioning")]
    [SerializeField] private float FOV = 90f;
    [SerializeField] private float updateInterval = 1f;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 10f;

    [Header("Visuals (Flying Effect)")]
    [SerializeField] private float bobAmplitude = 0.15f; 
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Transform visualModel; // The actual mesh child to bob

    [Header("Dev Settings")]
    [Tooltip("Tick this if this robot should start moving immediately without waiting for the Intro sequence to complete.")]
    [SerializeField] private bool skipIntro = true;

    private NavMeshAgent agent;
    private SpeechBubble speech;
    private PiperSpeakerComponent piperTTS;
    private float updateTimer;
    private Vector3 currentTarget;
    private float bobTimer;
    private Vector3? _overrideDestination; // If set, robot goes here instead of following player

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        WarpToNavMesh();

        agent.speed = maxSpeed;
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;

        agent.stoppingDistance = arrivalRadius;
        agent.autoBraking = true;

        agent.updateRotation = false;  // handled by this script or RobotVisual.cs

        agent.isStopped = true;

        if (IntroManager.Instance != null && !skipIntro && !IntroManager.introComplete)
        {
            enabled = false;
            IntroManager.OnIntroComplete += () => enabled = true;
        }
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer > updateInterval)
        {
            updateTimer = 0;
            UpdatePosition();
        }
        UpdateSpeed();
        //HandleVisuals();
    }

    private void HandleVisuals()
    {
        if (visualModel == null) return;

        // 1. Gentle Bobbing (apply to localPosition of the mesh)
        bobTimer += Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(bobTimer) * bobAmplitude;
        visualModel.localPosition = new Vector3(0, bob, 0);

        // 2. Smooth Rotation
        Vector3 lookDir;
        if (_overrideDestination.HasValue)
        {
            lookDir = (_overrideDestination.Value - transform.position).normalized;
        }
        else
        {
            lookDir = (player.position - transform.position).normalized;
        }

        lookDir.y = 0; // Keep horizontal
        if (lookDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>Forces the robot to a specific spot. Pass null to return to player follow.</summary>
    public void SetDestinationOverride(Vector3? destination)
    {
        _overrideDestination = destination;
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (destination.HasValue)
        {
            if (!agent.isOnNavMesh) WarpToNavMesh(); 

            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.stoppingDistance = 0.1f;
                agent.isStopped = false;
                agent.speed = maxSpeed; 
                agent.SetDestination(destination.Value);
                Debug.Log($"[RobotAI] Override set -> {destination.Value} | speed={agent.speed} | isMoving={agent.velocity.magnitude > 0.1f}");
            }
        }
        else
        {
            agent.stoppingDistance = arrivalRadius;
        }
    }

    public void WarpToNavMesh()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (speech == null) speech = GetComponentInChildren<SpeechBubble>();
        if (piperTTS == null) piperTTS = GetComponentInChildren<PiperSpeakerComponent>();
        
        if (agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 50f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            Debug.Log("[RobotAI] Warped to nearest NavMesh point.");
        }
        else
        {
            Debug.LogWarning("[RobotAI] Could not find NavMesh near robot!");
            string stuckMsg = "I'm stuck! I can't find the floor. Please bake the NavMesh!";
            if (piperTTS != null) piperTTS.Speak(stuckMsg);
            if (speech != null) speech.Say(new string[] { stuckMsg });
        }
    }

    void UpdatePosition()
    {
        // If we have an override, do NOT reset the destination every frame.
        if (_overrideDestination.HasValue) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist > triggerRadius)
        {
            PickNewTargetPosition();
        }
    }

    void PickNewTargetPosition()
    {
        int totalAttempts = 5;
        for (int attempt = 0; attempt < totalAttempts; attempt++)  // try a bit closer each time
        {
            Vector3 forward = player.forward;
            float randomAngle = Random.Range(-FOV / 2, FOV / 2);
            Vector3 unitVector = Quaternion.AngleAxis(randomAngle, Vector3.up) * forward;

            float distanceMultiplier = (float)(totalAttempts - attempt) / totalAttempts;
            Vector3 targetPos = player.position + (arrivalRadius * unitVector) * distanceMultiplier;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                    agent.SetDestination(hit.position);
                return;
            }
        }

        // Fallback 
        Vector3 fallback = player.position - player.forward * arrivalRadius;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.SetDestination(fallback);
    }

    void UpdateSpeed()
    {
        if (agent == null) return;

        float targetDist = _overrideDestination.HasValue 
            ? Vector3.Distance(_overrideDestination.Value, transform.position)
            : Vector3.Distance(player.position, transform.position);

        float currentArrival = _overrideDestination.HasValue ? 1.5f : arrivalRadius;
        float currentSlowing = _overrideDestination.HasValue ? 3.0f : slowingRadius;

        if (targetDist <= currentArrival)
        {
            agent.speed = 0;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else if (targetDist < currentSlowing)
        {
            float speedMultiplier = (targetDist - currentArrival) / (currentSlowing - currentArrival);
            agent.speed = maxSpeed * speedMultiplier;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
        else
        {
            agent.speed = maxSpeed;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    public bool IsMoving()
    {
        return agent != null && agent.velocity.sqrMagnitude > 0.1f;
    }
}