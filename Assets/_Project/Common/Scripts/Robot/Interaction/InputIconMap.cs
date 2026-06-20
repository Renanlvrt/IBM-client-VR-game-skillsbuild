using UnityEngine;

[CreateAssetMenu(fileName = "InputIconMap", menuName = "Game/Input Icon Map")]
public class InputIconMap : ScriptableObject
{
    [System.Serializable]
    public class ActionIcons
    {
        public string actionName;
        public Sprite vrIcon;
        public Sprite kbmIcon;
        public string vrLabel;
        public string kbmLabel;
    }

    [SerializeField] private ActionIcons[] actions;

    public ActionIcons GetAction(string actionName)
    {
        foreach (var action in actions)
        {
            if (action.actionName == actionName)
                return action;
        }

        Debug.LogWarning($"No icon mapping found for action: {actionName}");
        return null;
    }
}