using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementCC : MonoBehaviour
{
    [Header("Hoverboard Settings")]
    [SerializeField] private float hoverboardSpeed = 8f;
    [SerializeField] private float hoverboardAccel = 16f;
    [SerializeField] private float hoverboardDecel = 16f;

    [Header("Walking Settings")]
    [SerializeField] private float walkingSpeed = 2.5f;
    [SerializeField] private float walkingAccel = 80f;
    [SerializeField] private float walkingDecel = 80f;

    [Header("VR Components")]
    [SerializeField] private Transform cameraOffsetTransform;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("VR Turn")]
    [SerializeField] private float vrTurnSpeed = 90f;

    private Vector2 lookInput;



    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 currentVelocity;
    private float verticalVelocity;

    private float maxSpeed;
    private float accel;
    private float decel;

    private Vector3 lastTrackedPosition;
    private bool blockPhysicalMovement;

    public void SetHoverboardMode()
    {
        maxSpeed = hoverboardSpeed;
        accel = hoverboardAccel;
        decel = hoverboardDecel;
    }

    public void SetStationMode()
    {
        maxSpeed = walkingSpeed;
        accel = walkingAccel;
        decel = walkingDecel;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        maxSpeed = hoverboardSpeed;
        accel = hoverboardAccel;
        decel = hoverboardDecel;
        lastTrackedPosition = cameraOffsetTransform != null
            ? cameraOffsetTransform.localPosition
            : Vector3.zero;
    }

    void Update()
    {
        HandleMovement();
        ApplyGravity();
    }

    void LateUpdate()
    {
        if (blockPhysicalMovement)
        {
            //CounterPhysicalMovement();
        }
    }

    void HandleMovement()
    {
        // Use the main camera for direction on both KBM and VR
        // In VR this is the headset, in KBM this is the mouse-look camera
        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cam.right;
        right.y = 0f;
        right.Normalize();

        Vector3 targetVelocity = (forward * moveInput.y + right * moveInput.x) * maxSpeed;

        float currentAccel = (moveInput.magnitude > 0) ? accel : decel;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity, targetVelocity, currentAccel * Time.deltaTime
        );

        Vector3 motion = currentVelocity + Vector3.up * verticalVelocity;

        Vector3 posBefore = transform.position;
        CollisionFlags flags = controller.Move(motion * Time.deltaTime);
        Vector3 actualMove = transform.position - posBefore;

        if ((flags & CollisionFlags.Sides) != 0 && Time.deltaTime > 0.0001f)
        {
            currentVelocity = new Vector3(
                actualMove.x / Time.deltaTime, 0f, actualMove.z / Time.deltaTime
            );
        }

        if (GameSettings.isVR && Mathf.Abs(lookInput.x) > 0.1f)
        {
            transform.Rotate(Vector3.up, lookInput.x * vrTurnSpeed * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -53f);
        }
    }

    void CounterPhysicalMovement()
    {
        Vector3 currentPos = cameraOffsetTransform.localPosition;
        Vector3 physicalMovement = new Vector3(
            currentPos.x - lastTrackedPosition.x,
            0f,
            currentPos.z - lastTrackedPosition.z
        );
        controller.Move(-physicalMovement);
        lastTrackedPosition = cameraOffsetTransform.localPosition;
    }

    public void ResetPhysicalTracking()
    {
        if (cameraOffsetTransform != null)
            lastTrackedPosition = cameraOffsetTransform.localPosition;
    }

    public void SetBlockPhysicalMovement(bool block)
    {
        blockPhysicalMovement = block;
        ResetPhysicalTracking();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}