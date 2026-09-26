using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Persistence
{
    [Serializable]
    public class AvatarRosterEntry
    {
        public string Id;
        public string AvatarName;
        public string StudentCode;
    }

    // Wrapper porque JsonUtility no serializa una List<> suelta como raíz.
    [Serializable]
    public class AvatarRoster
    {
        public List<AvatarRosterEntry> Entries = new();
    }

    /// <summary>
    /// Guarda y carga el AvatarProfile en JSON. Pensado para desacoplar la
    /// escena de personalización de la escena de galería — todavía no está
    /// confirmado si van a vivir en la misma pantalla/PC o en dos separadas,
    /// así que no se hablan directamente en memoria: solo a través de estos
    /// archivos en disco. Si terminan siendo 2 PCs, no hay que rediseñar nada.
    ///
    /// Por cada avatar: avatars/{id}.json (datos) + avatars/{id}.png (la
    /// captura de AvatarCapture, opcional).
    ///
    /// Además mantiene roster.json (afuera de la carpeta avatars/, un nivel
    /// arriba) con solo AvatarName + StudentCode de TODOS los avatares
    /// guardados, para poder tomar asistencia leyendo un solo archivo en vez
    /// de abrir los JSON uno por uno. Se actualiza en cada Save().
    /// </summary>
    public static class AvatarJsonStorage
    {
        private static string FolderPath => Path.Combine(Application.persistentDataPath, "avatars");
        private static string RosterPath => Path.Combine(Application.persistentDataPath, "roster.json");

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

            UpdateRoster(profile);

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

        /// <summary>
        /// True si algún avatar ya guardado tiene este código estudiantil
        /// (comparación sin distinguir mayúsculas ni espacios extra).
        /// </summary>
        public static bool StudentCodeExists(string code, string excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string normalized = code.Trim().ToLowerInvariant();

            foreach (var id in GetAllAvatarIds())
            {
                if (id == excludeId) continue;
                var profile = Load(id);
                if (profile == null || string.IsNullOrWhiteSpace(profile.StudentCode)) continue;
                if (profile.StudentCode.Trim().ToLowerInvariant() == normalized) return true;
            }
            return false;
        }

        // ================= ROSTER (para tomar asistencia) =================

        /// <summary>Carga roster.json completo. Devuelve uno vacío si todavía no existe.</summary>
        public static AvatarRoster LoadRoster()
        {
            if (!File.Exists(RosterPath))
                return new AvatarRoster();

            string json = File.ReadAllText(RosterPath);
            var roster = JsonUtility.FromJson<AvatarRoster>(json);
            return roster ?? new AvatarRoster();
        }

        private static void UpdateRoster(AvatarProfile profile)
        {
            var roster = LoadRoster();

            int existingIndex = roster.Entries.FindIndex(e => e.Id == profile.Id);
            var entry = new AvatarRosterEntry
            {
                Id = profile.Id,
                AvatarName = profile.AvatarName,
                StudentCode = profile.StudentCode
            };

            if (existingIndex >= 0)
                roster.Entries[existingIndex] = entry;
            else
                roster.Entries.Add(entry);

            string json = JsonUtility.ToJson(roster, prettyPrint: true);
            File.WriteAllText(RosterPath, json);
        }

        private static string GetJsonPath(string id) => Path.Combine(FolderPath, $"{id}.json");
        private static string GetImagePath(string id) => Path.Combine(FolderPath, $"{id}.png");
    }
}
