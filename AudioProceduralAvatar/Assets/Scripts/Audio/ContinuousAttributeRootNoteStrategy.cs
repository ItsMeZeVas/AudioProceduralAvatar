using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Convierte un atributo continuo en una tónica MIDI.
    ///
    /// Es opcional dentro del sistema.
    /// La asignación principal del proyecto utiliza:
    ///
    /// Hair        -> Scale
    /// SkinTone    -> Tempo
    /// Eyes        -> Instrument
    /// UpperBody   -> ADSR
    /// LowerBody   -> Rhythm
    /// Accessories -> Dynamics
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContinuousAttributeRootNoteStrategy",
        menuName = "AudioProceduralAvatar/Root Note Strategy/By Continuous Attribute"
    )]
    public class ContinuousAttributeRootNoteStrategy
        : RootNoteStrategy
    {
        [Tooltip(
            "Nombre del atributo continuo."
        )]
        public string AttributeName = "SkinTone";

        public bool Invert = false;

        [Range(0f, 1f)]
        public float FallbackValue = 0.5f;


        public override int GetRootMidi(
            AvatarProfile profile,
            int minRoot,
            int maxRoot)
        {
            float value =
                profile.GetContinuousValue(
                    AttributeName,
                    FallbackValue
                );


            if (Invert)
                value = 1f - value;


            int range =
                Mathf.Max(
                    1,
                    maxRoot - minRoot
                );


            return minRoot +
                   Mathf.RoundToInt(
                       value * range
                   );
        }
    }
}