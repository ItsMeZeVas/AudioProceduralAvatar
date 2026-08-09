using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Fuente de la nota tónica del leitmotiv. Se deja como ScriptableObject
    /// abstracto A PROPÓSITO: todavía no está decidido de dónde sale la
    /// tónica (¿de las capas elegidas? ¿de un color si se agrega selector?
    /// ¿de otra cosa?). Cualquier implementación futura solo necesita
    /// heredar de esta clase y asignarse en el Inspector del
    /// LeitmotivGenerator — cero cambios en el resto del sistema.
    /// </summary>
    public abstract class RootNoteStrategy : ScriptableObject
    {
        public abstract int GetRootMidi(AvatarProfile profile, int minRoot, int maxRoot);
    }
}
