using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderLogger : MonoBehaviour
{
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Debug.Log(
            $"RENDERING CAMERA: {cam.name} | scene={cam.gameObject.scene.name} | active={cam.gameObject.activeInHierarchy} | enabled={cam.enabled}",
            cam.gameObject
        );
    }

    [ContextMenu("List All Cameras")]
    private void ListAllCameras()
    {
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();

        foreach (Camera cam in cameras)
        {
            Debug.Log(
                $"FOUND CAMERA: {cam.name} | scene={cam.gameObject.scene.name} | active={cam.gameObject.activeInHierarchy} | enabled={cam.enabled} | hideFlags={cam.hideFlags}",
                cam.gameObject
            );
        }
    }
}