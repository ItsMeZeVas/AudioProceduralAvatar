using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AudioProceduralAvatar.World;

namespace AudioProceduralAvatar.UI
{
    /// <summary>
    /// Ficha pop-up EN EL MUNDO (Canvas en World Space) que aparece encima
    /// del avatar seleccionado y se oculta sola después de unos segundos.
    /// Este script vive directamente en el GameObject del Canvas, y lo mueve
    /// en coordenadas del mundo — sin conversiones de pantalla, sin
    /// depender del modo de renderizado del Canvas.
    ///
    /// CÓMO ARMAR LA UI (desde cero):
    /// 1. Hierarchy -> UI -> Canvas. Selecciónalo, y en el componente
    ///    "Canvas" cambia Render Mode a "World Space".
    /// 2. En su RectTransform (mismo Canvas): Width = 400, Height = 200.
    ///    En su Transform: Scale X/Y/Z = 0.01 (un Canvas World Space se
    ///    mide en píxeles, por eso hay que achicarlo para que se vea del
    ///    tamaño correcto dentro del mundo 3D).
    /// 3. Dentro del Canvas: UI -> Image (fondo de la ficha, ponle un color
    ///    sólido para que se lea bien). Dentro de esa Image: otra
    ///    UI -> Image (para la foto del avatar) y un
    ///    UI -> Text - TextMeshPro (para el nombre).
    /// 4. Agrega ESTE script directo al GameObject del Canvas (no a un hijo).
    /// 5. Asigna en el Inspector: Panel (arrastra el Image de fondo del
    ///    paso 3), Avatar Image, Name Text.
    /// 6. Desactiva el GameObject del Canvas en el editor — este script lo
    ///    activa solo cuando se selecciona un avatar.
    /// </summary>
    public class AvatarInfoCardController : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nameText;

        [Header("Comportamiento")]
        [SerializeField] private float autoHideSeconds = 3f;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private Camera worldCamera;
        [Tooltip("Si está activo, la ficha siempre gira para mirar hacia la cámara (recomendado, para que el texto no se vea de lado).")]
        [SerializeField] private bool faceCamera = true;

        private Transform _followTarget;
        private Coroutine _hideRoutine;

        private void Start()
        {
            if (AvatarGalleryManager.Instance != null)
            {
                AvatarGalleryManager.Instance.AvatarSelected += HandleAvatarSelected;
            }
            else
            {
                Debug.LogWarning("AvatarInfoCardController: no se encontró AvatarGalleryManager.Instance en la escena.");
            }

            if (worldCamera == null) worldCamera = Camera.main;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (AvatarGalleryManager.Instance != null)
                AvatarGalleryManager.Instance.AvatarSelected -= HandleAvatarSelected;
        }

        private void HandleAvatarSelected(AvatarDisplay display)
        {
            if (display == null || display.Data == null) return;
            if (panel == null)
            {
                Debug.LogWarning("AvatarInfoCardController: falta asignar 'Panel' en el Inspector.");
                return;
            }

            _followTarget = display.transform;

            if (nameText != null)
                nameText.text = display.Data.Profile.AvatarName;
            else
                Debug.LogWarning("AvatarInfoCardController: falta asignar 'Name Text' en el Inspector.");

            if (avatarImage != null)
            {
                avatarImage.sprite = display.Data.CapturedImage;
                avatarImage.enabled = display.Data.CapturedImage != null;
            }
            else
            {
                Debug.LogWarning("AvatarInfoCardController: falta asignar 'Avatar Image' en el Inspector.");
            }

            panel.SetActive(true);
            UpdatePosition(); // posicionar de inmediato, no esperar al próximo frame

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(autoHideSeconds);
            _followTarget = null;
            if (panel != null) panel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_followTarget == null) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            transform.position = _followTarget.position + worldOffset;

            if (faceCamera && worldCamera != null)
                transform.rotation = worldCamera.transform.rotation;
        }
    }
}
