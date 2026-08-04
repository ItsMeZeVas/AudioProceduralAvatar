using System.Collections.Generic;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Un "piso" de profundidad (posición fija en Z) donde viven avatares.
    /// Puede venir de un Transform ubicado a mano por Diseño en el editor
    /// (plano autorado), o crearse en tiempo de ejecución cuando los planos
    /// autorados ya no tienen espacio.
    /// </summary>
    public class GalleryPlane
    {
        public int Index;
        public float ZPosition;
        public int Capacity;
        public List<AvatarDisplay> Occupants = new();

        public bool HasRoom => Occupants.Count < Capacity;

        public GalleryPlane(int index, float zPosition, int capacity)
        {
            Index = index;
            ZPosition = zPosition;
            Capacity = capacity;
        }
    }
}
