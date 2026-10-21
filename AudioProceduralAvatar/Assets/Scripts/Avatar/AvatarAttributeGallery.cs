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

        AvatarAudioManager audioManager = FindAnyObjectByType<AvatarAudioManager>();

        for (int i = 0; i < layer.sprites.Length; i++)
        {
            if (layer.IsLocked(i)) continue;
            int index = i;

            GameObject btnObj = Instantiate(optionButtonPrefab, content);
            btnObj.GetComponent<Image>().sprite = layer.sprites[i];

            Button btn = btnObj.GetComponent<Button>();

            // Limpiamos cualquier evento previo que el prefab arrastre por defecto
            btn.onClick.RemoveAllListeners();

            // Añadimos la acción limpia
            btn.onClick.AddListener(() =>
            {
                avatarCreator.SetIndex(layerName, index);

                if (audioManager != null)
                {
                    audioManager.PlaySelect();
                }
            });
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