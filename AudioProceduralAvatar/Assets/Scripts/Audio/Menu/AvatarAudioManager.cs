using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AvatarAudioManager : MonoBehaviour
{
    [Header("Presets")]
    public AudioPreset selectPreset;
    public AudioPreset acceptPreset;
    public AudioPreset undoPreset;
    public AudioPreset keyboardPreset;

    // ============================================================
    // SELECT
    // ============================================================

    [Header("Variaciones de Select")]
    [Min(2)]
    public int selectVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float selectPitchVariation = 0.6f;

    [Tooltip("Volumen predeterminado de Select.")]
    [Range(0f, 1f)]
    public float selectVolume = 1f;

    // ============================================================
    // ACCEPT
    // ============================================================

    [Header("Variaciones de Accept")]
    [Min(2)]
    public int acceptVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float acceptPitchVariation = 0.5f;

    [Tooltip("Volumen predeterminado de Accept.")]
    [Range(0f, 1f)]
    public float acceptVolume = 1f;

    // ============================================================
    // UNDO
    // ============================================================

    [Header("Variaciones de Undo")]
    [Min(4)]
    public int undoVariants = 16;

    [Tooltip("Variación general de pitch.")]
    [Range(0f, 3f)]
    public float undoPitchVariation = 1.2f;

    [Tooltip("Variación de duración.")]
    [Range(0f, 0.10f)]
    public float undoDurationVariation = 0.025f;

    [Tooltip("Variación de los niveles de los osciladores.")]
    [Range(0f, 5f)]
    public float undoOscillatorDbVariation = 1.5f;

    [Tooltip("Variación de los semitonos internos de los osciladores.")]
    [Range(0f, 2f)]
    public float undoOscillatorSemitoneVariation = 0.5f;

    [Tooltip("Variación de la envolvente Attack.")]
    [Range(0f, 0.05f)]
    public float undoAttackVariation = 0.012f;

    [Tooltip("Variación de la envolvente Decay.")]
    [Range(0f, 0.10f)]
    public float undoDecayVariation = 0.025f;

    [Tooltip("Variación del Sustain.")]
    [Range(0f, 0.20f)]
    public float undoSustainVariation = 0.08f;

    [Tooltip("Variación del Release.")]
    [Range(0f, 0.10f)]
    public float undoReleaseVariation = 0.025f;

    [Tooltip("Variación del filtro.")]
    [Range(0f, 1500f)]
    public float undoFilterVariation = 500f;

    [Tooltip("Variación de resonancia.")]
    [Range(0f, 0.20f)]
    public float undoResonanceVariation = 0.08f;

    [Tooltip("Volumen predeterminado del Undo.")]
    [Range(0f, 1f)]
    public float undoVolume = 0.50f;

    // ============================================================
    // KEYBOARD
    // ============================================================

    [Header("Keyboard")]
    public float keyBaseFrequency = 800f;
    public float keyPitchVariation = 1.5f;
    public int keyVariants = 8;
    public float keyDuration = 0.07f;
    public float backspaceSemitones = -4f;

    [Tooltip("Volumen predeterminado de las teclas.")]
    [Range(0f, 1f)]
    public float keyboardVolume = 1f;

    [Tooltip("Volumen predeterminado de Backspace.")]
    [Range(0f, 1f)]
    public float backspaceVolume = 0.8f;

    // ============================================================
    // AUDIO
    // ============================================================

    private AudioSource audioSource;
    private ProceduralSynth synthesizer;

    // ============================================================
    // BANKS
    // ============================================================

    private AudioClip[] selectClips;
    private AudioClip[] undoClips;
    private AudioClip[][] acceptClips;

    private AudioClip[] keyClips;
    private AudioClip[] backspaceClips;

    // ============================================================
    // INDICES
    // ============================================================

    private int lastSelectIndex = -1;
    private int lastUndoIndex = -1;

    private int lastAcceptCIndex = -1;
    private int lastAcceptEIndex = -1;
    private int lastAcceptGIndex = -1;

    private int lastKeyIndex = -1;
    private int lastBackspaceIndex = -1;

    // ============================================================
    // ORDEN DE UNDO
    // ============================================================

    private List<int> undoPlayOrder =
        new List<int>();

    private int undoOrderPosition = 0;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

        synthesizer =
            new ProceduralSynth();

        BuildAllSoundBanks();
    }

    // ============================================================
    // BUILD ALL BANKS
    // ============================================================

    public void BuildAllSoundBanks()
    {
        DestroyAllBanks();

        lastSelectIndex = -1;
        lastUndoIndex = -1;

        lastAcceptCIndex = -1;
        lastAcceptEIndex = -1;
        lastAcceptGIndex = -1;

        lastKeyIndex = -1;
        lastBackspaceIndex = -1;

        // ========================================================
        // SELECT
        // ========================================================

        if (selectPreset != null)
        {
            selectClips =
                GenerateBank(
                    selectPreset,
                    440f,
                    0.25f,
                    selectVariants,
                    selectPitchVariation
                );
        }

        // ========================================================
        // UNDO
        // ========================================================

        if (undoPreset != null)
        {
            BuildUndoBank();
        }

        // ========================================================
        // ACCEPT
        // ========================================================

        if (acceptPreset != null)
        {
            acceptClips =
                new AudioClip[3][];

            // C5
            acceptClips[0] =
                GenerateBank(
                    acceptPreset,
                    523.25f,
                    0.18f,
                    acceptVariants,
                    acceptPitchVariation
                );

            // E5
            acceptClips[1] =
                GenerateBank(
                    acceptPreset,
                    659.25f,
                    0.18f,
                    acceptVariants,
                    acceptPitchVariation
                );

            // G5
            acceptClips[2] =
                GenerateBank(
                    acceptPreset,
                    783.99f,
                    0.25f,
                    acceptVariants,
                    acceptPitchVariation
                );
        }

        // ========================================================
        // KEYBOARD
        // ========================================================

        if (keyboardPreset != null)
        {
            BuildKeyboardBanks();
        }
    }

    // ============================================================
    // UNDO BANK MEJORADO
    // ============================================================

    private void BuildUndoBank()
    {
        int variants =
            Mathf.Max(
                4,
                undoVariants
            );

        undoClips =
            new AudioClip[variants];

        // --------------------------------------------------------
        // GENERAR CADA VARIANTE
        // --------------------------------------------------------

        for (int i = 0; i < variants; i++)
        {
            undoClips[i] =
                GenerateUndoVariant();
        }

        // --------------------------------------------------------
        // CREAR ORDEN ALEATORIO
        // --------------------------------------------------------

        RebuildUndoPlayOrder();
    }

    // ============================================================
    // GENERAR UNA VARIANTE DE UNDO
    // ============================================================

    private AudioClip GenerateUndoVariant()
    {
        // ========================================================
        // GUARDAR VALORES ORIGINALES
        // ========================================================

        float originalVolume =
            undoPreset.volume;

        float originalOscillatorASemitones =
            undoPreset.oscillatorASemitones;

        float originalOscillatorBSemitones =
            undoPreset.oscillatorBSemitones;

        float originalOscillatorCSemitones =
            undoPreset.oscillatorCSemitones;

        float originalOscillatorADb =
            undoPreset.oscillatorADb;

        float originalOscillatorBDb =
            undoPreset.oscillatorBDb;

        float originalOscillatorCDb =
            undoPreset.oscillatorCDb;

        float originalPhaseModAmount =
            undoPreset.phaseModAmount;

        float originalPhaseModCToB =
            undoPreset.phaseModCToB;

        float originalPhaseModDToA =
            undoPreset.phaseModDToA;

        float originalAttack =
            undoPreset.attack;

        float originalDecay =
            undoPreset.decay;

        float originalSustain =
            undoPreset.sustain;

        float originalRelease =
            undoPreset.release;

        float originalFilterCutoff =
            undoPreset.filterCutoff;

        float originalFilterResonance =
            undoPreset.filterResonance;

        bool originalRandomizeLfoPhase =
            undoPreset.randomizeLfoPhase;

        float originalLfo1Rate =
            undoPreset.lfo1.rateHz;

        float originalLfo2Rate =
            undoPreset.lfo2.rateHz;

        // ========================================================
        // VARIABLES DE GENERACIÓN
        // ========================================================

        float baseFrequency = 440f;

        float pitchShift =
            Random.Range(
                -undoPitchVariation,
                undoPitchVariation
            );

        float frequency =
            baseFrequency *
            Mathf.Pow(
                2f,
                pitchShift / 12f
            );

        // ========================================================
        // OSCILADORES
        // ========================================================

        undoPreset.oscillatorASemitones =
            originalOscillatorASemitones +
            Random.Range(
                -undoOscillatorSemitoneVariation,
                undoOscillatorSemitoneVariation
            );

        undoPreset.oscillatorBSemitones =
            originalOscillatorBSemitones +
            Random.Range(
                -undoOscillatorSemitoneVariation,
                undoOscillatorSemitoneVariation
            );

        undoPreset.oscillatorCSemitones =
            originalOscillatorCSemitones +
            Random.Range(
                -undoOscillatorSemitoneVariation,
                undoOscillatorSemitoneVariation
            );

        undoPreset.oscillatorADb =
            originalOscillatorADb +
            Random.Range(
                -undoOscillatorDbVariation,
                undoOscillatorDbVariation
            );

        undoPreset.oscillatorBDb =
            originalOscillatorBDb +
            Random.Range(
                -undoOscillatorDbVariation,
                undoOscillatorDbVariation
            );

        undoPreset.oscillatorCDb =
            originalOscillatorCDb +
            Random.Range(
                -undoOscillatorDbVariation,
                undoOscillatorDbVariation
            );

        // ========================================================
        // ENVOLVENTE PRINCIPAL
        // ========================================================

        undoPreset.attack =
            Mathf.Max(
                0.001f,
                originalAttack +
                Random.Range(
                    -undoAttackVariation,
                    undoAttackVariation
                )
            );

        undoPreset.decay =
            Mathf.Max(
                0.001f,
                originalDecay +
                Random.Range(
                    -undoDecayVariation,
                    undoDecayVariation
                )
            );

        undoPreset.sustain =
            Mathf.Clamp01(
                originalSustain +
                Random.Range(
                    -undoSustainVariation,
                    undoSustainVariation
                )
            );

        undoPreset.release =
            Mathf.Max(
                0.001f,
                originalRelease +
                Random.Range(
                    -undoReleaseVariation,
                    undoReleaseVariation
                )
            );

        // ========================================================
        // FILTRO
        // ========================================================

        if (undoPreset.useFilter)
        {
            undoPreset.filterCutoff =
                Mathf.Clamp(
                    originalFilterCutoff +
                    Random.Range(
                        -undoFilterVariation,
                        undoFilterVariation
                    ),
                    100f,
                    10000f
                );

            undoPreset.filterResonance =
                Mathf.Clamp01(
                    originalFilterResonance +
                    Random.Range(
                        -undoResonanceVariation,
                        undoResonanceVariation
                    )
                );
        }

        // ========================================================
        // LFO
        // ========================================================

        undoPreset.randomizeLfoPhase = true;

        undoPreset.lfo1.rateHz =
            Mathf.Max(
                0.01f,
                originalLfo1Rate *
                Random.Range(
                    0.80f,
                    1.20f
                )
            );

        undoPreset.lfo2.rateHz =
            Mathf.Max(
                0.01f,
                originalLfo2Rate *
                Random.Range(
                    0.80f,
                    1.20f
                )
            );

        // ========================================================
        // VOLUMEN INTERNO
        // ========================================================

        undoPreset.volume =
            Mathf.Clamp(
                originalVolume *
                Random.Range(
                    0.94f,
                    1.02f
                ),
                0.01f,
                2f
            );

        // ========================================================
        // DURACIÓN
        // ========================================================

        float duration =
            Random.Range(
                0.45f -
                undoDurationVariation,

                0.45f +
                undoDurationVariation
            );

        duration =
            Mathf.Max(
                0.05f,
                duration
            );

        // ========================================================
        // GENERAR SONIDO
        // ========================================================

        AudioClip generatedClip = null;

        try
        {
            generatedClip =
                synthesizer.Generate(
                    undoPreset,
                    frequency,
                    duration
                );
        }
        finally
        {
            // ====================================================
            // RESTAURAR TODO
            // ====================================================

            undoPreset.volume =
                originalVolume;

            undoPreset.oscillatorASemitones =
                originalOscillatorASemitones;

            undoPreset.oscillatorBSemitones =
                originalOscillatorBSemitones;

            undoPreset.oscillatorCSemitones =
                originalOscillatorCSemitones;

            undoPreset.oscillatorADb =
                originalOscillatorADb;

            undoPreset.oscillatorBDb =
                originalOscillatorBDb;

            undoPreset.oscillatorCDb =
                originalOscillatorCDb;

            undoPreset.phaseModAmount =
                originalPhaseModAmount;

            undoPreset.phaseModCToB =
                originalPhaseModCToB;

            undoPreset.phaseModDToA =
                originalPhaseModDToA;

            undoPreset.attack =
                originalAttack;

            undoPreset.decay =
                originalDecay;

            undoPreset.sustain =
                originalSustain;

            undoPreset.release =
                originalRelease;

            undoPreset.filterCutoff =
                originalFilterCutoff;

            undoPreset.filterResonance =
                originalFilterResonance;

            undoPreset.randomizeLfoPhase =
                originalRandomizeLfoPhase;

            undoPreset.lfo1.rateHz =
                originalLfo1Rate;

            undoPreset.lfo2.rateHz =
                originalLfo2Rate;
        }

        return generatedClip;
    }

    // ============================================================
    // ORDEN DE UNDO
    // ============================================================

    private void RebuildUndoPlayOrder()
    {
        undoPlayOrder.Clear();

        if (
            undoClips == null ||
            undoClips.Length == 0
        )
        {
            return;
        }

        for (
            int i = 0;
            i < undoClips.Length;
            i++
        )
        {
            undoPlayOrder.Add(i);
        }

        // Fisher-Yates.
        for (
            int i = undoPlayOrder.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            int temp =
                undoPlayOrder[i];

            undoPlayOrder[i] =
                undoPlayOrder[randomIndex];

            undoPlayOrder[randomIndex] =
                temp;
        }

        // Evitar que el primer sonido del nuevo ciclo
        // sea igual al último del ciclo anterior.
        if (
            undoPlayOrder.Count > 1 &&
            lastUndoIndex >= 0 &&
            undoPlayOrder[0] == lastUndoIndex
        )
        {
            int swapIndex =
                Random.Range(
                    1,
                    undoPlayOrder.Count
                );

            int temp =
                undoPlayOrder[0];

            undoPlayOrder[0] =
                undoPlayOrder[swapIndex];

            undoPlayOrder[swapIndex] =
                temp;
        }

        undoOrderPosition = 0;
    }

    // ============================================================
    // PLAY SELECT
    // ============================================================

    public void PlaySelect()
    {
        if (
            selectClips == null ||
            selectClips.Length == 0
        )
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco de Select. " +
                "Verifica que selectPreset esté asignado."
            );

            return;
        }

        int index =
            PickIndex(
                selectClips.Length,
                ref lastSelectIndex
            );

        audioSource.PlayOneShot(
            selectClips[index],
            selectVolume
        );
    }

    // ============================================================
    // PLAY ACCEPT
    // ============================================================

    public void PlayAcceptChanges()
    {
        if (
            acceptClips == null ||
            acceptClips.Length != 3 ||
            acceptClips[0] == null ||
            acceptClips[1] == null ||
            acceptClips[2] == null
        )
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco de Accept. " +
                "Verifica que acceptPreset esté asignado."
            );

            return;
        }

        StartCoroutine(
            AcceptSequence()
        );
    }

    private IEnumerator AcceptSequence()
    {
        int cIndex =
            PickIndex(
                acceptClips[0].Length,
                ref lastAcceptCIndex
            );

        audioSource.PlayOneShot(
            acceptClips[0][cIndex],
            acceptVolume
        );

        yield return
            new WaitForSeconds(
                0.07f
            );

        int eIndex =
            PickIndex(
                acceptClips[1].Length,
                ref lastAcceptEIndex
            );

        audioSource.PlayOneShot(
            acceptClips[1][eIndex],
            acceptVolume
        );

        yield return
            new WaitForSeconds(
                0.07f
            );

        int gIndex =
            PickIndex(
                acceptClips[2].Length,
                ref lastAcceptGIndex
            );

        audioSource.PlayOneShot(
            acceptClips[2][gIndex],
            acceptVolume
        );
    }

    // ============================================================
    // PLAY UNDO
    // ============================================================

    public void PlayUndo()
    {
        if (
            undoClips == null ||
            undoClips.Length == 0
        )
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco de Undo. " +
                "Verifica que undoPreset esté asignado."
            );

            return;
        }

        if (
            undoPlayOrder == null ||
            undoPlayOrder.Count == 0
        )
        {
            RebuildUndoPlayOrder();
        }

        int index =
            undoPlayOrder[
                undoOrderPosition
            ];

        undoOrderPosition++;

        if (
            undoOrderPosition >=
            undoPlayOrder.Count
        )
        {
            RebuildUndoPlayOrder();
        }

        lastUndoIndex = index;

        audioSource.PlayOneShot(
            undoClips[index],
            undoVolume
        );
    }

    // ============================================================
    // KEYBOARD
    // ============================================================

    private void BuildKeyboardBanks()
    {
        keyClips =
            GenerateBank(
                keyboardPreset,
                keyBaseFrequency,
                keyDuration,
                keyVariants,
                keyPitchVariation
            );

        backspaceClips =
            GenerateBank(
                keyboardPreset,
                keyBaseFrequency,
                keyDuration,
                keyVariants,
                keyPitchVariation,
                backspaceSemitones
            );
    }

    public void PlayKeyClick()
    {
        if (
            keyClips == null ||
            keyClips.Length == 0
        )
        {
            return;
        }

        int index =
            PickIndex(
                keyClips.Length,
                ref lastKeyIndex
            );

        audioSource.PlayOneShot(
            keyClips[index],
            keyboardVolume
        );
    }

    public void PlayKeyBackspace()
    {
        if (
            backspaceClips == null ||
            backspaceClips.Length == 0
        )
        {
            return;
        }

        int index =
            PickIndex(
                backspaceClips.Length,
                ref lastBackspaceIndex
            );

        audioSource.PlayOneShot(
            backspaceClips[index],
            backspaceVolume
        );
    }

    // ============================================================
    // GENERAR BANCO NORMAL
    // ============================================================

    private AudioClip[] GenerateBank(
        AudioPreset preset,
        float baseFrequency,
        float duration,
        int variants,
        float pitchVariation
    )
    {
        return GenerateBank(
            preset,
            baseFrequency,
            duration,
            variants,
            pitchVariation,
            0f
        );
    }

    private AudioClip[] GenerateBank(
        AudioPreset preset,
        float baseFrequency,
        float duration,
        int variants,
        float pitchVariation,
        float additionalSemitones
    )
    {
        if (preset == null)
        {
            return null;
        }

        variants =
            Mathf.Max(
                2,
                variants
            );

        AudioClip[] bank =
            new AudioClip[variants];

        for (
            int i = 0;
            i < variants;
            i++
        )
        {
            float randomSemitones =
                Random.Range(
                    -pitchVariation,
                    pitchVariation
                );

            randomSemitones +=
                additionalSemitones;

            float frequency =
                baseFrequency *
                Mathf.Pow(
                    2f,
                    randomSemitones / 12f
                );

            bank[i] =
                synthesizer.Generate(
                    preset,
                    frequency,
                    duration
                );
        }

        return bank;
    }

    // ============================================================
    // ELEGIR VARIACIÓN NORMAL
    // ============================================================

    private int PickIndex(
        int length,
        ref int lastIndex
    )
    {
        if (length <= 0)
        {
            return 0;
        }

        if (length == 1)
        {
            lastIndex = 0;

            return 0;
        }

        int index =
            Random.Range(
                0,
                length
            );

        if (index == lastIndex)
        {
            index =
                (index + 1) %
                length;
        }

        lastIndex = index;

        return index;
    }

    // ============================================================
    // DESTRUIR BANKS
    // ============================================================

    private void DestroyAllBanks()
    {
        DestroyBank(
            selectClips
        );

        DestroyBank(
            undoClips
        );

        DestroyBank(
            keyClips
        );

        DestroyBank(
            backspaceClips
        );

        if (acceptClips != null)
        {
            for (
                int i = 0;
                i < acceptClips.Length;
                i++
            )
            {
                DestroyBank(
                    acceptClips[i]
                );
            }
        }

        selectClips = null;
        undoClips = null;
        acceptClips = null;

        keyClips = null;
        backspaceClips = null;

        if (undoPlayOrder != null)
        {
            undoPlayOrder.Clear();
        }

        undoOrderPosition = 0;
    }

    private void DestroyBank(
        AudioClip[] bank
    )
    {
        if (bank == null)
        {
            return;
        }

        for (
            int i = 0;
            i < bank.Length;
            i++
        )
        {
            if (bank[i] != null)
            {
                Destroy(
                    bank[i]
                );
            }
        }
    }

    // ============================================================
    // LIMPIEZA
    // ============================================================

    private void OnDestroy()
    {
        DestroyAllBanks();
    }
}
