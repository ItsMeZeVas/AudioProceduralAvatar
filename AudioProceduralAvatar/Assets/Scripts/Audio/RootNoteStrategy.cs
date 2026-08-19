using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Fuente de la nota tónica del leitmotiv. Se deja como ScriptableObject
    /// abstracto A PROPÓSITO: la fuente real puede cambiar (hash de capas,
    /// un atributo continuo como el tono de piel, o algo distinto en el
    /// futuro). Cualquier implementación nueva solo necesita heredar de esta
    /// clase y asignarse en el Inspector del LeitmotivGenerator — cero
    /// cambios en el resto del sistema.
    /// </summary>
    public abstract class RootNoteStrategy : ScriptableObject
    {
        public abstract int GetRootMidi(AvatarProfile profile, int minRoot, int maxRoot);
    }
}
