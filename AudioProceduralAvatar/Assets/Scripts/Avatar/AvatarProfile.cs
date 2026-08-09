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

    /// <summary>
    /// Perfil real de un avatar personalizado: se basa en las capas de
    /// sprites que arma AvatarCreator (Body, Head, Hair, ...) en vez de
    /// categorías fijas tipo "ropa"/"accesorio". Así el sistema no depende
    /// de que el número o nombre de las capas se mantenga igual — si mañana
    /// se agrega una capa "Accesorio", no hay que tocar esta clase.
    ///
    /// Esta es también la estructura que se serializa tal cual a JSON
    /// (ver AvatarJsonStorage). SchemaVersion existe para poder cambiar el
    /// formato más adelante sin romper archivos ya guardados.
    /// </summary>
    [Serializable]
    public class AvatarProfile
    {
        public int SchemaVersion = 1;
        public string Id;
        public string AvatarName = "Avatar";
        public string StudentCode = "";
        public List<LayerSelection> Layers = new();

        public int GetSpriteIndex(string layerName, int fallback = 0)
        {
            foreach (var l in Layers)
                if (l.LayerName == layerName) return l.SpriteIndex;
            return fallback;
        }
    }
}
