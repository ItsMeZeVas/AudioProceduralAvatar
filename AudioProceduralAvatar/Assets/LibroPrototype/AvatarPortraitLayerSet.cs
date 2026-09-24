using UnityEngine;

// Una capa del retrato plano (2D, sin riggear) usado en las miniaturas del
// álbum. Debe contener EXACTAMENTE los mismos sprites, en el mismo orden,
// que el AvatarLayer correspondiente en AvatarCreator (escena de
// personalización) -- mismo layerName, mismo array de sprites por índice.
//
// Índices NEGATIVOS (-1, -2, ...) resuelven contra secretSprites en vez de
// sprites, igual que AvatarLayerOptionSet.GetOption: -1 -> secretSprites[0],
// -2 -> secretSprites[1], etc. Debe contener EXACTAMENTE los mismos sprites,
// en el mismo orden, que secretOptions en el AvatarLayerOptionSet
// correspondiente.
[CreateAssetMenu(fileName = "NewPortraitLayerSet", menuName = "Avatar/Portrait Layer Set")]
public class AvatarPortraitLayerSet : ScriptableObject
{
    public string layerName;
    public Sprite[] sprites;

    [Tooltip("Prendas secretas, mismo orden que secretOptions en AvatarLayerOptionSet. -1 = secretSprites[0], -2 = secretSprites[1], etc.")]
    public Sprite[] secretSprites;

    public Sprite GetSprite(int index)
    {
        if (index < 0)
        {
            int secretIndex = -index - 1;

            if (secretSprites == null || secretIndex < 0 || secretIndex >= secretSprites.Length)
            {
                Debug.LogWarning($"AvatarPortraitLayerSet '{layerName}': índice secreto {index} fuera de rango.");
                return null;
            }

            return secretSprites[secretIndex];
        }

        if (sprites == null || index >= sprites.Length)
            return null;
        return sprites[index];
    }
}
