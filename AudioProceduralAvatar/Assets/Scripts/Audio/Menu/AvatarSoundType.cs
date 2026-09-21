using UnityEngine;

public enum AvatarSoundType
{
    Select,
    AcceptChanges,
    Undo,
    KeyboardTyping      // NUEVO (valor 3)
}

// =====================================================
// Envolvente ADSR auxiliar (ENV2 / ENV3)
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
// LFO simple (bipolar -1..1)
// =====================================================
[System.Serializable]
public class LfoSettings
{
    public WaveType wave = WaveType.Square;
    public float rateHz = 30f;
}

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

    [Header("Oscillator A")]
    public WaveType oscillatorA = WaveType.Sine;
    public float oscillatorASemitones = 0f;
    public float oscillatorADb = -5f;

    [Header("Oscillator B")]
    public WaveType oscillatorB = WaveType.Triangle;
    public float oscillatorBSemitones = 12f;
    public float oscillatorBDb = -13f;

    [Header("Oscillator C")]
    public WaveType oscillatorC = WaveType.Square;
    public float oscillatorCSemitones = 19f;
    public float oscillatorCDb = -33f;

    // =================================================
    // NUEVO: Operador D (solo modulador, no suena directo)
    // =================================================
    [Header("Oscillator D (solo modulador)")]
    public bool useOscillatorD = false;
    public WaveType oscillatorD = WaveType.Triangle;
    public float oscillatorDSemitones = 0f;

    [Header("Phase Modulation")]
    [Tooltip("B modula la fase de A.")]
    public bool usePhaseModulation = false;

    [Tooltip("Cantidad B → A. 0.1 equivale al ~10% del parche de Heisenberg.")]
    [Range(0f, 1f)]
    public float phaseModAmount = 0.1f;

    [Tooltip("C modula la fase de B. 0 = apagado.")]
    [Range(0f, 1f)]
    public float phaseModCToB = 0f;

    [Tooltip("D modula la fase de A. 0 = apagado.")]
    [Range(0f, 1f)]
    public float phaseModDToA = 0f;

    [Header("Amplitude Envelope (MAIN)")]
    public float attack = 0.008f;
    public float decay = 0.08f;
    [Range(0f, 1f)]
    public float sustain = 0.55f;
    public float release = 0.12f;

    // =================================================
    // NUEVO: envolventes auxiliares y LFOs
    // =================================================
    [Header("Envelopes auxiliares")]
    public EnvelopeSettings env2 = new EnvelopeSettings();
    public EnvelopeSettings env3 = new EnvelopeSettings();

    [Header("LFOs")]
    public LfoSettings lfo1 = new LfoSettings();
    public LfoSettings lfo2 = new LfoSettings();

    [Tooltip("Cada vez que se genera el sonido, los LFOs arrancan en una fase aleatoria.\nHace que dos pulsaciones nunca suenen idénticas.")]
    public bool randomizeLfoPhase = false;

    // =================================================
    // NUEVO: Amplitude Modulation por operador
    // 0 = sin efecto. 1 = el operador sigue por completo a la fuente.
    // nivel = 1 - amount + amount * fuente(0..1)
    // =================================================
    [Header("Amplitude Modulation")]
    [Range(0f, 1f)] public float ampModEnv3ToA = 0f;
    [Range(0f, 1f)] public float ampModLfo1ToC = 0f;
    [Range(0f, 1f)] public float ampModMainToD = 0f;

    [Header("Pitch Envelope")]
    public bool pitchEnvelope = false;

    [Tooltip("Desplazamiento inicial en semitonos (t = 0).")]
    public float pitchAmount = 0f;

    [Tooltip("Semitonos al terminar el Attack. 0 = comportamiento anterior.")]
    public float pitchAttackLevel = 0f;

    [Tooltip("Semitonos al terminar el Decay, y valor que se mantiene después. 0 = comportamiento anterior.")]
    public float pitchEndAmount = 0f;

    [Tooltip("Segundos que tarda en ir de pitchAmount a pitchAttackLevel.")]
    public float pitchAttack = 0.005f;

    [Tooltip("Segundos que tarda en ir de pitchAttackLevel a pitchEndAmount.")]
    public float pitchDecay = 0.04f;

    [Header("Filter")]
    public bool useFilter = true;

    [Range(100f, 10000f)]
    public float filterCutoff = 2500f;

    [Range(0f, 1f)]
    public float filterResonance = 0.3f;

    [Tooltip("Desactivado: filtro simple de 6 dB/oct (sin resonancia).\nActivado: Low-Pass de 12 dB/oct con resonancia real.")]
    public bool resonantFilter = false;

    // =================================================
    // NUEVO: Filter Modulation
    // Cada valor es -1..1 (equivale a -100%..+100% de Heisenberg).
    // La suma desplaza el cutoff en octavas: cutoff * 2^(suma * filterModOctaves)
    // =================================================
    [Header("Filter Modulation")]
    [Range(-1f, 1f)] public float filterModMain = 0f;
    [Range(-1f, 1f)] public float filterModEnv2 = 0f;
    [Range(-1f, 1f)] public float filterModEnv3 = 0f;
    [Range(-1f, 1f)] public float filterModLfo1 = 0f;
    [Range(-1f, 1f)] public float filterModLfo2 = 0f;

    [Tooltip("Octavas que recorre el cutoff cuando la suma de modulaciones vale 1.")]
    [Range(0f, 4f)]
    public float filterModOctaves = 1.2f;

    [Header("EQ")]
    public bool useEQ = false;

    public float lowShelfDb = 0f;
    public float lowMidDb = 0f;
    public float highMidDb = 0f;
}

public enum WaveType
{
    Sine,
    Triangle,
    Square,
    Saw
}
