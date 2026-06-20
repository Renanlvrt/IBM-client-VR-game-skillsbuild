using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class GameSettings : MonoBehaviour
{
    public static bool isVR;
    [SerializeField] private bool forceVR = false;

    void Awake()
    {
        if (forceVR)
        {
            isVR = true;
            Debug.Log("VR mode: true (forced)");
            return;
        }

        // Check XR Management subsystem
        var xrManager = XRGeneralSettings.Instance;
        if (xrManager != null && xrManager.Manager != null)
        {
            var activeLoader = xrManager.Manager.activeLoader;
            isVR = activeLoader != null;
        }
        else
        {
            isVR = XRSettings.isDeviceActive;
        }

        Debug.Log($"VR mode: {isVR}");
    }
}