using UnityEngine;
using System;
using System.Collections.Generic;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Avatar
{
    [Serializable]
    public struct SecretLayerSelection
    {
        [Tooltip("Debe coincidir EXACTO con AvatarLayer.layerName")]
        public string layerName;

        [Tooltip("Marca esto si este personaje secreto NO debe tener nada en esta capa (ej. sin pelo). Si está marcado, se ignoran spriteIndex y previewSprite y la capa queda oculta.")]
        public bool noPiece;

        [Tooltip("Índice que se guarda en el JSON del avatar. Usa un índice NEGATIVO (-1, -2, ...) para una prenda secreta (agregada en secretOptions de AvatarLayerOptionSet). Usa un índice normal (0, 1, 2...) si para esta capa quieres usar una prenda que SÍ es pública.")]
        public int spriteIndex;

        [Tooltip("Solo si spriteIndex es NEGATIVO: el sprite a mostrar en vivo en la escena de personalización (esa prenda no vive en AvatarLayer.sprites[], así que no se puede pedir por índice ahí). Debe ser el mismo sprite que corresponde a esta capa dentro del AvatarPieceSet puesto en secretOptions, en la misma posición.")]
        public Sprite previewSprite;
    }


    /// <summary>
    /// Un avatar ya armado que se carga automáticamente cuando alguien
    /// ingresa un código secreto (en vez de un código estudiantil normal)
    /// en la pantalla de guardado.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSecretAvatarPreset", menuName = "Avatar/Secret Avatar Preset")]
    public class SecretAvatarPreset : ScriptableObject
    {
        [Tooltip("El código/cédula que activa este preset. Se compara sin distinguir mayúsculas ni espacios.")]
        public string secretCode;

        [Tooltip("Nombre fijo para este avatar. Si se deja vacío, se usa el nombre que la persona escriba en pantalla.")]
        public string fixedAvatarName = "";

        [Tooltip("Una entrada por cada capa del avatar.")]
        public List<SecretLayerSelection> layers = new();

        [Tooltip("Opcional: atributos continuos del preset (ej. tono de piel).")]
        public List<ContinuousAttribute> continuousAttributes = new();

        /// <summary>
        /// Construye un AvatarProfile a partir de este preset (para guardar
        /// en el JSON). El nombre que escribió la persona se usa solo si el
        /// preset no trae uno fijo.
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
            {
                if (layer.noPiece)
                {
                    // Antes: se saltaba por completo, y eso era indistinguible
                    // de "capa no especificada" -> el álbum/rig caían al
                    // fallback público (índice 0), mostrando una prenda que
                    // el preset nunca quiso. Ahora queda una marca explícita.
                    profile.Layers.Add(new LayerSelection
                    {
                        LayerName = layer.layerName,
                        SpriteIndex = 0,
                        Hidden = true
                    });
                    continue;
                }

                profile.Layers.Add(new LayerSelection
                {
                    LayerName = layer.layerName,
                    SpriteIndex = layer.spriteIndex
                });
            }

            foreach (var attr in continuousAttributes)
                profile.ContinuousAttributes.Add(attr);

            return profile;
        }
    }
}
