using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalDoor : MonoBehaviour
{
    [Header("Door Parts")]
    [SerializeField] private Transform topDoor;
    [SerializeField] private Transform bottomDoor;

    [Header("Door Animation")]
    [SerializeField] private float topOpenOffset = 2f;
    [SerializeField] private float bottomOpenOffset = -2f;
    [SerializeField] private float doorSpeed = 2f;

    [Header("Trigger")]
    [SerializeField] private Transform doorTriggerPoint;
    [SerializeField] private float triggerDistance = 3f;
    [SerializeField] private Transform player;

    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private float transitionDelay = 1.5f;
    [SerializeField] private Renderer fadeSphere;
    [SerializeField] private string shaderColorProperty = "_BaseColor";

    private Vector3 topClosedPos;
    private Vector3 bottomClosedPos;
    private Vector3 topOpenPos;
    private Vector3 bottomOpenPos;

    private bool doorOpen = false;
    private bool transitioning = false;
    private bool allowOpen = false;
    private bool forceOpen = false;

    public void EnableDoor()
    {
        allowOpen = true;
    }

    public void ForceOpen()
    {
        forceOpen = true;
    }

    private void Start()
    {
        topClosedPos = topDoor.localPosition;
        bottomClosedPos = bottomDoor.localPosition;

        topOpenPos = topClosedPos + Vector3.up * topOpenOffset;
        bottomOpenPos = bottomClosedPos + Vector3.up * bottomOpenOffset;
    }

    private void Update()
    {
        if (player == null || doorTriggerPoint == null) return;

        float dist = Vector3.Distance(player.position, doorTriggerPoint.position);
        bool shouldBeOpen = (dist < triggerDistance && allowOpen) || forceOpen;

        if (shouldBeOpen && !doorOpen)
        {
            doorOpen = true;
        }
        else if (!shouldBeOpen && doorOpen && !transitioning)
        {
            doorOpen = false;
        }

        Vector3 topTarget = doorOpen ? topOpenPos : topClosedPos;
        Vector3 bottomTarget = doorOpen ? bottomOpenPos : bottomClosedPos;

        topDoor.localPosition = Vector3.Lerp(
            topDoor.localPosition, topTarget, Time.deltaTime * doorSpeed
        );
        bottomDoor.localPosition = Vector3.Lerp(
            bottomDoor.localPosition, bottomTarget, Time.deltaTime * doorSpeed
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (transitioning) return;
        if (!allowOpen) return;

        if (other.CompareTag("Player"))
        {
            StartCoroutine(TransitionToWorld());
        }
    }

    private IEnumerator TransitionToWorld()
    {
        transitioning = true;

        if (fadeSphere != null)
        {
            Material mat = fadeSphere.material;
            mat.SetColor(shaderColorProperty, new Color(1f, 1f, 1f, 0f));

            float elapsed = 0f;
            while (elapsed < transitionDelay)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / transitionDelay;
                mat.SetColor(shaderColorProperty, new Color(1f, 1f, 1f, alpha));
                yield return null;
            }

            mat.SetColor(shaderColorProperty, new Color(1f, 1f, 1f, 1f));
        }
        else
        {
            yield return new WaitForSeconds(transitionDelay);
        }

        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"[PORTAL] Loading scene: '{targetSceneName}'");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("[PORTAL] targetSceneName is EMPTY! Set it in the Inspector on the PortalDoor GameObject.");
        }
    }
}