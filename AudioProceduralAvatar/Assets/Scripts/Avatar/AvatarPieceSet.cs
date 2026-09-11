using UnityEngine;

// Un AvatarPieceSet representa UNA opción del menú (ej. "Camisa Roja",
// "Cabello 2", "Pantalón Negro"), NO un look completo de pies a cabeza.
// Deja en None los campos que no le correspondan a esa prenda.
[CreateAssetMenu(fileName = "NewPieceSet", menuName = "Avatar/Piece Set")]
public class AvatarPieceSet : ScriptableObject
{
    [Header("Cuerpo")]
    public Sprite head;
    public Sprite torso;
    public Sprite lArm;
    public Sprite rArm;
    public Sprite lLeg;
    public Sprite rLeg;

    [Header("Piezas rígidas (cuelgan de la cabeza)")]
    public Sprite eyes;
    public Sprite hair;
    public Sprite subBoca;
    public Sprite subBarba;
    public Sprite accessory;
}
