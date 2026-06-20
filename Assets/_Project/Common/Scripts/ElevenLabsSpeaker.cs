using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class ElevenLabsSpeaker : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "YOUR_ELEVEN_LABS_API_KEY";
    [SerializeField] private string voiceId = "EXAVITQu4vr4xnSDxMaL"; // Default "Bella" or your choice
    [SerializeField] private string modelId = "eleven_monolingual_v1";

    [Header("Audio Output")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Call this to speak text using ElevenLabs API.
    /// </summary>
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_ELEVEN_LABS_API_KEY")
        {
            Debug.LogError("[ElevenLabs] API Key is missing! Voice will not play.");
            return;
        }

        if (string.IsNullOrEmpty(text)) return;

        StartCoroutine(ExecuteTTS(text));
    }

    private IEnumerator ExecuteTTS(string text)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        // JSON body
        string jsonPayload = $"{{\"text\": \"{text.Replace("\"", "\\\"")}\", \"model_id\": \"{modelId}\", \"voice_settings\": {{\"stability\": 0.5, \"similarity_boost\": 0.5}}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ElevenLabs] API Error: {request.error}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
        }
    }
}
