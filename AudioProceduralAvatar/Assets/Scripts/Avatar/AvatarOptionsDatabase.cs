using UnityEngine;

// Base de datos de todas las capas del avatar (Hair, UpperBody, LowerBody,
// Accessories, SubBoca, SubBarba, etc.). Un solo asset central que el
// AvatarSkeletonBuilder consulta para resolver cada LayerSelection del JSON.
[CreateAssetMenu(fileName = "AvatarOptionsDatabase", menuName = "Avatar/Options Database")]
public class AvatarOptionsDatabase : ScriptableObject
{
    public AvatarLayerOptionSet[] layers;

    public AvatarPieceSet Resolve(string layerName, int index)
    {
        if (layers == null) return null;

        foreach (var layer in layers)
        {
            if (layer != null && layer.layerName == layerName)
                return layer.GetOption(index);
        }

        Debug.LogWarning($"AvatarOptionsDatabase: no se encontró la capa '{layerName}'.");
        return null;
    }
}
