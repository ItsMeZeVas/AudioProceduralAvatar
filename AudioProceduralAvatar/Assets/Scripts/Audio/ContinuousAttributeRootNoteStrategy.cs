using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Implementación de RootNoteStrategy que deriva la tónica de un
    /// atributo CONTINUO del avatar (ej. tono de piel), en vez de un hash de
    /// las capas discretas. Esta es la pieza que activa la decisión que
    /// habíamos dejado pendiente: "¿de dónde sale la tónica?".
    ///
    /// Para usarla: créala como asset (Create -> AudioProceduralAvatar ->
    /// Root Note Strategy -> By Continuous Attribute), pon "Attribute Name"
    /// igual al que uses en tu SkinToneSelector (por defecto "SkinTone"), y
    /// asígnala en el campo Root Note Strategy del LeitmotivGenerator —
    /// reemplaza a LayerHashRootNoteStrategy sin tocar nada más del sistema.
    /// </summary>
    [CreateAssetMenu(fileName = "ContinuousAttributeRootNoteStrategy", menuName = "AudioProceduralAvatar/Root Note Strategy/By Continuous Attribute")]
    public class ContinuousAttributeRootNoteStrategy : RootNoteStrategy
    {
        [Tooltip("Debe coincidir con el Attribute Name configurado en el SkinToneSelector (u otro selector continuo que se agregue después).")]
        public string AttributeName = "SkinTone";

        [Tooltip("Si está activo, invierte la dirección (valor bajo -> tónica alta, en vez de baja).")]
        public bool Invert = false;

        [Tooltip("Si el avatar no tiene este atributo guardado, se usa este valor (0.5 = mitad del rango).")]
        [Range(0f, 1f)] public float FallbackValue = 0.5f;

        public override int GetRootMidi(AvatarProfile profile, int minRoot, int maxRoot)
        {
            float value = profile.GetContinuousValue(AttributeName, FallbackValue);
            if (Invert) value = 1f - value;

            int range = Mathf.Max(1, maxRoot - minRoot);
            return minRoot + Mathf.RoundToInt(value * range);
        }
    }
}
