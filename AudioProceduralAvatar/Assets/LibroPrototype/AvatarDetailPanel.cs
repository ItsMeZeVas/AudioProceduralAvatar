using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

// Panel de detalle del álbum: overlay con blur, avatar riggeado (mismo
// AvatarSkeletonBuilder que el mundo abierto, en Idle) y reproducción del
// leitmotiv ya exportado a WAV -- se puede repetir sin límite; solo se
// genera si es la primera vez que se abre un avatar viejo sin WAV.
//
// Fallback: si no hay rigPrefab/rigContainer asignado (rig 3D todavía sin
// terminar), se muestra en su lugar un AvatarThumbnailSlot -- el mismo
// componente plano/2D que usan las miniaturas del libro -- para no dejar el
// panel vacío mientras el rig no esté listo.
public class AvatarDetailPanel : MonoBehaviour
{
    [Header("Overlay")]
    [Tooltip("Contenido visible del panel (blur + preview + botones). Se oculta AL INSTANTE al cerrar. El GameObject raíz que tiene este script debe quedar SIEMPRE activo (no lo pongas aquí), para que el fade de audio en segundo plano pueda terminar aunque el panel ya esté oculto.")]
    public GameObject visualContent;
    public Button closeButton;

    [Header("Datos del avatar")]
    public TMP_Text avatarNameText;
    public TMP_Text studentCodeText;

    [Header("Rig")]
    public AvatarSkeletonBuilder rigPrefab;
    public Transform rigContainer;

    [Header("Vista previa (cámara dedicada + RenderTexture)")]
    [Tooltip("Layer exclusivo para el rig del panel, para que la cámara de vista previa no renderice nada más (el libro, el resto de la escena, etc).")]
    public string previewLayerName = "AvatarPreview";

    [Header("Fallback plano (si no hay rig 3D asignado todavía)")]
    [Tooltip("Se activa y se usa en vez del rig cuando rigPrefab o rigContainer están sin asignar. Mismo componente que usan las miniaturas del álbum.")]
    public AvatarThumbnailSlot fallbackPortrait;
    public AvatarPortraitDatabase portraitDatabase;

    [Header("Solo para el fallback (avatar viejo sin WAV aún)")]
    public AvatarOptionsDatabase optionsDatabase;
    public LeitmotivGenerator leitmotivGenerator;
    public SimpleSynthRenderer offlineRendererForExport;

    [Header("Audio")]
    public AudioSource audioSource;
    public Button playButton;
    public float closeFadeSeconds = 1f;

    [Header("Descarga por QR")]
    public AvatarUploadConfig uploadConfig;
    public Button qrButton;
    public RawImage qrImage;
    public GameObject qrLoadingIndicator;
    public TMP_Text qrStatusText;

