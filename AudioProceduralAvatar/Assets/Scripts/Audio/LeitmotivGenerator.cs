using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Genera el leitmotiv a partir del perfil del avatar.
    ///
    /// Las decisiones musicales se obtienen de LeitmotivMappingConfig:
    ///
    /// Hair       -> Scale
    /// SkinTone   -> Tempo
    /// Eyes       -> Instrument
    /// UpperBody  -> ADSR
    /// LowerBody  -> Rhythm
    /// Accessories-> Dynamics
    /// </summary>
    public class LeitmotivGenerator : MonoBehaviour
    {
        [SerializeField]
        private LeitmotivMappingConfig mappingConfig;

        [Tooltip(
            "Determina la tónica. Si está vacío se utiliza el fallback."
        )]
        [SerializeField]
        private RootNoteStrategy rootNoteStrategy;

        [Header("Longitud del motivo")]
        [SerializeField]
        private int noteCount = 6;


        public LeitmotivData Generate(AvatarProfile profile)
        {
            int minRoot = mappingConfig != null
                ? mappingConfig.MinRootMidi
                : 48;

            int maxRoot = mappingConfig != null
                ? mappingConfig.MaxRootMidi
                : 60;


            // ----------------------------------------------------
            // DECISIONES MUSICALES
            // ----------------------------------------------------

            MusicalScale scale = mappingConfig != null
                ? mappingConfig.GetScale(profile)
                : MusicalScale.Mayor;


            float tempo = mappingConfig != null
                ? mappingConfig.GetTempo(profile)
                : 100f;


            string instrument = mappingConfig != null
                ? mappingConfig.GetInstrumentPresetId(profile)
                : "pluck";


            RhythmPattern rhythm = mappingConfig != null
                ? mappingConfig.GetRhythm(profile)
                : RhythmPattern.Balanced;


            float dynamics = mappingConfig != null
                ? mappingConfig.GetDynamicMultiplier(profile)
                : 1f;


            // ----------------------------------------------------
            // ADSR
            // ----------------------------------------------------

            float attack = 0.01f;
            float decay = 0.1f;
            float sustain = 0.7f;
            float release = 0.15f;

            bool hasMappedEnvelope = false;

            if (mappingConfig != null)
            {
                hasMappedEnvelope =
                    mappingConfig.GetEnvelope(
                        profile,
                        out attack,
                        out decay,
                        out sustain,
                        out release
                    );
            }


            // ----------------------------------------------------
            // TÓNICA
            // ----------------------------------------------------

            int rootNote =
                rootNoteStrategy != null
                    ? rootNoteStrategy.GetRootMidi(
                        profile,
                        minRoot,
                        maxRoot
                    )
                    : FallbackHashRoot(
                        profile,
                        minRoot,
                        maxRoot
                    );


            // ----------------------------------------------------
            // DATA
            // ----------------------------------------------------

            var data = new LeitmotivData
            {
                OwnerAvatarName = profile.AvatarName,

                Scale = scale,

                RootNoteMidi = rootNote,

                TempoBpm = tempo,

                InstrumentHint = instrument,

                TimbreVariation = ComputeTimbreVariation(profile),

                HasMappedEnvelope = hasMappedEnvelope,

                Attack = attack,
                Decay = decay,
                Sustain = sustain,
                Release = release,

                Rhythm = rhythm,

                DynamicMultiplier = dynamics,

                Notes = GenerateNotes(
                    profile,
                    rhythm,
                    dynamics
                )
            };

            return data;
        }


        // ========================================================
        // TÓNICA FALLBACK
        // ========================================================

        private int FallbackHashRoot(
            AvatarProfile profile,
            int minRoot,
            int maxRoot)
        {
            int hash =
                !string.IsNullOrEmpty(profile.Id)
                    ? profile.Id.GetHashCode()
                    : profile.AvatarName.GetHashCode();

            int range = Mathf.Max(
                1,
                maxRoot - minRoot
            );

            return minRoot +
                   Mathf.Abs(hash) %
                   (range + 1);
        }


        // ========================================================
        // VARIACIÓN DE TIMBRE
        // ========================================================

        private float ComputeTimbreVariation(
            AvatarProfile profile)
        {
            int hash = 7;

            foreach (var layer in profile.Layers)
            {
                hash =
                    hash * 13 +
                    layer.LayerName.GetHashCode() * 7 +
                    layer.SpriteIndex * 31;
            }

            foreach (var attr in profile.ContinuousAttributes)
            {
                hash =
                    hash * 19 +
                    attr.Name.GetHashCode() * 3 +
                    Mathf.RoundToInt(attr.Value * 1000);
            }

            if (!string.IsNullOrEmpty(profile.Id))
            {
                hash =
                    hash * 17 +
                    profile.Id.GetHashCode();
            }

            uint u = unchecked((uint)hash);

            return (u % 1000) / 1000f;
        }


        // ========================================================
        // GENERACIÓN DE NOTAS
        // ========================================================

        private List<NoteEvent> GenerateNotes(
            AvatarProfile profile,
            RhythmPattern rhythm,
            float dynamics)
        {
            var notes = new List<NoteEvent>();

            string seedSource =
                !string.IsNullOrEmpty(profile.Id)
                    ? profile.Id
                    : profile.AvatarName;

            var rnd =
                new System.Random(
                    seedSource.GetHashCode()
                );


            int[] stepChoices =
            {
                -2,
                -1,
                -1,
                0,
                1,
                1,
                2
            };


            int currentDegree = 0;

            float beat = 0f;


            for (int i = 0; i < noteCount; i++)
            {
                bool isLastNote =
                    i == noteCount - 1;

                int degree =
                    isLastNote
                        ? 0
                        : currentDegree;


                float duration =
                    PickDuration(
                        rnd,
                        rhythm
                    );


                // Acento básico
                float baseVelocity =
                    (i % 2 == 0)
                        ? 0.85f
                        : 0.65f;


                // Aplicamos la dinámica de accesorios
                float velocity =
                    Mathf.Clamp01(
                        baseVelocity *
                        dynamics
                    );


                notes.Add(
                    new NoteEvent
                    {
                        ScaleDegree = degree,

                        StartBeat = beat,

                        DurationBeats = duration,

                        Velocity = velocity
                    }
                );


                beat += duration;


                if (!isLastNote)
                {
                    int step =
                        stepChoices[
                            rnd.Next(
                                stepChoices.Length
                            )
                        ];

                    currentDegree =
                        Mathf.Clamp(
                            currentDegree + step,
                            -1,
                            7
                        );
                }
            }

            return notes;
        }


        // ========================================================
        // RITMO
        // ========================================================

        private static float PickDuration(
            System.Random rnd,
            RhythmPattern rhythm)
        {
            switch (rhythm)
            {
                case RhythmPattern.Short:

                    // Mayor cantidad de notas cortas
                    int shortRoll = rnd.Next(100);

                    if (shortRoll < 60)
                        return 0.25f;

                    if (shortRoll < 90)
                        return 0.5f;

                    return 0.75f;


                case RhythmPattern.Long:

                    // Motivo más pausado
                    int longRoll = rnd.Next(100);

                    if (longRoll < 45)
                        return 0.5f;

                    if (longRoll < 80)
                        return 0.75f;

                    return 1f;


                case RhythmPattern.Syncopated:

                    // Alternancia de duraciones
                    int syncRoll = rnd.Next(100);

                    if (syncRoll < 30)
                        return 0.25f;

                    if (syncRoll < 70)
                        return 0.75f;

                    return 0.5f;


                case RhythmPattern.Balanced:

                default:

                    int roll = rnd.Next(100);

                    if (roll < 45)
                        return 0.5f;

                    if (roll < 70)
                        return 0.25f;

                    if (roll < 90)
                        return 0.75f;

                    return 1f;
            }
        }
    }
}