using UnityEngine;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Lista de todos los SecretAvatarPreset disponibles. Se consulta
    /// desde AvatarCreationController antes de armar el perfil desde los
    /// sliders actuales.
    /// </summary>
    [CreateAssetMenu(fileName = "SecretPresetDatabase", menuName = "Avatar/Secret Preset Database")]
    public class SecretPresetDatabase : ScriptableObject
    {
        [Tooltip("Todos los presets secretos disponibles.")]
        public SecretAvatarPreset[] presets;

        /// <summary>
        /// Busca un preset cuyo secretCode coincida con el código ingresado
        /// (sin distinguir mayúsculas ni espacios). Devuelve null si no hay match.
        /// </summary>
        public SecretAvatarPreset Find(string enteredCode)
        {
            if (string.IsNullOrWhiteSpace(enteredCode) || presets == null)
                return null;

            string normalized = enteredCode.Trim().ToLowerInvariant();

            foreach (var preset in presets)
            {
                if (preset == null || string.IsNullOrWhiteSpace(preset.secretCode))
                    continue;

                if (preset.secretCode.Trim().ToLowerInvariant() == normalized)
                    return preset;
            }

            return null;
        }
    }
}
