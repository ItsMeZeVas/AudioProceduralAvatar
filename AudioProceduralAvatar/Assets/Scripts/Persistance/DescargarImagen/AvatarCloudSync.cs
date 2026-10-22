using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Persistence
{
    // ---------- Modelos del intercambio con Supabase ----------

    [Serializable]
    public class AvatarCloudInsert
    {
        public string id;
        public string student_code;
        public string avatar_name;
        public string profile_json;
        public string pc_id;
        public bool has_image;
    }

    [Serializable]
    public class AvatarCloudRow
    {
        public string id;
        public string student_code;
        public string avatar_name;
        public string profile_json;
        public bool has_image;
        public string created_at;
    }

    [Serializable]
    public class AvatarCloudRows
    {
        public List<AvatarCloudRow> rows = new();
    }

    // Lo que este PC ya sabe de la nube (se guarda en cloud_sync_state.json).
    [Serializable]
    public class AvatarCloudState
    {
        public List<string> SyncedIds = new();        // avatares que ya estan en la tabla
        public List<string> WavDoneIds = new();       // avatares cuyo audio no hay que subir (ya subido, o ajeno)
        public List<string> ImageInCloudIds = new();  // avatares con imagen en el bucket
        public List<string> DuplicateIds = new();     // avatares locales que perdieron contra otro PC (mismo codigo)
        public string Cursor = "";                    // created_at de la ultima fila bajada
    }

    // Sincroniza los avatares entre los PCs a traves de Supabase. Archivo NUEVO.
    //
    //  - SUBE: cada avatar local que todavia no esta en la nube (imagen + fila en la tabla
    //    `avatars`), y despues su audio cuando exista. Si no hay red, reintenta en el
    //    siguiente ciclo: crear un avatar nunca espera a la red.
    //  - BAJA: los avatares creados en otros PCs, escribiendolos como JSON local con
    //    AvatarJsonStorage.Save (asi tambien llegan al roster.json y a StudentCodeExists).
    //  - Si el mismo codigo estudiantil llego antes desde otro PC, ese gana: el avatar
    //    local se aparta a avatars/duplicados/ y sale del roster.
    //
    // Se crea solo al iniciar el juego, SOLO si hay un AvatarUploadConfig dentro de una
    // carpeta "Resources". Sin ese asset no hace nada (es el interruptor de apagado).
    public class AvatarCloudSync : MonoBehaviour
    {
        public static event Action OnAvatarsDownloaded;
        public static AvatarCloudSync Instance { get; private set; }
        public static string PcId { get; private set; } = "PC";

        private const float FirstDelaySeconds = 3f;
        private const float PollSeconds = 30f;
        private const int PageSize = 200;
        private const int MaxFailedAttemptsPerSession = 5;

        private static readonly string[] UpsertHeader = { "x-upsert", "true" };

        private AvatarUploadConfig _config;
        private AvatarCloudState _state = new();
        private readonly Dictionary<string, int> _failedAttempts = new();
        private bool _busy;
        private bool _networkDown;

        private struct HttpResult
        {
            public long Code;     // 0 = no hubo respuesta (sin red / timeout)
            public string Body;
            public string Error;
        }

        // ================= ARRANQUE =================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            AvatarUploadConfig[] configs = Resources.LoadAll<AvatarUploadConfig>("");
            if (configs == null || configs.Length == 0)
            {
                Debug.Log("[AvatarCloudSync] No hay un AvatarUploadConfig dentro de una carpeta Resources: la sincronizacion entre PCs queda desactivada.");
                return;
            }

            AvatarUploadConfig config = configs[0];
            if (string.IsNullOrEmpty(config.supabaseUrl) ||
                string.IsNullOrEmpty(config.supabaseAnonKey) ||
                string.IsNullOrEmpty(config.bucketName))
            {
                Debug.LogWarning("[AvatarCloudSync] El AvatarUploadConfig esta incompleto (URL, key o bucket): la sincronizacion queda desactivada.");
                return;
            }

            var go = new GameObject("AvatarCloudSync");
            DontDestroyOnLoad(go);
            go.AddComponent<AvatarCloudSync>().Init(config);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Init(AvatarUploadConfig config)
        {
            _config = config;
            Instance = this;

            string device = SystemInfo.deviceName;
            PcId = string.IsNullOrWhiteSpace(device) || device == "<unknown>" ? "PC-desconocido" : device;

            LoadState();
            StartCoroutine(Loop());
            Debug.Log($"[AvatarCloudSync] Activo. PC: {PcId}.");
        }

        private IEnumerator Loop()
        {
            yield return new WaitForSecondsRealtime(FirstDelaySeconds);
            while (true)
            {
                yield return SyncNow();
                yield return new WaitForSecondsRealtime(PollSeconds);
            }
        }

        // Un ciclo completo. Publico por si luego se quiere un boton "sincronizar ahora".
        public IEnumerator SyncNow()
        {
            if (_busy) yield break;
            _busy = true;
            _networkDown = false;

            yield return UploadNewAvatars();
            yield return UploadPendingWavs();
            yield return DownloadNewAvatars();

            _busy = false;
        }

        public static void RequestSyncNow()
        {
            if (Instance != null && Instance._config != null)
                Instance.StartCoroutine(Instance.SyncNow());
        }

        // ================= CONSULTAS PARA OTROS SCRIPTS =================

        // True si la imagen de este avatar ya esta en el bucket (la subio este u otro PC).
        public static bool HasCloudImage(string avatarId)
        {
            return Instance != null && Instance._state.ImageInCloudIds.Contains(avatarId);
        }

        // Baja el .wav de un avatar creado en otro PC y lo deja donde el panel lo busca.
        // Si no esta en la nube (o no hay red), no hace nada: el panel genera el suyo.
        public static IEnumerator TryDownloadWav(string avatarId, string destinationPath)
        {
            if (Instance == null || Instance._config == null) yield break;

            string url = $"{Instance._config.supabaseUrl}/storage/v1/object/public/{Instance._config.bucketName}/{avatarId}.wav";
            using var request = UnityWebRequest.Get(url);
            request.timeout = 8;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) yield break;

            byte[] data = request.downloadHandler.data;
            if (data == null || data.Length < 44) yield break; // menos que un encabezado WAV

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.WriteAllBytes(destinationPath, data);
                Debug.Log($"[AvatarCloudSync] Audio de {avatarId} bajado de la nube.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] No se pudo guardar el audio bajado: {e.Message}");
            }
        }

        // ================= SUBIDA =================

        private IEnumerator UploadNewAvatars()
        {
            foreach (string id in AvatarJsonStorage.GetAllAvatarIds())
            {
                if (_networkDown) yield break;
                if (_state.SyncedIds.Contains(id) || _state.DuplicateIds.Contains(id)) continue;
                if (TooManyFailures(id)) continue;

                AvatarProfile profile = SafeLoadProfile(id);
                if (profile == null) continue;

                // 1) Imagen (antes que la fila, para que quien vea la fila ya la encuentre)
                bool hasImage = false;
                string pngPath = AvatarImageStorage.GetPngPath(id);
                byte[] png = File.Exists(pngPath) ? SafeReadAll(pngPath) : null;
                if (png != null && png.Length > 0)
                {
                    HttpResult up = default;
                    yield return Send("POST", StorageUrl(id + ".png"), png, "image/png", null, UpsertHeader, r => up = r);
                    if (IsSuccess(up)) hasImage = true;
                    else { LogFailure("imagen", id, up); NoteFailure(id, up); continue; }
                }

                // 2) Fila en la tabla
                string code = string.IsNullOrWhiteSpace(profile.StudentCode) ? "sin-codigo-" + id : profile.StudentCode.Trim();
                var insert = new AvatarCloudInsert
                {
                    id = id,
                    student_code = code,
                    avatar_name = profile.AvatarName ?? "",
                    profile_json = JsonUtility.ToJson(profile),
                    pc_id = PcId,
                    has_image = hasImage
                };

                HttpResult ins = default;
                yield return Send("POST", RestUrl("avatars"), Encoding.UTF8.GetBytes(JsonUtility.ToJson(insert)),
                    "application/json", "return=minimal", null, r => ins = r);

                if (IsSuccess(ins))
                {
                    MarkSynced(id, hasImage);
                    Debug.Log($"[AvatarCloudSync] Subido: {profile.AvatarName} ({code}).");
                    continue;
                }

                if (ins.Code == 409)
                {
                    yield return ResolveConflict(id, ins.Body, hasImage);
                    continue;
                }

                LogFailure("avatar", id, ins);
                NoteFailure(id, ins);
            }
        }

        // 409 = o ya estaba subido (mismo id) o el codigo ya lo uso otro avatar (gana el primero).
        private IEnumerator ResolveConflict(string id, string body, bool hasImage)
        {
            string b = body ?? "";

            if (b.Contains("avatars_pkey")) { MarkSynced(id, hasImage); yield break; }
            if (b.Contains("student_code")) { MarkDuplicate(id); yield break; }

            // Mensaje que no reconocemos: se pregunta directo si ese id ya existe en la nube.
            HttpResult check = default;
            yield return Send("GET", RestUrl("avatars?select=id&id=eq." + id), null, null, null, null, r => check = r);
            if (!IsSuccess(check)) yield break;

            if (check.Body != null && check.Body.Contains(id)) MarkSynced(id, hasImage);
            else MarkDuplicate(id);
        }

        // El audio se sube cuando exista en este PC (puede generarse despues de crear el avatar).
        private IEnumerator UploadPendingWavs()
        {
            foreach (string id in new List<string>(_state.SyncedIds))
            {
                if (_networkDown) yield break;
                if (_state.WavDoneIds.Contains(id)) continue;

                string wavPath = AvatarAudioStorage.GetWavPath(id);
                if (!File.Exists(wavPath)) continue;

                byte[] wav = SafeReadAll(wavPath);
                if (wav == null || wav.Length == 0) continue;

                HttpResult up = default;
                yield return Send("POST", StorageUrl(id + ".wav"), wav, "audio/wav", null, UpsertHeader, r => up = r);

                if (IsSuccess(up))
                {
                    AddUnique(_state.WavDoneIds, id);
                    SaveState();
                }
                else
                {
                    LogFailure("audio", id, up);
                }
            }
        }

        // ================= BAJADA =================

        private IEnumerator DownloadNewAvatars()
        {
            bool anyNew = false;

            for (int page = 0; page < 20; page++)
            {
                if (_networkDown) break;

                string query = "avatars?select=id,student_code,avatar_name,profile_json,has_image,created_at" +
                               "&order=created_at.asc&limit=" + PageSize;
                // gte (no gt): no se pierde una fila con la misma hora; las repetidas se ignoran por id.
                if (!string.IsNullOrEmpty(_state.Cursor))
                    query += "&created_at=gte." + UnityWebRequest.EscapeURL(_state.Cursor);

                HttpResult res = default;
                yield return Send("GET", RestUrl(query), null, null, null, null, r => res = r);
                if (!IsSuccess(res))
                {
                    if (res.Code > 0) LogFailure("lista de avatares", "-", res);
                    break;
                }

                List<AvatarCloudRow> rows = ParseRows(res.Body);
                if (rows == null) break;

                foreach (AvatarCloudRow row in rows)
                {
                    if (ApplyRow(row)) anyNew = true;
                    if (!string.IsNullOrEmpty(row.created_at)) _state.Cursor = row.created_at;
                }
                SaveState();

                if (rows.Count < PageSize) break;
            }

            if (anyNew) OnAvatarsDownloaded?.Invoke();
        }

        // True si se creo un avatar local nuevo.
        private bool ApplyRow(AvatarCloudRow row)
        {
            if (row == null || string.IsNullOrEmpty(row.id)) return false;
            if (_state.DuplicateIds.Contains(row.id)) return false;

            bool createdLocally = false;

            if (SafeLoadProfile(row.id) == null)
            {
                AvatarProfile profile = null;
                try { profile = JsonUtility.FromJson<AvatarProfile>(row.profile_json); }
                catch (Exception e) { Debug.LogWarning($"[AvatarCloudSync] Perfil ilegible ({row.id}): {e.Message}"); }

                if (profile == null) return false;

                profile.Id = row.id;
                try
                {
                    AvatarJsonStorage.Save(profile);
                    createdLocally = true;
                    // Es de otro PC: su audio lo sube el PC de origen, aqui no.
                    AddUnique(_state.WavDoneIds, row.id);
                    Debug.Log($"[AvatarCloudSync] Bajado: {profile.AvatarName} ({profile.StudentCode}).");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AvatarCloudSync] No se pudo guardar el avatar bajado ({row.id}): {e.Message}");
                    return false;
                }
            }

            AddUnique(_state.SyncedIds, row.id);
            if (row.has_image) AddUnique(_state.ImageInCloudIds, row.id);
            return createdLocally;
        }

        private static List<AvatarCloudRow> ParseRows(string body)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<AvatarCloudRows>("{\"rows\":" + body + "}");
                return wrapper != null && wrapper.rows != null ? wrapper.rows : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] Respuesta ilegible de la nube: {e.Message}");
                return null;
            }
        }

        // ================= DUPLICADOS =================

        private void MarkDuplicate(string id)
        {
            AddUnique(_state.DuplicateIds, id);

            string folder = Path.Combine(Application.persistentDataPath, "avatars");
            string aside = Path.Combine(folder, "duplicados");
            try
            {
                Directory.CreateDirectory(aside);
                foreach (string name in new[] { id + ".json", id + ".png", id + "_full.png", id + ".wav" })
                {
                    string from = Path.Combine(folder, name);
                    if (!File.Exists(from)) continue;
                    string to = Path.Combine(aside, name);
                    if (File.Exists(to)) File.Delete(to);
                    File.Move(from, to);
                }

                AvatarRoster roster = AvatarJsonStorage.LoadRoster();
                roster.Entries.RemoveAll(e => e.Id == id);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "roster.json"),
                    JsonUtility.ToJson(roster, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] No se pudo apartar el avatar duplicado: {e.Message}");
            }

            SaveState();
            Debug.LogWarning($"[AvatarCloudSync] El codigo del avatar {id} ya lo habia usado otro PC: se aparto a avatars/duplicados.");
        }

        // ================= HTTP =================

        private IEnumerator Send(string method, string url, byte[] body, string contentType,
            string prefer, string[] extraHeaders, Action<HttpResult> done)
        {
            using var request = new UnityWebRequest(url, method);
            if (body != null) request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 20;
            request.SetRequestHeader("apikey", _config.supabaseAnonKey);
            request.SetRequestHeader("Authorization", "Bearer " + _config.supabaseAnonKey);
            if (!string.IsNullOrEmpty(contentType)) request.SetRequestHeader("Content-Type", contentType);
            if (!string.IsNullOrEmpty(prefer)) request.SetRequestHeader("Prefer", prefer);
            if (extraHeaders != null)
                for (int i = 0; i + 1 < extraHeaders.Length; i += 2)
                    request.SetRequestHeader(extraHeaders[i], extraHeaders[i + 1]);

            yield return request.SendWebRequest();

            var result = new HttpResult
            {
                Code = request.responseCode,
                Body = request.downloadHandler != null ? request.downloadHandler.text : "",
                Error = request.error
            };
            if (result.Code == 0) _networkDown = true;
            done(result);
        }

        private string RestUrl(string pathAndQuery) =>
            $"{_config.supabaseUrl}/rest/v1/{pathAndQuery}";

        private string StorageUrl(string objectName) =>
            $"{_config.supabaseUrl}/storage/v1/object/{_config.bucketName}/{objectName}";

        private static bool IsSuccess(HttpResult r) => r.Code >= 200 && r.Code < 300;

        private void LogFailure(string what, string id, HttpResult r)
        {
            string detail = r.Code == 0 ? "sin conexion" : $"HTTP {r.Code} {Trim(r.Body)}";
            Debug.LogWarning($"[AvatarCloudSync] No se pudo subir/leer {what} ({id}): {detail}");
        }

        private static string Trim(string s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 200 ? s.Substring(0, 200) : s);

        // Solo cuenta los fallos con respuesta del servidor (4xx/5xx), no los cortes de red.
        private void NoteFailure(string id, HttpResult r)
        {
            if (r.Code <= 0) return;
            _failedAttempts[id] = (_failedAttempts.TryGetValue(id, out int n) ? n : 0) + 1;
        }

        private bool TooManyFailures(string id) =>
            _failedAttempts.TryGetValue(id, out int n) && n >= MaxFailedAttemptsPerSession;

        // ================= ESTADO LOCAL =================

        private static string StatePath => Path.Combine(Application.persistentDataPath, "cloud_sync_state.json");

        private void LoadState()
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    var loaded = JsonUtility.FromJson<AvatarCloudState>(File.ReadAllText(StatePath));
                    if (loaded != null) _state = loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] No se pudo leer el estado, se empieza de cero: {e.Message}");
            }

            _state.SyncedIds ??= new List<string>();
            _state.WavDoneIds ??= new List<string>();
            _state.ImageInCloudIds ??= new List<string>();
            _state.DuplicateIds ??= new List<string>();
            _state.Cursor ??= "";
        }

        private void SaveState()
        {
            try { File.WriteAllText(StatePath, JsonUtility.ToJson(_state, true)); }
            catch (Exception e) { Debug.LogWarning($"[AvatarCloudSync] No se pudo guardar el estado: {e.Message}"); }
        }

        private void MarkSynced(string id, bool hasImage)
        {
            AddUnique(_state.SyncedIds, id);
            if (hasImage) AddUnique(_state.ImageInCloudIds, id);
            SaveState();
        }

        private static void AddUnique(List<string> list, string id)
        {
            if (!list.Contains(id)) list.Add(id);
        }

        private static AvatarProfile SafeLoadProfile(string id)
        {
            try { return AvatarJsonStorage.Load(id); }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] JSON ilegible ({id}): {e.Message}");
                return null;
            }
        }

        private static byte[] SafeReadAll(string path)
        {
            try { return File.ReadAllBytes(path); }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarCloudSync] No se pudo leer {path}: {e.Message}");
                return null;
            }
        }
    }
}
