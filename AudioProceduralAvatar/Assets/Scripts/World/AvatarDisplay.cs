using System;
using UnityEngine;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Vive en cada avatar instanciado en la galería. Ahora que existe una
    /// captura real del avatar armado (AvatarCapture), este componente ya
    /// muestra esa imagen como sprite plano ("cartón") en vez del cubo
    /// placeholder anterior — coincide con el diorama 2.5D que definimos
    /// (sprite fijo, sin rotación hacia cámara).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider))]
    public class AvatarDisplay : MonoBehaviour
    {
        public AvatarInstance Data { get; private set; }

        /// <summary>Se dispara cuando el jugador hace clic/toca este avatar.</summary>
        public event Action<AvatarInstance> Selected;

        [Tooltip("Se usa si el avatar todavía no tiene imagen capturada (ej. AvatarCapture no está armado aún).")]
        [SerializeField] private Sprite fallbackSprite;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(AvatarInstance data)
        {
            Data = data;
            _spriteRenderer.sprite = data.CapturedImage != null ? data.CapturedImage : fallbackSprite;
            gameObject.name = $"Avatar_{data.Profile.AvatarName}";
        }

        // Placeholder de input: requiere Collider + una Camera con Physics
        // Raycaster, o simplemente clic del mouse sobre el collider 3D.
        private void OnMouseDown()
        {
            Selected?.Invoke(Data);
        }
    }
}
