using UnityEngine;

public enum AvatarSoundType
{
    Select,
    AcceptChanges,
    Undo,

    KeyboardTyping,
    KeyboardBackspace,

    Delete,
    ColorSlider,

    PagePrevious,
    PageNext,

    QRSuccess
}

// =====================================================
// ENVOLVENTE ADSR AUXILIAR
// =====================================================

[System.Serializable]
public class EnvelopeSettings
{
    public float attack = 0.001f;
    public float decay = 0.03f;

    [Range(0f, 1f)]
    public float sustain = 0f;

    public float release = 0.02f;
}

// =====================================================
// LFO
// =====================================================

[System.Serializable]
public class LfoSettings
{
    public WaveType wave = WaveType.Square;
    public float rateHz = 30f;
}

// =====================================================
// AUDIO PRESET
// =====================================================

[CreateAssetMenu(
    fileName = "AudioPreset",
    menuName = "Avatar Audio/Audio Preset"
)]
public class AudioPreset : ScriptableObject
{
    [Header("General")]

    public AvatarSoundType soundType;

    [Range(0.01f, 2f)]
    public float volume = 1f;

    // =====================================================
    // OSCILLATOR A
    // =====================================================

    [Header("Oscillator A")]

    public WaveType oscillatorA = WaveType.Sine;

    public float oscillatorASemitones = 0f;

    public float oscillatorADb = -5f;

    // =====================================================
    // OSCILLATOR B
    // =====================================================

    [Header("Oscillator B")]

    public WaveType oscillatorB = WaveType.Triangle;

    public float oscillatorBSemitones = 12f;

    public float oscillatorBDb = -13f;

    // =====================================================
    // OSCILLATOR C
    // =====================================================

    [Header("Oscillator C")]

    public WaveType oscillatorC = WaveType.Square;

    public float oscillatorCSemitones = 19f;

    public float oscillatorCDb = -33f;

    // =====================================================
    // OSCILLATOR D
    // =====================================================

    [Header("Oscillator D (solo modulador)")]

    public bool useOscillatorD = false;

    public WaveType oscillatorD = WaveType.Triangle;

    public float oscillatorDSemitones = 0f;

    // =====================================================
    // PHASE MODULATION
    // =====================================================

    [Header("Phase Modulation")]

    [Tooltip("B modula la fase de A.")]
    public bool usePhaseModulation = false;

    [Tooltip("Cantidad B → A.")]
    [Range(0f, 1f)]
    public float phaseModAmount = 0.1f;

    [Tooltip("C modula la fase de B.")]
    [Range(0f, 1f)]
    public float phaseModCToB = 0f;

    [Tooltip("D modula la fase de A.")]
    [Range(0f, 1f)]
    public float phaseModDToA = 0f;

    // =====================================================
    // ADSR PRINCIPAL
    // =====================================================

    [Header("Amplitude Envelope (MAIN)")]

    public float attack = 0.008f;

    public float decay = 0.08f;

    [Range(0f, 1f)]
    public float sustain = 0.55f;

    public float release = 0.12f;

    // =====================================================
    // ENVOLVENTES AUXILIARES
    // =====================================================

    [Header("Envelopes auxiliares")]

    public EnvelopeSettings env2 =
        new EnvelopeSettings();

    public EnvelopeSettings env3 =
        new EnvelopeSettings();

    // =====================================================
    // LFO
    // =====================================================

    [Header("LFOs")]

    public LfoSettings lfo1 =
        new LfoSettings();

    public LfoSettings lfo2 =
        new LfoSettings();

    [Tooltip(
        "Hace que los LFO comiencen en una fase diferente " +
        "cada vez que se genera el sonido."
    )]
    public bool randomizeLfoPhase = false;

    // =====================================================
    // AMPLITUDE MODULATION
    // =====================================================

    [Header("Amplitude Modulation")]

    [Range(0f, 1f)]
    public float ampModEnv3ToA = 0f;

    [Range(0f, 1f)]
    public float ampModLfo1ToC = 0f;

    [Range(0f, 1f)]
    public float ampModMainToD = 0f;

    // =====================================================
    // PITCH ENVELOPE
    // =====================================================

    [Header("Pitch Envelope")]

    public bool pitchEnvelope = false;

    [Tooltip("Desplazamiento inicial en semitonos.")]
    public float pitchAmount = 0f;

    [Tooltip("Semitonos al terminar el Attack.")]
    public float pitchAttackLevel = 0f;

    [Tooltip("Semitonos al terminar el Decay.")]
    public float pitchEndAmount = 0f;

    [Tooltip("Duración del Pitch Attack.")]
    public float pitchAttack = 0.005f;

    [Tooltip("Duración del Pitch Decay.")]
    public float pitchDecay = 0.04f;

    // =====================================================
    // FILTER
    // =====================================================

    [Header("Filter")]

    public bool useFilter = true;

    [Range(100f, 10000f)]
    public float filterCutoff = 2500f;

    [Range(0f, 1f)]
    public float filterResonance = 0.3f;

    [Tooltip(
        "Activado = Low Pass resonante de 12 dB/oct."
    )]
    public bool resonantFilter = false;

    // =====================================================
    // FILTER MODULATION
    // =====================================================

    [Header("Filter Modulation")]

    [Range(-1f, 1f)]
    public float filterModMain = 0f;

    [Range(-1f, 1f)]
    public float filterModEnv2 = 0f;

    [Range(-1f, 1f)]
    public float filterModEnv3 = 0f;

    [Range(-1f, 1f)]
    public float filterModLfo1 = 0f;

    [Range(-1f, 1f)]
    public float filterModLfo2 = 0f;

    [Range(0f, 4f)]
    public float filterModOctaves = 1.2f;

    // =====================================================
    // EQ
    // =====================================================

    [Header("EQ")]

    public bool useEQ = false;

    public float lowShelfDb = 0f;

    public float lowMidDb = 0f;

    public float highMidDb = 0f;
}

// =====================================================
// TIPOS DE ONDA
// =====================================================

public enum WaveType
{
    Sine,
    Triangle,
    Square,
    Saw
}