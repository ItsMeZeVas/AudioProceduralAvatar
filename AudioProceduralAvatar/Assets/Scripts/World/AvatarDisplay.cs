using System;
using UnityEngine;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Vive en cada avatar instanciado dentro de la galería. Hoy usa una
    /// representación placeholder (color plano según AccentColor) — cuando
    /// Diseño tenga los sprites/modelos finales, solo hay que reemplazar
    /// ApplyPlaceholderVisual() por la lógica real; nada más de este script cambia.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Collider))]
    public class AvatarDisplay : MonoBehaviour
    {
        public AvatarInstance Data { get; private set; }

        /// <summary>Se dispara cuando el jugador hace clic/toca este avatar.</summary>
        public event Action<AvatarInstance> Selected;

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void Initialize(AvatarInstance data)
        {
            Data = data;
            ApplyPlaceholderVisual();
            gameObject.name = $"Avatar_{data.Attributes.AvatarName}";
        }

        // TODO (Diseño/Desarrollo, cuando haya arte): sustituir por asignación
        // de sprite/modelo real según Data.Attributes (Clothing, Accessory, etc.)
        private void ApplyPlaceholderVisual()
        {
            if (_renderer != null)
            {
                // instance para no modificar el material compartido del prefab
                _renderer.material = new Material(_renderer.material)
                {
                    color = Data.Attributes.AccentColor
                };
            }
        }

        // Placeholder de input: funciona con el sistema de físicas normal de
        // Unity (requiere Collider + una Camera con Physics Raycaster, o
        // simplemente un collider 3D y clic del mouse). Se reemplaza cuando
        // se construya el sistema de selección definitivo.
        private void OnMouseDown()
        {
            Selected?.Invoke(Data);
        }
    }
}
