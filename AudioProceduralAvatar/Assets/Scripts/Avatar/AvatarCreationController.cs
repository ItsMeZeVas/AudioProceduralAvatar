using System.Collections.Generic;
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


        [Header("=== CÓDIGOS SECRETOS ===")]

        [Tooltip("Si el código ingresado coincide con uno de esta base de datos, se carga ese avatar ya armado en vez de usar las capas seleccionadas en pantalla. También activa un preview en vivo mientras la persona escribe.")]
        [SerializeField]
        private SecretPresetDatabase secretPresetDatabase;


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
        // ESTADO DEL PREVIEW EN VIVO
        // ========================================================

        private bool secretPreviewActive = false;
        private SecretAvatarPreset activeSecretPreset = null;
        private readonly List<LayerSelection> preSecretSnapshot = new();
        private float preSecretSkinTone = 0.5f;


        // ========================================================
        // CICLO DE VIDA
        // ========================================================

        private void Start()
        {
            if (avatarData != null && avatarData.studentCodeInput != null)
            {
                avatarData.studentCodeInput.onValueChanged.AddListener(
                    OnCodeInputChanged
                );
            }
        }


        private void OnDestroy()
        {
            if (avatarData != null && avatarData.studentCodeInput != null)
            {
                avatarData.studentCodeInput.onValueChanged.RemoveListener(
                    OnCodeInputChanged
                );
            }
        }


        // ========================================================
        // PREVIEW EN VIVO AL ESCRIBIR EL CÓDIGO
        // ========================================================

        private void OnCodeInputChanged(string typedCode)
        {
            if (secretPresetDatabase == null || avatarCreator == null)
                return;

            SecretAvatarPreset preset =
                secretPresetDatabase.Find(typedCode);

            if (preset != null)
            {
                // Nuevo match: guarda lo que había antes SOLO si todavía no
                // hay un preview activo, para no pisar el snapshot original
                // con capas ya "contaminadas" por un preset anterior.
                if (!secretPreviewActive)
                    SnapshotCurrentSelection();

                ApplyPresetPreview(preset);

                secretPreviewActive = true;
                activeSecretPreset = preset;
            }
            else if (secretPreviewActive)
            {
                // Ya no coincide con ningún código secreto: revertir.
                RestoreSnapshot();

                secretPreviewActive = false;
                activeSecretPreset = null;
            }
        }


        private void SnapshotCurrentSelection()
        {
            preSecretSnapshot.Clear();

            foreach (var layer in avatarCreator.layers)
            {
                preSecretSnapshot.Add(new LayerSelection
                {
                    LayerName = layer.layerName,
                    SpriteIndex = layer.currentIndex
                });
            }

            preSecretSkinTone =
                skinToneSelector != null
                    ? skinToneSelector.CurrentValue
                    : 0.5f;
        }


        private void ApplyPresetPreview(SecretAvatarPreset preset)
        {
            foreach (var layerSelection in preset.layers)
            {
                if (layerSelection.spriteIndex < 0)
                {
                    // Prenda secreta: no vive en AvatarLayer.sprites[], así
                    // que se pone el sprite directo, sin pasar por índices.
                    SetLayerSpriteDirect(
                        layerSelection.layerName,
                        layerSelection.previewSprite
                    );
                }
                else
                {
                    // Prenda pública normal: sí puede pedirse por índice.
                    avatarCreator.SetIndex(
                        layerSelection.layerName,
                        layerSelection.spriteIndex
                    );
                }
            }

            if (skinToneSelector != null)
            {
                foreach (var attribute in preset.continuousAttributes)
                {
                    if (attribute.Name == skinToneSelector.AttributeName)
                    {
                        skinToneSelector.SetValue(attribute.Value);
                        break;
                    }
                }
            }
        }


        private void SetLayerSpriteDirect(string layerName, Sprite sprite)
        {
            if (sprite == null)
                return;

            foreach (var layer in avatarCreator.layers)
            {
                if (layer.layerName == layerName)
                {
                    layer.image.sprite = sprite;
                    return;
                }
            }
        }


        private void RestoreSnapshot()
        {
            foreach (var selection in preSecretSnapshot)
            {
                avatarCreator.SetIndex(
                    selection.LayerName,
                    selection.SpriteIndex
                );
            }

            if (skinToneSelector != null)
                skinToneSelector.SetValue(preSecretSkinTone);
        }


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


            // Si el código coincide con un preset secreto, se usa ese
            // avatar ya armado en vez de las capas elegidas en pantalla.
            // El código sigue pasando por la MISMA validación de abajo
            // (incluida la unicidad), así que un código secreto solo
            // se puede usar una vez.
            SecretAvatarPreset matchedPreset =
                secretPresetDatabase != null
                    ? secretPresetDatabase.Find(code)
                    : null;


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
                matchedPreset != null
                    ? matchedPreset.BuildProfile(name, code)
                    : BuildProfile(name, code);


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


            // Una vez guardado, el preview ya no debe revertirse si la
            // persona vuelve a tocar el campo de código.
            secretPreviewActive = false;
            activeSecretPreset = null;


            Debug.Log(
                $"[AvatarCreationController] " +
                $"'{profile.AvatarName}' guardado " +
                (matchedPreset != null ? "(preset secreto). " : ". ") +

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
