using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Image crosshairImage;
    [SerializeField] private float size = 150f;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float grabRange = 3f;

    private RectTransform rectTransform;

    private void Start()
    {
        Debug.Log($"Crosshair Start - isVR: {GameSettings.isVR}");

        rectTransform = crosshairImage.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(size, size);

        if (GameSettings.isVR)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Brighten when looking at something grabbable
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        bool hovering = Physics.Raycast(ray, grabRange, grabbableLayer);
        crosshairImage.color = hovering ? hoverColor : normalColor;
    }
}