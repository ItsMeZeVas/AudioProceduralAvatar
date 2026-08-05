using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AvatarLayer
{
    [Header("Nombre")]
    public string layerName;

    [Header("Imagen UI")]
    public Image image;

    [Header("Sprites disponibles")]
    public Sprite[] sprites;

    [HideInInspector]
    public int currentIndex;

    public void UpdateSprite()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        image.sprite = sprites[currentIndex];
    }

    public void Next()
    {
        if (sprites.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= sprites.Length)
            currentIndex = 0;

        UpdateSprite();
    }

    public void Previous()
    {
        if (sprites.Length == 0)
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = sprites.Length - 1;

        UpdateSprite();
    }
}