using UnityEngine;

public class AvatarCreator : MonoBehaviour
{
    public AvatarLayer[] layers;

    private void Start()
    {
        foreach (AvatarLayer layer in layers)
        {
            layer.currentIndex = 0;
            layer.UpdateSprite();
        }
    }

    public void Next(string layerName)
    {
        foreach (AvatarLayer layer in layers)
        {
            if (layer.layerName == layerName)
            {
                layer.Next();
                return;
            }
        }
    }

    public void Previous(string layerName)
    {
        foreach (AvatarLayer layer in layers)
        {
            if (layer.layerName == layerName)
            {
                layer.Previous();
                return;
            }
        }
    }

    public Sprite GetCurrentSprite(string layerName)
    {
        foreach (AvatarLayer layer in layers)
        {
            if (layer.layerName == layerName)
                return layer.image.sprite;
        }

        return null;
    }
}