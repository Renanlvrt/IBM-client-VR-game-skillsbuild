using UnityEngine;

/// <summary>
/// Simple proximity trigger for NPCs. 
/// Put this on the NPC (ensure it has a SphereCollider with 'Is Trigger' checked).
/// </summary>
public class NPCProximityTrigger : MonoBehaviour
{
    public RobotHintAI targetNPC;
    public string playerTag = "MainCamera"; // Typical tag for VR head

    private void Awake()
    {
        if (targetNPC == null) targetNPC = GetComponent<RobotHintAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<CharacterController>() != null)
        {
            if (targetNPC != null) targetNPC.SetPlayerNear(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<CharacterController>() != null)
        {
            if (targetNPC != null) targetNPC.SetPlayerNear(false);
        }
    }
}
