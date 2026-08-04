using System;
using UnityEngine;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Representa un avatar ya creado, con su leitmotiv ya generado, listo
    /// para vivir en la galería. Es el "expediente completo" de un personaje:
    /// junta lo que eligió el participante (Attributes) con lo que decidió
    /// el motor musical (Leitmotiv). No tiene lógica, solo datos + identidad.
    /// </summary>
    [Serializable]
    public class AvatarInstance
    {
        public string Id; // GUID, para no depender del nombre (puede repetirse)
        public AvatarAttributes Attributes;
        public LeitmotivData Leitmotiv;
        public DateTime CreatedAt;

        public AvatarInstance(AvatarAttributes attrs, LeitmotivData leitmotiv)
        {
            Id = Guid.NewGuid().ToString();
            Attributes = attrs;
            Leitmotiv = leitmotiv;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
