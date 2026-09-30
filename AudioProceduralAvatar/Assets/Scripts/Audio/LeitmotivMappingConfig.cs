using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    // ============================================================
    // ESCALA
    // ============================================================

    [Serializable]
    public struct LayerScaleRule
    {
        public string LayerName;
        public int SpriteIndex;
        public MusicalScale Scale;
    }

    // ============================================================
    // TÓNICA
    // ============================================================

    [Serializable]
    public struct LayerRootRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Range(-12, 12)]
        public int SemitoneOffset;
    }

    // ============================================================
    // TEMPO
    // ============================================================

    [Serializable]
    public struct ContinuousTempoRule
    {
        public string AttributeName;

        [Range(0f, 1f)]
        public float MinValue;

        [Range(0f, 1f)]
        public float MaxValue;

        [Range(60f, 180f)]
        public float TempoBpm;
    }

    // ============================================================
    // INSTRUMENTO
    // ============================================================

    [Serializable]
    public struct LayerInstrumentRule
    {
        public string LayerName;

        public int SpriteIndex;

        [InstrumentId]
        public string InstrumentPresetId;
    }

    // ============================================================
    // ADSR
    // ============================================================

    [Serializable]
    public struct LayerEnvelopeRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Range(0f, 1f)]
        public float Attack;

        [Range(0f, 1f)]
        public float Decay;

        [Range(0f, 1f)]
        public float Sustain;

        [Range(0f, 1f)]
        public float Release;
    }

    // ============================================================
    // RITMO
    // ============================================================

    [Serializable]
    public struct LayerRhythmRule
    {
        public string LayerName;
        public int SpriteIndex;

        public RhythmPattern Rhythm;
    }

    // ============================================================
    // DINÁMICA
    // ============================================================

    [Serializable]
    public struct LayerDynamicsRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Range(0.65f, 1.20f)]
        public float DynamicMultiplier;
    }

    // ============================================================
    // CONFIGURACIÓN
    // ============================================================

    [CreateAssetMenu(
        fileName = "LeitmotivMappingConfig",
        menuName = "AudioProceduralAvatar/Leitmotiv Mapping Config"
    )]
    public class LeitmotivMappingConfig
        : ScriptableObject
    {
        // ========================================================
        // CAPAS
        // ========================================================

        [Header("=== CAPAS DEL AVATAR ===")]

        public string HeadLayerName = "Head";

        public string HairLayerName = "Hair";

        public string SkinToneAttributeName =
            "SkinTone";

        public string UpperBodyLayerName =
            "UpperBody";

        public string LowerBodyLayerName =
            "LowerBody";

        public string AccessoriesLayerName =
            "Accessories";

        public string MouthLayerName =
            "SubBoca";

        public string BeardLayerName =
            "SubBarba";

        // ========================================================
        // HEAD -> TÓNICA
        // ========================================================

        [Header("=== CABEZA → TÓNICA ===")]

        public List<LayerRootRule> RootRules =
            new List<LayerRootRule>();

        // ========================================================
        // HAIR -> ESCALA
        // ========================================================

        [Header("=== CABELLO → ESCALA ===")]

        public List<LayerScaleRule> ScaleRules =
            new List<LayerScaleRule>();

        public MusicalScale DefaultScale =
            MusicalScale.Mayor;

        // ========================================================
        // SKIN -> TEMPO
        // ========================================================

        [Header("=== COLOR DE PIEL → TEMPO ===")]

        public List<ContinuousTempoRule> TempoRules =
            new List<ContinuousTempoRule>();

        [Range(60f, 180f)]
        public float DefaultTempoBpm = 100f;

        // ========================================================
        // HEAD -> INSTRUMENTO
        // ========================================================

        [Header("=== CABEZA → INSTRUMENTO PRINCIPAL ===")]

        [Tooltip(
            "El instrumento se selecciona mediante el ID del preset."
        )]
        public List<LayerInstrumentRule> InstrumentRules =
            new List<LayerInstrumentRule>();

        [InstrumentId]
        public string DefaultInstrumentPresetId =
            "warm_pluck";

        // ========================================================
        // UPPER BODY -> ADSR
        // ========================================================

        [Header("=== ROPA SUPERIOR → ADSR ===")]

        public List<LayerEnvelopeRule> EnvelopeRules =
            new List<LayerEnvelopeRule>();

        [Range(0f, 1f)]
        public float DefaultAttack = 0.015f;

        [Range(0f, 1f)]
        public float DefaultDecay = 0.12f;

        [Range(0f, 1f)]
        public float DefaultSustain = 0.65f;

        [Range(0f, 1f)]
        public float DefaultRelease = 0.18f;

        // ========================================================
        // LOWER BODY -> RITMO
        // ========================================================

        [Header("=== ROPA INFERIOR → RITMO ===")]

        public List<LayerRhythmRule> RhythmRules =
            new List<LayerRhythmRule>();

        public RhythmPattern DefaultRhythm =
            RhythmPattern.Balanced;

        // ========================================================
        // ACCESSORIES -> DINÁMICA
        // ========================================================

        [Header("=== ACCESORIOS → DINÁMICA ===")]

        public List<LayerDynamicsRule> DynamicsRules =
            new List<LayerDynamicsRule>();

        [Range(0.65f, 1.20f)]
        public float DefaultDynamicMultiplier = 1f;

        // ========================================================
        // RANGO
        // ========================================================

        [Header("=== RANGO DE TÓNICA ===")]

        public int MinRootMidi = 48;

        public int MaxRootMidi = 60;

        // ========================================================
        // INSTRUMENTACIÓN SECUNDARIA
        // ========================================================

        [Header("=== INSTRUMENTACIÓN SECUNDARIA ===")]

        [InstrumentId]
        public string DefaultBassInstrumentId =
            "deep_bass";

        [InstrumentId]
        public string DefaultTextureInstrumentId =
            "air_texture";

        [InstrumentId]
        public string DefaultPercussionInstrumentId =
            "soft_percussion";

        // ========================================================
        // AUTO CONFIGURACIÓN
        // ========================================================

        private void OnValidate()
        {
            EnsureDefaultRules();
        }

        private void OnEnable()
        {
            EnsureDefaultRules();
        }

        [ContextMenu("Restaurar reglas recomendadas")]
        public void EnsureDefaultRules()
        {
            // ----------------------------------------------------
            // ROOT
            // ----------------------------------------------------

            if (RootRules.Count == 0)
            {
                int[] offsets =
                {
                    -5,
                    -3,
                    -1,
                    2,
                    4,
                    7
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    RootRules.Add(
                        new LayerRootRule
                        {
                            LayerName =
                                HeadLayerName,

                            SpriteIndex = i,

                            SemitoneOffset =
                                offsets[i]
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // SCALE
            // ----------------------------------------------------

            if (ScaleRules.Count == 0)
            {
                MusicalScale[] scales =
                {
                    MusicalScale.Mayor,
                    MusicalScale.Pentatonica,
                    MusicalScale.Dorico,
                    MusicalScale.Lidio,
                    MusicalScale.MenorNatural,
                    MusicalScale.Mayor
                };

                for (int i = 0; i < scales.Length; i++)
                {
                    ScaleRules.Add(
                        new LayerScaleRule
                        {
                            LayerName =
                                HairLayerName,

                            SpriteIndex = i,

                            Scale =
                                scales[i]
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // TEMPO
            // ----------------------------------------------------

            if (TempoRules.Count == 0)
            {
                AddTempoRule(0f, 0.2f, 82f);
                AddTempoRule(0.2f, 0.4f, 92f);
                AddTempoRule(0.4f, 0.6f, 102f);
                AddTempoRule(0.6f, 0.8f, 112f);
                AddTempoRule(0.8f, 1f, 124f);
            }

            // ----------------------------------------------------
            // INSTRUMENTOS
            // ----------------------------------------------------

            if (InstrumentRules.Count == 0)
            {
                string[] instruments =
                {
                    "warm_pluck",
                    "bright_pluck",
                    "harp",
                    "acoustic_piano",
                    "electric_piano",
                    "music_box",
                    "kalimba",
                    "marimba",
                    "celesta",
                    "soft_bell",
                    "chime",
                    "violin",
                    "cello",
                    "string_ensemble",
                    "flute",
                    "clarinet",
                    "organ",
                    "warm_pad",
                    "choir_pad",
                    "dream_pad",
                    "synth_lead",
                    "soft_synth",
                    "deep_bass",
                    "sub_bass",
                    "upright_bass",
                    "pluck_bass",
                    "air_texture",
                    "warm_texture",
                    "dark_texture",
                    "shimmer",
                    "soft_percussion",
                    "kick_soft",
                    "snare_soft",
                    "shaker",
                    "timpani",
                    "bell_low"
                };

                for (
                    int i = 0;
                    i < instruments.Length;
                    i++)
                {
                    InstrumentRules.Add(
                        new LayerInstrumentRule
                        {
                            LayerName =
                                HeadLayerName,

                            SpriteIndex =
                                i,

                            InstrumentPresetId =
                                instruments[i]
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // ADSR
            // ----------------------------------------------------

            if (EnvelopeRules.Count == 0)
            {
                EnvelopeRules.Add(
                    CreateEnvelope(
                        0,
                        0.008f,
                        0.08f,
                        0.75f,
                        0.12f
                    )
                );

                EnvelopeRules.Add(
                    CreateEnvelope(
                        1,
                        0.02f,
                        0.15f,
                        0.65f,
                        0.20f
                    )
                );

                EnvelopeRules.Add(
                    CreateEnvelope(
                        2,
                        0.05f,
                        0.20f,
                        0.80f,
                        0.35f
                    )
                );

                EnvelopeRules.Add(
                    CreateEnvelope(
                        3,
                        0.01f,
                        0.10f,
                        0.55f,
                        0.10f
                    )
                );

                EnvelopeRules.Add(
                    CreateEnvelope(
                        4,
                        0.08f,
                        0.25f,
                        0.85f,
                        0.45f
                    )
                );

                EnvelopeRules.Add(
                    CreateEnvelope(
                        5,
                        0.015f,
                        0.12f,
                        0.70f,
                        0.25f
                    )
                );
            }

            // ----------------------------------------------------
            // RITMO
            // ----------------------------------------------------

            if (RhythmRules.Count == 0)
            {
                RhythmPattern[] rhythms =
                {
                    RhythmPattern.Balanced,
                    RhythmPattern.Short,
                    RhythmPattern.Long,
                    RhythmPattern.Syncopated,
                    RhythmPattern.Short,
                    RhythmPattern.Balanced
                };

                for (int i = 0; i < rhythms.Length; i++)
                {
                    RhythmRules.Add(
                        new LayerRhythmRule
                        {
                            LayerName =
                                LowerBodyLayerName,

                            SpriteIndex = i,

                            Rhythm =
                                rhythms[i]
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // DINÁMICA
            // ----------------------------------------------------

            if (DynamicsRules.Count == 0)
            {
                float[] dynamics =
                {
                    0.78f,
                    0.88f,
                    0.98f,
                    1.05f,
                    1.12f,
                    1.18f
                };

                for (int i = 0; i < dynamics.Length; i++)
                {
                    DynamicsRules.Add(
                        new LayerDynamicsRule
                        {
                            LayerName =
                                AccessoriesLayerName,

                            SpriteIndex = i,

                            DynamicMultiplier =
                                dynamics[i]
                        }
                    );
                }
            }
        }

        private void AddTempoRule(
            float min,
            float max,
            float tempo)
        {
            TempoRules.Add(
                new ContinuousTempoRule
                {
                    AttributeName =
                        SkinToneAttributeName,

                    MinValue = min,

                    MaxValue = max,

                    TempoBpm = tempo
                }
            );
        }

        private LayerEnvelopeRule CreateEnvelope(
            int index,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            return new LayerEnvelopeRule
            {
                LayerName =
                    UpperBodyLayerName,

                SpriteIndex =
                    index,

                Attack =
                    attack,

                Decay =
                    decay,

                Sustain =
                    sustain,

                Release =
                    release
            };
        }

        // ========================================================
        // ROOT
        // ========================================================

        public int GetRootMidi(
            AvatarProfile profile,
            int minRoot,
            int maxRoot)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    HeadLayerName
                );

            foreach (var rule in RootRules)
            {
                if (
                    rule.LayerName ==
                    HeadLayerName &&
                    rule.SpriteIndex ==
                    index
                )
                {
                    return Mathf.Clamp(
                        54 +
                        rule.SemitoneOffset,
                        minRoot,
                        maxRoot
                    );
                }
            }

            return Mathf.Clamp(
                57,
                minRoot,
                maxRoot
            );
        }

        // ========================================================
        // SCALE
        // ========================================================

        public MusicalScale GetScale(
            AvatarProfile profile)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    HairLayerName
                );

            foreach (var rule in ScaleRules)
            {
                if (
                    rule.LayerName ==
                    HairLayerName &&
                    rule.SpriteIndex ==
                    index
                )
                {
                    return rule.Scale;
                }
            }

            return DefaultScale;
        }

        // ========================================================
        // TEMPO
        // ========================================================

        public float GetTempo(
            AvatarProfile profile)
        {
            float value =
                profile.GetContinuousValue(
                    SkinToneAttributeName,
                    0.5f
                );

            foreach (var rule in TempoRules)
            {
                if (
                    rule.AttributeName ==
                    SkinToneAttributeName &&
                    value >= rule.MinValue &&
                    value <= rule.MaxValue
                )
                {
                    return rule.TempoBpm;
                }
            }

            return DefaultTempoBpm;
        }

        // ========================================================
        // INSTRUMENTO
        // ========================================================

        public string GetInstrumentPresetId(
            AvatarProfile profile)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    HeadLayerName
                );

            foreach (var rule in InstrumentRules)
            {
                if (
                    rule.LayerName ==
                    HeadLayerName &&
                    rule.SpriteIndex ==
                    index &&
                    !string.IsNullOrEmpty(
                        rule.InstrumentPresetId
                    )
                )
                {
                    return rule.InstrumentPresetId;
                }
            }

            return DefaultInstrumentPresetId;
        }

        // ========================================================
        // ADSR
        // ========================================================

        public bool GetEnvelope(
            AvatarProfile profile,
            out float attack,
            out float decay,
            out float sustain,
            out float release)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    UpperBodyLayerName
                );

            foreach (var rule in EnvelopeRules)
            {
                if (
                    rule.LayerName ==
                    UpperBodyLayerName &&
                    rule.SpriteIndex ==
                    index
                )
                {
                    attack = rule.Attack;
                    decay = rule.Decay;
                    sustain = rule.Sustain;
                    release = rule.Release;

                    return true;
                }
            }

            attack = DefaultAttack;
            decay = DefaultDecay;
            sustain = DefaultSustain;
            release = DefaultRelease;

            return false;
        }

        // ========================================================
        // RITMO
        // ========================================================

        public RhythmPattern GetRhythm(
            AvatarProfile profile)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    LowerBodyLayerName
                );

            foreach (var rule in RhythmRules)
            {
                if (
                    rule.LayerName ==
                    LowerBodyLayerName &&
                    rule.SpriteIndex ==
                    index
                )
                {
                    return rule.Rhythm;
                }
            }

            return DefaultRhythm;
        }

        // ========================================================
        // DINÁMICA
        // ========================================================

        public float GetDynamicMultiplier(
            AvatarProfile profile)
        {
            int index =
                GetLayerSpriteIndex(
                    profile,
                    AccessoriesLayerName
                );

            foreach (var rule in DynamicsRules)
            {
                if (
                    rule.LayerName ==
                    AccessoriesLayerName &&
                    rule.SpriteIndex ==
                    index
                )
                {
                    return Mathf.Clamp(
                        rule.DynamicMultiplier,
                        0.65f,
                        1.20f
                    );
                }
            }

            return DefaultDynamicMultiplier;
        }

        // ========================================================
        // CARACTERÍSTICAS EXTRA
        // ========================================================

        public float GetTimbreBrightness(
            AvatarProfile profile)
        {
            float r =
                profile.GetContinuousValue(
                    "HairColorR",
                    0.5f
                );

            float g =
                profile.GetContinuousValue(
                    "HairColorG",
                    0.5f
                );

            float b =
                profile.GetContinuousValue(
                    "HairColorB",
                    0.5f
                );

            return Mathf.Clamp01(
                r * 0.25f +
                g * 0.35f +
                b * 0.40f
            );
        }

        public float GetWarmth(
            AvatarProfile profile)
        {
            float r =
                profile.GetContinuousValue(
                    "BeardColorR",
                    0.5f
                );

            float g =
                profile.GetContinuousValue(
                    "BeardColorG",
                    0.5f
                );

            float b =
                profile.GetContinuousValue(
                    "BeardColorB",
                    0.5f
                );

            return Mathf.Clamp01(
                r * 0.50f +
                g * 0.35f +
                b * 0.15f
            );
        }

        public float GetArticulation(
            AvatarProfile profile)
        {
            int mouth =
                GetLayerSpriteIndex(
                    profile,
                    MouthLayerName
                );

            return Mathf.Lerp(
                0.55f,
                0.95f,
                Mathf.InverseLerp(
                    0,
                    5,
                    mouth
                )
            );
        }

        public float GetBassLevel(
            AvatarProfile profile)
        {
            int lower =
                GetLayerSpriteIndex(
                    profile,
                    LowerBodyLayerName
                );

            return Mathf.Lerp(
                0.32f,
                0.55f,
                Mathf.InverseLerp(
                    0,
                    5,
                    lower
                )
            );
        }

        public float GetTextureLevel(
            AvatarProfile profile)
        {
            int beard =
                GetLayerSpriteIndex(
                    profile,
                    BeardLayerName
                );

            return Mathf.Lerp(
                0.18f,
                0.40f,
                Mathf.InverseLerp(
                    0,
                    5,
                    beard
                )
            );
        }

        public float GetPercussionLevel(
            AvatarProfile profile)
        {
            int accessories =
                GetLayerSpriteIndex(
                    profile,
                    AccessoriesLayerName
                );

            return Mathf.Lerp(
                0.18f,
                0.42f,
                Mathf.InverseLerp(
                    0,
                    5,
                    accessories
                )
            );
        }

        // ========================================================
        // UTILIDAD
        // ========================================================

        private static int GetLayerSpriteIndex(
            AvatarProfile profile,
            string layerName)
        {
            if (
                profile == null ||
                profile.Layers == null
            )
            {
                return 0;
            }

            foreach (var layer in profile.Layers)
            {
                if (
                    layer.LayerName ==
                    layerName
                )
                {
                    return Mathf.Max(
                        0,
                        layer.SpriteIndex
                    );
                }
            }

            return 0;
        }
    }
}