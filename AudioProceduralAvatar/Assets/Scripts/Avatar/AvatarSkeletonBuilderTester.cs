using UnityEngine;

// Componente de PRUEBA temporal. Ponlo en el mismo GameObject que ya
// tiene AvatarSkeletonBuilder ("base cuerpo rigg"). Arrastra un
// AvatarPieceSet en el campo de abajo, dale Play, y luego click derecho
// sobre el nombre de este componente en el Inspector (o los 3 puntitos
// arriba a la derecha del componente) -> vas a ver la opción "Apply Test"
// en el menú. Se puede borrar este script cuando ya no lo necesites.
public class AvatarSkeletonBuilderTester : MonoBehaviour
{
    public AvatarPieceSet pieceSetDePrueba;
    public string layerNameDePrueba = "UpperBody";

    [ContextMenu("Apply Test")]
    public void ApplyTest()
    {
        AvatarSkeletonBuilder builder = GetComponent<AvatarSkeletonBuilder>();

        if (builder == null)
        {
            Debug.LogError("Este GameObject no tiene AvatarSkeletonBuilder.");
            return;
        }

        if (pieceSetDePrueba == null)
        {
            Debug.LogWarning("Arrastra un AvatarPieceSet en 'Piece Set De Prueba' antes de probar.");
            return;
        }

        builder.Apply(layerNameDePrueba, pieceSetDePrueba);
        Debug.Log("AvatarSkeletonBuilderTester: Apply ejecutado.");
    }
}
