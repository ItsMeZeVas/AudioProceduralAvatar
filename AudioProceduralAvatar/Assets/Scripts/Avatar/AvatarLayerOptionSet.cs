using UnityEngine;

// Une una capa (ej. "UpperBody") con la lista de opciones/prendas disponibles
// para esa capa, EN EL MISMO ORDEN que sprites[] en AvatarLayer de la escena
// de personalización. El índice aquí debe coincidir con el SpriteIndex
// guardado en el JSON del avatar.
//
// Índices NEGATIVOS (-1, -2, ...) resuelven contra secretOptions en vez de
// options: -1 -> secretOptions[0], -2 -> secretOptions[1], etc. Esas prendas
// NUNCA se agregan a AvatarLayer.sprites[], así que no aparecen navegando
// con Next/Previous en la personalización pública.
[CreateAssetMenu(fileName = "NewLayerOptionSet", menuName = "Avatar/Layer Option Set")]
public class AvatarLayerOptionSet : ScriptableObject
{
    [Tooltip("Debe coincidir EXACTO con AvatarLayer.layerName de la personalización")]
    public string layerName;

    [Tooltip("Mismo orden que AvatarLayer.sprites[] en la escena de personalización")]
    public AvatarPieceSet[] options;

    [Tooltip("Prendas secretas, solo alcanzables por código. -1 = secretOptions[0], -2 = secretOptions[1], etc.")]
    public AvatarPieceSet[] secretOptions;

    public AvatarPieceSet GetOption(int index)
    {
        if (index < 0)
        {
            int secretIndex = -index - 1;

            if (secretOptions == null || secretIndex < 0 || secretIndex >= secretOptions.Length)
            {
                Debug.LogWarning($"AvatarLayerOptionSet '{layerName}': índice secreto {index} fuera de rango.");
                return null;
            }

            return secretOptions[secretIndex];
        }

        if (options == null || index >= options.Length)
        {
            Debug.LogWarning($"AvatarLayerOptionSet '{layerName}': índice {index} fuera de rango.");
            return null;
        }

        return options[index];
    }
}
