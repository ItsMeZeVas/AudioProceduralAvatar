using System.IO;
using System.Text;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Escribe muestras de audio (float, rango -1..1) como WAV PCM16.
    /// Uso: exportar el leitmotiv renderizado offline por
    /// SimpleSynthRenderer.RenderOffline() a disco, una sola vez, en el
    /// momento de creación del avatar (o como fallback perezoso).
    /// </summary>
    public static class WavUtility
    {
        public static void Save(string path, float[] samples, int sampleRate, int channels = 1)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            int byteRate = sampleRate * channels * 2; // 16-bit
            int dataSize = samples.Length * 2;
            int riffChunkSize = 36 + dataSize;

            // ---- RIFF header ----
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(riffChunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // ---- fmt chunk ----
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // tamaño del chunk fmt
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2)); // block align
            writer.Write((short)16); // bits por muestra

            // ---- data chunk ----
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(s);
            }
        }
    }
}
