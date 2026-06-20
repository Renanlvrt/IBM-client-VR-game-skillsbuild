using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAbsorb : MonoBehaviour
{
    [SerializeField] private PlayerGrab playerGrab;
    [SerializeField] private Transform robotTransform;
    [SerializeField] private Transform playerCamera;

    [Header("KBM")]
    [SerializeField] private float kbmAbsorbRange = 5f;
    [SerializeField] private float kbmAbsorbAngle = 30f;

    [Header("VR / Thrown")]
    [SerializeField] private float absorbDistance = 1f;

    [Header("Throw Timeout")]
    [SerializeField] private float throwTimeout = 4f;

    private bool absorbing = false;
    private Dictionary<GrabbableObject, Coroutine> thrownTimers = new Dictionary<GrabbableObject, Coroutine>();

    private void OnEnable()
    {
        playerGrab.RegisterWorldInteraction(TryAbsorbKBM);
        PlayerGrab.OnObjectThrown += OnObjectThrown;
    }

    private void OnDisable()
    {
        playerGrab.UnregisterWorldInteraction();
        PlayerGrab.OnObjectThrown -= OnObjectThrown;
    }

    private void OnObjectThrown(GrabbableObject obj, Vector3 velocity)
    {
        Coroutine timer = StartCoroutine(ThrowTimeoutRoutine(obj));
        thrownTimers[obj] = timer;
    }

    private IEnumerator ThrowTimeoutRoutine(GrabbableObject obj)
    {
        yield return new WaitForSeconds(throwTimeout);

        if (obj != null && obj.IsThrown)
        {
            obj.ReturnToOriginal();
        }

        thrownTimers.Remove(obj);
    }

    private void CancelThrowTimer(GrabbableObject obj)
    {
        if (thrownTimers.ContainsKey(obj))
        {
            StopCoroutine(thrownTimers[obj]);
            thrownTimers.Remove(obj);
        }
    }

    private void Update()
    {
        if (absorbing) return;

        if (GameSettings.isVR && playerGrab.IsHolding)
        {
            float dist = Vector3.Distance(
                playerGrab.HeldObject.transform.position,
                robotTransform.position
            );

            if (dist < absorbDistance)
            {
                StartAbsorb(playerGrab.HeldObject, true);
            }
        }

        CheckThrownObjects();
    }

    private void CheckThrownObjects()
    {
        Collider[] colliders = Physics.OverlapSphere(robotTransform.position, absorbDistance);

        foreach (Collider col in colliders)
        {
            GrabbableObject obj = col.GetComponentInParent<GrabbableObject>();
            if (obj != null && obj.IsThrown)
            {
                CancelThrowTimer(obj);
                StartAbsorb(obj, false);
                break;
            }
        }
    }

    private bool TryAbsorbKBM()
    {
        if (absorbing) return false;
        if (!playerGrab.IsHolding) return false;

        Vector3 dirToRobot = (robotTransform.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, dirToRobot);
        float distance = Vector3.Distance(playerCamera.position, robotTransform.position);

        if (angle < kbmAbsorbAngle && distance < kbmAbsorbRange)
        {
            StartAbsorb(playerGrab.HeldObject, true);
            return true;
        }

        return false;
    }

    private void StartAbsorb(GrabbableObject obj, bool fromHand)
    {
        absorbing = true;

        if (fromHand)
        {
            playerGrab.ForceRelease();
        }

        obj.Absorb();

        IngredientAbsorb absorbAnim = obj.GetComponent<IngredientAbsorb>();
        IngredientPickup pickup = obj.GetComponent<IngredientPickup>();

        if (absorbAnim != null && pickup != null)
        {
            absorbAnim.Absorb(robotTransform, playerCamera, () =>
            {
                IngredientTracker.Instance.CollectFromStation(
                    pickup.stationID,
                    pickup.data,
                    obj,
                    pickup.maxPerStation
                );
                absorbing = false;
            });
        }
        else
        {
            absorbing = false;
        }
    }
}