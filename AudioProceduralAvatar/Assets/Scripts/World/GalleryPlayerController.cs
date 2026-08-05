using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Movimiento del visitante dentro del diorama multiplano:
    /// - X: Movimiento horizontal dentro del plano.
    /// - Z: Cambio discreto entre planos con transición suave.
    /// - Cámara con seguimiento suavizado y rotación interpolada.
    /// </summary>
    public class GalleryPlayerController : MonoBehaviour
    {
        [Header("Movimiento dentro del plano (X)")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private AvatarGalleryManager galleryManager;

        [Header("Transición entre planos")]
        [Tooltip("Duración de la transición entre planos.")]
        [SerializeField] private float planeTransitionDuration = 0.45f;

        [Header("Cámara")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -6f);

        [Tooltip("Mientras mayor sea, más 'pesada' se siente la cámara.")]
        [SerializeField] private float cameraSmoothTime = 0.3f;

        [Tooltip("Velocidad con la que la cámara gira para mirar al jugador.")]
        [SerializeField] private float cameraRotationSpeed = 6f;

        [Tooltip("Posición inicial de la cámara.")]
        [SerializeField] private Vector3 initialCameraPosition = Vector3.zero;

        [SerializeField] private bool useCustomInitialCameraPosition = false;

        private int _currentPlaneIndex;

        private float _startZ;
        private float _targetZ;
        private float _transitionTime;
        private bool _isTransitioning;

        private Vector3 _cameraVelocity;

        private void Start()
        {
            _currentPlaneIndex = 0;

            _targetZ = GetPlaneZ(_currentPlaneIndex);

            Vector3 pos = transform.position;
            pos.z = _targetZ;
            transform.position = pos;

            InitializeCameraPosition();
        }

        private void InitializeCameraPosition()
        {
            if (cameraTransform == null)
                return;

            cameraTransform.position = useCustomInitialCameraPosition
                ? initialCameraPosition
                : transform.position + cameraOffset;

            cameraTransform.LookAt(transform.position + Vector3.up * 1.2f);
        }

        private void Update()
        {
            HandleHorizontalMovement();
            HandlePlaneSwitch();
            HandleZTransition();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
                return;

            Vector3 targetPosition = transform.position + cameraOffset;

            cameraTransform.position = Vector3.SmoothDamp(
                cameraTransform.position,
                targetPosition,
                ref _cameraVelocity,
                cameraSmoothTime);

            Quaternion targetRotation = Quaternion.LookRotation(
                (transform.position + Vector3.up * 1.2f) - cameraTransform.position
            );

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                Time.deltaTime * cameraRotationSpeed);
        }

        private void HandleHorizontalMovement()
        {
            float horizontal = ReadAxis(true);

            if (Mathf.Approximately(horizontal, 0f))
                return;

            Vector3 pos = transform.position;
            pos.x += horizontal * moveSpeed * Time.deltaTime;

            if (galleryManager != null)
            {
                var (min, max) = galleryManager.GetPlaneXBounds(_currentPlaneIndex);
                pos.x = Mathf.Clamp(pos.x, min, max);
            }

            transform.position = pos;
        }

        private void HandlePlaneSwitch()
        {
            if (KeyDownThisFrame(true))
            {
                TryChangePlane(+1);
            }
            else if (KeyDownThisFrame(false))
            {
                TryChangePlane(-1);
            }
        }

        private void TryChangePlane(int direction)
        {
            if (galleryManager == null)
                return;

            if (_isTransitioning)
                return;

            int targetIndex = _currentPlaneIndex + direction;

            if (targetIndex < 0 || targetIndex >= galleryManager.Planes.Count)
                return;

            _currentPlaneIndex = targetIndex;

            _startZ = transform.position.z;
            _targetZ = GetPlaneZ(_currentPlaneIndex);

            _transitionTime = 0f;
            _isTransitioning = true;
        }

        private void HandleZTransition()
        {
            if (!_isTransitioning)
                return;

            _transitionTime += Time.deltaTime;

            float t = Mathf.Clamp01(_transitionTime / planeTransitionDuration);

            // Ease In / Ease Out
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = transform.position;
            pos.z = Mathf.Lerp(_startZ, _targetZ, t);
            transform.position = pos;

            if (t >= 1f)
            {
                pos.z = _targetZ;
                transform.position = pos;
                _isTransitioning = false;
            }
        }

        private float GetPlaneZ(int planeIndex)
        {
            if (galleryManager == null ||
                planeIndex < 0 ||
                planeIndex >= galleryManager.Planes.Count)
                return transform.position.z;

            return galleryManager.Planes[planeIndex].ZPosition;
        }

        private float ReadAxis(bool negativeLeft)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
                return 0f;

            float value = 0f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                value += 1f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                value -= 1f;

            return value;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        private bool KeyDownThisFrame(bool forward)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
                return false;

            return forward
                ? Keyboard.current.upArrowKey.wasPressedThisFrame
                : Keyboard.current.downArrowKey.wasPressedThisFrame;
#else
            return forward
                ? Input.GetKeyDown(KeyCode.UpArrow)
                : Input.GetKeyDown(KeyCode.DownArrow);
#endif
        }
    }
}