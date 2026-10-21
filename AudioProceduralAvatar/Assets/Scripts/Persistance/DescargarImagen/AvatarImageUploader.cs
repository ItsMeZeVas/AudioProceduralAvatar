using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AudioProceduralAvatar.Persistence
{
    // Gemelo de AvatarWavUploader para el PNG de cuerpo completo.
    // Mismo bucket, misma config y mismo mecanismo (POST con x-upsert),
    // asi que usa las mismas politicas de Storage que ya funcionan para el .wav.
    // Archivo NUEVO: no modifica ni reemplaza nada existente.
    public static class AvatarImageUploader
    {
        public static string PublicUrl(AvatarUploadConfig config, string avatarId)
        {
            return $"{config.supabaseUrl}/storage/v1/object/public/{config.bucketName}/{avatarId}.png";
        }

        // Sube un PNG que ya esta guardado en disco.
        public static IEnumerator Upload(
            string pngPath,
            string avatarId,
            AvatarUploadConfig config,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (!File.Exists(pngPath))
            {
                onError?.Invoke($"No existe el archivo: {pngPath}");
                yield break;
            }

            byte[] bytes = File.ReadAllBytes(pngPath);
            yield return UploadBytes(bytes, avatarId, config, onSuccess, onError);
        }

        // Sube bytes PNG en memoria (por ejemplo, recien salidos de EncodeToPNG).
        public static IEnumerator UploadBytes(
            byte[] pngBytes,
            string avatarId,
            AvatarUploadConfig config,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (config == null ||
                string.IsNullOrEmpty(config.supabaseUrl) ||
                string.IsNullOrEmpty(config.supabaseAnonKey) ||
                string.IsNullOrEmpty(config.bucketName))
            {
                onError?.Invoke("AvatarUploadConfig incompleto (falta URL, key o bucket).");
                yield break;
            }

            if (pngBytes == null || pngBytes.Length == 0)
            {
                onError?.Invoke("El PNG esta vacio.");
                yield break;
            }

            if (string.IsNullOrEmpty(avatarId))
            {
                onError?.Invoke("avatarId vacio.");
                yield break;
            }

            string uploadUrl = $"{config.supabaseUrl}/storage/v1/object/{config.bucketName}/{avatarId}.png";

            using var request = new UnityWebRequest(uploadUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(pngBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {config.supabaseAnonKey}");
            request.SetRequestHeader("apikey", config.supabaseAnonKey);
            request.SetRequestHeader("Content-Type", "image/png");
            request.SetRequestHeader("x-upsert", "true");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error subiendo imagen a Supabase ({request.responseCode}): {request.error} -- {request.downloadHandler.text}");
                yield break;
            }

            onSuccess?.Invoke(PublicUrl(config, avatarId));
        }
    }
}
