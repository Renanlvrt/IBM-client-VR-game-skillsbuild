using System.Collections;
using UnityEngine;

public class Station : MonoBehaviour
{
    [SerializeField] private Renderer fadeRenderer;
    private Material fadeMaterial;

    private StationZone stationZone;
    private Transform playerTransform;

    private bool isTransitioning = false;

    private void SetFadeAlpha(float alpha)
    {
        Color c = fadeMaterial.GetColor("_BaseColor");
        c.a = alpha;
        fadeMaterial.SetColor("_BaseColor", c);
    }

    IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }
    }

    IEnumerator FadeToClear(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }
    }

    IEnumerator BlackScreen(float duration)
    {
        float elapsed = 0f;
        SetFadeAlpha(1f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator EnterStationTransition()
    {
        GameState.interactionInputEnabled = false;
        isTransitioning = true;

        yield return FadeToBlack(1f);

        Vector3 center = stationZone.transform.position;
        center.y = playerTransform.position.y;

        // Disable CC before teleport, re-enable after
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerTransform.position = center;
        if (cc != null) cc.enabled = true;

        playerTransform.GetComponent<PlayerMovementCC>()?.SetBlockPhysicalMovement(false);
        GameState.currentState = GameState.PlayerState.Station;
        GameState.currentStation = this;

        yield return BlackScreen(0.5f);
        yield return FadeToClear(1f);

        GameState.interactionInputEnabled = true;
        isTransitioning = false;
    }

    IEnumerator ExitStationTransition()
    {
        GameState.interactionInputEnabled = false;
        isTransitioning = true;

        yield return FadeToBlack(1f);

        Vector3 center = stationZone.transform.position;
        center.y = playerTransform.position.y;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerTransform.position = center;
        if (cc != null) cc.enabled = true;

        playerTransform.GetComponent<PlayerMovementCC>()?.SetBlockPhysicalMovement(true);
        GameState.currentState = GameState.PlayerState.Hoverboard;
        GameState.currentStation = null;

        yield return BlackScreen(0.5f);
        yield return FadeToClear(1f);

        GameState.interactionInputEnabled = true;
        isTransitioning = false;
    }

    /// <summary>
    /// Seamless exit — no fade, no teleport. Just switches state.
    /// Used when KBM player walks out of the station zone naturally.
    /// </summary>
    public void ExitStationSeamless(Transform player)
    {
        if (isTransitioning) return;

        playerTransform = player;
        playerTransform.GetComponent<PlayerMovementCC>()?.SetBlockPhysicalMovement(true);

        GameState.currentState = GameState.PlayerState.Hoverboard;
        GameState.currentStation = null;
    }

    void Start()
    {
        fadeMaterial = fadeRenderer.material;
        stationZone = GetComponent<StationZone>();
    }

    public void EnterStation(Transform player)
    {
        if (isTransitioning) return;
        playerTransform = player;
        StartCoroutine(EnterStationTransition());
    }

    public void ExitStation(Transform player)
    {
        if (isTransitioning) return;
        playerTransform = player;
        StartCoroutine(ExitStationTransition());
    }
}