
using UnityEngine;
using UnityEngine.U2D.Animation;

public class AvatarSkeletonBuilder : MonoBehaviour
{
    [Header("Piel (Body) - se ve donde la ropa no cubre")]
    public SpriteRenderer skinHeadRenderer;
    public SpriteRenderer skinTorsoRenderer;
    public SpriteRenderer skinLArmRenderer;
    public SpriteRenderer skinRArmRenderer;
    public SpriteRenderer skinLLegRenderer;
    public SpriteRenderer skinRLegRenderer;

    [Header("Ropa (UpperBody / LowerBody) - ranuras existentes en el rig, ej. OS7/OI2")]
    public SpriteRenderer upperBodyTorsoRenderer;
    public SpriteRenderer upperBodyLArmRenderer;
    public SpriteRenderer upperBodyRArmRenderer;
    public SpriteRenderer lowerBodyLLegRenderer;
    public SpriteRenderer lowerBodyRLegRenderer;

    [Header("Piezas rígidas (cuelgan de la cabeza)")]
    public SpriteRenderer eyesRenderer;
    public SpriteRenderer hairRenderer;
    public SpriteRenderer subBocaRenderer;
    public SpriteRenderer subBarbaRenderer;
    public SpriteRenderer accessoryRenderer;

    // layerName debe coincidir EXACTO con AvatarLayer.layerName
    public void Apply(string layerName, AvatarPieceSet pieces)
    {
        if (pieces == null)
        {
            Debug.LogWarning(
                $"AvatarSkeletonBuilder: no se recibió AvatarPieceSet para la capa '{layerName}'."
            );
            return;
        }

        switch (layerName)
        {
            case "Body":
                SetIfNotNull(skinHeadRenderer, pieces.head);
                SetIfNotNull(skinTorsoRenderer, pieces.torso);
                SetIfNotNull(skinLArmRenderer, pieces.lArm);
                SetIfNotNull(skinRArmRenderer, pieces.rArm);
                SetIfNotNull(skinLLegRenderer, pieces.lLeg);
                SetIfNotNull(skinRLegRenderer, pieces.rLeg);
                break;

            case "UpperBody":
                SetIfNotNull(upperBodyTorsoRenderer, pieces.torso);
                SetIfNotNull(upperBodyLArmRenderer, pieces.lArm);
                SetIfNotNull(upperBodyRArmRenderer, pieces.rArm);
                break;

            case "LowerBody":
                SetIfNotNull(lowerBodyLLegRenderer, pieces.lLeg);
                SetIfNotNull(lowerBodyRLegRenderer, pieces.rLeg);
                break;

            case "Head":
                SetIfNotNull(skinHeadRenderer, pieces.head);
                break;

            case "Hair":
                SetIfNotNull(hairRenderer, pieces.hair);
                break;

            case "Eyes":
                SetIfNotNull(eyesRenderer, pieces.eyes);
                break;

            case "SubBoca":
                SetIfNotNull(subBocaRenderer, pieces.subBoca);
                break;

            case "SubBarba":
                SetIfNotNull(subBarbaRenderer, pieces.subBarba);
                break;

            case "Accessories":
                SetIfNotNull(accessoryRenderer, pieces.accessory);
                break;

            default:
                Debug.LogWarning(
                    $"AvatarSkeletonBuilder: layerName '{layerName}' no reconocido."
                );
                break;
        }
    }

    /// <summary>Aplica un tinte de color a Hair o SubBarba (elegido con HairColorPicker).</summary>
    public void ApplyColor(string layerName, Color color)
    {
        switch (layerName)
        {
            case "Hair":
                if (hairRenderer != null) hairRenderer.color = color;
                break;

            case "SubBarba":
                if (subBarbaRenderer != null) subBarbaRenderer.color = color;
                break;
        }
    }

    private void SetIfNotNull(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null) return;
        if (sprite == null) return;

        renderer.sprite = sprite;

        // Buscar Sprite Skin en el mismo objeto.
        SpriteSkin spriteSkin = renderer.GetComponent<SpriteSkin>();

        if (spriteSkin != null)
        {
            // Activar Auto Rebind.
            spriteSkin.autoRebind = true;

            // Activar el Sprite Skin.
            spriteSkin.enabled = false;
            spriteSkin.enabled = true;
        }
    }
}
