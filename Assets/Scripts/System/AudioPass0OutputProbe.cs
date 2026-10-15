#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

// Opt-in editor evidence only. Copies output samples without altering the audio buffer.
public sealed class AudioPass0OutputProbe : MonoBehaviour
{
    float[] samples;
    string output;
    int rate, channels, written;
    volatile int phase;
    public void Begin(string path, float seconds)
    {
        Flush(); output = path; rate = AudioSettings.outputSampleRate;
        channels = 2; written = 0; samples = new float[(int)(rate * 2 * Mathf.Clamp(seconds, 1, 30))]; phase = 1;
    }
    void OnAudioFilterRead(float[] data, int channelCount)
    {
        if (phase != 1 || samples == null) return;
        channels = channelCount;
        int count = Math.Min(data.Length, samples.Length - written);
        Array.Copy(data, 0, samples, written, count); written += count;
        if (written == samples.Length) phase = 2;
    }
    void Update() { if (phase == 2) Flush(); }
    public void Flush()
    {
        phase = 0;
        if (samples == null || written == 0 || string.IsNullOrEmpty(output)) { samples = null; return; }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        double square = 0; float peak = 0; int clipped = 0;
        using (var writer = new BinaryWriter(File.Create(output)))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + written * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(rate);
            writer.Write(rate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(written * 2);
            for (int i = 0; i < written; i++)
            {
                float value = samples[i]; peak = Math.Max(peak, Math.Abs(value)); square += value * value;
                if (Math.Abs(value) >= 1) clipped++;
                writer.Write((short)(Math.Max(-1, Math.Min(1, value)) * 32767));
            }
        }
        var report = new Capture { stamp = DateTime.Now.ToString("O"), sampleRate = rate, channelCount = channels, sampleCount = written,
            seconds = (double)written / rate / channels, peak = peak, rms = Math.Sqrt(square / written), clippedSamples = clipped };
        File.WriteAllText(output + ".json", JsonUtility.ToJson(report, true)); samples = null; output = null;
    }
    [Serializable] class Capture { public string stamp; public int sampleRate, channelCount, sampleCount, clippedSamples; public double seconds, peak, rms; }
    void OnDestroy() { Flush(); }
}
#endif
