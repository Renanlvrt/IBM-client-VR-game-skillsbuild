using UnityEngine;
using UnityEngine.AI;

public class RobotVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pathFollower;
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private float hoverHeight = 2f;
    [SerializeField] private float followSpeed = 5f;

    [Header("Hover Animation")]
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobFrequency = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;

    private NavMeshAgent agent;
    private Vector3 smoothVelocity;
    private float bobOffset;
    private Vector3 targetPosition;

    void Start()
    {
        agent = pathFollower.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (pathFollower == null) return;
        FollowPathFollower();
        ApplyHoverBob();
        HandleRotation();
    }

    void FollowPathFollower()
    {
        targetPosition = pathFollower.position + Vector3.up * hoverHeight;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref smoothVelocity,
            1f / followSpeed
        );
    }

    void ApplyHoverBob()
    {
        bobOffset += Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(bobOffset) * bobAmplitude;
        Vector3 bobPosition = transform.position;
        bobPosition.y += bob * Time.deltaTime;
        transform.position = bobPosition;
    }

    void HandleRotation()
    {
        // Check the agent velocity directly, not RobotAI.IsMoving()
        // This works regardless of whether RobotAI is enabled or disabled
        bool isMoving = agent != null && agent.velocity.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            // Face the movement direction
            Vector3 direction = new Vector3(agent.velocity.x, 0f, agent.velocity.z).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else if (player != null)
        {
            // Stopped — face the player
            Vector3 direction = new Vector3(
                player.position.x - transform.position.x,
                0f,
                player.position.z - transform.position.z
            ).normalized;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}