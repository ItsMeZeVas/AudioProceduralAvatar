using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    [Serializable]
    public struct LayerScaleRule
    {
        public string LayerName;
        public int SpriteIndex;
        public MusicalScale Scale;
    }

    [Serializable]
    public struct LayerTempoRule
    {
        public string LayerName;
        public int SpriteIndex;
        [Range(60f, 180f)] public float TempoBpm;
    }

    [Serializable]
    public struct LayerInstrumentRule
    {
        public string LayerName;
        public int SpriteIndex;
        [Tooltip("Debe coincidir con el PresetId de un InstrumentPreset existente.")]
        public string InstrumentPresetId;
    }

    /// <summary>
    /// Reglas "capa + índice de sprite -> decisión musical", editables por
    /// Diseño en el Inspector. No asume cuáles capas existen (hoy: Body,
    /// Head, Hair) — cada regla dice explícitamente a qué capa e índice
    /// aplica, así que agregar/quitar capas después no rompe nada aquí.
    /// Si ninguna regla matchea ninguna capa del avatar, se usa el Default.
    /// </summary>
    [CreateAssetMenu(fileName = "LeitmotivMappingConfig", menuName = "AudioProceduralAvatar/Leitmotiv Mapping Config")]
    public class LeitmotivMappingConfig : ScriptableObject
    {
        [Header("Reglas de escala (capa + índice -> escala)")]
        public List<LayerScaleRule> ScaleRules = new();
        public MusicalScale DefaultScale = MusicalScale.Mayor;

        [Header("Reglas de tempo (capa + índice -> tempo)")]
        public List<LayerTempoRule> TempoRules = new();
        public float DefaultTempoBpm = 100f;

        [Header("Reglas de instrumento (capa + índice -> preset)")]
        public List<LayerInstrumentRule> InstrumentRules = new();
        public string DefaultInstrumentPresetId = "pluck";

        [Header("Rango de tónica permitido (lo usa RootNoteStrategy)")]
        public int MinRootMidi = 48;
        public int MaxRootMidi = 60;

        public MusicalScale GetScale(AvatarProfile profile)
        {
            foreach (var rule in ScaleRules)
                if (Matches(profile, rule.LayerName, rule.SpriteIndex)) return rule.Scale;
            return DefaultScale;
        }

        public float GetTempo(AvatarProfile profile)
        {
            foreach (var rule in TempoRules)
                if (Matches(profile, rule.LayerName, rule.SpriteIndex)) return rule.TempoBpm;
            return DefaultTempoBpm;
        }

        public string GetInstrumentPresetId(AvatarProfile profile)
        {
            foreach (var rule in InstrumentRules)
                if (Matches(profile, rule.LayerName, rule.SpriteIndex)) return rule.InstrumentPresetId;
            return DefaultInstrumentPresetId;
        }

        private static bool Matches(AvatarProfile profile, string layerName, int spriteIndex)
        {
            foreach (var l in profile.Layers)
                if (l.LayerName == layerName && l.SpriteIndex == spriteIndex) return true;
            return false;
        }
    }
}
