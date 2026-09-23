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

    // ============================================================
    // ACCEPT
    // ============================================================

    [Header("Variaciones de Accept")]
    [Min(2)]
    public int acceptVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float acceptPitchVariation = 0.5f;

    // ============================================================
    // UNDO
    // ============================================================

    [Header("Variaciones de Undo")]

    [Tooltip("Cantidad de versiones diferentes que se generan.")]
    [Min(16)]
    public int undoVariants = 16;

    [Tooltip("Variación de pitch del Undo.")]
    [Range(0f, 3f)]
    public float undoPitchVariation = 1.2f;

    [Tooltip("Variación máxima de duración.")]
    [Range(0f, 0.10f)]
    public float undoDurationVariation = 0.025f;

    [Tooltip("Volumen base del Undo.")]
    [Range(0f, 1f)]
    public float undoVolume = 0.50f;

    [Tooltip("Pequeña variación de volumen entre versiones.")]
    [Range(0f, 0.15f)]
    public float undoVolumeVariation = 0.03f;

    // ============================================================
    // KEYBOARD
    // ============================================================

    [Header("Keyboard")]

    public float keyBaseFrequency = 800f;
    public float keyPitchVariation = 1.5f;
    public int keyVariants = 8;
    public float keyDuration = 0.07f;
    public float backspaceSemitones = -4f;

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
    // UNDO
    // ============================================================

    private List<int> undoPlayOrder = new List<int>();

    private int undoOrderPosition = 0;

    private float[] undoVariantVolumes;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

        synthesizer = new ProceduralSynth();

        // Aseguramos que Undo tenga 16 variantes.
        undoVariants = 16;

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
            selectClips = GenerateBank(
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
            acceptClips = new AudioClip[3][];

            // C5
            acceptClips[0] = GenerateBank(
                acceptPreset,
                523.25f,
                0.18f,
                acceptVariants,
                acceptPitchVariation
            );

            // E5
            acceptClips[1] = GenerateBank(
                acceptPreset,
                659.25f,
                0.18f,
                acceptVariants,
                acceptPitchVariation
            );

            // G5
            acceptClips[2] = GenerateBank(
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
    // CREAR BANCO DE UNDO
    // ============================================================

    private void BuildUndoBank()
    {
        int variants = 16;

        undoClips = new AudioClip[variants];

        undoVariantVolumes = new float[variants];

        // Guardamos el estado original.
        bool originalRandomizeLfoPhase =
            undoPreset.randomizeLfoPhase;

        // Activamos la variación de fase de LFO mientras
        // generamos los sonidos.
        undoPreset.randomizeLfoPhase = true;

        // ========================================================
        // GENERAR LAS 16 VERSIONES
        // ========================================================

        for (int i = 0; i < variants; i++)
        {
            // ----------------------------------------------------
            // 1. PITCH
            // ----------------------------------------------------

            float randomSemitones = Random.Range(
                -undoPitchVariation,
                undoPitchVariation
            );

            float frequency =
                440f *
                Mathf.Pow(
                    2f,
                    randomSemitones / 12f
                );

            // ----------------------------------------------------
            // 2. DURACIÓN
            // ----------------------------------------------------

            float duration = Random.Range(
                0.45f - undoDurationVariation,
                0.45f + undoDurationVariation
            );

            duration = Mathf.Max(
                0.05f,
                duration
            );

            // ----------------------------------------------------
            // 3. GENERAR CLIP
            // ----------------------------------------------------

            undoClips[i] = synthesizer.Generate(
                undoPreset,
                frequency,
                duration
            );

            // ----------------------------------------------------
            // 4. VOLUMEN
            // ----------------------------------------------------

            float volume = Random.Range(
                undoVolume - undoVolumeVariation,
                undoVolume + undoVolumeVariation
            );

            undoVariantVolumes[i] =
                Mathf.Clamp01(volume);
        }

        // Restaurar exactamente el estado original.
        undoPreset.randomizeLfoPhase =
            originalRandomizeLfoPhase;

        // Crear orden aleatorio.
        RebuildUndoPlayOrder();
    }

    // ============================================================
    // ORDEN ALEATORIO DE UNDO
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

        // Meter las 16 variantes.
        for (int i = 0; i < undoClips.Length; i++)
        {
            undoPlayOrder.Add(i);
        }

        // Mezclar.
        for (
            int i = undoPlayOrder.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex = Random.Range(
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

        // Evitar que el comienzo del siguiente grupo
        // sea exactamente el último sonido utilizado.
        if (
            undoPlayOrder.Count > 1 &&
            lastUndoIndex >= 0 &&
            undoPlayOrder[0] == lastUndoIndex
        )
        {
            int swapIndex = Random.Range(
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
    // SELECT
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

        int index = PickIndex(
            selectClips.Length,
            ref lastSelectIndex
        );

        audioSource.PlayOneShot(
            selectClips[index]
        );
    }

    // ============================================================
    // ACCEPT
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
        // C5
        int cIndex = PickIndex(
            acceptClips[0].Length,
            ref lastAcceptCIndex
        );

        audioSource.PlayOneShot(
            acceptClips[0][cIndex]
        );

        yield return new WaitForSeconds(
            0.07f
        );

        // E5
        int eIndex = PickIndex(
            acceptClips[1].Length,
            ref lastAcceptEIndex
        );

        audioSource.PlayOneShot(
            acceptClips[1][eIndex]
        );

        yield return new WaitForSeconds(
            0.07f
        );

        // G5
        int gIndex = PickIndex(
            acceptClips[2].Length,
            ref lastAcceptGIndex
        );

        audioSource.PlayOneShot(
            acceptClips[2][gIndex]
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

        // Tomar la siguiente variante.
        int index =
            undoPlayOrder[undoOrderPosition];

        undoOrderPosition++;

        // Si ya usamos las 16,
        // crear otro orden completamente nuevo.
        if (
            undoOrderPosition >=
            undoPlayOrder.Count
        )
        {
            RebuildUndoPlayOrder();
        }

        lastUndoIndex = index;

        // Obtener volumen de esta variante.
        float volume =
            undoVolume;

        if (
            undoVariantVolumes != null &&
            index >= 0 &&
            index < undoVariantVolumes.Length
        )
        {
            volume =
                undoVariantVolumes[index];
        }

        // Reproducir.
        audioSource.PlayOneShot(
            undoClips[index],
            volume
        );
    }

    // ============================================================
    // KEYBOARD
    // ============================================================

    private void BuildKeyboardBanks()
    {
        keyClips = GenerateBank(
            keyboardPreset,
            keyBaseFrequency,
            keyDuration,
            keyVariants,
            keyPitchVariation
        );

        backspaceClips = GenerateBank(
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

        int index = PickIndex(
            keyClips.Length,
            ref lastKeyIndex
        );

        audioSource.PlayOneShot(
            keyClips[index]
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

        int index = PickIndex(
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

        variants = Mathf.Max(
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

        undoVariantVolumes = null;

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