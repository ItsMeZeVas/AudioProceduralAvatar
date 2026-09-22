using UnityEngine;

// Una capa del retrato plano (2D, sin riggear) usado en las miniaturas del
// álbum. Debe contener EXACTAMENTE los mismos sprites, en el mismo orden,
// que el AvatarLayer correspondiente en AvatarCreator (escena de
// personalización) -- mismo layerName, mismo array de sprites por índice.
[CreateAssetMenu(fileName = "NewPortraitLayerSet", menuName = "Avatar/Portrait Layer Set")]
public class AvatarPortraitLayerSet : ScriptableObject
{
    public string layerName;
    public Sprite[] sprites;

    public Sprite GetSprite(int index)
    {
        if (sprites == null || index < 0 || index >= sprites.Length)
            return null;
        return sprites[index];
    }
}
