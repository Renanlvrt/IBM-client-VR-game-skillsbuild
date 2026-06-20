using System.Collections;
using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isGrabbed = false;
    private bool isThrown = false;

    [SerializeField] private float grabSmoothSpeed = 15f;

    public bool IsGrabbed => isGrabbed;
    public bool IsThrown => isThrown;

    private Coroutine smoothGrab;
    private Rigidbody rb;

    private void Start()
    {
        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
    }

    // KBM grab — smooth lerp to hold point
    public void Grab(Transform grabParent)
    {
        isGrabbed = true;
        isThrown = false;

        rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        transform.SetParent(grabParent);
        smoothGrab = StartCoroutine(SmoothToLocal(Vector3.zero, Quaternion.identity));
    }

    // VR grab — snap to controller
    public void GrabVR(Transform controller)
    {
        isGrabbed = true;
        isThrown = false;

        rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (smoothGrab != null) StopCoroutine(smoothGrab);

        transform.SetParent(controller);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Release()
    {
        isGrabbed = false;
        isThrown = false;

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        transform.SetParent(originalParent);
        smoothGrab = StartCoroutine(SmoothToLocal(originalLocalPos, originalLocalRot));
    }

    public void Throw(Vector3 velocity)
    {
        isGrabbed = false;
        isThrown = true;

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        transform.SetParent(null);

        rb = gameObject.AddComponent<Rigidbody>();
        rb.linearVelocity = velocity;
        rb.useGravity = true;
    }

    public void ReturnToOriginal()
    {
        isGrabbed = false;
        isThrown = false;

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        gameObject.SetActive(true);
        transform.SetParent(originalParent);
        smoothGrab = StartCoroutine(SmoothToLocal(originalLocalPos, originalLocalRot));
    }

    public void SnapToOriginal()
    {
        isGrabbed = false;
        isThrown = false;

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        transform.SetParent(originalParent);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
        gameObject.SetActive(true);
    }

    public Vector3 GetOriginalWorldPosition()
    {
        if (originalParent != null)
            return originalParent.TransformPoint(originalLocalPos);
        return originalLocalPos;
    }

    public void Absorb()
    {
        isGrabbed = false;
        isThrown = false;

        if (smoothGrab != null) StopCoroutine(smoothGrab);
        rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        transform.SetParent(null);
    }



    private IEnumerator SmoothToLocal(Vector3 targetPos, Quaternion targetRot)
    {
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, targetPos, Time.deltaTime * grabSmoothSpeed
            );
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, targetRot, Time.deltaTime * grabSmoothSpeed
            );
            yield return null;
        }
        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
    }
}