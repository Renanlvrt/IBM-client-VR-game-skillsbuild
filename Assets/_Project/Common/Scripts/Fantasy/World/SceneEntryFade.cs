using System.Collections;
using UnityEngine;

public class SceneEntryFade : MonoBehaviour
{
    [SerializeField] private Renderer fadeSphere;
    [SerializeField] private string shaderColorProperty = "_BaseColor";
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private Color startColor = Color.white;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        Material mat = fadeSphere.material;

        // Start fully opaque white
        mat.SetColor(shaderColorProperty, new Color(startColor.r, startColor.g, startColor.b, 1f));

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);
            mat.SetColor(shaderColorProperty, new Color(startColor.r, startColor.g, startColor.b, alpha));
            yield return null;
        }

        mat.SetColor(shaderColorProperty, new Color(startColor.r, startColor.g, startColor.b, 0f));
    }
}