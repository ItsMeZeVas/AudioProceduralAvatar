using System.IO;
using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Persistence
{
    /// <summary>
    /// Guarda y carga el leitmotiv de cada avatar como WAV en disco, junto a
    /// su JSON (avatars/{id}_vN.wav, mismo folder que AvatarJsonStorage). Se
    /// genera UNA sola vez -- en creación, o como fallback perezoso la
    /// primera vez que el álbum lo necesita y no lo encuentra.
    ///
    /// WavVersion: súbelo cada vez que cambies el sintetizador
    /// (SimpleSynthRenderer) o el post-proceso del WAV (normalización). Los
    /// WAV con otra versión se ignoran y se regeneran solos la próxima vez
    /// que se abra el avatar. Sin esto, los avatares viejos siguen sonando
    /// con el audio renderizado por la versión anterior.
    ///
    /// v3: normalización de pico a NormalizeTargetPeak (antes el WAV salía
    /// con volumen muy bajo, pico ~0.3).
    /// </summary>
    public static class AvatarAudioStorage
    {
        private const int WavVersion = 3;

        // Pico objetivo del WAV exportado (1.0 = máximo digital). 0.92 deja
        // un pequeño margen para que no sature al convertir a 16 bits.
        private const float NormalizeTargetPeak = 0.92f;

        private static string FolderPath => Path.Combine(Application.persistentDataPath, "avatars");

        public static string GetWavPath(string avatarId) =>
            Path.Combine(FolderPath, $"{avatarId}_v{WavVersion}.wav");

        public static bool Exists(string avatarId) => File.Exists(GetWavPath(avatarId));

        /// <summary>
        /// Renderiza el leitmotiv fuera de tiempo real (SimpleSynthRenderer.RenderOffline),
        /// lo normaliza, lo guarda como WAV, actualiza profile.LeitmotivPath y
        /// persiste el JSON del avatar con esa referencia. Devuelve la ruta
        /// guardada, o null si no se pudo renderizar (por ejemplo, sin
        /// InstrumentPreset).
        /// </summary>
        public static string ExportAndLink(AvatarProfile profile, LeitmotivData data, SimpleSynthRenderer renderer)
        {
            AvatarJsonStorage.EnsureFolder();

            float[] samples = renderer.RenderOffline(data);
            if (samples.Length == 0)
            {
                Debug.LogWarning($"[AvatarAudioStorage] No se pudo renderizar el leitmotiv de '{profile.Id}' (sin InstrumentPreset).");
                return null;
            }

            // Sube el volumen del WAV: el sintetizador deja el pico muy por
            // debajo de 1.0 (preset.Volume * velocity * masterVolume + tanh).
            NormalizePeak(samples, NormalizeTargetPeak);

            string path = GetWavPath(profile.Id);

            // Mismo sample rate con el que se renderizó (antes estaba fijo en
            // 44100: si el dispositivo usaba 48000, el WAV sonaba más lento
            // y grave).
            WavUtility.Save(path, samples, renderer.RenderSampleRate, channels: 1);

            profile.LeitmotivPath = path;

            // El WAV cambió: la URL subida antes (QR) apunta al audio viejo.
            profile.WavPublicUrl = null;

            AvatarJsonStorage.Save(profile);

            Debug.Log($"[AvatarAudioStorage] Leitmotiv exportado: {path}");
            return path;
        }

        /// <summary>
        /// Escala todo el buffer para que su pico absoluto quede en
        /// targetPeak. No hace nada si el audio es (casi) silencio.
        /// </summary>
        private static void NormalizePeak(float[] samples, float targetPeak)
        {
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float a = Mathf.Abs(samples[i]);
                if (a > peak) peak = a;
            }

            if (peak < 0.0001f) return; // silencio, no dividir

            float gain = targetPeak / peak;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= gain;
        }
    }
}
