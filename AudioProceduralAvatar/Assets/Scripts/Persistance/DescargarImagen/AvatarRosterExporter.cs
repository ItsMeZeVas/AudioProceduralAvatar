using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AudioProceduralAvatar.Persistence
{
    // ---------- Formato del archivo exportado ----------

    [Serializable]
    public class AvatarRosterExportEntry
    {
        public string Id;
        public string AvatarName;
        public string StudentCode;
        public string PcId;            // PC donde se creo (vacio si no se sabe)
        public string CreatedAt;       // hora de llegada a la nube (vacio si no se sabe)
        public bool PendingUpload;     // true = creado en este PC y aun no sube a la nube
    }

    [Serializable]
    public class AvatarRosterExport
    {
        public string ExportedAt;
        public string Source;          // "nube" (completo) o "local" (solo este PC)
        public int Count;
        public List<AvatarRosterExportEntry> Entries = new();
    }

    [Serializable]
    public class AvatarRosterRow
    {
        public string id;
        public string student_code;
        public string avatar_name;
        public string pc_id;
        public string created_at;
    }

    [Serializable]
    public class AvatarRosterRows
    {
        public List<AvatarRosterRow> rows = new();
    }

    // Arma UN solo JSON de asistencia con los avatares de todos los PCs, leyendolo de la
    // nube (la fuente completa). Si no hay red, exporta el roster de este PC y lo avisa.
    // Archivo NUEVO: no modifica roster.json ni nada existente.
    public static class AvatarRosterExporter
    {
        private const int PageSize = 1000;
        private const string NoCodePrefix = "sin-codigo-";

        public class Result
        {
            public bool Ok;
            public string Source;
            public int Count;
            public int Pending;
            public string FilePath;
            public string DesktopPath;   // null si no se pudo copiar al escritorio
            public string Folder;        // donde mirar (escritorio si se pudo, si no la carpeta de exports)
            public string Message;
        }

        public static IEnumerator Export(AvatarUploadConfig config, Action<Result> done)
        {
            var rows = new List<AvatarRosterRow>();
            bool cloudOk = false;
            string cloudError = null;

            if (config != null &&
                !string.IsNullOrEmpty(config.supabaseUrl) &&
                !string.IsNullOrEmpty(config.supabaseAnonKey))
            {
                cloudOk = true;
                for (int page = 0; page < 50; page++)
                {
                    string url = $"{config.supabaseUrl}/rest/v1/avatars" +
                                 "?select=id,student_code,avatar_name,pc_id,created_at" +
                                 $"&order=created_at.asc,id.asc&limit={PageSize}&offset={page * PageSize}";

                    using var request = UnityWebRequest.Get(url);
                    request.timeout = 15;
                    request.SetRequestHeader("apikey", config.supabaseAnonKey);
                    request.SetRequestHeader("Authorization", "Bearer " + config.supabaseAnonKey);
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        cloudOk = false;
                        cloudError = request.responseCode == 0 ? "sin conexion" : $"HTTP {request.responseCode}";
                        break;
                    }

                    AvatarRosterRows parsed = null;
                    try { parsed = JsonUtility.FromJson<AvatarRosterRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
                    catch (Exception e) { cloudError = "respuesta ilegible: " + e.Message; }

                    if (parsed == null || parsed.rows == null) { cloudOk = false; break; }

                    rows.AddRange(parsed.rows);
                    if (parsed.rows.Count < PageSize) break;
                }
            }
            else
            {
                cloudError = "falta el AvatarUploadConfig";
            }

            done(Build(cloudOk ? rows : null, cloudError));
        }

        // Separado de la descarga para poder probarlo sin red.
        public static Result Build(List<AvatarRosterRow> cloudRows, string cloudError)
        {
            var result = new Result();
            var export = new AvatarRosterExport
            {
                ExportedAt = DateTime.UtcNow.ToString("o"),
                Source = cloudRows != null ? "nube" : "local"
            };

            List<AvatarRosterEntry> local;
            try { local = AvatarJsonStorage.LoadRoster().Entries; }
            catch { local = new List<AvatarRosterEntry>(); }

            if (cloudRows != null)
            {
                var ids = new HashSet<string>();
                var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (AvatarRosterRow r in cloudRows)
                {
                    string code = CleanCode(r.student_code);
                    export.Entries.Add(new AvatarRosterExportEntry
                    {
                        Id = r.id,
                        AvatarName = r.avatar_name,
                        StudentCode = code,
                        PcId = r.pc_id ?? "",
                        CreatedAt = r.created_at ?? ""
                    });
                    ids.Add(r.id);
                    if (code.Length > 0) codes.Add(code);
                }

                // Creados en este PC y todavia sin subir: tambien asistieron.
                // (Si su codigo ya esta en la nube con otro id, es un duplicado en resolucion: se omite.)
                string thisPc = AvatarCloudSync.Instance != null ? AvatarCloudSync.PcId : SystemInfo.deviceName;
                foreach (AvatarRosterEntry e in local)
                {
                    if (ids.Contains(e.Id)) continue;
                    string code = (e.StudentCode ?? "").Trim();
                    if (code.Length > 0 && codes.Contains(code)) continue;

                    export.Entries.Add(new AvatarRosterExportEntry
                    {
                        Id = e.Id,
                        AvatarName = e.AvatarName,
                        StudentCode = code,
                        PcId = thisPc,
                        CreatedAt = "",
                        PendingUpload = true
                    });
                    result.Pending++;
                }
            }
            else
            {
                foreach (AvatarRosterEntry e in local)
                {
                    export.Entries.Add(new AvatarRosterExportEntry
                    {
                        Id = e.Id,
                        AvatarName = e.AvatarName,
                        StudentCode = (e.StudentCode ?? "").Trim(),
                        PcId = "",
                        CreatedAt = ""
                    });
                }
            }

            export.Count = export.Entries.Count;
            result.Source = export.Source;
            result.Count = export.Count;

            try
            {
                string json = JsonUtility.ToJson(export, true);
                string fileName = $"roster_unificado_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                string dir = Path.Combine(Application.persistentDataPath, "exports");
                Directory.CreateDirectory(dir);
                result.FilePath = Path.Combine(dir, fileName);
                File.WriteAllText(result.FilePath, json, new UTF8Encoding(false));
                result.Folder = dir;

                try
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                    {
                        result.DesktopPath = Path.Combine(desktop, fileName);
                        File.Copy(result.FilePath, result.DesktopPath, true);
                        result.Folder = desktop;
                    }
                }
                catch (Exception) { result.DesktopPath = null; }

                result.Ok = true;
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Message = "No se pudo guardar el archivo: " + e.Message;
                return result;
            }

            string where = result.DesktopPath != null ? "en el Escritorio" : "en " + result.Folder;
            if (result.Source == "nube")
            {
                result.Message = $"Exportados {result.Count} avatares de todos los PCs {where}.";
                if (result.Pending > 0)
                    result.Message += $" ({result.Pending} de este PC aun sin subir a la nube.)";
            }
            else
            {
                result.Message = $"SIN CONEXION ({cloudError ?? "?"}): se exporto solo el roster de ESTE PC " +
                                 $"({result.Count} avatares) {where}. Pueden faltar los de otros PCs.";
            }
            return result;
        }

        private static string CleanCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            return code.StartsWith(NoCodePrefix, StringComparison.Ordinal) ? "" : code.Trim();
        }
    }
}
