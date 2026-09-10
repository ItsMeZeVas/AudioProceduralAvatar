using UnityEngine;

// Une una capa (ej. "UpperBody") con la lista de opciones/prendas disponibles
// para esa capa, EN EL MISMO ORDEN que sprites[] en AvatarLayer de la escena
// de personalización. El índice aquí debe coincidir con el SpriteIndex
// guardado en el JSON del avatar.
[CreateAssetMenu(fileName = "NewLayerOptionSet", menuName = "Avatar/Layer Option Set")]
public class AvatarLayerOptionSet : ScriptableObject
{
    [Tooltip("Debe coincidir EXACTO con AvatarLayer.layerName de la personalización")]
    public string layerName;

    [Tooltip("Mismo orden que AvatarLayer.sprites[] en la escena de personalización")]
    public AvatarPieceSet[] options;

    public AvatarPieceSet GetOption(int index)
    {
        if (options == null || index < 0 || index >= options.Length)
        {
            Debug.LogWarning($"AvatarLayerOptionSet '{layerName}': índice {index} fuera de rango.");
            return null;
        }

        return options[index];
    }
}
