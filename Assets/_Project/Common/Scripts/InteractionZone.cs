using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject in the scene.
/// Press Tab to toggle cursor lock. While unlocked, checks if the mouse
/// is over any Button using RectTransformUtility (bypasses GraphicRaycaster).
/// </summary>
public class InteractionZone : MonoBehaviour
{
    private bool cursorUnlocked = false;
    private MouseLook mouseLook;
    private Button lastHovered;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            mouseLook = player.GetComponentInChildren<MouseLook>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleCursor();

        if (cursorUnlocked)
            HandleMouseOverButtons();
    }

    void ToggleCursor()
    {
        cursorUnlocked = !cursorUnlocked;

        if (cursorUnlocked)
        {
            if (mouseLook != null) mouseLook.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Mouse.current.WarpCursorPosition(center);
            InputState.Change(Mouse.current.position, center);

            Debug.Log("[InteractionZone] Cursor unlocked — press Tab to re-lock");
        }
        else
        {
            ClearHover();
            if (mouseLook != null) mouseLook.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[InteractionZone] Cursor locked");
        }
    }

    void HandleMouseOverButtons()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Camera cam = Camera.main;
        if (cam == null) return;

        Button hoveredButton = null;
        Button[] buttons = FindObjectsOfType<Button>(false);

        foreach (var btn in buttons)
        {
            if (!btn.interactable) continue;
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, cam))
            {
                hoveredButton = btn;
                break;
            }
        }

        if (hoveredButton != lastHovered)
        {
            ClearHover();
            if (hoveredButton != null)
                SetButtonHighlight(hoveredButton, true);
            lastHovered = hoveredButton;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && hoveredButton != null)
            hoveredButton.onClick.Invoke();
    }

    void ClearHover()
    {
        if (lastHovered != null)
            SetButtonHighlight(lastHovered, false);
        lastHovered = null;
    }

    void SetButtonHighlight(Button btn, bool highlighted)
    {
        if (btn.targetGraphic == null) return;
        ColorBlock cb = btn.colors;
        btn.targetGraphic.color = highlighted ? cb.highlightedColor : cb.normalColor;
    }
}
