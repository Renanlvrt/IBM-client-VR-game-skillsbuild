using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerGrab : MonoBehaviour
{
    [Header("KBM Settings")]
    [SerializeField] private Transform kbmHoldPoint;
    [SerializeField] private float kbmGrabRange = 5f;
    [SerializeField] private LayerMask grabbableLayer;

    [Header("VR Settings")]
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;
    [SerializeField] private float vrGrabRadius = 0.15f;

    [Header("Throw")]
    [SerializeField] private int velocitySamples = 5;

    private GrabbableObject heldObject;
    private bool isGrabbing = false;
    private Transform activeController;

    // VR velocity tracking
    private Queue<Vector3> velocityHistory = new Queue<Vector3>();
    private Vector3 lastControllerPos;

    // World interaction delegate
    private Func<bool> worldInteraction;

    // Events
    public static event Action<GrabbableObject> OnObjectGrabbed;
    public static event Action<GrabbableObject> OnObjectReleased;
    public static event Action<GrabbableObject, Vector3> OnObjectThrown;

    public bool IsHolding => heldObject != null;
    public GrabbableObject HeldObject => heldObject;

    public void RegisterWorldInteraction(Func<bool> interaction)
    {
        worldInteraction = interaction;
    }

    public void UnregisterWorldInteraction()
    {
        worldInteraction = null;
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        if (GameState.currentState != GameState.PlayerState.Station) return;


        if (GameSettings.isVR)
        {
            HandleVRGrab(context);
        }
        else
        {
            HandleKBMGrab(context);
        }
    }

    // Separate bindings for left and right grip
    public void OnGrabLeft(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        if (!GameSettings.isVR) return;
        if (GameState.currentState != GameState.PlayerState.Station) return;


        if (context.started)
        {
            TryGrabVRHand(leftController);
        }
        else if (context.canceled)
        {
            if (activeController == leftController)
            {
                ReleaseVR();
            }
        }
    }

    public void OnGrabRight(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        if (!GameSettings.isVR) return;
        if (GameState.currentState != GameState.PlayerState.Station) return;


        if (context.started)
        {
            TryGrabVRHand(rightController);
        }
        else if (context.canceled)
        {
            if (activeController == rightController)
            {
                ReleaseVR();
            }
        }
    }

    private void Update()
    {
        if (!GameSettings.isVR) return;
        if (heldObject == null) return;
        if (activeController == null) return;

        // Only track velocity, don't set position
        // Position is handled by parenting in GrabVR
        Vector3 currentVel = (activeController.position - lastControllerPos) / Time.deltaTime;
        lastControllerPos = activeController.position;

        velocityHistory.Enqueue(currentVel);
        if (velocityHistory.Count > velocitySamples)
            velocityHistory.Dequeue();
    }

    // ─── KBM ────────────────────────────────────────────

    private void HandleKBMGrab(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (heldObject != null)
        {
            if (worldInteraction != null && worldInteraction.Invoke())
            {
                return;
            }

            ReleaseKBM();
            return;
        }

        TryGrabKBM();
    }

    private void TryGrabKBM()
    {
        Transform cam = Camera.main.transform;
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, kbmGrabRange, grabbableLayer))
        {
            GrabbableObject obj = hit.collider.GetComponentInParent<GrabbableObject>();
            if (obj != null && !obj.IsGrabbed)
            {
                heldObject = obj;
                isGrabbing = true;
                obj.Grab(kbmHoldPoint);
                OnObjectGrabbed?.Invoke(obj);
            }
        }
    }

    private void ReleaseKBM()
    {
        if (heldObject == null) return;

        GrabbableObject obj = heldObject;
        heldObject = null;
        isGrabbing = false;
        obj.Release();
        OnObjectReleased?.Invoke(obj);
    }

    // ─── VR ─────────────────────────────────────────────

    private void HandleVRGrab(InputAction.CallbackContext context)
    {
        // Fallback if using single Grab action for both hands
        if (context.started)
        {
            TryGrabVR();
        }
        else if (context.canceled)
        {
            ReleaseVR();
        }
    }

    private void TryGrabVR()
    {
        // Check both controllers
        Transform[] controllers = { rightController, leftController };
        TryGrabVRFromControllers(controllers);
    }

    private void TryGrabVRHand(Transform controller)
    {
        if (controller == null) return;
        if (heldObject != null) return;

        Collider[] colliders = Physics.OverlapSphere(
            controller.position, vrGrabRadius, grabbableLayer
        );

        GrabbableObject closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider col in colliders)
        {
            GrabbableObject obj = col.GetComponentInParent<GrabbableObject>();
            if (obj != null && !obj.IsGrabbed)
            {
                float dist = Vector3.Distance(controller.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closest = obj;
                    closestDist = dist;
                }
            }
        }

        if (closest != null)
        {
            heldObject = closest;
            isGrabbing = true;
            activeController = controller;
            lastControllerPos = activeController.position;
            velocityHistory.Clear();

            closest.GrabVR(activeController);
            OnObjectGrabbed?.Invoke(closest);
        }
    }

    private void TryGrabVRFromControllers(Transform[] controllers)
    {
        if (heldObject != null) return;

        GrabbableObject closest = null;
        float closestDist = float.MaxValue;
        Transform closestController = null;

        foreach (Transform controller in controllers)
        {
            if (controller == null) continue;

            Collider[] colliders = Physics.OverlapSphere(
                controller.position, vrGrabRadius, grabbableLayer
            );

            foreach (Collider col in colliders)
            {
                GrabbableObject obj = col.GetComponentInParent<GrabbableObject>();
                if (obj != null && !obj.IsGrabbed)
                {
                    float dist = Vector3.Distance(controller.position, obj.transform.position);
                    if (dist < closestDist)
                    {
                        closest = obj;
                        closestDist = dist;
                        closestController = controller;
                    }
                }
            }
        }

        if (closest != null)
        {
            heldObject = closest;
            isGrabbing = true;
            activeController = closestController;
            lastControllerPos = activeController.position;
            velocityHistory.Clear();

            closest.GrabVR(activeController);
            OnObjectGrabbed?.Invoke(closest);
        }
    }

    private void ReleaseVR()
    {
        if (heldObject == null) return;

        // Try world interaction first
        if (worldInteraction != null && worldInteraction.Invoke())
        {
            return;
        }

        GrabbableObject obj = heldObject;
        heldObject = null;
        isGrabbing = false;

        Vector3 throwVelocity = Vector3.zero;
        if (velocityHistory.Count > 0)
        {
            foreach (Vector3 v in velocityHistory)
            {
                throwVelocity += v;
            }
            throwVelocity /= velocityHistory.Count;
        }

        if (throwVelocity.magnitude > 1f)
        {
            obj.Throw(throwVelocity);
            OnObjectThrown?.Invoke(obj, throwVelocity);
        }
        else
        {
            obj.Release();
            OnObjectReleased?.Invoke(obj);
        }

        activeController = null;
        velocityHistory.Clear();
    }

    public void ForceRelease()
    {
        if (heldObject == null) return;

        GrabbableObject obj = heldObject;
        heldObject = null;
        isGrabbing = false;
        activeController = null;
        velocityHistory.Clear();
    }
}