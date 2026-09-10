using UnityEngine;

public class AvatarSkeletonBuilder : MonoBehaviour
{
    [Header("Renderers del rig (arrastrar desde el Prefab instanciado)")]
    public SpriteRenderer headRenderer;
    public SpriteRenderer torsoRenderer;
    public SpriteRenderer lArmRenderer;
    public SpriteRenderer rArmRenderer;
    public SpriteRenderer lLegRenderer;
    public SpriteRenderer rLegRenderer;

    [Header("Piezas rígidas (cuelgan de la cabeza)")]
    public SpriteRenderer hairRenderer;
    public SpriteRenderer subBocaRenderer;
    public SpriteRenderer subBarbaRenderer;
    public SpriteRenderer accessoryRenderer;

    public void Apply(AvatarPieceSet pieces)
    {
        if (pieces == null)
        {
            Debug.LogWarning("AvatarSkeletonBuilder: no se recibió ningún AvatarPieceSet.");
            return;
        }

        SetIfNotNull(headRenderer, pieces.head);
        SetIfNotNull(torsoRenderer, pieces.torso);
        SetIfNotNull(lArmRenderer, pieces.lArm);
        SetIfNotNull(rArmRenderer, pieces.rArm);
        SetIfNotNull(lLegRenderer, pieces.lLeg);
        SetIfNotNull(rLegRenderer, pieces.rLeg);
        SetIfNotNull(hairRenderer, pieces.hair);
        SetIfNotNull(subBocaRenderer, pieces.subBoca);
        SetIfNotNull(subBarbaRenderer, pieces.subBarba);
        SetIfNotNull(accessoryRenderer, pieces.accessory);
    }

    private void SetIfNotNull(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null) return;
        renderer.sprite = sprite; // null es válido: oculta la pieza (ej. sin accesorio)
    }
}
