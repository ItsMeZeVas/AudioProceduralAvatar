using System.IO;
using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Persistence
{
    /// <summary>
    /// Guarda y carga el leitmotiv de cada avatar como WAV en disco, junto a
    /// su JSON (avatars/{id}.wav, mismo folder que AvatarJsonStorage). Se
    /// genera UNA sola vez -- en creación, o como fallback perezoso la
    /// primera vez que el álbum lo necesita y no lo encuentra -- nunca se
    /// regenera después de eso.
    /// </summary>
    public static class AvatarAudioStorage
    {
        private static string FolderPath => Path.Combine(Application.persistentDataPath, "avatars");

        public static string GetWavPath(string avatarId) => Path.Combine(FolderPath, $"{avatarId}.wav");

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
            WavUtility.Save(path, samples, sampleRate: 44100, channels: 1);

            profile.LeitmotivPath = path;
            AvatarJsonStorage.Save(profile);

            Debug.Log($"[AvatarAudioStorage] Leitmotiv exportado: {path}");
            return path;
        }
    }
}
