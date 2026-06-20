using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class PuterSpeaker : MonoBehaviour
{
    [Header("Puter API Settings")]
    [Header("Puter Relay Settings")]
    [Tooltip("The local relay URL (e.g., http://localhost:3000/speak)")]
    [SerializeField] private string relayUrl = "http://localhost:3000/speak";
    
    [Header("Voice Configuration")]
    [Tooltip("The provider to use: 'elevenlabs' or 'openai'")]
    [SerializeField] private string provider = "openai";
    [Tooltip("The model to use (e.g., 'tts-1')")]
    [SerializeField] private string model = "tts-1";
    [Tooltip("The voice ID (e.g., 'alloy', 'echo', 'fable', 'onyx', 'nova', 'shimmer')")]
    [SerializeField] private string voiceId = "alloy";

    [Header("Audio Output")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Fallback")]
    [Tooltip("If true, use Piper if Puter fails. If false, character will remain silent on error.")]
    [SerializeField] private bool useFallback = false;
    [Tooltip("Optional PiperSpeaker to use if Puter fails")]
    [SerializeField] private PiperSpeakerComponent piperFallback;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        // Only auto-get if fallback is explicitly intended
        if (useFallback && piperFallback == null) piperFallback = GetComponent<PiperSpeakerComponent>();
    }

    /// <summary>
    /// Configure voice settings at runtime from an NpcProfile.
    /// Called by WorldNPCController to set per-NPC voice identity.
    /// </summary>
    public void SetVoice(string newProvider, string newModel, string newVoiceId)
    {
        provider = newProvider;
        model = newModel;
        voiceId = newVoiceId;
    }

    /// <summary>
    /// Sets the fallback voice model name (e.g. for Piper).
    /// </summary>
    public void SetFallbackVoice(string modelName)
    {
        if (piperFallback != null)
        {
            _ = piperFallback.SetVoiceModel(modelName);
        }
    }

    /// <summary>
    /// Call this to speak text using the Puter Relay (ElevenLabs backend).
    /// </summary>
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // Puter character limit
        if (text.Length > 2900) 
        {
            Debug.LogWarning("[PuterSpeaker] Text too long.");
            if (useFallback && piperFallback != null) piperFallback.Speak(text);
            return;
        }

        StartCoroutine(ExecuteRelayTTS(text));
    }

    private IEnumerator ExecuteRelayTTS(string text)
    {
        // JSON Payload for the local relay
        string jsonPayload = $"{{\"text\": \"{text.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ")}\", " +
                             $"\"provider\": \"{provider}\", " +
                             $"\"model\": \"{model}\", " +
                             $"\"voiceId\": \"{voiceId}\"}}";
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(relayUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(relayUrl, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15; // 15 seconds timeout before fallback

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[PuterSpeaker] Relay error ({request.responseCode}): {request.error}. Triggering Piper fallback.");
                if (useFallback && piperFallback != null) 
                {
                    piperFallback.Speak(text);
                }
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogWarning("[PuterSpeaker] Success but clip null, falling back.");
                    if (useFallback && piperFallback != null) piperFallback.Speak(text);
                }
            }
        }
    }
}
