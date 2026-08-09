using System;
using System.IO;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Persistence
{
    /// <summary>
    /// Guarda y carga el AvatarProfile en JSON. Pensado para desacoplar la
    /// escena de personalización de la escena de galería — todavía no está
    /// confirmado si van a vivir en la misma pantalla/PC o en dos separadas,
    /// así que no se hablan directamente en memoria: solo a través de estos
    /// archivos en disco. Si terminan siendo 2 PCs, no hay que rediseñar nada.
    ///
    /// Por cada avatar: avatars/{id}.json (datos) + avatars/{id}.png (la
    /// captura de AvatarCapture, opcional).
    /// </summary>
    public static class AvatarJsonStorage
    {
        private static string FolderPath => Path.Combine(Application.persistentDataPath, "avatars");

        public static void EnsureFolder()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
        }

        public static void Save(AvatarProfile profile, Texture2D capturedImage = null)
        {
            EnsureFolder();
            if (string.IsNullOrEmpty(profile.Id))
                profile.Id = Guid.NewGuid().ToString();

            string json = JsonUtility.ToJson(profile, prettyPrint: true);
            File.WriteAllText(GetJsonPath(profile.Id), json);

            if (capturedImage != null)
            {
                byte[] png = capturedImage.EncodeToPNG();
                File.WriteAllBytes(GetImagePath(profile.Id), png);
            }

            Debug.Log($"[AvatarJsonStorage] Guardado: {GetJsonPath(profile.Id)}");
        }

        public static AvatarProfile Load(string id)
        {
            string path = GetJsonPath(id);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<AvatarProfile>(json);
        }

        public static Texture2D LoadImage(string id)
        {
            string path = GetImagePath(id);
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes); // se redimensiona automáticamente al tamaño real
            return tex;
        }

        /// <summary>Ids de todos los avatares guardados hasta ahora (nombre de archivo sin extensión).</summary>
        public static string[] GetAllAvatarIds()
        {
            EnsureFolder();
            var files = Directory.GetFiles(FolderPath, "*.json");
            var ids = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                ids[i] = Path.GetFileNameWithoutExtension(files[i]);
            return ids;
        }

        private static string GetJsonPath(string id) => Path.Combine(FolderPath, $"{id}.json");
        private static string GetImagePath(string id) => Path.Combine(FolderPath, $"{id}.png");
    }
}
