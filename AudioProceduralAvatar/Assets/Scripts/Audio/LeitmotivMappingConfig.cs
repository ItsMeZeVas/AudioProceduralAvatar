using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    [Serializable]
    public struct TraitScaleMapping
    {
        public CharacterTrait Trait;
        public MusicalScale Scale;
    }

    [Serializable]
    public struct AccessoryTempoMapping
    {
        public AccessoryType Accessory;
        [Range(60f, 180f)] public float TempoBpm;
    }

    [Serializable]
    public struct ClothingInstrumentMapping
    {
        public ClothingType Clothing;
        [Tooltip("Debe coincidir con el PresetId de un InstrumentPreset existente.")]
        public string InstrumentPresetId;
    }

    /// <summary>
    /// Todas las reglas "atributo -> decisión musical" en un solo asset editable.
    /// Diseño puede crear este asset (Create -> AudioProceduralAvatar ->
    /// Leitmotiv Mapping Config), llenar las listas en el Inspector, y
    /// LeitmotivGenerator las usa directamente. Sin recompilar nada.
    ///
    /// Si un atributo no está en la lista, se usa el valor Default
    /// correspondiente (para que nunca falte un mapeo y explote en runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "LeitmotivMappingConfig", menuName = "AudioProceduralAvatar/Leitmotiv Mapping Config")]
    public class LeitmotivMappingConfig : ScriptableObject
    {
        [Header("Trait -> Escala")]
        public List<TraitScaleMapping> TraitScaleMappings = new();
        public MusicalScale DefaultScale = MusicalScale.Mayor;

        [Header("Accesorio -> Tempo")]
        public List<AccessoryTempoMapping> AccessoryTempoMappings = new();
        public float DefaultTempoBpm = 100f;

        [Header("Ropa -> Instrumento")]
        public List<ClothingInstrumentMapping> ClothingInstrumentMappings = new();
        public string DefaultInstrumentPresetId = "pluck";

        [Header("Rango de tónica permitido (evita que suene fuera de rango)")]
        public int MinRootMidi = 48; // C3
        public int MaxRootMidi = 60; // C4

        public MusicalScale GetScale(CharacterTrait trait)
        {
            foreach (var m in TraitScaleMappings)
                if (m.Trait == trait) return m.Scale;
            return DefaultScale;
        }

        public float GetTempo(AccessoryType accessory)
        {
            foreach (var m in AccessoryTempoMappings)
                if (m.Accessory == accessory) return m.TempoBpm;
            return DefaultTempoBpm;
        }

        public string GetInstrumentPresetId(ClothingType clothing)
        {
            foreach (var m in ClothingInstrumentMappings)
                if (m.Clothing == clothing) return m.InstrumentPresetId;
            return DefaultInstrumentPresetId;
        }
    }
}
