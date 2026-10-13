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

    [Header("Bloqueo (lo llena AvatarCatalogSync; mismo orden que sprites[])")]
    public bool[] locked;

    [HideInInspector]
    public int currentIndex;

    /// <summary>True si la prenda en ese indice esta bloqueada (no se ofrece al usuario).</summary>
    public bool IsLocked(int index)
    {
        return locked != null && index >= 0 && index < locked.Length && locked[index];
    }

    public void UpdateSprite()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        image.sprite = sprites[currentIndex];
    }

    /// <summary>Aplica un tinte de color a esta capa (Hair/SubBarba con el color picker).</summary>
    public void SetTint(Color color)
    {
        if (image != null) image.color = color;
    }

    public void Next()
    {
        if (sprites.Length == 0)
            return;

        // Igual que antes, pero saltando las prendas bloqueadas.
        for (int step = 0; step < sprites.Length; step++)
        {
            currentIndex++;

            if (currentIndex >= sprites.Length)
                currentIndex = 0;

            if (!IsLocked(currentIndex))
                break;
        }

        UpdateSprite();
    }

    public void Previous()
    {
        if (sprites.Length == 0)
            return;

        for (int step = 0; step < sprites.Length; step++)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = sprites.Length - 1;

            if (!IsLocked(currentIndex))
                break;
        }

        UpdateSprite();
    }
}
