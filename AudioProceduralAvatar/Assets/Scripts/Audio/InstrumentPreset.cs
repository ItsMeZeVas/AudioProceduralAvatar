using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    public enum WaveformType { Sine, Square, Sawtooth, Triangle }

    /// <summary>
    /// Un "timbre" completo. Diseño crea uno de estos por cada sonido que
    /// quiera tener disponible (clic derecho en Project -> Create ->
    /// AudioProceduralAvatar -> Instrument Preset), le pone el nombre que
    /// use en LeitmotivMappingConfig, y ajusta los sliders. No requiere código.
    /// </summary>
    [CreateAssetMenu(fileName = "NewInstrumentPreset", menuName = "AudioProceduralAvatar/Instrument Preset")]
    public class InstrumentPreset : ScriptableObject
    {
        [Tooltip("Debe coincidir con el InstrumentHint que genera LeitmotivGenerator (ver LeitmotivMappingConfig).")]
        public string PresetId = "pluck";

        public WaveformType Waveform = WaveformType.Sine;

        [Range(0f, 1f)] public float Volume = 0.6f;

        [Header("Envolvente ADSR (segundos, excepto Sustain que es nivel 0-1)")]
        [Range(0f, 1f)] public float Attack = 0.01f;
        [Range(0f, 1f)] public float Decay = 0.1f;
        [Range(0f, 1f)] public float Sustain = 0.7f;
        [Range(0f, 1f)] public float Release = 0.15f;
    }
}
