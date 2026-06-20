using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Mouse settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float verticalLookLimit = 80f; // Max up/down angle

    [Header("References")]
    [SerializeField] private Transform playerBody; // XR Origin
    [SerializeField] private Transform cameraTransform; // main camera

    private float xRotation = 0f; // roatation about x axis (up and down)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        playerBody.Rotate(Vector3.up * mouseX); // horizontal rot applied to entire body

        xRotation -= mouseY; //inverted - rotation down is up

        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localEulerAngles = new Vector3(xRotation, 0f, 0f); // vertical rot applied to camera only

    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

}
