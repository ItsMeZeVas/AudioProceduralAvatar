using NUnit.Framework.Constraints;
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;
#endif

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Movimiento del visitante dentro del diorama multiplano:
    /// - Eje X: side-scroller libre dentro del plano actual (A/D o flechas izq/der),
    ///   acotado a los avatares presentesp en ese plano.
    /// - Eje Z: salto discreto entre planos con tecla dedicada (flechas arriba/abajo),
    ///   con una transición corta (no instantánea, no continua).
    /// La cámara sigue al jugador en ambos ejes.
    /// </summary>
    public class GalleryPlayerController : MonoBehaviour
    {
        [Header("Movimiento dentro del plano (X)")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private AvatarGalleryManager galleryManager;

        [Header("Salto entre planos (Z)")]   
        [SerializeField] private float planeTransitionSpeed = 10f;
        //para cambio automatico en segundos
        [SerializeField] private float autoSwitchTime = 10f;
        [SerializeField] private float autoDelayTime = 5f;

        [Header("Cámara")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -6f);
        [SerializeField] private float cameraSmoothTime = 0.15f;

        private int _currentPlaneIndex;
        private float _targetZ;
        private Vector3 _cameraVelocity;
        private bool _switchPlaneKeyHeld;

        private float currentTime = 0f;
        private float delayTime = 0f;
        private bool autoDir = true; //true 1, false -1
        private bool autoMode = true;
        private bool switchPlane = false;

        bool edgeTransition = false;
        bool transitionFinished = false;
        float planeSpacingZ = 2.5f;

        private void Start()
        {
            _currentPlaneIndex = 0;
            _targetZ = GetPlaneZ(_currentPlaneIndex);
            var pos = transform.position;
            pos.z = _targetZ;
            transform.position = pos;
            currentTime = 0f;
        }
          
        private void Update()
        {
            if (autoMode)
                UpdateTimer();
            else
                UpdateDelay();

            HandleHorizontalMovement();
            HandlePlaneSwitch();

            if(switchPlane)
            {
                int aux = galleryManager.Planes.Count;
                foreach (var plane in galleryManager.Planes)
                {
                    if (plane.Index == 0 && plane.TargetIndex == (galleryManager.Planes.Count - 1) && edgeTransition == false)
                    {
                        edgeTransition = plane.HandleZExit(planeSpacingZ, planeTransitionSpeed);
                    }
                    else if (plane.Index == (galleryManager.Planes.Count - 1) && plane.TargetIndex == 0 && edgeTransition == false)
                    {
                        edgeTransition = plane.HandleZExit(-planeSpacingZ, planeTransitionSpeed);
                    }
                    else
                    {
                        transitionFinished = plane.HandleZTransition(planeTransitionSpeed);
                        if (transitionFinished)
                            aux--;
                    }
                }

                if (aux == 0)
                {
                    galleryManager.UpdatePlaneList();
                    switchPlane = false;
                    edgeTransition = false;
                    transitionFinished = false;
                }
            }
           

            //HandleZTransition();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector3 targetPosition = transform.position + cameraOffset;
            cameraTransform.position = Vector3.SmoothDamp(
                cameraTransform.position, targetPosition, ref _cameraVelocity, cameraSmoothTime);
            cameraTransform.LookAt(transform.position + Vector3.up * 1.2f);
        }

        private void HandleHorizontalMovement()
        {
            float horizontal = ReadAxis(negativeLeft: true);
            if (Mathf.Approximately(horizontal, 0f)) return;

            TurnOffAuto();
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

            if (forwardPressed)
            {
                TurnOffAuto();
                TryChangePlanes(-1);
            }
            else if (backwardPressed)
            {
                TurnOffAuto();
                TryChangePlanes(1);
            }
            else if (autoMode && currentTime >= autoSwitchTime) AutoSwitchPlanes();
        }

        private void TryChangePlane(int direction)
        {
            if (galleryManager == null) return;

            int targetIndex = _currentPlaneIndex + direction;
            if (targetIndex < 0 || targetIndex >= galleryManager.Planes.Count)
                return; // no hay plano en esa dirección todavía

            _currentPlaneIndex = targetIndex;
            _targetZ = GetPlaneZ(_currentPlaneIndex);
            currentTime = 0; 
        }

        private void TryChangePlanes(int direction)
        {
            if (galleryManager == null) return;

            foreach (var plane in galleryManager.Planes)
            {
                int targetIndex = plane.Index + direction;

                if (plane.Index == 0 && galleryManager.Planes.Count > 1 && direction == -1)
                {
                    targetIndex = galleryManager.Planes.Count - 1;
                }
                else if (plane.Index == (galleryManager.Planes.Count-1) && galleryManager.Planes.Count > 1 && direction == 1)
                {
                    targetIndex = 0;
                }

                plane.TargetZ = GetPlaneZ(targetIndex);
                plane.TargetIndex = targetIndex;
            }

            currentTime = 0;
            switchPlane = true;
        }


        private void HandleZTransition()
        {
            currentTime = 0;
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
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) value -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) value += 1f;
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

        //Cambio automatico entre planos z

        private void UpdateTimer()
        {
            currentTime += Time.deltaTime;
        }
        private void AutoSwitchPlane()
        {
            if (_currentPlaneIndex == 0 || _currentPlaneIndex == galleryManager.Planes.Count-1)
            {
                if(_currentPlaneIndex == 0) autoDir = true;
                else autoDir = false;
            }

            TryChangePlanes(autoDir == true ? 1 : -1);
            
        }
        private void AutoSwitchPlanes()
        {
            TryChangePlanes(-1);
        }

        private void TurnOffAuto()
        {
            autoMode = false;
            delayTime = 0;
        }
        private void UpdateDelay()
        {
            delayTime += Time.deltaTime;
            if (delayTime >= autoDelayTime)
            {
                autoMode = true;
            }
        }

    }
}
