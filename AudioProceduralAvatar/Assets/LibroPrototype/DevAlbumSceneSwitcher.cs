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
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (Input.GetKeyDown(openKey) && activeScene != albumSceneName)
        {
            _previousSceneName = activeScene;
            SceneManager.LoadScene(albumSceneName);
        }
        else if (Input.GetKeyDown(closeKey) && activeScene == albumSceneName)
        {
            if (!string.IsNullOrEmpty(_previousSceneName))
                SceneManager.LoadScene(_previousSceneName);
        }
    }
}
