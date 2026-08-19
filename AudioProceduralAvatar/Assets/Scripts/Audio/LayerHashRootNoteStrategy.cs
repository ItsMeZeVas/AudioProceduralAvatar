using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Estrategia basada en las capas discretas (Body/Head/Hair...): deriva
    /// un hash determinista de todas las selecciones de capa del avatar.
    /// Mismo avatar (mismas capas) -> siempre la misma tónica.
    /// </summary>
    [CreateAssetMenu(fileName = "LayerHashRootNoteStrategy", menuName = "AudioProceduralAvatar/Root Note Strategy/Layer Hash")]
    public class LayerHashRootNoteStrategy : RootNoteStrategy
    {
        public override int GetRootMidi(AvatarProfile profile, int minRoot, int maxRoot)
        {
            int hash = 17;
            foreach (var layer in profile.Layers)
                hash = hash * 31 + layer.LayerName.GetHashCode() * 31 + layer.SpriteIndex;

            int range = Mathf.Max(1, maxRoot - minRoot);
            int offset = Mathf.Abs(hash) % (range + 1);
            return minRoot + offset;
        }
    }
}
