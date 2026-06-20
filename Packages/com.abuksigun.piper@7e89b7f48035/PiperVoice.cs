using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abuksigun.Piper
{
    public sealed unsafe class PiperVoice : IDisposable
    {
        Piper piper;
        PiperLib.Voice* voice;

        public Piper Piper => piper;
        internal PiperLib.Voice* Voice => voice;

        PiperVoice(Piper piper, PiperLib.Voice* voice)
        {
            this.piper = piper;
            this.voice = voice;
        }

        ~PiperVoice()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (voice != null)
            {
                PiperLib.destroy_Voice(voice);
                voice = null;
            }
        }

        public static Task<PiperVoice> LoadPiperVoice(Piper piper, string fullModelPath)
        {
            if (!File.Exists(fullModelPath))
                throw new FileNotFoundException("Model file not found", fullModelPath);
            if (!File.Exists(fullModelPath + ".json"))
                throw new FileNotFoundException("Model descriptor not found (Make sure it has the same name as model + .json)", fullModelPath);

            return Task.Run(() =>
            {
                var newVoice = PiperLib.create_Voice();
                try
                {
                    PiperLib.loadVoice(piper.Config, fullModelPath, fullModelPath + ".json", newVoice, null);
                    return Task.FromResult(new PiperVoice(piper, newVoice));
                }
                catch
                {
                    PiperLib.destroy_Voice(newVoice);
                    throw;
                }
            });
        }

        [ThreadStatic]
        private static List<float> _currentAudioBuffer;

        [AOT.MonoPInvokeCallback(typeof(PiperLib.AudioCallbackDelegate))]
        private static void StaticAudioCallback(short* data, int length)
        {
            if (_currentAudioBuffer != null)
            {
                for (int i = 0; i < length; i++)
                {
                    _currentAudioBuffer.Add(data[i] / 32768f);
                }
            }
        }

        private static readonly PiperLib.AudioCallbackDelegate _pinnedCallback = StaticAudioCallback;

        public float[] TextToPCMAudio(string text)
        {
            _currentAudioBuffer = new List<float>();

            PiperLib.SynthesisResult result = new PiperLib.SynthesisResult();
            PiperLib.textToAudio(piper.Config, voice, text, &result, _pinnedCallback);

            float[] finalArray = _currentAudioBuffer.ToArray();
            _currentAudioBuffer = null;
            return finalArray;
        }

        public void TextToAudioStream(string text, PiperLib.AudioCallbackDelegate audioCallback)
        {
            PiperLib.SynthesisResult result = new PiperLib.SynthesisResult();
            PiperLib.textToAudio(piper.Config, voice, text, &result, audioCallback);
        }
    }
}
