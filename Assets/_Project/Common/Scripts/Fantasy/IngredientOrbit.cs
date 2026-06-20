using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientOrbit : MonoBehaviour
{
    [SerializeField] private Transform orbitCenter;
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private float orbitRadius = 0.4f;
    [SerializeField] private float orbitSpeed = 30f;
    [SerializeField] private float orbitHeight = 0.2f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float repositionSpeed = 5f;
    [SerializeField] private float orbitObjectScale = 0.1f;

    [Header("Arc Settings")]
    [SerializeField] private float arcHeight = 1.5f;
    [SerializeField] private float arcDuration = 0.8f;

    [Header("Pop Effect")]
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float popOvershoot = 0.3f;

    private List<OrbRecord> orbs = new List<OrbRecord>();
    private float angle = 0f;

    [System.Serializable]
    private class OrbRecord
    {
        public string ingredientID;
        public Transform transform;

        public OrbRecord(string id, Transform t)
        {
            ingredientID = id;
            transform = t;
        }
    }

    private void OnEnable()
    {
        IngredientTracker.OnIngredientCollected += OnCollected;
        IngredientTracker.OnIngredientSwapped += OnSwapped;
    }

    private void OnDisable()
    {
        IngredientTracker.OnIngredientCollected -= OnCollected;
        IngredientTracker.OnIngredientSwapped -= OnSwapped;
    }

    private void OnCollected(IngredientData data)
    {
        SpawnOrb(data);
    }

    private void OnSwapped(IngredientData oldData, IngredientData newData)
    {
        OrbRecord old = orbs.Find(o => o.ingredientID == oldData.ingredientID);
        if (old != null)
        {
            orbs.Remove(old);

            GrabbableObject returnTarget = IngredientTracker.Instance.GetGrabbableForLastSwap();
            if (returnTarget != null)
            {
                StartCoroutine(ArcReturn(old.transform, returnTarget));
            }
            else
            {
                Destroy(old.transform.gameObject);
            }
        }

        SpawnOrb(newData);
    }

    private void SpawnOrb(IngredientData data)
    {
        GameObject orb;

        if (data.prefab != null)
        {
            // Use a mini version of the actual object
            orb = Instantiate(data.prefab, orbitCenter.position, Quaternion.identity);
        }
        else
        {
            // Fallback to generic orb
            orb = Instantiate(orbPrefab, orbitCenter.position, Quaternion.identity);
        }

        orb.transform.SetParent(orbitCenter);
        orb.transform.localPosition = Vector3.zero;
        orb.transform.localScale = Vector3.one * orbitObjectScale;

        // Remove interaction components — orbit objects are visual only
        Collider col = orb.GetComponent<Collider>();
        if (col != null) Destroy(col);
        GrabbableObject grab = orb.GetComponent<GrabbableObject>();
        if (grab != null) Destroy(grab);
        IngredientPickup pickup = orb.GetComponent<IngredientPickup>();
        if (pickup != null) Destroy(pickup);
        IngredientAbsorb absorb = orb.GetComponent<IngredientAbsorb>();
        if (absorb != null) Destroy(absorb);

        // Tint trail if present
        TrailRenderer trail = orb.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.startColor = data.orbColor;
            trail.endColor = new Color(data.orbColor.r, data.orbColor.g, data.orbColor.b, 0f);
        }

        // Tint light if present
        Light light = orb.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.color = data.orbColor;
        }

        orbs.Add(new OrbRecord(data.ingredientID, orb.transform));
    }

    // ─────────────────────────────────
    // Cauldron: Send all orbs
    // ─────────────────────────────────
    public void SendAllOrbsTo(Vector3 target, float delayBetween)
    {
        StartCoroutine(SendOrbsSequence(target, delayBetween));
    }

    private IEnumerator SendOrbsSequence(Vector3 target, float delayBetween)
    {
        List<OrbRecord> orbsCopy = new List<OrbRecord>(orbs);
        orbs.Clear();

        foreach (OrbRecord orb in orbsCopy)
        {
            StartCoroutine(OrbToCauldron(orb.transform, target));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    private IEnumerator OrbToCauldron(Transform orb, Vector3 target)
    {
        orb.SetParent(null);

        TrailRenderer trail = orb.GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();

        Vector3 startPos = orb.position;
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 pos = Vector3.Lerp(startPos, target, t);
            float arc = 4f * arcHeight * t * (1f - t);
            pos.y += arc;

            orb.position = pos;
            yield return null;
        }

        Destroy(orb.gameObject);
    }

    // ─────────────────────────────────
    // Cauldron: Reject orb back
    // ─────────────────────────────────
    public void RejectOrb(IngredientData data, GrabbableObject target, Vector3 fromPosition)
    {
        GameObject orb;

        if (data.prefab != null)
        {
            orb = Instantiate(data.prefab, fromPosition, Quaternion.identity);
        }
        else
        {
            orb = Instantiate(orbPrefab, fromPosition, Quaternion.identity);
        }

        orb.transform.localScale = Vector3.one * orbitObjectScale;

        // Remove interaction components
        Collider col = orb.GetComponent<Collider>();
        if (col != null) Destroy(col);
        GrabbableObject grab = orb.GetComponent<GrabbableObject>();
        if (grab != null) Destroy(grab);
        IngredientPickup pickup = orb.GetComponent<IngredientPickup>();
        if (pickup != null) Destroy(pickup);
        IngredientAbsorb absorbComp = orb.GetComponent<IngredientAbsorb>();
        if (absorbComp != null) Destroy(absorbComp);

        // Tint trail
        TrailRenderer trail = orb.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.startColor = data.orbColor;
            trail.endColor = new Color(data.orbColor.r, data.orbColor.g, data.orbColor.b, 0f);
        }

        // Tint light
        Light light = orb.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.color = data.orbColor;
        }

        StartCoroutine(ArcReturn(orb.transform, target));
    }

    // ─────────────────────────────────
    // Shared: Arc return to original
    // ─────────────────────────────────
    private IEnumerator ArcReturn(Transform orb, GrabbableObject target)
    {
        orb.SetParent(null);

        TrailRenderer trail = orb.GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();

        Vector3 startPos = orb.position;
        Vector3 startScale = orb.localScale;
        Vector3 endPos = target.GetOriginalWorldPosition();

        float elapsed = 0f;

        while (elapsed < arcDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / arcDuration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            float arc = 4f * arcHeight * t * (1f - t);
            pos.y += arc;

            orb.position = pos;

            float scale = Mathf.Lerp(1f, 0.3f, t * t);
            orb.localScale = startScale * scale;

            yield return null;
        }

        Destroy(orb.gameObject);

        target.SnapToOriginal();

        StartCoroutine(PopScale(target.transform));
    }

    private IEnumerator PopScale(Transform obj)
    {
        Vector3 originalScale = obj.localScale;

        obj.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            float bounce = 1f + popOvershoot * Mathf.Sin(t * Mathf.PI);
            float ease = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            obj.localScale = originalScale * ease * bounce;

            yield return null;
        }

        obj.localScale = originalScale;
    }

    private void Update()
    {
        if (orbs.Count == 0) return;

        angle += orbitSpeed * Time.deltaTime;

        float spacing = 360f / orbs.Count;

        for (int i = 0; i < orbs.Count; i++)
        {
            float orbAngle = (angle + spacing * i) * Mathf.Deg2Rad;
            float bob = Mathf.Sin(Time.time * bobSpeed + i) * bobAmount;

            Vector3 targetOffset = new Vector3(
                Mathf.Cos(orbAngle) * orbitRadius,
                orbitHeight + bob,
                Mathf.Sin(orbAngle) * orbitRadius
            );

            orbs[i].transform.localPosition = Vector3.Lerp(
                orbs[i].transform.localPosition, targetOffset, Time.deltaTime * repositionSpeed
            );
        }
    }
}