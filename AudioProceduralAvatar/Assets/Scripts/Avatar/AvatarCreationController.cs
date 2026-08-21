using TMPro;
using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Pega los scripts de personalización que ya existen (AvatarCreator,
    /// AvatarData, AvatarCapture — sin namespace) con nuestro pipeline de
    /// leitmotiv, validación, y persistencia en JSON.
    ///
    /// CÓMO CONECTARLO:
    /// 1. Agrega este componente a cualquier GameObject de la escena de
    ///    personalización (ej. el mismo "Managers").
    /// 2. Asigna: Avatar Creator, Avatar Data, Avatar Capture (opcional).
    /// 3. Asigna un Leitmotiv Generator (agrégalo a este mismo GameObject si
    ///    no existe ya en la escena).
    /// 4. (Opcional) Asigna un Skin Tone Selector si ya armaste ese control.
    /// 5. (Opcional pero recomendado) Asigna un Profanity Filter.
    /// 6. (Opcional) Asigna un Feedback Text (TMP_Text).
    /// 7. En el botón "Crear avatar" de la UI, en su OnClick() arrastra este
    ///    GameObject y selecciona CreateAvatar().
    /// </summary>
    public class AvatarCreationController : MonoBehaviour
    {
        [Header("Referencias a los scripts existentes de personalización")]
        [SerializeField] private global::AvatarCreator avatarCreator;
        [SerializeField] private global::AvatarData avatarData;
        [Tooltip("Opcional. Si no está asignado, el avatar se guarda sin imagen.")]
        [SerializeField] private global::AvatarCapture avatarCapture;

        [Header("Atributos continuos (opcional)")]
        [Tooltip("Si está asignado, su valor actual se guarda en el AvatarProfile y puede afectar el leitmotiv (ver ContinuousAttributeRootNoteStrategy).")]
        [SerializeField] private SkinToneSelector skinToneSelector;

        [Header("Nuestro pipeline")]
        [SerializeField] private LeitmotivGenerator leitmotivGenerator;

        [Tooltip("Debe coincidir EXACTO con los layerName configurados en el AvatarCreator.")]
        [SerializeField] private string[] layerNames = { "Body", "Head", "Hair" };

        [Header("Validación")]
        [Tooltip("Opcional. Si no se asigna, no se filtran palabras.")]
        [SerializeField] private ProfanityFilter profanityFilter;
        [SerializeField] private bool requireUniqueStudentCode = true;
        [Tooltip("Opcional. Muestra aquí el motivo si la creación falla.")]
        [SerializeField] private TMP_Text feedbackText;

        /// <summary>Conectar al OnClick() del botón "Crear avatar".</summary>
        public void CreateAvatar()
        {
            if (avatarCreator == null || avatarData == null || leitmotivGenerator == null)
            {
                Debug.LogWarning("AvatarCreationController: faltan referencias por asignar en el Inspector.");
                return;
            }

            string name = avatarData.GetAvatarName();
            string code = avatarData.GetStudentCode();

            if (!Validate(name, code, out string error))
            {
                Debug.LogWarning($"[AvatarCreationController] Validación falló: {error}");
                if (feedbackText != null) feedbackText.text = error;
                return;
            }
            if (feedbackText != null) feedbackText.text = "";

            var profile = BuildProfile(name, code);
            var leitmotiv = leitmotivGenerator.Generate(profile);

            Texture2D capturedTexture = null;
            if (avatarCapture != null)
            {
                var sprite = avatarCapture.CaptureAvatar();
                if (sprite != null) capturedTexture = sprite.texture;
            }

            AvatarJsonStorage.Save(profile, capturedTexture);

            Debug.Log($"[AvatarCreationController] '{profile.AvatarName}' guardado (id {profile.Id}). " +
                      $"Leitmotiv: Scale={leitmotiv.Scale} Root={leitmotiv.RootNoteMidi} Tempo={leitmotiv.TempoBpm} Instrument={leitmotiv.InstrumentHint}");
        }

        private bool Validate(string name, string code, out string error)
        {
            if (profanityFilter != null && profanityFilter.ContainsProfanity(name))
            {
                error = "Por favor elige un nombre apropiado.";
                return false;
            }

            if (profanityFilter != null && profanityFilter.ContainsProfanity(code))
            {
                error = "El código ingresado no es válido.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                bool allDigits = true;
                foreach (char c in code)
                {
                    if (!char.IsDigit(c)) { allDigits = false; break; }
                }

                if (!allDigits || code.Length < 6 || code.Length > 10)
                {
                    error = "El código debe tener entre 6 y 10 dígitos numéricos.";
                    return false;
                }
            }

            if (requireUniqueStudentCode)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    error = "Ingresa tu código estudiantil.";
                    return false;
                }

                if (AvatarJsonStorage.StudentCodeExists(code))
                {
                    error = "Ese código estudiantil ya fue usado por otro avatar.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private AvatarProfile BuildProfile(string name, string code)
        {
            var profile = new AvatarProfile
            {
                Id = System.Guid.NewGuid().ToString(),
                AvatarName = name,
                StudentCode = code
            };

            foreach (var layerName in layerNames)
            {
                int index = GetCurrentIndex(layerName);
                profile.Layers.Add(new LayerSelection { LayerName = layerName, SpriteIndex = index });
            }

            if (skinToneSelector != null)
            {
                profile.ContinuousAttributes.Add(new ContinuousAttribute
                {
                    Name = skinToneSelector.AttributeName,
                    Value = skinToneSelector.CurrentValue
                });
            }

            return profile;
        }

        private int GetCurrentIndex(string layerName)
        {
            foreach (var layer in avatarCreator.layers)
            {
                if (layer.layerName == layerName)
                    return layer.currentIndex;
            }
            return 0;
        }
    }
}
