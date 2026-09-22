using UnityEngine;

// Base de datos central de retratos planos (2D) para el álbum, análoga a
// AvatarOptionsDatabase pero resolviendo a un Sprite plano en vez de un
// AvatarPieceSet (para riggear).
[CreateAssetMenu(fileName = "AvatarPortraitDatabase", menuName = "Avatar/Portrait Database")]
public class AvatarPortraitDatabase : ScriptableObject
{
    public AvatarPortraitLayerSet[] layers;

    public Sprite Resolve(string layerName, int index)
    {
        if (layers == null) return null;

        foreach (var layer in layers)
        {
            if (layer != null && layer.layerName == layerName)
                return layer.GetSprite(index);
        }

        Debug.LogWarning($"AvatarPortraitDatabase: no se encontró la capa '{layerName}'.");
        return null;
    }
}
