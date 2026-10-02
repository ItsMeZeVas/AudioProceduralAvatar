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
    /// (SimpleSynthRenderer). Los WAV con otra versión se ignoran y se
    /// regeneran solos la próxima vez que se abra el avatar. Sin esto, los
    /// avatares viejos siguen sonando con el audio renderizado por la
    /// versión anterior del sintetizador.
    /// </summary>
    public static class AvatarAudioStorage
    {
        private const int WavVersion = 2;

        private static string FolderPath => Path.Combine(Application.persistentDataPath, "avatars");

        public static string GetWavPath(string avatarId) =>
            Path.Combine(FolderPath, $"{avatarId}_v{WavVersion}.wav");

        public static bool Exists(string avatarId) => File.Exists(GetWavPath(avatarId));

        /// <summary>
        /// Renderiza el leitmotiv fuera de tiempo real (SimpleSynthRenderer.RenderOffline),
        /// lo guarda como WAV, actualiza profile.LeitmotivPath y persiste el
        /// JSON del avatar con esa referencia. Devuelve la ruta guardada, o
        /// null si no se pudo renderizar (por ejemplo, sin InstrumentPreset).
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
    }
}