using System;
using System.Collections.Generic;

namespace AudioProceduralAvatar.Avatar
{
    [Serializable]
    public struct LayerSelection
    {
        public string LayerName;
        public int SpriteIndex;
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