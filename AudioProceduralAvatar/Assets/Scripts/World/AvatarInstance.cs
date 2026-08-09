using System;
using UnityEngine;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Representa un avatar ya creado, con su leitmotiv ya generado, listo
    /// para vivir en la galería. Junta el perfil real (Profile) con lo que
    /// decidió el motor musical (Leitmotiv) y, si existe, la imagen
    /// capturada del avatar armado (CapturedImage).
    /// </summary>
    [Serializable]
    public class AvatarInstance
    {
        public AvatarProfile Profile;
        public LeitmotivData Leitmotiv;
        public Sprite CapturedImage;
        public DateTime CreatedAt;

        public AvatarInstance(AvatarProfile profile, LeitmotivData leitmotiv, Sprite capturedImage = null)
        {
            Profile = profile;
            Leitmotiv = leitmotiv;
            CapturedImage = capturedImage;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
