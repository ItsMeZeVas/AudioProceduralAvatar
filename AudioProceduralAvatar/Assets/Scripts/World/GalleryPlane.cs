using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        public float TargetZ = 0;
        public int TargetIndex = 0;
        public List<AvatarDisplay> Occupants = new();
        public Transform Transform;

        public bool HasRoom => Occupants.Count < Capacity;

        public GalleryPlane(int index, float zPosition, int capacity, Transform parent)
        {
            Index = index;
            ZPosition = zPosition;
            Capacity = capacity;
            CreateTransform(parent);
        }

        public void CreateTransform(Transform parent)
        {
            GameObject transformPlane = new GameObject($"Plane_{Index}");

            Transform = transformPlane.transform;

            Transform.position = new Vector3(
                0f,
                0f,
                ZPosition
            );

            Transform.SetParent(parent);
        }

        public bool HandleZTransition(float planeTransitionSpeed)
        {
            Vector3 pos = Transform.position;

            if (Mathf.Approximately(pos.z, TargetZ))
            {
                pos.z = TargetZ;
                Transform.position = pos;
                Index = TargetIndex;
                ZPosition = TargetZ;
                return true;
            }

            pos.z = Mathf.MoveTowards(pos.z, TargetZ, planeTransitionSpeed * Time.deltaTime);

            Transform.position = pos;
            return false;
        }

        public void HandleZInstant()
        {
            Vector3 pos = Transform.position;
            pos.z = TargetZ;
            Transform.position = pos;
            Index = TargetIndex;
            ZPosition = TargetZ;
            return;
        }

    }
}
