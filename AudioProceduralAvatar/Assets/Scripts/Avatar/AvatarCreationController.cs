using TMPro;
using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Conecta el sistema de creación de avatar con el sistema de
    /// generación del leitmotiv.
    /// </summary>
    public class AvatarCreationController : MonoBehaviour
    {
        [Header("=== PERSONALIZACIÓN ===")]

        [SerializeField]
        private global::AvatarCreator avatarCreator;

        [SerializeField]
        private global::AvatarData avatarData;

        [Tooltip("Opcional.")]
        [SerializeField]
        private global::AvatarCapture avatarCapture;


        [Header("=== ATRIBUTO CONTINUO ===")]

        [SerializeField]
        private SkinToneSelector skinToneSelector;


        [Header("=== LEITMOTIV ===")]

        [SerializeField]
        private LeitmotivGenerator leitmotivGenerator;


        [Tooltip(
            "Todas las capas que deben guardarse en el perfil."
        )]
        [SerializeField]
        private string[] layerNames =
        {
            "Body",
            "Head",
            "Hair",
            "Eyes",
            "UpperBody",
            "LowerBody",
            "Accessories"
        };


        [Header("=== VALIDACIÓN ===")]

        [SerializeField]
        private ProfanityFilter profanityFilter;

        [SerializeField]
        private bool requireUniqueStudentCode = true;

        [SerializeField]
        private TMP_Text feedbackText;


        // ========================================================
        // CREAR AVATAR
        // ========================================================

        public void CreateAvatar()
        {
            if (
                avatarCreator == null ||
                avatarData == null ||
                leitmotivGenerator == null)
            {
                Debug.LogWarning(
                    "AvatarCreationController: faltan referencias."
                );

                return;
            }


            string name =
                avatarData.GetAvatarName();

            string code =
                avatarData.GetStudentCode();


            if (
                !Validate(
                    name,
                    code,
                    out string error))
            {
                Debug.LogWarning(
                    $"[AvatarCreationController] Validación falló: {error}"
                );


                if (feedbackText != null)
                    feedbackText.text = error;


                return;
            }


            if (feedbackText != null)
                feedbackText.text = "";


            AvatarProfile profile =
                BuildProfile(
                    name,
                    code
                );


            LeitmotivData leitmotiv =
                leitmotivGenerator.Generate(
                    profile
                );


            Texture2D capturedTexture = null;


            if (avatarCapture != null)
            {
                var sprite =
                    avatarCapture.CaptureAvatar();


                if (sprite != null)
                    capturedTexture =
                        sprite.texture;
            }


            AvatarJsonStorage.Save(
                profile,
                capturedTexture
            );


            Debug.Log(
                $"[AvatarCreationController] " +
                $"'{profile.AvatarName}' guardado. " +

                $"Scale={leitmotiv.Scale} | " +

                $"Root={leitmotiv.RootNoteMidi} | " +

                $"Tempo={leitmotiv.TempoBpm} | " +

                $"Instrument={leitmotiv.InstrumentHint} | " +

                $"Rhythm={leitmotiv.Rhythm} | " +

                $"Dynamics={leitmotiv.DynamicMultiplier}"
            );
        }


        // ========================================================
        // VALIDACIÓN
        // ========================================================

        private bool Validate(
            string name,
            string code,
            out string error)
        {
            if (
                profanityFilter != null &&
                profanityFilter.ContainsProfanity(name))
            {
                error =
                    "Por favor elige un nombre apropiado.";

                return false;
            }


            if (
                profanityFilter != null &&
                profanityFilter.ContainsProfanity(code))
            {
                error =
                    "El código ingresado no es válido.";

                return false;
            }


            if (!string.IsNullOrWhiteSpace(code))
            {
                bool allDigits = true;


                foreach (char c in code)
                {
                    if (!char.IsDigit(c))
                    {
                        allDigits = false;
                        break;
                    }
                }


                if (
                    !allDigits ||
                    code.Length < 6 ||
                    code.Length > 10)
                {
                    error =
                        "El código debe tener entre 6 y 10 dígitos numéricos.";

                    return false;
                }
            }


            if (requireUniqueStudentCode)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    error =
                        "Ingresa tu código estudiantil.";

                    return false;
                }


                if (
                    AvatarJsonStorage.StudentCodeExists(
                        code))
                {
                    error =
                        "Ese código estudiantil ya fue usado por otro avatar.";

                    return false;
                }
            }


            error = null;

            return true;
        }


        // ========================================================
        // CREAR PERFIL
        // ========================================================

        private AvatarProfile BuildProfile(
            string name,
            string code)
        {
            var profile =
                new AvatarProfile
                {
                    Id =
                        System.Guid.NewGuid()
                            .ToString(),

                    AvatarName = name,

                    StudentCode = code
                };


            foreach (var layerName in layerNames)
            {
                int index =
                    GetCurrentIndex(
                        layerName
                    );


                profile.Layers.Add(
                    new LayerSelection
                    {
                        LayerName =
                            layerName,

                        SpriteIndex =
                            index
                    }
                );
            }


            // ----------------------------------------------------
            // SKIN TONE
            // ----------------------------------------------------

            if (skinToneSelector != null)
            {
                profile.ContinuousAttributes.Add(
                    new ContinuousAttribute
                    {
                        Name =
                            skinToneSelector
                                .AttributeName,

                        Value =
                            skinToneSelector
                                .CurrentValue
                    }
                );
            }


            return profile;
        }


        // ========================================================
        // OBTENER ÍNDICE
        // ========================================================

        private int GetCurrentIndex(
            string layerName)
        {
            foreach (
                var layer
                in avatarCreator.layers)
            {
                if (
                    layer.layerName ==
                    layerName)
                {
                    return layer.currentIndex;
                }
            }


            return 0;
        }
    }
}