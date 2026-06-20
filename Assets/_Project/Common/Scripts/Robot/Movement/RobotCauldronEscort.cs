using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Attach to the Robot alongside NavMeshAgent and RobotAI (RobotPathFollower).
///
/// FLOW:
///   Player presses X → ObjectLoreInspector calls RequestPickup()
///   Robot walks to item's world position
///   Robot arrives → item collected into inventory
///   After all items: robot walks to cauldron
///   Player follows → robot submits items one-by-one
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotCauldronEscort : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Targets")]
    public Transform cauldronTarget;

    [Header("Distances")]
    public float itemArrivalRadius     = 2.5f;   // Increased for better tolerance
    public float cauldronArrivalRadius = 2.0f;   // How close robot stops to cauldron
    public float playerFollowRadius    = 4.0f;   // Player proximity needed before submitting

    [Header("Timing")]
    public float collectPause    = 0.5f;   // Brief pause at item before collecting
    public float submitDelay     = 0.7f;   // Delay between each cauldron item drop
    public float finishPauseTime = 2.0f;  // Pause after last drop before returning to normal
    public float stuckTimeout    = 2.0f;  // Seconds of zero-velocity before forcing collection

    [Header("Player")]
    public Transform playerTransform;      // Auto-found if null

    // ── Pending pickup struct ──────────────────────────────────────────────

    private struct PendingPickup
    {
        public ItemData    item;
        public Vector3     worldPosition;
        public GameObject  itemObject;    // The item in the world (disabled when robot arrives)

        public PendingPickup(ItemData i, Vector3 pos, GameObject obj)
        { item = i; worldPosition = pos; itemObject = obj; }
    }

    // ── State machine ──────────────────────────────────────────────────────

    private enum State { Idle, WalkingToItem, Collecting, WalkingToCauldron, WaitingForPlayer, Submitting, Done }
    private State _state = State.Idle;

    // ── Private references ─────────────────────────────────────────────────

    private NavMeshAgent          _agent;
    private RobotAI               _robotAI;      // The follow AI — must be disabled when we drive
    private Cauldron              _cauldron;
    private SpeechBubble          _speech;       // To tell the player why we are stuck
    private PiperSpeakerComponent _piperTTS;
    private Queue<PendingPickup>  _pickupQueue   = new Queue<PendingPickup>();
    private List<ItemData>        _collectedItems = new List<ItemData>();
    private Coroutine             _activeRoutine;

    private float _stuckTimer = 0f;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _agent   = GetComponent<NavMeshAgent>();
        _robotAI = GetComponent<RobotAI>();
        _speech  = GetComponentInChildren<SpeechBubble>();
        _piperTTS = GetComponentInChildren<PiperSpeakerComponent>();

        if (_robotAI == null)
            Debug.LogWarning("[RobotEscort] RobotAI not found on same GameObject!");
    }

    private void Start()
    {
        if (playerTransform == null && Camera.main != null)
            playerTransform = Camera.main.transform;

        if (cauldronTarget != null)
            _cauldron = cauldronTarget.GetComponent<Cauldron>();
    }

    private void Update()
    {
        switch (_state)
        {
            case State.WalkingToCauldron:  CheckCauldronArrival(); break;
            case State.WaitingForPlayer:   CheckPlayerArrived();  break;
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ObjectLoreInspector when the player presses X near an item.
    /// Item stays in world until robot arrives.
    /// </summary>
    public void RequestPickup(ItemData item, Vector3 worldPosition, GameObject itemObject)
    {
        _pickupQueue.Enqueue(new PendingPickup(item, worldPosition, itemObject));
        Debug.Log($"[RobotEscort] Pickup requested for: {item.itemName} — queue size: {_pickupQueue.Count}");

        if (_state == State.Idle)
            ProcessNextPickup();
    }

    // ── Pickup processing ──────────────────────────────────────────────────

    private void ProcessNextPickup()
    {
        if (_pickupQueue.Count == 0)
        {
            // All pending pickups done — check if we have enough to head to cauldron
            if (InventoryManager.Instance != null &&
                _collectedItems.Count >= InventoryManager.Instance.requiredItemCount)
            {
                HeadToCauldron();
            }
            else
            {
                // Return control to the normal follow AI
                SetRobotAI(true);
                _state = State.Idle;
            }
            return;
        }

        var pickup = _pickupQueue.Dequeue();
        _activeRoutine = StartCoroutine(WalkToItem(pickup));
    }

    private IEnumerator WalkToItem(PendingPickup pickup)
    {
        _state = State.WalkingToItem;
        _stuckTimer = 0f;
        
        // USE RobotAI to move
        if (_robotAI != null)
        {
            _robotAI.enabled = true; // FORCE ENABLE if it was waiting for intro
            _robotAI.SetDestinationOverride(pickup.worldPosition);
            Debug.Log($"[RobotEscort] Requested RobotAI move to: {pickup.worldPosition}");
        }

        // Diagnostic: If not on NavMesh, say so!
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("[RobotEscort] ERROR: Robot is NOT on NavMesh. It cannot move.");
            string stuckMsg = "I'm stuck! I can't find the floor (NavMesh missing).";
            if (_piperTTS != null) _piperTTS.Speak(stuckMsg);
            if (_speech != null) _speech.Say(new string[] { stuckMsg });
        }

        float logTimer = Time.time;
        // Wait until we arrive — use both remainingDistance AND direct distance as fallback
        yield return new WaitUntil(() =>
        {
            if (!_agent.isOnNavMesh) return false;

            float directDist = Vector3.Distance(transform.position, pickup.worldPosition);

            // Log progress every 5 seconds if stuck
            if (Time.time - logTimer > 5f)
            {
                logTimer = Time.time;
                Debug.Log($"[RobotEscort] Still walking... Dist: {_agent.remainingDistance:F2}, DirectDist: {directDist:F2}, PathPending: {_agent.pathPending}, Speed: {_agent.velocity.magnitude:F2}, isStopped: {_agent.isStopped}");
            }

            // ── SUCCESS CONDITIONS ──
            
            // 1. Pathfinding arrival
            bool navArrived    = !_agent.pathPending && _agent.remainingDistance <= itemArrivalRadius;
            
            // 2. Direct distance (fallback if mesh is clipped)
            bool directArrived = directDist <= itemArrivalRadius;
            
            // 3. STUCK FALLBACK: If velocity is near zero for long enough and we are "reasonably close"
            bool isMoving = _agent.velocity.magnitude > 0.15f;
            if (!isMoving && !_agent.pathPending)
                _stuckTimer += Time.deltaTime;
            else
                _stuckTimer = 0f;

            bool forcedArrived = (_stuckTimer > stuckTimeout && directDist < 6.0f);
            if (forcedArrived) Debug.LogWarning("[RobotEscort] Robot stalled near item. Forcing collection as fallback.");

            return navArrived || directArrived || forcedArrived;
        });

        // Brief pause at the item (looks like collecting)
        StopAgent();
        _state = State.Collecting;
        yield return new WaitForSeconds(collectPause);

        // Collect: add to inventory, hide the world item
        _collectedItems.Add(pickup.item);
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(pickup.item, pickup.worldPosition);

        // Clear the override so it stops moving to this spot
        if (_robotAI != null) _robotAI.SetDestinationOverride(null);

        if (pickup.itemObject != null)
            pickup.itemObject.SetActive(false);

        Debug.Log($"[RobotEscort] Collected: {pickup.item.itemName} ({_collectedItems.Count} total)");

        // Move to next pickup or head to cauldron
        ProcessNextPickup();
    }

    // ── Cauldron escort ────────────────────────────────────────────────────

    private void HeadToCauldron()
    {
        if (cauldronTarget == null)
        {
            Debug.LogError("[RobotEscort] cauldronTarget not assigned!");
            SetRobotAI(true);
            _state = State.Idle;
            return;
        }

        _state = State.WalkingToCauldron;

        // Ensure we pick a REACHABLE spot on the floor near the cauldron
        Vector3 destination = cauldronTarget.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(cauldronTarget.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        if (_robotAI != null)
        {
            _robotAI.enabled = true; // FORCE ENABLE to handle movement logic
            _robotAI.SetDestinationOverride(destination);
            Debug.Log($"[RobotEscort] Instructed RobotAI to walk to cauldron at {destination}.");
        }
        else
        {
            MoveAgentTo(destination);
        }
    }

    private void CheckCauldronArrival()
    {
        // ── Robust Arrival Check ──
        // 1. Wait until agent is on mesh and NOT pending a path calculation
        if (!_agent.isOnNavMesh || _agent.pathPending) return;

        // 2. Ensure the agent has actually received the path and started moving
        // (agent.hasPath can be false for a split second after SetDestination)
        if (!_agent.hasPath && _agent.velocity.magnitude < 0.1f) return;

        float dist = Vector3.Distance(transform.position, cauldronTarget.position);
        
        // 3. Check distance — use both direct and remainingDistance
        if (dist <= cauldronArrivalRadius || _agent.remainingDistance <= cauldronArrivalRadius)
        {
            StopAgent();
            _state = State.WaitingForPlayer;
            Debug.Log("[RobotEscort] → Arrived at cauldron. Waiting for player nearby...");
        }
    }

    private void CheckPlayerArrived()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(playerTransform.position, transform.position);
        if (dist <= playerFollowRadius)
        {
            _state = State.Submitting;
            Debug.Log("[RobotEscort] → Player arrived! Submitting ingredients...");
            StartCoroutine(SubmitIngredients());
        }
    }

    private IEnumerator SubmitIngredients()
    {
        if (_cauldron == null)
        {
            Debug.LogError("[RobotEscort] Cauldron script not found!");
            Finish();
            yield break;
        }

        // Face cauldron
        Vector3 look = cauldronTarget.position - transform.position;
        look.y = 0;
        if (look != Vector3.zero) transform.rotation = Quaternion.LookRotation(look);

        foreach (var item in _collectedItems)
        {
            _cauldron.TryAddSingleIngredient(item);
            Debug.Log($"[RobotEscort] Dropped: {item.itemName}");
            yield return new WaitForSeconds(submitDelay);
        }

        _cauldron.FinalizeSubmission();
        yield return new WaitForSeconds(finishPauseTime);

        Finish();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SetRobotAI(bool enabled)
    {
        if (_robotAI != null) _robotAI.enabled = enabled;
    }

    private void MoveAgentTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }
    }

    private void StopAgent()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = true;
    }

    private void Finish()
    {
        _collectedItems.Clear();
        _state = State.Done;
        
        // Clear override to return to normal follow behavior
        if (_robotAI != null) _robotAI.SetDestinationOverride(null); 

        Debug.Log("[RobotEscort] → Done! Handing back to RobotAI.");
    }

    /// <summary>Reset for a retry after a failed puzzle.</summary>
    public void ResetEscort()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _pickupQueue.Clear();
        _collectedItems.Clear();
        _state = State.Idle;
        StopAgent();
        if (_robotAI != null) _robotAI.SetDestinationOverride(null);
        if (InventoryManager.Instance != null) InventoryManager.Instance.ResetCollectedFlag();
    }
}
