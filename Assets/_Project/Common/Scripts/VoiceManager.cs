using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class VoiceManager : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private string serverUrl = "http://localhost:8001/transcribe";
    
    [Header("Recording Settings")]
    [SerializeField] private int maxDuration = 30;
    [SerializeField] private int frequency = 44100;
    
    public event Action<string> OnTranscriptionReceived;

    private AudioClip recordingClip;
    private bool isRecording = false;
    private float recordingStartTime;

    void Update()
    {
        // Hold Space to record (placeholder for VR input)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartRecording();
        }
        
        if (Input.GetKeyUp(KeyCode.Space))
        {
            StopRecording();
        }
    }

    public void StartRecording()
    {
        if (isRecording) return;
        
        Debug.Log("[VoiceManager] Recording started...");
        recordingClip = Microphone.Start(null, false, maxDuration, frequency);
        recordingStartTime = Time.time;
        isRecording = true;
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        int sampleCount = Microphone.GetPosition(null);
        Microphone.End(null);
        
        float length = Time.time - recordingStartTime;
        isRecording = false;
        
        Debug.Log($"[VoiceManager] Recording stopped. Length: {length:F2}s");

        if (sampleCount > 0)
        {
            // Trim the clip to actual recorded length
            AudioClip trimmedClip = AudioClip.Create("RecordedAudio", sampleCount, recordingClip.channels, frequency, false);
            float[] data = new float[sampleCount * recordingClip.channels];
            recordingClip.GetData(data, 0);
            trimmedClip.SetData(data, 0);

            StartCoroutine(SendAudioToServer(trimmedClip));
        }
    }

    private IEnumerator SendAudioToServer(AudioClip clip)
    {
        byte[] wavBytes = WavUtility.FromAudioClip(clip);
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", wavBytes, "voice.wav", "audio/wav"));

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, formData))
        {
            Debug.Log("[VoiceManager] Sending audio to server...");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[VoiceManager] Server Error: {www.error}");
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"[VoiceManager] Server Response: {jsonResponse}");
                
                // Parse response
                TranscriptionResponse response = JsonUtility.FromJson<TranscriptionResponse>(jsonResponse);
                if (response != null && response.status == "success")
                {
                    OnTranscriptionReceived?.Invoke(response.transcription);
                }
            }
        }
    }

    [Serializable]
    private class TranscriptionResponse
    {
        public string transcription;
        public string status;
    }
}
