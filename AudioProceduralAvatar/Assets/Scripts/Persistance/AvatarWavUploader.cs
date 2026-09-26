using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AudioProceduralAvatar.Persistence
{
    // Sube un .wav a un bucket público de Supabase Storage y arma la URL
    // pública. No depende de ningún SDK -- es un PUT directo por REST
    // (Supabase expone S3-compatible / REST bajo /storage/v1/object/...).
    public static class AvatarWavUploader
    {
        // x-upsert:true para poder re-subir el mismo avatar sin que falle
        // con "ya existe" si alguna vez se regenera el leitmotiv.
        public static IEnumerator Upload(
            string wavPath,
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

            if (!File.Exists(wavPath))
            {
                onError?.Invoke($"No existe el archivo: {wavPath}");
                yield break;
            }

            byte[] wavBytes = File.ReadAllBytes(wavPath);
            string objectPath = $"{avatarId}.wav";
            string uploadUrl = $"{config.supabaseUrl}/storage/v1/object/{config.bucketName}/{objectPath}";

            using var request = new UnityWebRequest(uploadUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(wavBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {config.supabaseAnonKey}");
            request.SetRequestHeader("apikey", config.supabaseAnonKey);
            request.SetRequestHeader("Content-Type", "audio/wav");
            request.SetRequestHeader("x-upsert", "true");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error subiendo a Supabase ({request.responseCode}): {request.error} -- {request.downloadHandler.text}");
                yield break;
            }

            string publicUrl = $"{config.supabaseUrl}/storage/v1/object/public/{config.bucketName}/{objectPath}";
            onSuccess?.Invoke(publicUrl);
        }
    }
}
