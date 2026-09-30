using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AvatarAudioManager : MonoBehaviour
{
    // ============================================================
    // PRESETS
    // ============================================================

    [Header("Presets principales")]

    public AudioPreset selectPreset;

    public AudioPreset acceptPreset;

    public AudioPreset undoPreset;

    // ============================================================
    // TECLADO
    // ============================================================

    [Header("Teclado - DOS SONIDOS")]

    [Tooltip(
        "Sonido para escribir una letra. " +
        "Debe ser diferente al Backspace."
    )]
    public AudioPreset keyboardTypingPreset;

    [Tooltip(
        "Sonido para borrar una letra con Backspace."
    )]
    public AudioPreset keyboardBackspacePreset;

    // Mantiene compatibilidad con el preset antiguo.
    [Tooltip(
        "Preset antiguo de teclado. " +
        "Solo se usa como respaldo si no asignas los nuevos."
    )]
    public AudioPreset keyboardPreset;

    // ============================================================
    // NUEVOS SONIDOS
    // ============================================================

    [Header("Nuevos sonidos de interfaz")]

    [Tooltip("Sonido para quitar/eliminar una selección.")]
    public AudioPreset deletePreset;

    [Tooltip("Sonido para mover un slider de color.")]
    public AudioPreset colorSliderPreset;

    [Tooltip("Sonido para pasar a la página anterior.")]
    public AudioPreset pagePreviousPreset;

    [Tooltip("Sonido para pasar a la página siguiente.")]
    public AudioPreset pageNextPreset;

    [Tooltip("Sonido de éxito al descargar/generar el QR.")]
    public AudioPreset qrSuccessPreset;

    // ============================================================
    // SELECT
    // ============================================================

    [Header("Variaciones de Select")]

    [Min(2)]
    public int selectVariants = 8;

    [Range(0f, 2f)]
    public float selectPitchVariation = 0.6f;

    [Range(0f, 1f)]
    public float selectVolume = 1f;

    // ============================================================
    // ACCEPT
    // ============================================================

    [Header("Variaciones de Accept")]

    [Min(2)]
    public int acceptVariants = 8;

    [Range(0f, 2f)]
    public float acceptPitchVariation = 0.5f;

    [Range(0f, 1f)]
    public float acceptVolume = 1f;

    // ============================================================
    // UNDO
    // ============================================================

    [Header("Variaciones de Undo")]

    [Min(4)]
    public int undoVariants = 16;

    [Range(0f, 3f)]
    public float undoPitchVariation = 1.2f;

    [Range(0f, 0.10f)]
    public float undoDurationVariation = 0.025f;

    [Range(0f, 5f)]
    public float undoOscillatorDbVariation = 1.5f;

    [Range(0f, 2f)]
    public float undoOscillatorSemitoneVariation = 0.5f;

    [Range(0f, 0.05f)]
    public float undoAttackVariation = 0.012f;

    [Range(0f, 0.10f)]
    public float undoDecayVariation = 0.025f;

    [Range(0f, 0.20f)]
    public float undoSustainVariation = 0.08f;

    [Range(0f, 0.10f)]
    public float undoReleaseVariation = 0.025f;

    [Range(0f, 1500f)]
    public float undoFilterVariation = 500f;

    [Range(0f, 0.20f)]
    public float undoResonanceVariation = 0.08f;

    [Range(0f, 1f)]
    public float undoVolume = 0.50f;

    // ============================================================
    // KEYBOARD
    // ============================================================

    [Header("Keyboard - Sonido más grave")]

    [Tooltip("Frecuencia base del sonido al escribir.")]
    [Range(80f, 800f)]
    public float keyBaseFrequency = 300f;

    [Tooltip("Variación de pitch de las teclas.")]
    [Range(0f, 2f)]
    public float keyPitchVariation = 0.7f;

    [Min(2)]
    public int keyVariants = 8;

    [Tooltip("Duración de la tecla normal.")]
    [Range(0.02f, 0.20f)]
    public float keyDuration = 0.065f;

    [Tooltip(
        "El Backspace queda más grave que la tecla normal."
    )]
    [Range(-24f, 0f)]
    public float backspaceSemitones = -5f;

    [Tooltip("Frecuencia base del Backspace.")]
    [Range(60f, 600f)]
    public float backspaceBaseFrequency = 220f;

    [Range(0.02f, 0.20f)]
    public float backspaceDuration = 0.075f;

    [Range(0f, 1f)]
    public float keyboardVolume = 0.65f;

    [Range(0f, 1f)]
    public float backspaceVolume = 0.60f;

    // ============================================================
    // NUEVOS SONIDOS - CONFIGURACIÓN
    // ============================================================

    [Header("Eliminar")]

    [Range(0f, 1f)]
    public float deleteVolume = 0.75f;

    [Range(0f, 3f)]
    public float deletePitch = -1f;

    [Range(0.03f, 0.50f)]
    public float deleteDuration = 0.14f;

    // ------------------------------------------------------------

    [Header("Slider de color")]

    [Range(0f, 1f)]
    public float colorSliderVolume = 0.45f;

    [Range(0f, 2f)]
    public float colorSliderPitchVariation = 0.35f;

    [Range(0.02f, 0.20f)]
    public float colorSliderDuration = 0.055f;

    // ------------------------------------------------------------

    [Header("Páginas del libro")]

    [Range(0f, 1f)]
    public float pageVolume = 0.60f;

    [Range(0f, 2f)]
    public float pagePitchVariation = 0.30f;

    [Range(0.05f, 0.50f)]
    public float pageDuration = 0.16f;

    // ------------------------------------------------------------

    [Header("Éxito QR")]

    [Range(0f, 1f)]
    public float qrSuccessVolume = 0.80f;

    [Range(0f, 2f)]
    public float qrSuccessPitchVariation = 0.25f;

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

    private AudioClip[] deleteClips;

    private AudioClip[] colorSliderClips;

    private AudioClip[] pagePreviousClips;

    private AudioClip[] pageNextClips;

    private AudioClip[] qrSuccessClips;

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

    private int lastDeleteIndex = -1;

    private int lastColorSliderIndex = -1;

    private int lastPagePreviousIndex = -1;

    private int lastPageNextIndex = -1;

    private int lastQRSuccessIndex = -1;

    // ============================================================
    // ORDEN UNDO
    // ============================================================

    private List<int> undoPlayOrder =
        new List<int>();

    private int undoOrderPosition = 0;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        audioSource =
            GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

        synthesizer =
            new ProceduralSynth();

        BuildAllSoundBanks();
    }

    // ============================================================
    // BUILD
    // ============================================================

    public void BuildAllSoundBanks()
    {
        DestroyAllBanks();

        ResetIndices();

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

            acceptClips[0] =
                GenerateBank(
                    acceptPreset,
                    523.25f,
                    0.18f,
                    acceptVariants,
                    acceptPitchVariation
                );

            acceptClips[1] =
                GenerateBank(
                    acceptPreset,
                    659.25f,
                    0.18f,
                    acceptVariants,
                    acceptPitchVariation
                );

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
        // TECLADO
        // ========================================================

        BuildKeyboardBanks();

        // ========================================================
        // ELIMINAR
        // ========================================================

        if (deletePreset != null)
        {
            deleteClips =
                GenerateBank(
                    deletePreset,
                    330f,
                    deleteDuration,
                    8,
                    0.35f,
                    deletePitch
                );
        }

        // ========================================================
        // SLIDER
        // ========================================================

        if (colorSliderPreset != null)
        {
            colorSliderClips =
                GenerateBank(
                    colorSliderPreset,
                    520f,
                    colorSliderDuration,
                    8,
                    colorSliderPitchVariation
                );
        }

        // ========================================================
        // PÁGINA ANTERIOR
        // ========================================================

        if (pagePreviousPreset != null)
        {
            pagePreviousClips =
                GenerateBank(
                    pagePreviousPreset,
                    300f,
                    pageDuration,
                    6,
                    pagePitchVariation
                );
        }

        // ========================================================
        // PÁGINA SIGUIENTE
        // ========================================================

        if (pageNextPreset != null)
        {
            pageNextClips =
                GenerateBank(
                    pageNextPreset,
                    380f,
                    pageDuration,
                    6,
                    pagePitchVariation
                );
        }

        // ========================================================
        // QR SUCCESS
        // ========================================================

        if (qrSuccessPreset != null)
        {
            BuildQRSuccessBank();
        }
    }

    // ============================================================
    // RESET INDICES
    // ============================================================

    private void ResetIndices()
    {
        lastSelectIndex = -1;

        lastUndoIndex = -1;

        lastAcceptCIndex = -1;
        lastAcceptEIndex = -1;
        lastAcceptGIndex = -1;

        lastKeyIndex = -1;
        lastBackspaceIndex = -1;

        lastDeleteIndex = -1;

        lastColorSliderIndex = -1;

        lastPagePreviousIndex = -1;
        lastPageNextIndex = -1;

        lastQRSuccessIndex = -1;
    }

    // ============================================================
    // UNDO
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

        for (int i = 0; i < variants; i++)
        {
            undoClips[i] =
                GenerateUndoVariant();
        }

        RebuildUndoPlayOrder();
    }

    private AudioClip GenerateUndoVariant()
    {
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

        float pitchShift =
            Random.Range(
                -undoPitchVariation,
                undoPitchVariation
            );

        float frequency =
            440f *
            Mathf.Pow(
                2f,
                pitchShift / 12f
            );

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

        float duration =
            Random.Range(
                0.45f - undoDurationVariation,
                0.45f + undoDurationVariation
            );

        duration =
            Mathf.Max(
                0.05f,
                duration
            );

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

        for (int i = 0; i < undoClips.Length; i++)
        {
            undoPlayOrder.Add(i);
        }

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
    // QR SUCCESS
    // ============================================================

    private void BuildQRSuccessBank()
    {
        qrSuccessClips =
            new AudioClip[6];

        float[] frequencies =
        {
            523.25f,
            587.33f,
            659.25f,
            698.46f,
            783.99f,
            880f
        };

        for (int i = 0; i < qrSuccessClips.Length; i++)
        {
            float pitch =
                Random.Range(
                    -qrSuccessPitchVariation,
                    qrSuccessPitchVariation
                );

            float frequency =
                frequencies[i] *
                Mathf.Pow(
                    2f,
                    pitch / 12f
                );

            qrSuccessClips[i] =
                synthesizer.Generate(
                    qrSuccessPreset,
                    frequency,
                    0.15f + i * 0.015f
                );
        }
    }

    // ============================================================
    // SELECT
    // ============================================================

    public void PlaySelect()
    {
        PlayFromBank(
            selectClips,
            ref lastSelectIndex,
            selectVolume,
            "Select"
        );
    }

    // ============================================================
    // ACCEPT
    // ============================================================

    public void PlayAcceptChanges()
    {
        if (
            acceptClips == null ||
            acceptClips.Length != 3
        )
        {
            return;
        }

        StartCoroutine(
            AcceptSequence()
        );
    }

    private IEnumerator AcceptSequence()
    {
        PlayFromBank(
            acceptClips[0],
            ref lastAcceptCIndex,
            acceptVolume,
            "Accept C"
        );

        yield return
            new WaitForSeconds(0.07f);

        PlayFromBank(
            acceptClips[1],
            ref lastAcceptEIndex,
            acceptVolume,
            "Accept E"
        );

        yield return
            new WaitForSeconds(0.07f);

        PlayFromBank(
            acceptClips[2],
            ref lastAcceptGIndex,
            acceptVolume,
            "Accept G"
        );
    }

    // ============================================================
    // UNDO
    // ============================================================

    public void PlayUndo()
    {
        if (
            undoClips == null ||
            undoClips.Length == 0
        )
        {
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
    // KEYBOARD BANKS
    // ============================================================

    private void BuildKeyboardBanks()
    {
        AudioPreset typingPreset =
            keyboardTypingPreset != null
                ? keyboardTypingPreset
                : keyboardPreset;

        AudioPreset backspacePreset =
            keyboardBackspacePreset != null
                ? keyboardBackspacePreset
                : keyboardPreset;

        if (typingPreset != null)
        {
            keyClips =
                GenerateBank(
                    typingPreset,
                    keyBaseFrequency,
                    keyDuration,
                    keyVariants,
                    keyPitchVariation
                );
        }

        if (backspacePreset != null)
        {
            backspaceClips =
                GenerateBank(
                    backspacePreset,
                    backspaceBaseFrequency,
                    backspaceDuration,
                    keyVariants,
                    keyPitchVariation,
                    backspaceSemitones
                );
        }
    }

    // ============================================================
    // KEYBOARD - ESCRIBIR
    // ============================================================

    public void PlayKeyClick()
    {
        PlayFromBank(
            keyClips,
            ref lastKeyIndex,
            keyboardVolume,
            "Keyboard typing"
        );
    }

    // ============================================================
    // KEYBOARD - BACKSPACE
    // ============================================================

    public void PlayKeyBackspace()
    {
        PlayFromBank(
            backspaceClips,
            ref lastBackspaceIndex,
            backspaceVolume,
            "Keyboard backspace"
        );
    }

    // ============================================================
    // ELIMINAR SELECCIÓN
    // ============================================================

    public void PlayDelete()
    {
        PlayFromBank(
            deleteClips,
            ref lastDeleteIndex,
            deleteVolume,
            "Delete"
        );
    }

    // Alias por si prefieres llamarlo Remove
    public void PlayRemove()
    {
        PlayDelete();
    }

    // ============================================================
    // SLIDER DE COLOR
    // ============================================================

    public void PlayColorSlider()
    {
        PlayFromBank(
            colorSliderClips,
            ref lastColorSliderIndex,
            colorSliderVolume,
            "Color Slider"
        );
    }

    // ============================================================
    // PÁGINA ANTERIOR
    // ============================================================

    public void PlayPagePrevious()
    {
        PlayFromBank(
            pagePreviousClips,
            ref lastPagePreviousIndex,
            pageVolume,
            "Page Previous"
        );
    }

    // ============================================================
    // PÁGINA SIGUIENTE
    // ============================================================

    public void PlayPageNext()
    {
        PlayFromBank(
            pageNextClips,
            ref lastPageNextIndex,
            pageVolume,
            "Page Next"
        );
    }

    // ============================================================
    // QR SUCCESS
    // ============================================================

    public void PlayQRSuccess()
    {
        if (
            qrSuccessClips == null ||
            qrSuccessClips.Length == 0
        )
        {
            return;
        }

        StartCoroutine(
            QRSuccessSequence()
        );
    }

    private IEnumerator QRSuccessSequence()
    {
        int[] melody =
        {
            0,
            2,
            4,
            5
        };

        for (int i = 0; i < melody.Length; i++)
        {
            int index =
                melody[i];

            if (
                index >= 0 &&
                index < qrSuccessClips.Length
            )
            {
                audioSource.PlayOneShot(
                    qrSuccessClips[index],
                    qrSuccessVolume
                );
            }

            yield return
                new WaitForSeconds(
                    0.075f
                );
        }
    }

    // ============================================================
    // GENERAR BANCO
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

        for (int i = 0; i < variants; i++)
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
    // PLAY BANK
    // ============================================================

    private void PlayFromBank(
        AudioClip[] bank,
        ref int lastIndex,
        float volume,
        string soundName
    )
    {
        if (
            bank == null ||
            bank.Length == 0
        )
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco para " +
                soundName
            );

            return;
        }

        int index =
            PickIndex(
                bank.Length,
                ref lastIndex
            );

        if (bank[index] == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            bank[index],
            volume
        );
    }

    // ============================================================
    // PICK INDEX
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
        DestroyBank(selectClips);

        DestroyBank(undoClips);

        DestroyBank(keyClips);

        DestroyBank(backspaceClips);

        DestroyBank(deleteClips);

        DestroyBank(colorSliderClips);

        DestroyBank(pagePreviousClips);

        DestroyBank(pageNextClips);

        DestroyBank(qrSuccessClips);

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

        deleteClips = null;

        colorSliderClips = null;

        pagePreviousClips = null;

        pageNextClips = null;

        qrSuccessClips = null;

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
                DestroyGeneratedClip(
                    bank[i]
                );
            }
        }
    }

    private void DestroyGeneratedClip(
        AudioClip clip
    )
    {
        if (clip == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(clip);
        }
        else
        {
            DestroyImmediate(clip);
        }
    }

    // ============================================================
    // REBUILD MANUAL
    // ============================================================

    [ContextMenu("Rebuild All Sound Banks")]
    public void RebuildAllSoundBanks()
    {
        BuildAllSoundBanks();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        DestroyAllBanks();
    }
}