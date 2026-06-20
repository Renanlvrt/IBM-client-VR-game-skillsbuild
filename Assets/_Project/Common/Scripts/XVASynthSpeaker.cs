using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class XVASynthSpeaker : MonoBehaviour
{
    [Header("xVASynth API Settings")]
    [Tooltip("The local URL where xVASynth is running.")]
    public string apiURL = "http://127.0.0.1:8008/synthesize"; 
    
    [Tooltip("The exact ID/name of the voice model loaded in xVASynth.")]
    public string voiceModel = "fallout4_codsworth"; 

    private AudioSource audioSource;

    void Start()
    {
        // Automatically grab the AudioSource attached to your character
        audioSource = GetComponent<AudioSource>();
    }

    // This is the method your RobotHintAI will call when it has the final text
    public void Speak(string textToSay)
    {
        StartCoroutine(GenerateAndPlayAudio(textToSay));
    }

    private IEnumerator GenerateAndPlayAudio(string text)
    {
        // 1. Format the JSON payload exactly how xVASynth expects it
        string jsonPayload = $@"{{
            ""model"": ""{voiceModel}"",
            ""text"": ""{text}""
        }}";

        // 2. Set up the web request to talk to the local xVASynth server
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(apiURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            
            // CRITICAL: We tell Unity we are expecting an audio file back, not text!
            request.downloadHandler = new DownloadHandlerAudioClip(apiURL, AudioType.WAV);

            Debug.Log($"<color=cyan>Sending text to xVASynth: {text}</color>");

            // 3. Wait for xVASynth to generate the audio file
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 4. Extract the audio, assign it to the speaker, and play!
                AudioClip generatedClip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = generatedClip;
                audioSource.Play();
                Debug.Log($"<color=green>xVASynth Audio Playing successfully!</color>");
            }
            else
            {
                Debug.LogError($"xVASynth Error: {request.error}. Make sure the xVASynth app is running in the background!");
            }
        }
    }
}