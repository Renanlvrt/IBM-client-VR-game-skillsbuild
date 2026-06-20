using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Abuksigun.Piper;

/// <summary>
/// A wrapper for the UnityPiper package to provide offline TTS.
/// </summary>
public class PiperSpeakerComponent : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public string voiceModelName = "en_GB-semaine-medium.onnx";
    public string espeakDataName = "espeak-ng-2023.9.7-4";

    private static Piper _sharedPiper;
    private static System.Collections.Generic.Dictionary<string, PiperVoice> _sharedVoices = new System.Collections.Generic.Dictionary<string, PiperVoice>();
    private static bool _isPiperLoading = false;

    private PiperSpeaker piperSpeaker;
    private bool isInitialized = false;
    private bool _isInitializing = false;
    
    [Header("Failsafe Settings")]
    [Tooltip("If true, disables TTS in built games to prevent crashes from missing C++ Debug DLLs (MSVCP140D.dll).")]
    public bool disableInBuilds = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
#if !UNITY_EDITOR
        // FORCE disable TTS in builds to prevent the native C++ DLL from crashing the application.
        disableInBuilds = true;
#endif
    }

    [DllImport("ucrtbased.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _CrtSetReportMode(int reportType, int reportMode);

    private static bool _debugAssertsDisabled = false;

    private static void DisableDebugAsserts()
    {
        if (_debugAssertsDisabled) return;
        _debugAssertsDisabled = true;
        try
        {
            // _CRT_WARN = 0, _CRT_ERROR = 1, _CRT_ASSERT = 2. 0 mode disables dialogs.
            _CrtSetReportMode(0, 0);
            _CrtSetReportMode(1, 0);
            _CrtSetReportMode(2, 0);
            Debug.Log("[PiperTTS] Successfully suppressed CRT Debug Asserts.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PiperTTS] Could not suppress CRT Debug Asserts: {ex.Message}");
        }
    }

    private async Task EnsureInitialized()
    {
        DisableDebugAsserts();

#if !UNITY_EDITOR
        if (disableInBuilds)
        {
            Debug.LogWarning($"[PiperTTS] ({gameObject.name}) TTS is disabled in this build.");
            return;
        }
#endif

        if (isInitialized || _isInitializing) return;
        _isInitializing = true;

        try
        {
            // Dispose previous speaker if we're re-initializing
            if (piperSpeaker != null) piperSpeaker = null;

            string streamingPath = Application.streamingAssetsPath.Replace("\\", "/");
            string espeakPath = Path.Combine(streamingPath, espeakDataName).Replace("\\", "/");
            string voicesPath = Path.Combine(streamingPath, "Voices").Replace("\\", "/");
            string modelPath = Path.Combine(voicesPath, voiceModelName).Replace("\\", "/");

            // 1. Initialize Shared Piper Engine & Voice (Static)
            if (_sharedPiper == null || !_sharedVoices.ContainsKey(voiceModelName))
            {
                while (_isPiperLoading) await Task.Delay(100);
                if (_sharedPiper == null || !_sharedVoices.ContainsKey(voiceModelName))
                {
                    _isPiperLoading = true;
                    try 
                    {
                        if (_sharedPiper == null)
                        {
                            Debug.Log($"[PiperTTS] Initializing Shared Engine. Espeak path: {espeakPath}");
                            if (!Directory.Exists(espeakPath))
                            {
                                Debug.LogError($"[PiperTTS] Espeak directory NOT FOUND at: {espeakPath}");
                                isInitialized = false;
                                return;
                            }
                            _sharedPiper = await Piper.LoadPiper(espeakPath);
                            Debug.Log($"[PiperTTS] Shared Engine initialized successfully.");
                        }
                        
                        if (!_sharedVoices.ContainsKey(voiceModelName))
                        {
                            Debug.Log($"[PiperTTS] Loading Shared Voice. Model path: {modelPath}");
                            if (!File.Exists(modelPath))
                            {
                                Debug.LogError($"[PiperTTS] Voice model NOT FOUND at: {modelPath}");
                                isInitialized = false;
                                return;
                            }
                            var newVoice = await PiperVoice.LoadPiperVoice(_sharedPiper, modelPath);
                            _sharedVoices[voiceModelName] = newVoice;
                            Debug.Log($"[PiperTTS] Shared Voice loaded successfully: {voiceModelName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PiperTTS] CRITICAL ERROR during initialization: {ex.Message}\n{ex.StackTrace}");
                        isInitialized = false;
                        return;
                    }
                    finally 
                    {
                        _isPiperLoading = false;
                    }
                }
            }

            // 3. Create Speaker for this instance
            Debug.Log($"[PiperTTS] ({gameObject.name}) Creating local speaker instance.");
            piperSpeaker = new PiperSpeaker(_sharedVoices[voiceModelName]);
            isInitialized = true;
            Debug.Log($"[PiperTTS] ({gameObject.name}) Initialized successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PiperTTS] ({gameObject.name}) Initialization failed: {ex.Message}");
            isInitialized = false;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// Changes the voice model and re-initializes the speaker.
    /// </summary>
    public async Task SetVoiceModel(string modelName)
    {
        if (voiceModelName == modelName && isInitialized) return;

        voiceModelName = modelName;
        isInitialized = false; // Force re-initialization
        await EnsureInitialized();
    }

    /// <summary>
    /// Speaks text using the offline Piper engine.
    /// </summary>
    public async void Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
#if !UNITY_EDITOR
            if (disableInBuilds) return;
#endif

            while (_isInitializing) await Task.Delay(100);

        if (!isInitialized)
        {
            await EnsureInitialized();
        }

        if (isInitialized && piperSpeaker != null)
        {
            Debug.Log($"[PiperTTS] ({gameObject.name}) Generating speech for: \"{text}\"");
            
            try 
            {
                // Start the generation (non-blocking for immediate audio streaming)
                var task = piperSpeaker.Speak(text);
                
                if (audioSource != null)
                {
                    audioSource.clip = piperSpeaker.AudioClip;
                    audioSource.loop = true; // CRITICAL: Must be true for procedural "stream" clips
                    
                    // Ensure volume is audible
                    if (audioSource.volume < 0.1f) audioSource.volume = 1.0f;
                    
                    audioSource.Stop(); 
                    Debug.Log($"[PiperTTS] ({gameObject.name}) Starting AudioSource playback.");
                    audioSource.Play();
                }
                else
                {
                    Debug.LogWarning($"[PiperTTS] ({gameObject.name}) No AudioSource assigned! You won't hear anything.");
                }

                await task;
                Debug.Log($"[PiperTTS] ({gameObject.name}) Generation task completed for: \"{text}\"");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PiperTTS] ({gameObject.name}) Error during speech generation: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[PiperTTS] ({gameObject.name}) Speak called but system is not ready. Initialized: {isInitialized}, Speaker: {piperSpeaker != null}");
        }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PiperTTS] ({gameObject.name}) Fatal error caught in async void Speak: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        // Reset statics on Play Mode enter to avoid holding native pointers from previous sessions
        if (_sharedVoices != null) 
        {
            foreach (var voice in _sharedVoices.Values) { voice?.Dispose(); }
            _sharedVoices.Clear();
        }
        if (_sharedPiper != null) { _sharedPiper.Dispose(); _sharedPiper = null; }
        _isPiperLoading = false;
    }

    private void OnDestroy()
    {
        // Do NOT dispose _sharedVoice or _sharedPiper here as they are shared across all instances.
        // They will be cleaned up when the app closes or domain reloads.
        piperSpeaker = null;
    }
}
