using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

// Panel de detalle del álbum: overlay con blur, avatar riggeado (mismo
// AvatarSkeletonBuilder que el mundo abierto, en Idle) y reproducción del
// leitmotiv ya exportado a WAV -- se puede repetir sin límite; solo se
// genera si es la primera vez que se abre un avatar viejo sin WAV.
public class AvatarDetailPanel : MonoBehaviour
{
    [Header("Overlay")]
    public GameObject panelRoot; // todo el overlay (blur + contenido)
    public Button closeButton;

    [Header("Rig")]
    public AvatarSkeletonBuilder rigPrefab;
    public Transform rigContainer;

    [Header("Vista previa (cámara dedicada + RenderTexture)")]
    [Tooltip("Layer exclusivo para el rig del panel, para que la cámara de vista previa no renderice nada más (el libro, el resto de la escena, etc).")]
    public string previewLayerName = "AvatarPreview";

    [Header("Solo para el fallback (avatar viejo sin WAV aún)")]
    public AvatarOptionsDatabase optionsDatabase;
    public LeitmotivGenerator leitmotivGenerator;
    public SimpleSynthRenderer offlineRendererForExport;

    [Header("Audio")]
    public AudioSource audioSource;
    public Button playButton;
    public float closeFadeSeconds = 1f;

    private AvatarSkeletonBuilder _rigInstance;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (playButton != null) playButton.onClick.AddListener(PlayLeitmotiv);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open(AvatarProfile profile)
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (playButton != null) playButton.interactable = false;

        BuildRig(profile);
        StartCoroutine(LoadAndPrepareAudio(profile));
    }

    public void Close()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutAndHide());
    }

    // ================= RIG =================

    private void BuildRig(AvatarProfile profile)
    {
        if (_rigInstance != null)
            Destroy(_rigInstance.gameObject);

        if (rigPrefab == null || rigContainer == null) return;

        // El rig trae su propio Animator en loop Idle por defecto (mismo
        // prefab que el mundo abierto) -- no hace falta dispararlo aquí.
        _rigInstance = Instantiate(rigPrefab, rigContainer);
        SetLayerRecursively(_rigInstance.gameObject, LayerMask.NameToLayer(previewLayerName));

        foreach (var layer in profile.Layers)
        {
            var pieces = optionsDatabase.Resolve(layer.LayerName, layer.SpriteIndex);
            _rigInstance.Apply(layer.LayerName, pieces);
        }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // ================= AUDIO =================

    private IEnumerator LoadAndPrepareAudio(AvatarProfile profile)
    {
        string path = AvatarAudioStorage.GetWavPath(profile.Id);

        if (!AvatarAudioStorage.Exists(profile.Id))
        {
            // Fallback perezoso: avatar viejo sin WAV -- se genera una sola
            // vez aquí y queda guardado para la próxima vez.
            if (leitmotivGenerator != null && offlineRendererForExport != null)
            {
                var data = leitmotivGenerator.Generate(profile);
                AvatarAudioStorage.ExportAndLink(profile, data, offlineRendererForExport);
            }
            else
            {
                Debug.LogWarning("[AvatarDetailPanel] No se encontró WAV y faltan referencias para generarlo (leitmotivGenerator / offlineRendererForExport).");
                yield break;
            }
        }

        using var request = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[AvatarDetailPanel] No se pudo cargar el WAV: {request.error}");
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(request);
        audioSource.clip = clip;
        audioSource.volume = 1f;

        if (playButton != null) playButton.interactable = true;
    }

    private void PlayLeitmotiv()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.Stop();
        audioSource.volume = 1f;
        audioSource.Play();
    }

    // ================= CIERRE =================

    private IEnumerator FadeOutAndHide()
    {
        float startVolume = audioSource != null ? audioSource.volume : 0f;
        float t = 0f;

        while (t < closeFadeSeconds)
        {
            t += Time.deltaTime;
            if (audioSource != null)
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t / closeFadeSeconds);
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = startVolume;
        }

        if (_rigInstance != null)
        {
            Destroy(_rigInstance.gameObject);
            _rigInstance = null;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
