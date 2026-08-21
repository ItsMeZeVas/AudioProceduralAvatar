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

    /// <summary>
    /// Define el timbre base de un instrumento.
    ///
    /// Diseño puede crear varios InstrumentPreset desde:
    /// Project -> Create -> AudioProceduralAvatar -> Instrument Preset
    ///
    /// El instrumento es seleccionado principalmente por la característica
    /// "Eyes" del avatar mediante LeitmotivMappingConfig.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewInstrumentPreset",
        menuName = "AudioProceduralAvatar/Instrument Preset"
    )]
    public class InstrumentPreset : ScriptableObject
    {
        [Tooltip(
            "Identificador utilizado por LeitmotivMappingConfig. " +
            "Debe coincidir exactamente."
        )]
        public string PresetId = "pluck";

        [Header("Timbre")]
        public WaveformType Waveform = WaveformType.Sine;

        [Range(0f, 1f)]
        public float Volume = 0.6f;

        [Header("Envolvente ADSR base")]
        [Tooltip("Tiempo de ataque en segundos.")]
        [Range(0f, 1f)]
        public float Attack = 0.01f;

        [Tooltip("Tiempo de decay en segundos.")]
        [Range(0f, 1f)]
        public float Decay = 0.1f;

        [Tooltip("Nivel de sustain de 0 a 1.")]
        [Range(0f, 1f)]
        public float Sustain = 0.7f;

        [Tooltip("Tiempo de release en segundos.")]
        [Range(0f, 1f)]
        public float Release = 0.15f;
    }
}