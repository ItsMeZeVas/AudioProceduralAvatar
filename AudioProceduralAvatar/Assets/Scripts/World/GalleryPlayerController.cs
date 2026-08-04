using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Movimiento del visitante dentro del diorama multiplano:
    /// - Eje X: side-scroller libre dentro del plano actual (A/D o flechas izq/der),
    ///   acotado a los avatares presentes en ese plano.
    /// - Eje Z: salto discreto entre planos con tecla dedicada (flechas arriba/abajo),
    ///   con una transición corta (no instantánea, no continua).
    /// La cámara sigue al jugador en X y Z, con posición inicial configurable.
    /// </summary>
    public class GalleryPlayerController : MonoBehaviour
    {
        [Header("Movimiento dentro del plano (X)")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private AvatarGalleryManager galleryManager;

        [Header("Salto entre planos (Z)")]
        [SerializeField] private float planeTransitionSpeed = 10f;

        [Header("Cámara")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -6f);
        [SerializeField] private float cameraSmoothTime = 0.15f;
        [Tooltip("Posición inicial de la cámara en el mundo. Solo se usa si 'useCustomInitialCameraPosition' está activo.")]
        [SerializeField] private Vector3 initialCameraPosition = Vector3.zero;
        [Tooltip("Si está activo, la cámara se coloca exactamente en 'initialCameraPosition' al arrancar (sin deslizamiento). Si está desactivado, arranca en jugador + cameraOffset.")]
        [SerializeField] private bool useCustomInitialCameraPosition = false;

        private int _currentPlaneIndex;
        private float _targetZ;
        private Vector3 _cameraVelocity;
        private bool _switchPlaneKeyHeld;

        private void Start()
        {
            _currentPlaneIndex = 0;
            _targetZ = GetPlaneZ(_currentPlaneIndex);
            var pos = transform.position;
            pos.z = _targetZ;
            transform.position = pos;

            InitializeCameraPosition();
        }

        private void InitializeCameraPosition()
        {
            if (cameraTransform == null) return;

            cameraTransform.position = useCustomInitialCameraPosition
                ? initialCameraPosition
                : transform.position + cameraOffset;
        }

        private void Update()
        {
            HandleHorizontalMovement();
            HandlePlaneSwitch();
            HandleZTransition();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            // Seguimiento explícito en X, Y y Z (el offset se aplica en los 3 ejes,
            // así que al cambiar de plano -eje Z- la cámara también se desplaza).
            Vector3 targetPosition = new Vector3(
                transform.position.x + cameraOffset.x,
                transform.position.y + cameraOffset.y,
                transform.position.z + cameraOffset.z
            );

            cameraTransform.position = Vector3.SmoothDamp(
                cameraTransform.position, targetPosition, ref _cameraVelocity, cameraSmoothTime);
            cameraTransform.LookAt(transform.position + Vector3.up * 1.2f);
        }

        private void HandleHorizontalMovement()
        {
            float horizontal = ReadAxis(negativeLeft: true);
            if (Mathf.Approximately(horizontal, 0f)) return;

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
            bool forwardPressed = KeyDownThisFrame(forward: true);
            bool backwardPressed = KeyDownThisFrame(forward: false);

            if (forwardPressed) TryChangePlane(+1);
            else if (backwardPressed) TryChangePlane(-1);
        }

        private void TryChangePlane(int direction)
        {
            if (galleryManager == null) return;

            int targetIndex = _currentPlaneIndex + direction;
            if (targetIndex < 0 || targetIndex >= galleryManager.Planes.Count)
                return; // no hay plano en esa dirección todavía

            _currentPlaneIndex = targetIndex;
            _targetZ = GetPlaneZ(_currentPlaneIndex);
        }

        private void HandleZTransition()
        {
            Vector3 pos = transform.position;
            if (Mathf.Approximately(pos.z, _targetZ)) return;

            pos.z = Mathf.MoveTowards(pos.z, _targetZ, planeTransitionSpeed * Time.deltaTime);
            transform.position = pos;
        }

        private float GetPlaneZ(int planeIndex)
        {
            if (galleryManager == null || planeIndex < 0 || planeIndex >= galleryManager.Planes.Count)
                return transform.position.z;
            return galleryManager.Planes[planeIndex].ZPosition;
        }

        private float ReadAxis(bool negativeLeft)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return 0f;
            float value = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.leftArrowKey.isPressed) value -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.rightArrowKey.isPressed) value += 1f;
            return value;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        // Flechas arriba/abajo = cambiar de plano (dedicado, no continuo).
        private bool KeyDownThisFrame(bool forward)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
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