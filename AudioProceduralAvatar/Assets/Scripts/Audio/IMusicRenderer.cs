using System;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Contrato para "cómo suena" el leitmotiv, separado de "qué se toca" (LeitmotivGenerator).
    /// Implementación inicial: SimpleSynthRenderer (osciladores en C#, sin dependencias externas).
    /// Implementación futura: FMODRenderer (cuando se integre FMOD Studio).
    /// Cambiar de motor de audio NO debería requerir tocar LeitmotivGenerator ni AvatarAttributes.
    /// </summary>
    public interface IMusicRenderer
    {
        void PlayLeitmotiv(LeitmotivData data);
        void Stop();
    }
}
