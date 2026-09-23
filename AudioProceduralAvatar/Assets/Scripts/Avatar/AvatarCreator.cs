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

    public void SetIndex(string layerName, int index)
    {
        foreach (AvatarLayer layer in layers)
        {
            if (layer.layerName == layerName)
            {
                layer.currentIndex = index;
                layer.image.sprite = layer.sprites[index];
                layer.image.enabled = true; // <- por si venía de "Ninguno"
                return;
            }
        }
    }
    public void Remove(string layerName)
    {
        foreach (AvatarLayer layer in layers)
        {
            if (layer.layerName == layerName)
            {
                layer.currentIndex = -1;
                layer.image.sprite = null;
                layer.image.enabled = false; // <- oculta el cuadro blanco
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