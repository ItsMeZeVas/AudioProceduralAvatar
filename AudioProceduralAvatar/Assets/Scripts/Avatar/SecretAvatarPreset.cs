using UnityEngine;
using System.Collections.Generic;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Un avatar ya armado que se carga automáticamente cuando alguien
    /// ingresa un código secreto (en vez de un código estudiantil normal)
    /// en la pantalla de guardado.
    ///
    /// Las capas se definen con los MISMOS layerName y los MISMOS índices
    /// que usa AvatarLayerOptionSet / AvatarCreator, para que el resultado
    /// se pueda reconstruir igual que un avatar armado a mano.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSecretAvatarPreset", menuName = "Avatar/Secret Avatar Preset")]
    public class SecretAvatarPreset : ScriptableObject
    {
        [Tooltip("El código/cédula que activa este preset. Se compara sin distinguir mayúsculas ni espacios.")]
        public string secretCode;

        [Tooltip("Nombre fijo para este avatar. Si se deja vacío, se usa el nombre que la persona escriba en pantalla.")]
        public string fixedAvatarName = "";

        [Tooltip("Mismos layerName y SpriteIndex que usaría AvatarCreationController al construir el perfil normal.")]
        public List<LayerSelection> layers = new();

        [Tooltip("Opcional: atributos continuos del preset (ej. tono de piel).")]
        public List<ContinuousAttribute> continuousAttributes = new();

        /// <summary>
        /// Construye un AvatarProfile a partir de este preset. El nombre
        /// que escribió la persona se usa solo si el preset no trae uno fijo.
        /// </summary>
        public AvatarProfile BuildProfile(string enteredName, string enteredCode)
        {
            var profile = new AvatarProfile
            {
                Id = System.Guid.NewGuid().ToString(),
                AvatarName = string.IsNullOrWhiteSpace(fixedAvatarName)
                    ? enteredName
                    : fixedAvatarName,
                StudentCode = enteredCode
            };

            foreach (var layer in layers)
                profile.Layers.Add(layer);

            foreach (var attr in continuousAttributes)
                profile.ContinuousAttributes.Add(attr);

            return profile;
        }
    }
}
