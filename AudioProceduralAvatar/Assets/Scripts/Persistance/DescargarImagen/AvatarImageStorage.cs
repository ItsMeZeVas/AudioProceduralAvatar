using System;
using System.IO;
using UnityEngine;

namespace AudioProceduralAvatar.Persistence
{
    // Guarda/lee la foto de cuerpo completo del avatar en la misma carpeta donde ya van
    // el JSON y el WAV: persistentDataPath/avatars/{guid}_full.png. Se llama "_full" a
    // proposito, para no chocar con ninguna miniatura que AvatarJsonStorage ya guarde.
    // (En el bucket de Supabase si se sube como {guid}.png.) Archivo NUEVO.
    public static class AvatarImageStorage
    {
        public static string GetPngPath(string avatarId)
        {
            return Path.Combine(Application.persistentDataPath, "avatars", avatarId + "_full.png");
        }

        public static bool Exists(string avatarId)
        {
            return !string.IsNullOrEmpty(avatarId) && File.Exists(GetPngPath(avatarId));
        }

        public static bool Save(string avatarId, byte[] png)
        {
            if (string.IsNullOrEmpty(avatarId) || png == null || png.Length == 0) return false;
            try
            {
                string path = GetPngPath(avatarId);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, png);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarImageStorage] No se pudo guardar la imagen: {e.Message}");
                return false;
            }
        }

        // null si no existe o no se pudo leer.
        public static byte[] Load(string avatarId)
        {
            if (!Exists(avatarId)) return null;
            try { return File.ReadAllBytes(GetPngPath(avatarId)); }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarImageStorage] No se pudo leer la imagen: {e.Message}");
                return null;
            }
        }
    }
}
