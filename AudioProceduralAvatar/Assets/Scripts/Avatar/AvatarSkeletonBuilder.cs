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
    public SpriteRenderer eyesRenderer;
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
        SetIfNotNull(eyesRenderer, pieces.eyes);
        SetIfNotNull(hairRenderer, pieces.hair);
        SetIfNotNull(subBocaRenderer, pieces.subBoca);
        SetIfNotNull(subBarbaRenderer, pieces.subBarba);
        SetIfNotNull(accessoryRenderer, pieces.accessory);
    }

    private void SetIfNotNull(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null) return;
        if (sprite == null) return; // campo vacío en el PieceSet: no tocar lo que ya había puesto

        renderer.sprite = sprite;

        // Sprite Skin a veces no refresca la deformación al cambiar el sprite
        // por código en tiempo de ejecución. Forzamos un refresco apagando y
        // prendiendo el componente para que recalcule la malla correctamente.
        var spriteSkin = renderer.GetComponent<UnityEngine.U2D.Animation.SpriteSkin>();
        if (spriteSkin != null)
        {
            spriteSkin.enabled = false;
            spriteSkin.enabled = true;
        }
    }
}
