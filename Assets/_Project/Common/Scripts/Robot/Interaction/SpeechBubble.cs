using System.Collections;
using UnityEngine;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] public Transform robot;
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Position Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.4f, 0.3f);

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float delayBetweenLines = 1.5f;

    [Header("Robot Voice (Legacy Blips)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip[] robotGibberishClips;
    [SerializeField] private float blipInterval = 0.08f;
    [SerializeField] private float pitchVariation = 0.15f;
    [SerializeField] private float basePitch = 1.2f;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 6f;

    private Coroutine currentDialogue;
    private bool isSpeaking = false;

    private void Start()
    {
        if (bubbleCanvasGroup != null) bubbleCanvasGroup.alpha = 0f;
        if (dialogueText != null) dialogueText.text = "";
    }

    private void Update()
    {
        if (!isSpeaking) return;

        UpdatePosition();
        UpdateBillboard();
    }

    private void UpdatePosition()
    {
        if (robot == null || playerCamera == null) return;

        Vector3 dirToPlayer = playerCamera.position - robot.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        dirToPlayer.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer);

        Vector3 bubblePos = robot.position
            + dirToPlayer * offset.z
            + right * offset.x
            + Vector3.up * offset.y;

        transform.position = bubblePos;
    }

    private void UpdateBillboard()
    {
        if (playerCamera == null) return;

        Vector3 directionToCamera = playerCamera.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }

    public void Say(string[] lines, System.Action onComplete = null)
    {
        Debug.Log($"[SPEECHBUBBLE] Say called with {lines?.Length ?? 0} lines on {gameObject.name}");
        Debug.Log($"[SPEECHBUBBLE] dialogueText={(dialogueText != null ? "OK" : "NULL")}, bubbleCanvasGroup={(bubbleCanvasGroup != null ? "OK" : "NULL")}");
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        isSpeaking = false;
        currentDialogue = StartCoroutine(PlayDialogue(lines, onComplete));
    }

    public void Say(string singleLine, System.Action onComplete = null)
    {
        Say(new string[] { singleLine }, onComplete);
    }

    private IEnumerator PlayDialogue(string[] lines, System.Action onComplete)
    {
        Debug.Log($"[SPEECHBUBBLE] PlayDialogue START ({lines?.Length ?? 0} lines)");
        isSpeaking = true;
        yield return StartCoroutine(FadeBubble(1f));

        foreach (string line in lines)
        {
            Debug.Log($"[SPEECHBUBBLE] Displaying line: '{line}'");
            if (dialogueText == null) { Debug.LogError("[SPEECHBUBBLE] dialogueText is NULL!"); yield break; }
            dialogueText.text = "";
            float blipTimer = 0f;

            for (int i = 0; i < line.Length; i++)
            {
                dialogueText.text = line.Substring(0, i + 1);

                blipTimer += 1f / charactersPerSecond;
                if (blipTimer >= blipInterval && line[i] != ' ')
                {
                    PlayRobotBlip();
                    blipTimer = 0f;
                }

                yield return new WaitForSeconds(1f / charactersPerSecond);
            }

            yield return new WaitForSeconds(delayBetweenLines);
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeBubble(0f));

        Debug.Log("[SPEECHBUBBLE] PlayDialogue DONE");
        isSpeaking = false;
        dialogueText.text = "";
        onComplete?.Invoke();
    }

    private void PlayRobotBlip()
    {
        if (robotGibberishClips == null || robotGibberishClips.Length == 0 || voiceSource == null) return;

        // NEW: If the robot is using the high-quality Piper TTS, don't play legacy blips
        if (robot != null && robot.GetComponentInChildren<PiperSpeakerComponent>() != null)
        {
            return;
        }

        AudioClip clip = robotGibberishClips[Random.Range(0, robotGibberishClips.Length)];
        voiceSource.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
        voiceSource.PlayOneShot(clip);
    }

    private IEnumerator FadeBubble(float targetAlpha)
    {
        if (bubbleCanvasGroup == null) yield break;

        while (Mathf.Abs(bubbleCanvasGroup.alpha - targetAlpha) > 0.01f)
        {
            bubbleCanvasGroup.alpha = Mathf.MoveTowards(
                bubbleCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed
            );
            yield return null;
        }
        bubbleCanvasGroup.alpha = targetAlpha;
    }
}
