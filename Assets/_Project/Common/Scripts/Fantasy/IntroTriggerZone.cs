using UnityEngine;

public class FantasyIntroTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Arrival,
        WizardArea
    }

    [SerializeField] private TriggerType triggerType;
    [SerializeField] private FantasyIntroController introController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (triggerType)
        {
            case TriggerType.Arrival:
                introController.OnArrivalTriggered();
                break;
            case TriggerType.WizardArea:
                introController.OnWizardAreaTriggered();
                break;
        }
    }
}