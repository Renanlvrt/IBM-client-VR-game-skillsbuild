using UnityEngine;
using LLMUnity;

public class LLMConsoleTester : MonoBehaviour
{
    public LLMCharacter aiCharacter;
    public string testMessage = "Hello Elara, give me a hint!";

    [ContextMenu("Test Chat")]
    public async void TestChat()
    {
        if (aiCharacter == null)
        {
            Debug.LogError("LLMConsoleTester: Please assign the AI Character (Granite Robot) in the Inspector.");
            return;
        }

        Debug.Log($"<color=cyan><b>[LLM TEST]</b> Sending message: {testMessage}</color>");
        
        string fullResponse = "";
        
        await aiCharacter.Chat(testMessage, 
            (response) => {
                // This shows tokens as they arrive
                fullResponse = response;
            }, 
            () => {
                // This shows the final result
                Debug.Log($"<color=green><b>[LLM TEST COMPLETE]</b> Full Response:</color>\n{fullResponse}");
            }
        );
    }
}
