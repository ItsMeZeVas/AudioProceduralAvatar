using System;
using UnityEngine;

namespace AudioProceduralAvatar.Avatar
{
    // TODO (Diseño): confirmar el set final de variantes por categoría.
    // Estos enums son un punto de partida — ajustar cuando el documento de
    // mapeo atributo→sonido (semana 1-2) quede cerrado.

    public enum ClothingType { Casual, Formal, Deportivo, Fantasia }
    public enum AccessoryType { Ninguno, Sombrero, Lentes, Collar, Mochila }
    public enum CharacterTrait { Alegre, Serio, Misterioso, Energico }

    /// <summary>
    /// Representa las elecciones de personalización de un avatar.
    /// Esta es la ÚNICA fuente de verdad que consume LeitmotivGenerator para
    /// decidir la música. No debe contener lógica, solo datos.
    /// </summary>
    [Serializable]
    public struct AvatarAttributes
    {
        public string AvatarName;
        public ClothingType Clothing;
        public Color AccentColor;
        public AccessoryType Accessory;
        public CharacterTrait Trait;
    }
}
