using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    public enum WaveformType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle
    }

    [CreateAssetMenu(
        fileName = "NewInstrumentPreset",
        menuName = "AudioProceduralAvatar/Instrument Preset"
    )]
    public class InstrumentPreset : ScriptableObject
    {
        [Tooltip(
            "Identificador utilizado por LeitmotivMappingConfig."
        )]
        public string PresetId = "pluck";

        [Header("OSCILADOR PRINCIPAL")]

        public WaveformType Waveform = WaveformType.Sine;

        [Range(0f, 1f)]
        public float Volume = 0.68f;

        [Header("SEGUNDO OSCILADOR")]

        public WaveformType SecondaryWaveform = WaveformType.Sine;

        [Range(0f, 1f)]
        public float SecondaryMix = 0.20f;

        [Range(-30f, 30f)]
        public float DetuneCents = 5f;

        [Header("CARÁCTER")]

        [Range(0f, 1f)]
        public float HarmonicAmount = 0.35f;

        [Range(0.05f, 2f)]
        public float Saturation = 0.15f;

        [Header("FILTRO")]

        [Range(200f, 18000f)]
        public float FilterCutoffHz = 8000f;

        [Header("ENVOLVENTE ADSR")]

        [Range(0f, 1f)]
        public float Attack = 0.01f;

        [Range(0f, 1f)]
        public float Decay = 0.1f;

        [Range(0f, 1f)]
        public float Sustain = 0.7f;

        [Range(0f, 1f)]
        public float Release = 0.15f;

        /// <summary>
        /// Crea un preset procedural en memoria.
        /// No necesita existir como asset.
        /// </summary>
        public static InstrumentPreset CreateRuntime(
            string id,
            WaveformType waveform,
            WaveformType secondary,
            float volume,
            float secondaryMix,
            float detune,
            float harmonic,
            float saturation,
            float cutoff,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            InstrumentPreset preset =
                CreateInstance<InstrumentPreset>();

            preset.name = "Runtime_" + id;

            preset.PresetId = id;

            preset.Waveform = waveform;
            preset.SecondaryWaveform = secondary;

            preset.Volume =
                Mathf.Clamp(
                    volume,
                    0.50f,
                    0.82f
                );

            preset.SecondaryMix =
                Mathf.Clamp01(
                    secondaryMix
                );

            preset.DetuneCents =
                Mathf.Clamp(
                    detune,
                    -30f,
                    30f
                );

            preset.HarmonicAmount =
                Mathf.Clamp01(
                    harmonic
                );

            preset.Saturation =
                Mathf.Clamp(
                    saturation,
                    0.05f,
                    2f
                );

            preset.FilterCutoffHz =
                Mathf.Clamp(
                    cutoff,
                    200f,
                    18000f
                );

            preset.Attack =
                Mathf.Clamp01(attack);

            preset.Decay =
                Mathf.Clamp01(decay);

            preset.Sustain =
                Mathf.Clamp01(sustain);

            preset.Release =
                Mathf.Clamp01(release);

            preset.hideFlags =
                HideFlags.HideAndDontSave;

            return preset;
        }
    }
}