using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AvatarAudioManager : MonoBehaviour
{
    [Header("Presets")]
    public AudioPreset selectPreset;
    public AudioPreset acceptPreset;
    public AudioPreset undoPreset;
    public AudioPreset keyboardPreset;

    [Header("Variaciones de Select")]
    [Min(2)]
    public int selectVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float selectPitchVariation = 0.6f;

    [Header("Variaciones de Accept")]
    [Min(2)]
    public int acceptVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float acceptPitchVariation = 0.5f;

    [Header("Variaciones de Undo")]
    [Min(2)]
    public int undoVariants = 8;

    [Tooltip("Variación máxima de pitch en semitonos.")]
    [Range(0f, 2f)]
    public float undoPitchVariation = 0.8f;

    [Range(0f, 1f)]
    public float undoVolume = 0.55f;

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
    // BANKS - SELECT / ACCEPT / UNDO
    // ============================================================

    private AudioClip[] selectClips;
    private AudioClip[] undoClips;

    // Accept tiene 3 notas:
    // 0 = C5
    // 1 = E5
    // 2 = G5
    private AudioClip[][] acceptClips;

    private int lastSelectIndex = -1;
    private int lastUndoIndex = -1;

    private int lastAcceptCIndex = -1;
    private int lastAcceptEIndex = -1;
    private int lastAcceptGIndex = -1;

    // ============================================================
    // KEYBOARD BANKS
    // ============================================================

    private AudioClip[] keyClips;
    private AudioClip[] backspaceClips;

    private int lastKeyIndex = -1;
    private int lastBackspaceIndex = -1;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

        synthesizer = new ProceduralSynth();

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

        // --------------------------------------------------------
        // SELECT
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // UNDO
        // --------------------------------------------------------

        if (undoPreset != null)
        {
            undoClips = GenerateBank(
                undoPreset,
                440f,
                0.45f,
                undoVariants,
                undoPitchVariation
            );
        }

        // --------------------------------------------------------
        // ACCEPT
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // KEYBOARD
        // --------------------------------------------------------

        if (keyboardPreset != null)
        {
            BuildKeyboardBanks();
        }
    }

    // ============================================================
    // SELECT
    // ============================================================

    public void PlaySelect()
    {
        if (selectClips == null || selectClips.Length == 0)
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

        audioSource.PlayOneShot(selectClips[index]);
    }

    // ============================================================
    // ACCEPT
    // ============================================================

    public void PlayAcceptChanges()
    {
        if (acceptClips == null ||
            acceptClips.Length != 3 ||
            acceptClips[0] == null ||
            acceptClips[1] == null ||
            acceptClips[2] == null)
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco de Accept. " +
                "Verifica que acceptPreset esté asignado."
            );

            return;
        }

        StartCoroutine(AcceptSequence());
    }

    private IEnumerator AcceptSequence()
    {
        // C5
        int cIndex = PickIndex(
            acceptClips[0].Length,
            ref lastAcceptCIndex
        );

        audioSource.PlayOneShot(acceptClips[0][cIndex]);

        yield return new WaitForSeconds(0.07f);

        // E5
        int eIndex = PickIndex(
            acceptClips[1].Length,
            ref lastAcceptEIndex
        );

        audioSource.PlayOneShot(acceptClips[1][eIndex]);

        yield return new WaitForSeconds(0.07f);

        // G5
        int gIndex = PickIndex(
            acceptClips[2].Length,
            ref lastAcceptGIndex
        );

        audioSource.PlayOneShot(acceptClips[2][gIndex]);
    }

    // ============================================================
    // UNDO
    // ============================================================

    public void PlayUndo()
    {
        if (undoClips == null || undoClips.Length == 0)
        {
            Debug.LogWarning(
                "AvatarAudioManager: No hay banco de Undo. " +
                "Verifica que undoPreset esté asignado."
            );

            return;
        }

        int index = PickIndex(
            undoClips.Length,
            ref lastUndoIndex
        );

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
        if (keyClips == null || keyClips.Length == 0)
        {
            return;
        }

        int index = PickIndex(
            keyClips.Length,
            ref lastKeyIndex
        );

        audioSource.PlayOneShot(keyClips[index]);
    }

    public void PlayKeyBackspace()
    {
        if (backspaceClips == null || backspaceClips.Length == 0)
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

        variants = Mathf.Max(2, variants);

        AudioClip[] bank = new AudioClip[variants];

        for (int i = 0; i < variants; i++)
        {
            // Variación pequeña de pitch.
            float randomSemitones = Random.Range(
                -pitchVariation,
                pitchVariation
            );

            randomSemitones += additionalSemitones;

            // Convertir semitonos a multiplicador de frecuencia.
            float frequency =
                baseFrequency *
                Mathf.Pow(2f, randomSemitones / 12f);

            bank[i] = synthesizer.Generate(
                preset,
                frequency,
                duration
            );
        }

        return bank;
    }

    // ============================================================
    // ELEGIR VARIACIÓN SIN REPETIR INMEDIATAMENTE
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

        int index = Random.Range(0, length);

        // Evita repetir exactamente la misma variación
        // dos veces seguidas.
        if (index == lastIndex)
        {
            index = (index + 1) % length;
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

        if (acceptClips != null)
        {
            for (int i = 0; i < acceptClips.Length; i++)
            {
                DestroyBank(acceptClips[i]);
            }
        }

        selectClips = null;
        undoClips = null;
        keyClips = null;
        backspaceClips = null;
        acceptClips = null;
    }

    private void DestroyBank(AudioClip[] bank)
    {
        if (bank == null)
        {
            return;
        }

        for (int i = 0; i < bank.Length; i++)
        {
            if (bank[i] != null)
            {
                Destroy(bank[i]);
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