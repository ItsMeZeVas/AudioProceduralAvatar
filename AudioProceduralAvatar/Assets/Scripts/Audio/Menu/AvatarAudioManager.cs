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

    [Header("Keyboard Typing")]
    [Tooltip("Nota base del click (Hz).")]
    public float keyBaseFrequency = 800f;

    [Tooltip("Variación aleatoria de tono, en semitonos (±).")]
    [Range(0f, 4f)]
    public float keyPitchVariation = 1.5f;

    [Tooltip("Cuántas variantes se pre-generan. Más = menos repetición.")]
    [Range(1, 32)]
    public int keyVariants = 8;

    public float keyDuration = 0.07f;

    [Tooltip("Cuánto se baja el tono al borrar (semitonos).")]
    public float backspaceSemitones = -4f;

    [Range(0f, 1f)]
    public float backspaceVolume = 0.8f;

    [Header("Audio")]
    public AudioSource audioSource;

    private ProceduralSynth synthesizer;

    private AudioClip[] keyClips;
    private AudioClip[] backspaceClips;
    private int lastKeyIndex = -1;
    private int lastBackspaceIndex = -1;

    private void Awake()
    {
        synthesizer =
            new ProceduralSynth();

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        // Se pre-generan al inicio para no calcular
        // audio en cada pulsación.
        if (keyboardPreset != null)
        {
            BuildKeyboardBanks();
        }
    }

    private void OnDestroy()
    {
        DestroyBank(keyClips);
        DestroyBank(backspaceClips);
    }

    // =================================================
    // SELECCIÓN
    // =================================================

    public void PlaySelect()
    {
        if (selectPreset == null)
        {
            Debug.LogWarning(
                "No hay Select Preset asignado."
            );

            return;
        }

        // A4 = 440 Hz
        PlayNote(
            selectPreset,
            440f,
            0.25f
        );
    }

    // =================================================
    // ACEPTAR
    // =================================================

    public void PlayAcceptChanges()
    {
        if (acceptPreset == null)
        {
            Debug.LogWarning(
                "No hay Accept Preset asignado."
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
        PlayNote(
            acceptPreset,
            523.25f,
            0.18f
        );

        yield return
            new WaitForSeconds(0.07f);

        // E5
        PlayNote(
            acceptPreset,
            659.25f,
            0.18f
        );

        yield return
            new WaitForSeconds(0.07f);

        // G5
        PlayNote(
            acceptPreset,
            783.99f,
            0.25f
        );
    }

    // =================================================
    // DESHACER / RETROCESO
    // =================================================

    public void PlayUndo()
    {
        if (undoPreset == null)
        {
            Debug.LogWarning(
                "No hay Undo Preset asignado."
            );

            return;
        }

        // A4 = 440 Hz. El pitch envelope del preset
        // baja desde +36 hasta -36 semitonos.
        PlayNote(
            undoPreset,
            440f,
            0.45f
        );
    }

    // =================================================
    // TECLADO   (NUEVO)
    // =================================================

    public void PlayKeyClick()
    {
        if (!EnsureKeyboardBanks())
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
            Random.Range(0.85f, 1f)
        );
    }

    public void PlayKeyBackspace()
    {
        if (!EnsureKeyboardBanks())
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
            backspaceVolume *
            Random.Range(0.85f, 1f)
        );
    }

    private bool EnsureKeyboardBanks()
    {
        if (keyboardPreset == null)
        {
            Debug.LogWarning(
                "No hay Keyboard Preset asignado."
            );

            return false;
        }

        if (keyClips == null)
        {
            BuildKeyboardBanks();
        }

        return true;
    }

    // Llamar de nuevo si cambias el preset o los
    // parámetros del teclado en tiempo de ejecución.
    public void BuildKeyboardBanks()
    {
        if (synthesizer == null)
        {
            synthesizer =
                new ProceduralSynth();
        }

        DestroyBank(keyClips);
        DestroyBank(backspaceClips);

        keyClips =
            GenerateBank(0f);

        backspaceClips =
            GenerateBank(backspaceSemitones);

        lastKeyIndex = -1;
        lastBackspaceIndex = -1;
    }

    private AudioClip[] GenerateBank(
        float semitoneShift)
    {
        AudioClip[] bank =
            new AudioClip[keyVariants];

        for (int i = 0; i < bank.Length; i++)
        {
            float variation =
                Random.Range(
                    -keyPitchVariation,
                    keyPitchVariation
                );

            float frequency =
                keyBaseFrequency *
                Mathf.Pow(
                    2f,
                    (semitoneShift + variation) / 12f
                );

            // Con randomizeLfoPhase activo en el preset,
            // cada variante además tiene un timbre distinto.
            bank[i] =
                synthesizer.Generate(
                    keyboardPreset,
                    frequency,
                    keyDuration
                );
        }

        return bank;
    }

    private void DestroyBank(
        AudioClip[] bank)
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

    // Evita repetir la misma variante dos veces seguidas.
    private int PickIndex(
        int length,
        ref int lastIndex)
    {
        int index =
            Random.Range(0, length);

        if (length > 1 && index == lastIndex)
        {
            index = (index + 1) % length;
        }

        lastIndex = index;

        return index;
    }

    // =================================================
    // GENERADOR
    // =================================================

    private void PlayNote(
        AudioPreset preset,
        float frequency,
        float duration)
    {
        AudioClip clip =
            synthesizer.Generate(
                preset,
                frequency,
                duration
            );

        audioSource.PlayOneShot(
            clip
        );
    }
}
