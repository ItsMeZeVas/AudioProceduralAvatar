using UnityEngine;
using UnityEngine.SceneManagement;

public class DevAlbumSceneSwitcher : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del álbum, tal como aparece en File > Build Settings > Scenes in Build.")]
    public string albumSceneName = "Album";

    public KeyCode openKey = KeyCode.L;
    public KeyCode closeKey = KeyCode.Escape;

    private static DevAlbumSceneSwitcher _instance;
    private string _previousSceneName;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // DontDestroyOnLoad solo funciona con objetos RAÍZ de la jerarquía.
        // Si este script está en un hijo de otro objeto, Unity lo destruye
        // al cambiar de escena y por eso no se puede volver.
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (Input.GetKeyDown(openKey) && activeScene != albumSceneName)
        {
            _previousSceneName = activeScene;
            Debug.Log($"[DevAlbumSceneSwitcher] Abriendo '{albumSceneName}' desde '{activeScene}'");
            SceneManager.LoadScene(albumSceneName);
        }
        else if (Input.GetKeyDown(closeKey) && activeScene == albumSceneName)
        {
            // Si por alguna razón no hay escena previa guardada, vuelve a la primera del build.
            string target = string.IsNullOrEmpty(_previousSceneName)
                ? SceneUtility.GetScenePathByBuildIndex(0)
                : _previousSceneName;

            Debug.Log($"[DevAlbumSceneSwitcher] Volviendo a '{target}'");

            if (string.IsNullOrEmpty(_previousSceneName))
                SceneManager.LoadScene(0);
            else
                SceneManager.LoadScene(_previousSceneName);
        }
    }
}
