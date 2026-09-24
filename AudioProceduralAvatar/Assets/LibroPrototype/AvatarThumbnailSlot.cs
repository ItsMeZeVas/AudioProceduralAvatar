using UnityEngine;
using UnityEngine.UI;
using AudioProceduralAvatar.Avatar;

// Una miniatura del álbum: SOLO la parte de arriba del avatar, sin riggear,
// armada apilando Image de UI (mismo enfoque que AvatarCreator/AvatarLayer
// en la escena de personalización). Se usa pooled: 24 instancias fijas
// (12 por página x 2 páginas) que se reasignan al cambiar de apertura.
public class AvatarThumbnailSlot : MonoBehaviour
{
    [System.Serializable]
    public struct LayerImage
    {
        public string layerName;
        public Image image;
    }

    [Header("Base de piel (personaje desnudo, sprite fijo teñido por tono de piel)")]
    public Image skinBaseImage;
    public SkinToneGradientAsset skinToneGradient;
    public string skinToneAttributeName = "SkinTone";

    [Header("Color de pelo/barba (tinte guardado como atributos continuos)")]
    public string hairColorAttributePrefix = "HairColor";
    public string beardColorAttributePrefix = "BeardColor";

    [Tooltip("Orden de dibujo de atrás hacia adelante: Body, Head, SubBarba, SubBoca, Hair, UpperBody, Accessories (ajusta según tu arte real).")]
    public LayerImage[] layerImages;

    [Header("Estado vacío / selección")]
    public GameObject emptyState;
    public Button selectButton;
    public Image highlightBorder;

    private string _avatarId;
    public System.Action<string> OnSelected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(() => OnSelected?.Invoke(_avatarId));
    }

    public void SetAvatar(AvatarProfile profile, AvatarPortraitDatabase database)
    {
        _avatarId = profile.Id;

        if (emptyState != null) emptyState.SetActive(false);
        if (selectButton != null) selectButton.interactable = true;
        SetHighlighted(false);

        if (skinBaseImage != null)
        {
            skinBaseImage.enabled = true;
            if (skinToneGradient != null)
            {
                float skinTone = profile.GetContinuousValue(skinToneAttributeName, 0.5f);
                skinBaseImage.color = skinToneGradient.Evaluate(skinTone);
            }
        }

        foreach (var layer in layerImages)
        {
            if (layer.image == null) continue;

            // Capa marcada explícitamente como "sin prenda" (avatar
            // secreto) -> apagar directo, sin pasar por el fallback público
            // de GetSpriteIndex.
            if (profile.IsLayerHidden(layer.layerName))
            {
                layer.image.enabled = false;
                continue;
            }

            int index = profile.GetSpriteIndex(layer.layerName);
            Sprite sprite = database.Resolve(layer.layerName, index);

            layer.image.sprite = sprite;
            layer.image.enabled = sprite != null;

            if (layer.layerName == "Hair")
                layer.image.color = ReadColor(profile, hairColorAttributePrefix, Color.white);
            else if (layer.layerName == "SubBarba")
                layer.image.color = ReadColor(profile, beardColorAttributePrefix, Color.white);
            else
                layer.image.color = Color.white;
        }
    }

    private static Color ReadColor(AvatarProfile profile, string attributePrefix, Color fallback)
    {
        float r = profile.GetContinuousValue(attributePrefix + "R", fallback.r);
        float g = profile.GetContinuousValue(attributePrefix + "G", fallback.g);
        float b = profile.GetContinuousValue(attributePrefix + "B", fallback.b);
        return new Color(r, g, b);
    }

    public void Clear()
    {
        _avatarId = null;

        if (emptyState != null) emptyState.SetActive(true);
        if (selectButton != null) selectButton.interactable = false;
        SetHighlighted(false);

        if (skinBaseImage != null) skinBaseImage.enabled = false;

        foreach (var layer in layerImages)
        {
            if (layer.image == null) continue;
            layer.image.enabled = false;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightBorder != null)
            highlightBorder.enabled = highlighted;
    }

    public string AvatarId => _avatarId;
}
