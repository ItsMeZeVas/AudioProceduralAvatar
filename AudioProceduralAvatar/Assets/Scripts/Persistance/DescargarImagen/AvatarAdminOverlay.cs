using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AudioProceduralAvatar.Persistence
{
    // Panel de administracion que se abre con Ctrl + Shift + F12 en CUALQUIER escena
    // (no hace falta armar una escena ni arrastrar nada: se crea solo al iniciar).
    // Hoy: exportar el roster unificado, sincronizar ahora, abrir la carpeta de exports y
    // FORMATEAR el evento (borra nube + los 6 PCs; pide clave de administracion y confirmacion).
    // Archivo NUEVO. Usa IMGUI a proposito: cero setup en el editor.
    public class AvatarAdminOverlay : MonoBehaviour
    {
        private static AvatarAdminOverlay _instance;

        private bool _open;
        private bool _busy;
        private string _status = "";
        private string _lastFolder;
        private int _localCount;
        private string _password = "";
        private string _confirm = "";

        private const string ConfirmWord = "FORMATEAR";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("AvatarAdminOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AvatarAdminOverlay>();
        }

        private void Update()
        {
            if (!ComboPressedThisFrame()) return;

            _open = !_open;
            if (_open) RefreshCounts();
        }

        private static bool ComboPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            return kb.f12Key.wasPressedThisFrame && kb.ctrlKey.isPressed && kb.shiftKey.isPressed;
#else
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            return Input.GetKeyDown(KeyCode.F12) && ctrl && shift;
#endif
        }

        private void RefreshCounts()
        {
            try { _localCount = AvatarJsonStorage.GetAllAvatarIds().Length; }
            catch (Exception) { _localCount = -1; }
        }

        private void OnGUI()
        {
            if (!_open) return;

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            const float w = 600f, h = 640f;
            var area = new Rect((Screen.width / scale - w) / 2f, (Screen.height / scale - h) / 2f, w, h);

            GUI.Box(area, "Administración  (Ctrl+Shift+F12 para cerrar)");
            GUILayout.BeginArea(new Rect(area.x + 16f, area.y + 34f, w - 32f, h - 48f));

            string pc = AvatarCloudSync.Instance != null ? AvatarCloudSync.PcId : SystemInfo.deviceName;
            GUILayout.Label($"PC: {pc}   |   avatares guardados en este PC: {_localCount}");
            GUILayout.Label(AvatarCloudSync.Instance != null
                ? "Sincronización con la nube: activa.   Evento: " + ShortId(AvatarCloudSync.CurrentEventId)
                : "Sincronización con la nube: DESACTIVADA (falta AvatarUploadConfig en una carpeta Resources).");
            GUILayout.Space(8f);

            GUI.enabled = !_busy;
            if (GUILayout.Button("Exportar roster unificado (todos los PCs)", GUILayout.Height(40f)))
                StartCoroutine(DoExport());

            if (GUILayout.Button("Sincronizar ahora", GUILayout.Height(40f)))
            {
                AvatarCloudSync.RequestSyncNow();
                _status = AvatarCloudSync.Instance != null ? "Sincronizando…" : "La sincronización está desactivada.";
                RefreshCounts();
            }

            if (GUILayout.Button("Abrir carpeta de exportaciones", GUILayout.Height(40f)))
                OpenExportFolder();
            GUI.enabled = true;

            GUILayout.Space(14f);
            GUILayout.Label("FORMATEAR PARA EL EVENTO: borra TODOS los avatares de la nube y de los PCs. No se puede deshacer.");
            GUILayout.Label("Clave de administración:");
            _password = GUILayout.PasswordField(_password, '*');
            GUILayout.Label($"Escribe {ConfirmWord} para confirmar:");
            _confirm = GUILayout.TextField(_confirm);

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            GUI.enabled = !_busy && _confirm == ConfirmWord && !string.IsNullOrEmpty(_password);
            if (GUILayout.Button("FORMATEAR TODO (nube + todos los PCs)", GUILayout.Height(44f)))
                StartCoroutine(DoFormat());
            GUI.enabled = true;
            GUI.backgroundColor = previousBackground;

            GUILayout.Space(8f);
            GUILayout.Label(_status);

            GUILayout.EndArea();
        }

        private IEnumerator DoExport()
        {
            _busy = true;
            _status = "Exportando…";

            AvatarUploadConfig[] configs = Resources.LoadAll<AvatarUploadConfig>("");
            AvatarUploadConfig config = configs != null && configs.Length > 0 ? configs[0] : null;

            AvatarRosterExporter.Result result = null;
            yield return AvatarRosterExporter.Export(config, r => result = r);

            _status = result != null ? result.Message : "Error inesperado al exportar.";
            if (result != null && !string.IsNullOrEmpty(result.Folder)) _lastFolder = result.Folder;
            RefreshCounts();
            _busy = false;
        }

        private IEnumerator DoFormat()
        {
            _busy = true;
            _status = "Formateando…";

            AvatarCloudSync.FormatResult result = null;
            yield return AvatarCloudSync.FormatEvent(_password, r => result = r);

            _status = result != null ? result.Message : "Error inesperado al formatear.";
            if (result != null && result.Ok)
            {
                _password = "";
                _confirm = "";
            }
            RefreshCounts();
            _busy = false;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "(aún no conocido)";
            return id.Length > 8 ? id.Substring(0, 8) : id;
        }

        private void OpenExportFolder()
        {
            try
            {
                string folder = _lastFolder;
                if (string.IsNullOrEmpty(folder))
                    folder = Path.Combine(Application.persistentDataPath, "exports");
                Directory.CreateDirectory(folder);
                Application.OpenURL(new Uri(folder).AbsoluteUri);
            }
            catch (Exception e)
            {
                _status = "No se pudo abrir la carpeta: " + e.Message;
            }
        }
    }
}
