using UnityEngine;
using System.IO;

public static class WavUtility
{
    const int HeaderSize = 44;

    /// <summary>
    /// Converts an AudioClip to a valid WAV byte array.
    /// </summary>
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            WriteWavHeader(stream, clip);
            WriteWavData(stream, clip);
            return stream.ToArray();
        }
    }

    private static void WriteWavHeader(Stream stream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        stream.Write(System.BitConverter.GetBytes(HeaderSize + (samples * channels * 2) - 8), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        stream.Write(System.BitConverter.GetBytes(16), 0, 4);
        stream.Write(System.BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(System.BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(System.BitConverter.GetBytes(hz), 0, 4);
        stream.Write(System.BitConverter.GetBytes(hz * channels * 2), 0, 4);
        stream.Write(System.BitConverter.GetBytes((short)(channels * 2)), 0, 2);
        stream.Write(System.BitConverter.GetBytes((short)16), 0, 2);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        stream.Write(System.BitConverter.GetBytes(samples * channels * 2), 0, 4);
    }

    private static void WriteWavData(Stream stream, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
            byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        stream.Write(bytesData, 0, bytesData.Length);
    }
}