    private AvatarSkeletonBuilder _rigInstance;
    private Coroutine _fadeRoutine;
    private AvatarProfile _currentProfile;
    private Coroutine _qrRoutine;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (playButton != null) playButton.onClick.AddListener(PlayLeitmotiv);
        if (qrButton != null) qrButton.onClick.AddListener(OnQrButtonClicked);
        if (visualContent != null) visualContent.SetActive(false);
        if (fallbackPortrait != null) fallbackPortrait.gameObject.SetActive(false);
        ResetQrUi();
    }

    public void Open(AvatarProfile profile)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 1f;
        }

        if (visualContent != null) visualContent.SetActive(true);
        if (playButton != null) playButton.interactable = false;

        _currentProfile = profile;
        ResetQrUi();

        if (avatarNameText != null) avatarNameText.text = profile.AvatarName;
        if (studentCodeText != null) studentCodeText.text = profile.StudentCode;

        BuildRig(profile);
        StartCoroutine(LoadAndPrepareAudio(profile));
    }

    public void Close()
    {
        if (visualContent != null) visualContent.SetActive(false);

        if (_qrRoutine != null)
        {
            StopCoroutine(_qrRoutine);
            _qrRoutine = null;
        }
        _currentProfile = null;

        if (_rigInstance != null)
        {
            Destroy(_rigInstance.gameObject);
            _rigInstance = null;
        }

        if (fallbackPortrait != null)
        {
            fallbackPortrait.Clear();
            fallbackPortrait.gameObject.SetActive(false);
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutAudio());
    }

    // ================= RIG =================

    private void BuildRig(AvatarProfile profile)
    {
        if (_rigInstance != null)
        {
            Destroy(_rigInstance.gameObject);
            _rigInstance = null;
        }

        bool canBuildRig = rigPrefab != null && rigContainer != null;

        if (rigContainer != null)
            rigContainer.gameObject.SetActive(canBuildRig);

        if (!canBuildRig)
        {
            // No hay rig 3D asignado todavía: fallback al avatar plano.
            if (fallbackPortrait != null && portraitDatabase != null)
            {
                fallbackPortrait.gameObject.SetActive(true);
                fallbackPortrait.SetAvatar(profile, portraitDatabase);
            }
            else
            {
                Debug.LogWarning(
                    "[AvatarDetailPanel] No hay rig3D asignado y falta " +
                    "fallbackPortrait/portraitDatabase para mostrar el " +
                    "avatar plano -- el panel va a quedar vacío."
                );
            }
            return;
        }

        if (fallbackPortrait != null)
        {
            fallbackPortrait.Clear();
            fallbackPortrait.gameObject.SetActive(false);
        }

        // El rig trae su propio Animator en loop Idle por defecto (mismo
        // prefab que el mundo abierto) -- no hace falta dispararlo aquí.
        _rigInstance = Instantiate(rigPrefab, rigContainer);
        SetLayerRecursively(_rigInstance.gameObject, LayerMask.NameToLayer(previewLayerName));

        foreach (var layer in profile.Layers)
        {
            var pieces = optionsDatabase.Resolve(layer.LayerName, layer.SpriteIndex);
            _rigInstance.Apply(layer.LayerName, pieces);
        }

        _rigInstance.ApplyColor("Hair", ReadColor(profile, "HairColor", Color.white));
        _rigInstance.ApplyColor("SubBarba", ReadColor(profile, "BeardColor", Color.white));
    }

    private static Color ReadColor(AvatarProfile profile, string namePrefix, Color fallback)
    {
        float r = profile.GetContinuousValue(namePrefix + "R", fallback.r);
        float g = profile.GetContinuousValue(namePrefix + "G", fallback.g);
        float b = profile.GetContinuousValue(namePrefix + "B", fallback.b);
        return new Color(r, g, b);
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

    private IEnumerator FadeOutAudio()
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
    }

    // ================= DESCARGA POR QR =================

    private void ResetQrUi()
    {
        if (qrImage != null)
        {
            qrImage.texture = null;
            qrImage.gameObject.SetActive(false);
        }
        if (qrLoadingIndicator != null) qrLoadingIndicator.SetActive(false);
        if (qrStatusText != null) qrStatusText.text = "";
        if (qrButton != null) qrButton.interactable = true;
    }

    private void OnQrButtonClicked()
    {
        if (_currentProfile == null) return;
        if (_qrRoutine != null) StopCoroutine(_qrRoutine);
        _qrRoutine = StartCoroutine(EnsureWavUrlAndShowQr(_currentProfile));
    }

    private IEnumerator EnsureWavUrlAndShowQr(AvatarProfile profile)
    {
        if (qrButton != null) qrButton.interactable = false;
        if (qrLoadingIndicator != null) qrLoadingIndicator.SetActive(true);
        if (qrStatusText != null) qrStatusText.text = "Preparando...";

        // Si ya se subió antes, nos ahorramos el upload.
        if (string.IsNullOrEmpty(profile.WavPublicUrl))
        {
            if (qrStatusText != null) qrStatusText.text = "Subiendo audio...";

            string wavPath = AvatarAudioStorage.GetWavPath(profile.Id);
            string uploadedUrl = null;
            string uploadError = null;

            yield return AvatarWavUploader.Upload(
                wavPath,
                profile.Id,
                uploadConfig,
                url => uploadedUrl = url,
                err => uploadError = err
            );

            if (!string.IsNullOrEmpty(uploadError))
            {
                Debug.LogWarning($"[AvatarDetailPanel] {uploadError}");
                if (qrStatusText != null) qrStatusText.text = "No se pudo subir el audio.";
                if (qrLoadingIndicator != null) qrLoadingIndicator.SetActive(false);
                if (qrButton != null) qrButton.interactable = true;
                yield break;
            }

            profile.WavPublicUrl = uploadedUrl;
            AvatarJsonStorage.Save(profile); // persiste la URL para no re-subir la próxima vez
        }

        if (qrStatusText != null) qrStatusText.text = "Generando QR...";

        string qrApiUrl =
            "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" +
            UnityWebRequest.EscapeURL(profile.WavPublicUrl);

        using var qrRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(qrApiUrl);
        yield return qrRequest.SendWebRequest();

        if (qrLoadingIndicator != null) qrLoadingIndicator.SetActive(false);
        if (qrButton != null) qrButton.interactable = true;

        if (qrRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[AvatarDetailPanel] No se pudo generar el QR: {qrRequest.error}");
            if (qrStatusText != null) qrStatusText.text = "No se pudo generar el QR.";
            yield break;
        }

        var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(qrRequest);
        if (qrImage != null)
        {
            qrImage.texture = texture;
            qrImage.gameObject.SetActive(true);
        }
        if (qrStatusText != null) qrStatusText.text = "";
    }
}
