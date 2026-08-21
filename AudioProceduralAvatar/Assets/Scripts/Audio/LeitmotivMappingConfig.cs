using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    // ============================================================
    // ESCALA - CABELLO
    // ============================================================

    [Serializable]
    public struct LayerScaleRule
    {
        public string LayerName;
        public int SpriteIndex;
        public MusicalScale Scale;
    }


    // ============================================================
    // TEMPO - COLOR DE PIEL
    // ============================================================

    [Serializable]
    public struct ContinuousTempoRule
    {
        [Tooltip("Nombre del atributo continuo. Ejemplo: SkinTone.")]
        public string AttributeName;

        [Range(0f, 1f)]
        public float MinValue;

        [Range(0f, 1f)]
        public float MaxValue;

        [Range(60f, 180f)]
        public float TempoBpm;
    }


    // ============================================================
    // INSTRUMENTO - OJOS
    // ============================================================

    [Serializable]
    public struct LayerInstrumentRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Tooltip(
            "Debe coincidir exactamente con el PresetId " +
            "de un InstrumentPreset."
        )]
        public string InstrumentPresetId;
    }


    // ============================================================
    // ADSR - ROPA SUPERIOR
    // ============================================================

    [Serializable]
    public struct LayerEnvelopeRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Header("Attack")]
        [Range(0f, 1f)]
        public float Attack;

        [Header("Decay")]
        [Range(0f, 1f)]
        public float Decay;

        [Header("Sustain")]
        [Range(0f, 1f)]
        public float Sustain;

        [Header("Release")]
        [Range(0f, 1f)]
        public float Release;
    }


    // ============================================================
    // RITMO - ROPA INFERIOR
    // ============================================================

    [Serializable]
    public struct LayerRhythmRule
    {
        public string LayerName;
        public int SpriteIndex;

        public RhythmPattern Rhythm;
    }


    // ============================================================
    // DINÁMICA - ACCESORIOS
    // ============================================================

    [Serializable]
    public struct LayerDynamicsRule
    {
        public string LayerName;
        public int SpriteIndex;

        [Range(0f, 2f)]
        [Tooltip(
            "Multiplicador de intensidad. " +
            "1 = normal, 0.5 = suave, 1.5 = fuerte."
        )]
        public float DynamicMultiplier;
    }


    // ============================================================
    // CONFIGURACIÓN PRINCIPAL
    // ============================================================

    [CreateAssetMenu(
        fileName = "LeitmotivMappingConfig",
        menuName = "AudioProceduralAvatar/Leitmotiv Mapping Config"
    )]
    public class LeitmotivMappingConfig : ScriptableObject
    {
        // ========================================================
        // NOMBRES DE LAS CAPAS
        // ========================================================

        [Header("=== CAPAS DEL AVATAR ===")]

        [Tooltip(
            "Nombre exacto de la capa utilizada para determinar la escala."
        )]
        public string HairLayerName = "Hair";

        [Tooltip(
            "Nombre exacto del atributo continuo utilizado para el tempo."
        )]
        public string SkinToneAttributeName = "SkinTone";

        [Tooltip(
            "Nombre exacto de la capa utilizada para determinar el instrumento."
        )]
        public string EyesLayerName = "Eyes";

        [Tooltip(
            "Nombre exacto de la capa utilizada para determinar el ADSR."
        )]
        public string UpperBodyLayerName = "UpperBody";

        [Tooltip(
            "Nombre exacto de la capa utilizada para determinar el ritmo."
        )]
        public string LowerBodyLayerName = "LowerBody";

        [Tooltip(
            "Nombre exacto de la capa utilizada para determinar la dinámica."
        )]
        public string AccessoriesLayerName = "Accessories";


        // ========================================================
        // ESCALA
        // ========================================================

        [Header("=== CABELLO → ESCALA ===")]

        public List<LayerScaleRule> ScaleRules = new();

        public MusicalScale DefaultScale = MusicalScale.Mayor;


        // ========================================================
        // TEMPO
        // ========================================================

        [Header("=== COLOR DE PIEL → TEMPO ===")]

        public List<ContinuousTempoRule> TempoRules = new();

        [Range(60f, 180f)]
        public float DefaultTempoBpm = 100f;


        // ========================================================
        // INSTRUMENTO
        // ========================================================

        [Header("=== OJOS → INSTRUMENTACIÓN ===")]

        public List<LayerInstrumentRule> InstrumentRules = new();

        public string DefaultInstrumentPresetId = "pluck";


        // ========================================================
        // ADSR
        // ========================================================

        [Header("=== ROPA SUPERIOR → ADSR ===")]

        public List<LayerEnvelopeRule> EnvelopeRules = new();

        [Header("ADSR por defecto")]

        [Range(0f, 1f)]
        public float DefaultAttack = 0.01f;

        [Range(0f, 1f)]
        public float DefaultDecay = 0.1f;

        [Range(0f, 1f)]
        public float DefaultSustain = 0.7f;

        [Range(0f, 1f)]
        public float DefaultRelease = 0.15f;


        // ========================================================
        // RITMO
        // ========================================================

        [Header("=== ROPA INFERIOR → RITMO ===")]

        public List<LayerRhythmRule> RhythmRules = new();

        public RhythmPattern DefaultRhythm = RhythmPattern.Balanced;


        // ========================================================
        // DINÁMICA
        // ========================================================

        [Header("=== ACCESORIOS → DINÁMICA ===")]

        public List<LayerDynamicsRule> DynamicsRules = new();

        [Range(0f, 2f)]
        public float DefaultDynamicMultiplier = 1f;


        // ========================================================
        // TÓNICA
        // ========================================================

        [Header("=== RANGO DE TÓNICA ===")]

        public int MinRootMidi = 48;
        public int MaxRootMidi = 60;


        // ========================================================
        // GET SCALE
        // ========================================================

        public MusicalScale GetScale(AvatarProfile profile)
        {
            foreach (var rule in ScaleRules)
            {
                if (MatchesLayer(
                    profile,
                    HairLayerName,
                    rule.SpriteIndex,
                    rule.LayerName))
                {
                    return rule.Scale;
                }
            }

            return DefaultScale;
        }


        // ========================================================
        // GET TEMPO
        // ========================================================

        public float GetTempo(AvatarProfile profile)
        {
            float value = profile.GetContinuousValue(
                SkinToneAttributeName,
                0.5f
            );

            foreach (var rule in TempoRules)
            {
                if (rule.AttributeName != SkinToneAttributeName)
                    continue;

                if (value >= rule.MinValue &&
                    value <= rule.MaxValue)
                {
                    return rule.TempoBpm;
                }
            }

            return DefaultTempoBpm;
        }


        // ========================================================
        // GET INSTRUMENT
        // ========================================================

        public string GetInstrumentPresetId(AvatarProfile profile)
        {
            foreach (var rule in InstrumentRules)
            {
                if (MatchesLayer(
                    profile,
                    EyesLayerName,
                    rule.SpriteIndex,
                    rule.LayerName))
                {
                    return rule.InstrumentPresetId;
                }
            }

            return DefaultInstrumentPresetId;
        }


        // ========================================================
        // GET ADSR
        // ========================================================

        public bool GetEnvelope(
            AvatarProfile profile,
            out float attack,
            out float decay,
            out float sustain,
            out float release)
        {
            foreach (var rule in EnvelopeRules)
            {
                if (MatchesLayer(
                    profile,
                    UpperBodyLayerName,
                    rule.SpriteIndex,
                    rule.LayerName))
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
        // GET RITMO
        // ========================================================

        public RhythmPattern GetRhythm(AvatarProfile profile)
        {
            foreach (var rule in RhythmRules)
            {
                if (MatchesLayer(
                    profile,
                    LowerBodyLayerName,
                    rule.SpriteIndex,
                    rule.LayerName))
                {
                    return rule.Rhythm;
                }
            }

            return DefaultRhythm;
        }


        // ========================================================
        // GET DINÁMICA
        // ========================================================

        public float GetDynamicMultiplier(AvatarProfile profile)
        {
            foreach (var rule in DynamicsRules)
            {
                if (MatchesLayer(
                    profile,
                    AccessoriesLayerName,
                    rule.SpriteIndex,
                    rule.LayerName))
                {
                    return rule.DynamicMultiplier;
                }
            }

            return DefaultDynamicMultiplier;
        }


        // ========================================================
        // MATCH DE CAPAS
        // ========================================================

        private static bool MatchesLayer(
            AvatarProfile profile,
            string configuredLayerName,
            int spriteIndex,
            string ruleLayerName)
        {
            // El nombre definido en la regla debe coincidir
            // con el nombre configurado para esa característica.
            if (ruleLayerName != configuredLayerName)
                return false;

            foreach (var layer in profile.Layers)
            {
                if (layer.LayerName == configuredLayerName &&
                    layer.SpriteIndex == spriteIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}