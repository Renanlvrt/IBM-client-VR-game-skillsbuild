using UnityEngine;

namespace AntiGravity.AI
{
    public enum HubSituation
    {
        Guidance,
        Rules,
        LowEnergy,
        SecurityAlert,
        Encouragement
    }

    public class SituationManager : MonoBehaviour
    {
        [Header("Current State")]
        public HubSituation currentSituation = HubSituation.Guidance;

        public string GetSituationContext()
        {
            switch (currentSituation)
            {
                case HubSituation.Guidance:
                    return "[SITUATION: Guidance. CONTEXT: The user is trapped in the Main Hub. The user must travel to four different worlds and return with four keys to unlock the exit. Encourage them to explore.]";

                case HubSituation.Rules:
                    return "[SITUATION: Rules of the World. CONTEXT: 1. Do not touch red beams. 2. Gravity is unstable. 3. Respect the machines. 4. Collect all keys. Remind the user of these rules strictly but kindly.]";

                case HubSituation.LowEnergy:
                    return "[SITUATION: Low Energy. CONTEXT: Your battery is running low. You are moving slower and acting a bit tired. Urge the user to hurry and finish the current task.]";

                case HubSituation.SecurityAlert:
                    return "[SITUATION: Security Alert. CONTEXT: You have detected an unknown anomaly in the station's security grid. Tell the user to stay alert and be cautious of their surroundings.]";

                case HubSituation.Encouragement:
                    return "[SITUATION: Encouragement. CONTEXT: The user is making great progress. They look confident. Praise their puzzle-solving skills and keep their spirits high.]";

                default:
                    return "[SITUATION: Normal operation.]";
            }
        }

        // Helper method to change situation from other scripts/Triggers
        public void SetSituation(HubSituation newSituation)
        {
            currentSituation = newSituation;
            Debug.Log($"<color=orange><b>[AI CONTEXT]</b> Situation changed to: {newSituation}</color>");
        }
    }
}
