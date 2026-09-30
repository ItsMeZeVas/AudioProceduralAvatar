using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de sonidos de teclado.
///
/// InputField:
/// - Escribir = Keyboard Typing
/// - Borrar = Keyboard Backspace
///
/// TypeOut:
/// - Cada carácter utiliza Keyboard Typing
/// </summary>
public class KeyboardTypingEffect : MonoBehaviour
{
    public enum Mode
    {
        InputField,
        TypeOut
    }

    // ============================================================
    // REFERENCIAS
    // ============================================================

    [Header("Referencias")]

    public AvatarAudioManager audioManager;

    // ============================================================
    // MODO
    // ============================================================

    [Header("Modo")]

    public Mode mode =
        Mode.InputField;

    // ============================================================
    // INPUT FIELD
    // ============================================================

    [Header("Modo InputField")]

    [Tooltip(
        "TMP_InputField donde escribe el jugador."
    )]
    public TMP_InputField inputField;

    // ============================================================
    // TYPE OUT
    // ============================================================

    [Header("Modo TypeOut")]

    public TMP_Text targetText;

    [Tooltip("Letras por segundo.")]
    public float charactersPerSecond = 25f;

    [Tooltip(
        "Variación aleatoria del ritmo."
    )]
    [Range(0f, 0.6f)]
    public float timingJitter = 0.3f;

    [Tooltip(
        "No reproduce sonido para espacios."
    )]
    public bool silentOnSpaces = true;

    public float startDelay = 0f;

    [Tooltip(
        "Comienza automáticamente al activarse."
    )]
    public bool playOnEnable = true;

    public UnityEvent onFinished;

    // ============================================================
    // ESTADO
    // ============================================================

    private Coroutine typingRoutine;

    private int lastLength;

    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        if (mode == Mode.InputField)
        {
            if (inputField != null)
            {
                lastLength =
                    inputField.text.Length;

                inputField.onValueChanged
                    .AddListener(
                        HandleValueChanged
                    );
            }
        }
        else if (playOnEnable)
        {
            Play();
        }
    }

    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged
                .RemoveListener(
                    HandleValueChanged
                );
        }

        StopTyping();
    }

    // ============================================================
    // INPUT FIELD
    // ============================================================

    private void HandleValueChanged(
        string value
    )
    {
        if (audioManager == null)
        {
            return;
        }

        int length =
            value.Length;

        // ========================================================
        // ESCRIBIR
        // ========================================================

        if (length > lastLength)
        {
            audioManager.PlayKeyClick();
        }

        // ========================================================
        // BORRAR
        // ========================================================

        else if (length < lastLength)
        {
            audioManager.PlayKeyBackspace();
        }

        lastLength =
            length;
    }

    // ============================================================
    // PLAY
    // ============================================================

    public void Play()
    {
        if (targetText == null)
        {
            Debug.LogWarning(
                "KeyboardTypingEffect: " +
                "falta asignar Target Text."
            );

            return;
        }

        StopTyping();

        typingRoutine =
            StartCoroutine(
                TypeRoutine()
            );
    }

    // ============================================================
    // PLAY CON TEXTO NUEVO
    // ============================================================

    public void Play(
        string newText
    )
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text =
            newText;

        Play();
    }

    // ============================================================
    // SKIP
    // ============================================================

    public void Skip()
    {
        StopTyping();

        if (targetText != null)
        {
            targetText.maxVisibleCharacters =
                int.MaxValue;
        }
    }

    // ============================================================
    // STOP
    // ============================================================

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(
                typingRoutine
            );

            typingRoutine = null;
        }
    }

    // ============================================================
    // TYPE ROUTINE
    // ============================================================

    private IEnumerator TypeRoutine()
    {
        targetText.maxVisibleCharacters =
            0;

        targetText.ForceMeshUpdate();

        int total =
            targetText.textInfo.characterCount;

        if (startDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    startDelay
                );
        }

        float baseDelay =
            1f /
            Mathf.Max(
                1f,
                charactersPerSecond
            );

        for (
            int i = 0;
            i < total;
            i++
        )
        {
            targetText.maxVisibleCharacters =
                i + 1;

            char c =
                targetText
                    .textInfo
                    .characterInfo[i]
                    .character;

            bool silent =
                silentOnSpaces &&
                char.IsWhiteSpace(c);

            if (
                !silent &&
                audioManager != null
            )
            {
                audioManager.PlayKeyClick();
            }

            float jitter =
                1f +
                Random.Range(
                    -timingJitter,
                    timingJitter
                );

            yield return
                new WaitForSecondsRealtime(
                    baseDelay * jitter
                );
        }

        targetText.maxVisibleCharacters =
            int.MaxValue;

        typingRoutine =
            null;

        onFinished?.Invoke();
    }
}