using UnityEngine;
using UnityEngine.UI;

public class AvatarAttributeGallery : MonoBehaviour
{
    [Header("Debe coincidir EXACTO con AvatarLayer.layerName")]
    public string layerName; // "Hair", "Head", "UpperBody", "LowerBody", "Accessories", etc.

    [Header("Referencias")]
    public AvatarCreator avatarCreator;
    public GameObject optionButtonPrefab; // prefab con Button + Image
    public Transform content;             // Content del ScrollView

    void Start()
    {
        PopulateFromLayer();
    }

    void PopulateFromLayer()
    {
        AvatarLayer layer = FindLayer();

        if (layer == null)
        {
            Debug.LogWarning($"AvatarAttributeGallery: no se encontró layer '{layerName}' en AvatarCreator.");
            return;
        }

        for (int i = 0; i < layer.sprites.Length; i++)
        {
            int index = i; // captura local — evita el bug clásico de closures en loops de C#

            GameObject btnObj = Instantiate(optionButtonPrefab, content);
            btnObj.GetComponent<Image>().sprite = layer.sprites[i];

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => avatarCreator.SetIndex(layerName, index));
        }
    }

    AvatarLayer FindLayer()
    {
        foreach (var layer in avatarCreator.layers)
            if (layer.layerName == layerName)
                return layer;
        return null;
    }
}