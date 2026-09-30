using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    public class LeitmotivGenerator : MonoBehaviour
    {
        [SerializeField]
        private LeitmotivMappingConfig mappingConfig;

        [SerializeField]
        private RootNoteStrategy rootNoteStrategy;

        [Header("Generación")]

        [SerializeField]
        [Range(6, 16)]
        private int noteCount = 10;

        [SerializeField]
        private bool preferMappingConfigRoot = true;

        public LeitmotivData Generate(
            AvatarProfile profile)
        {
            // ----------------------------------------------------
            // FALLBACK COMPLETAMENTE VÁLIDO
            // ----------------------------------------------------

            if (profile == null)
            {
                return CreateFallbackData();
            }

            int minRoot =
                mappingConfig != null
                    ? mappingConfig.MinRootMidi
                    : 48;

            int maxRoot =
                mappingConfig != null
                    ? mappingConfig.MaxRootMidi
                    : 60;

            // ----------------------------------------------------
            // ESCALA
            // ----------------------------------------------------

            MusicalScale scale =
                mappingConfig != null
                    ? mappingConfig.GetScale(profile)
                    : MusicalScale.Mayor;

            // ----------------------------------------------------
            // TEMPO
            // ----------------------------------------------------

            float tempo =
                mappingConfig != null
                    ? mappingConfig.GetTempo(profile)
                    : 100f;

            tempo =
                Mathf.Clamp(
                    tempo,
                    72f,
                    132f
                );

            // ----------------------------------------------------
            // INSTRUMENTOS
            // ----------------------------------------------------

            string instrument =
                mappingConfig != null
                    ? mappingConfig.GetInstrumentPresetId(
                        profile)
                    : "warm_pluck";

            string bass =
                mappingConfig != null
                    ? mappingConfig.DefaultBassInstrumentId
                    : "deep_bass";

            string texture =
                mappingConfig != null
                    ? mappingConfig.DefaultTextureInstrumentId
                    : "air_texture";

            string percussion =
                mappingConfig != null
                    ? mappingConfig.DefaultPercussionInstrumentId
                    : "soft_percussion";

            // ----------------------------------------------------
            // RITMO
            // ----------------------------------------------------

            RhythmPattern rhythm =
                mappingConfig != null
                    ? mappingConfig.GetRhythm(profile)
                    : RhythmPattern.Balanced;

            // ----------------------------------------------------
            // DINÁMICA
            // ----------------------------------------------------

            float dynamics =
                mappingConfig != null
                    ? mappingConfig.GetDynamicMultiplier(profile)
                    : 1f;

            dynamics =
                Mathf.Clamp(
                    dynamics,
                    0.72f,
                    1.18f
                );

            // ----------------------------------------------------
            // ADSR
            // ----------------------------------------------------

            float attack = 0.015f;
            float decay = 0.12f;
            float sustain = 0.65f;
            float release = 0.18f;

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

            attack =
                Mathf.Clamp(
                    attack,
                    0.005f,
                    0.8f
                );

            decay =
                Mathf.Clamp(
                    decay,
                    0.02f,
                    0.8f
                );

            sustain =
                Mathf.Clamp01(
                    sustain
                );

            release =
                Mathf.Clamp(
                    release,
                    0.03f,
                    0.8f
                );

            // ----------------------------------------------------
            // TÓNICA
            // ----------------------------------------------------

            int rootNote;

            if (preferMappingConfigRoot &&
                mappingConfig != null)
            {
                rootNote =
                    mappingConfig.GetRootMidi(
                        profile,
                        minRoot,
                        maxRoot
                    );
            }
            else if (rootNoteStrategy != null)
            {
                rootNote =
                    rootNoteStrategy.GetRootMidi(
                        profile,
                        minRoot,
                        maxRoot
                    );
            }
            else
            {
                rootNote =
                    FallbackHashRoot(
                        profile,
                        minRoot,
                        maxRoot
                    );
            }

            rootNote =
                Mathf.Clamp(
                    rootNote,
                    minRoot,
                    maxRoot
                );

            // ----------------------------------------------------
            // TIMBRE
            // ----------------------------------------------------

            float timbreVariation =
                ComputeTimbreVariation(
                    profile
                );

            float brightness =
                mappingConfig != null
                    ? mappingConfig.GetTimbreBrightness(
                        profile)
                    : 0.5f;

            float warmth =
                mappingConfig != null
                    ? mappingConfig.GetWarmth(
                        profile)
                    : 0.5f;

            float articulation =
                mappingConfig != null
                    ? mappingConfig.GetArticulation(
                        profile)
                    : 0.7f;

            float bassLevel =
                mappingConfig != null
                    ? mappingConfig.GetBassLevel(
                        profile)
                    : 0.4f;

            float textureLevel =
                mappingConfig != null
                    ? mappingConfig.GetTextureLevel(
                        profile)
                    : 0.25f;

            float percussionLevel =
                mappingConfig != null
                    ? mappingConfig.GetPercussionLevel(
                        profile)
                    : 0.25f;

            float harmonicAmount =
                Mathf.Clamp01(
                    0.35f +
                    timbreVariation * 0.25f
                );

            // ----------------------------------------------------
            // DATA
            // ----------------------------------------------------

            LeitmotivData data =
                new LeitmotivData
                {
                    OwnerAvatarName =
                        string.IsNullOrEmpty(
                            profile.AvatarName)
                            ? "Avatar"
                            : profile.AvatarName,

                    Scale = scale,

                    RootNoteMidi =
                        rootNote,

                    TempoBpm =
                        tempo,

                    InstrumentHint =
                        string.IsNullOrEmpty(
                            instrument)
                            ? "warm_pluck"
                            : instrument,

                    BassInstrumentHint =
                        bass,

                    TextureInstrumentHint =
                        texture,

                    PercussionInstrumentHint =
                        percussion,

                    TimbreVariation =
                        timbreVariation,

                    TimbreBrightness =
                        brightness,

                    HarmonicAmount =
                        harmonicAmount,

                    Warmth =
                        warmth,

                    HasMappedEnvelope =
                        hasMappedEnvelope,

                    Attack =
                        attack,

                    Decay =
                        decay,

                    Sustain =
                        sustain,

                    Release =
                        release,

                    Rhythm =
                        rhythm,

                    DynamicMultiplier =
                        dynamics,

                    BassLevel =
                        bassLevel,

                    TextureLevel =
                        textureLevel,

                    PercussionLevel =
                        percussionLevel,

                    Articulation =
                        articulation,

                    Notes =
                        GenerateNotes(
                            profile,
                            rhythm,
                            dynamics,
                            articulation
                        )
                };

            // Nunca dejamos Notes en null.
            if (data.Notes == null)
            {
                data.Notes =
                    new List<NoteEvent>();
            }

            return data;
        }

        // ========================================================
        // FALLBACK
        // ========================================================

        private LeitmotivData CreateFallbackData()
        {
            return new LeitmotivData
            {
                OwnerAvatarName = "Avatar",

                Scale = MusicalScale.Mayor,

                RootNoteMidi = 57,

                TempoBpm = 100f,

                InstrumentHint = "warm_pluck",

                BassInstrumentHint = "deep_bass",

                TextureInstrumentHint = "air_texture",

                PercussionInstrumentHint =
                    "soft_percussion",

                TimbreVariation = 0.5f,

                TimbreBrightness = 0.5f,

                HarmonicAmount = 0.35f,

                Warmth = 0.5f,

                HasMappedEnvelope = false,

                Attack = 0.015f,

                Decay = 0.12f,

                Sustain = 0.65f,

                Release = 0.18f,

                Rhythm =
                    RhythmPattern.Balanced,

                DynamicMultiplier = 1f,

                BassLevel = 0.4f,

                TextureLevel = 0.25f,

                PercussionLevel = 0.25f,

                Articulation = 0.7f,

                Notes =
                    new List<NoteEvent>
                    {
                        new NoteEvent
                        {
                            ScaleDegree = 0,
                            StartBeat = 0f,
                            DurationBeats = 0.5f,
                            Velocity = 0.9f
                        },

                        new NoteEvent
                        {
                            ScaleDegree = 2,
                            StartBeat = 0.5f,
                            DurationBeats = 0.5f,
                            Velocity = 0.75f
                        },

                        new NoteEvent
                        {
                            ScaleDegree = 4,
                            StartBeat = 1f,
                            DurationBeats = 1f,
                            Velocity = 0.85f
                        },

                        new NoteEvent
                        {
                            ScaleDegree = 0,
                            StartBeat = 2f,
                            DurationBeats = 1f,
                            Velocity = 0.95f
                        }
                    }
            };
        }

        // ========================================================
        // TÓNICA FALLBACK
        // ========================================================

        private int FallbackHashRoot(
            AvatarProfile profile,
            int minRoot,
            int maxRoot)
        {
            string source =
                !string.IsNullOrEmpty(profile.Id)
                    ? profile.Id
                    : profile.AvatarName;

            if (string.IsNullOrEmpty(source))
                source = "Avatar";

            int hash =
                StableHash(source);

            int range =
                Mathf.Max(
                    1,
                    maxRoot - minRoot
                );

            return
                minRoot +
                Mathf.Abs(hash) %
                (range + 1);
        }

        // ========================================================
        // TIMBRE
        // ========================================================

        private float ComputeTimbreVariation(
            AvatarProfile profile)
        {
            int hash = 17;

            if (profile.Layers != null)
            {
                foreach (var layer in profile.Layers)
                {
                    hash =
                        hash * 31 +
                        StableHash(
                            layer.LayerName
                        );

                    hash =
                        hash * 17 +
                        layer.SpriteIndex;
                }
            }

            if (profile.ContinuousAttributes != null)
            {
                foreach (
                    var attribute
                    in profile.ContinuousAttributes)
                {
                    hash =
                        hash * 29 +
                        StableHash(
                            attribute.Name
                        );

                    hash =
                        hash * 13 +
                        Mathf.RoundToInt(
                            attribute.Value * 1000f
                        );
                }
            }

            if (!string.IsNullOrEmpty(profile.Id))
            {
                hash =
                    hash * 23 +
                    StableHash(profile.Id);
            }

            uint value =
                unchecked((uint)hash);

            return
                (value % 1000u) /
                1000f;
        }

        // ========================================================
        // NOTAS
        // ========================================================

        private List<NoteEvent> GenerateNotes(
            AvatarProfile profile,
            RhythmPattern rhythm,
            float dynamics,
            float articulation)
        {
            List<NoteEvent> notes =
                new List<NoteEvent>();

            string seedSource =
                !string.IsNullOrEmpty(profile.Id)
                    ? profile.Id
                    : profile.AvatarName;

            if (string.IsNullOrEmpty(seedSource))
                seedSource = "Avatar";

            int seed =
                StableHash(seedSource);

            var rnd =
                new System.Random(seed);

            int[] stepChoices =
            {
                -2,
                -1,
                0,
                1,
                1,
                2,
                3,
                -3
            };

            int currentDegree = 0;

            float beat = 0f;

            int count =
                Mathf.Clamp(
                    noteCount,
                    6,
                    16
                );

            for (int i = 0; i < count; i++)
            {
                bool last =
                    i == count - 1;

                int degree =
                    last
                        ? 0
                        : currentDegree;

                float duration =
                    PickDuration(
                        rnd,
                        rhythm
                    );

                // Algunas notas entran un poco antes
                // para evitar un patrón mecánico.
                float microVariation =
                    articulation > 0.75f &&
                    !last &&
                    rnd.Next(100) < 18
                        ? 0.125f
                        : 0f;

                float velocity;

                if (i % 4 == 0)
                {
                    velocity = 0.92f;
                }
                else if (i % 2 == 0)
                {
                    velocity = 0.82f;
                }
                else
                {
                    velocity = 0.72f;
                }

                velocity +=
                    (float)
                    rnd.NextDouble() *
                    0.08f;

                velocity *=
                    Mathf.Lerp(
                        0.92f,
                        1.05f,
                        dynamics
                    );

                velocity =
                    Mathf.Clamp(
                        velocity,
                        0.58f,
                        1f
                    );

                notes.Add(
                    new NoteEvent
                    {
                        ScaleDegree =
                            degree,

                        StartBeat =
                            beat + microVariation,

                        DurationBeats =
                            duration,

                        Velocity =
                            velocity
                    }
                );

                beat += duration;

                // Pequeñas respiraciones.
                if (
                    rhythm ==
                    RhythmPattern.Syncopated &&
                    rnd.Next(100) < 25)
                {
                    beat += 0.125f;
                }

                if (!last)
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
                            -2,
                            7
                        );
                }
            }

            return notes;
        }

        // ========================================================
        // DURACIONES
        // ========================================================

        private static float PickDuration(
            System.Random rnd,
            RhythmPattern rhythm)
        {
            int roll =
                rnd.Next(100);

            switch (rhythm)
            {
                case RhythmPattern.Short:

                    if (roll < 50)
                        return 0.25f;

                    if (roll < 85)
                        return 0.5f;

                    return 0.75f;

                case RhythmPattern.Long:

                    if (roll < 30)
                        return 0.5f;

                    if (roll < 70)
                        return 0.75f;

                    return 1f;

                case RhythmPattern.Syncopated:

                    if (roll < 35)
                        return 0.25f;

                    if (roll < 70)
                        return 0.5f;

                    return 0.75f;

                default:

                    if (roll < 35)
                        return 0.5f;

                    if (roll < 65)
                        return 0.25f;

                    if (roll < 90)
                        return 0.75f;

                    return 1f;
            }
        }

        // ========================================================
        // HASH DETERMINISTA
        // ========================================================

        private static int StableHash(
            string text)
        {
            unchecked
            {
                int hash = 23;

                if (string.IsNullOrEmpty(text))
                    return hash;

                for (int i = 0; i < text.Length; i++)
                {
                    hash =
                        hash * 31 +
                        text[i];
                }

                return hash;
            }
        }
    }
}