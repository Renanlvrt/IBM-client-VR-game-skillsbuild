using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform _cameraTransform;

    private void Start()
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;

        // Look at the camera but only rotate around Y if we want it to stay upright, 
        // or full rotation for a floating book effect.
        // For a book on a pedestal, we usually just want it to face the player.
        
        Vector3 direction = _cameraTransform.position - transform.position;
        // Optional: Ensure it doesn't tilt up/down if attached to a fixed stand
        // direction.y = 0; 

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-direction);
        }
    }
}
