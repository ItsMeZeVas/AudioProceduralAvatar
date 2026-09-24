using System;
using System.Collections.Generic;

namespace AudioProceduralAvatar.Avatar
{
    [Serializable]
    public struct LayerSelection
    {
        public string LayerName;
        public int SpriteIndex;

        // True solo para capas de avatares secretos que el preset marcó
        // explícitamente como "sin prenda" (noPiece). Distinto de que la
        // capa simplemente no esté en la lista (caso normal / avatares
        // viejos), donde SÍ se quiere usar el fallback público.
        public bool Hidden;
    }


    [Serializable]
    public struct ContinuousAttribute
    {
        public string Name;

        [UnityEngine.Range(0f, 1f)]
        public float Value;
    }


    [Serializable]
    public class AvatarProfile
    {
        public int SchemaVersion = 1;

        public string Id;

        public string AvatarName = "Avatar";

        public string StudentCode = "";

        public string LeitmotivPath = "";

        public List<LayerSelection> Layers = new();

        public List<ContinuousAttribute>
            ContinuousAttributes = new();


        public int GetSpriteIndex(
            string layerName,
            int fallback = 0)
        {
            foreach (var l in Layers)
            {
                if (l.LayerName == layerName)
                    return l.SpriteIndex;
            }

            return fallback;
        }


        // True si esta capa fue marcada explícitamente como "sin prenda"
        // (ej. un avatar secreto sin pelo). Hay que chequear esto ANTES de
        // resolver el sprite con GetSpriteIndex, porque una capa oculta no
        // debe caer en el fallback público.
        public bool IsLayerHidden(string layerName)
        {
            foreach (var l in Layers)
            {
                if (l.LayerName == layerName)
                    return l.Hidden;
            }

            return false;
        }


        public float GetContinuousValue(
            string attributeName,
            float fallback = 0.5f)
        {
            foreach (var a in ContinuousAttributes)
            {
                if (a.Name == attributeName)
                    return a.Value;
            }

            return fallback;
        }
    }
}
