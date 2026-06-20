using System.Collections;
using UnityEngine;

public class IngredientAbsorb : MonoBehaviour
{
    [Header("Magnetic Pull")]
    [SerializeField] private float pullDuration = 0.4f;

    [Header("Hold In Front")]
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float holdDistance = 0.3f;

    [Header("Shrink")]
    [SerializeField] private float shrinkDuration = 0.3f;

    public void Absorb(Transform robot, Transform playerCamera, System.Action onComplete = null)
    {
        StartCoroutine(AbsorbRoutine(robot, playerCamera, onComplete));
    }

    private IEnumerator AbsorbRoutine(Transform robot, Transform playerCamera, System.Action onComplete)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;

        // Phase 1: Magnetic pull to front of robot
        float elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pullDuration;
            float smooth = t * t;

            Vector3 dirToPlayer = (playerCamera.position - robot.position).normalized;
            dirToPlayer.y = 0f;
            Vector3 holdPos = robot.position + dirToPlayer * holdDistance;
            holdPos.y = robot.position.y;

            transform.position = Vector3.Lerp(startPos, holdPos, smooth);
            yield return null;
        }

        // Phase 2: Hold in front of robot
        float holdElapsed = 0f;
        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;

            Vector3 dirToPlayer = (playerCamera.position - robot.position).normalized;
            dirToPlayer.y = 0f;
            Vector3 holdPos = robot.position + dirToPlayer * holdDistance;
            holdPos.y = robot.position.y;
            holdPos.y += Mathf.Sin(holdElapsed * 5f) * 0.02f;

            transform.position = holdPos;
            yield return null;
        }

        // Phase 3: Shrink into robot
        Vector3 shrinkStart = transform.position;
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            float smooth = t * t * t;

            transform.position = Vector3.Lerp(shrinkStart, robot.position, smooth);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smooth);
            yield return null;
        }

        onComplete?.Invoke();
        gameObject.SetActive(false);
        transform.localScale = startScale;
    }
}