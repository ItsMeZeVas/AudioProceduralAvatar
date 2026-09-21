using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sonido de teclado para un campo de texto.
///
/// Modo InputField: suena cada vez que el jugador escribe (o borra) en un TMP_InputField.
/// Modo TypeOut:    el texto de un TMP_Text se "escribe solo", letra por letra, con sonido.
/// </summary>
public class KeyboardTypingEffect : MonoBehaviour
{
    public enum Mode
    {
        InputField,
        TypeOut
    }

    [Header("Referencias")]
    public AvatarAudioManager audioManager;

    [Header("Modo")]
    public Mode mode = Mode.InputField;

    // =================================================
    // MODO INPUT FIELD
    // =================================================

    [Header("Modo InputField (el jugador escribe)")]
    [Tooltip("El objeto que contiene 'Text Area' como hijo.")]
    public TMP_InputField inputField;

    // =================================================
    // MODO TYPE OUT
    // =================================================

    [Header("Modo TypeOut (el texto se escribe solo)")]
    public TMP_Text targetText;

    [Tooltip("Letras por segundo.")]
    public float charactersPerSecond = 25f;

    [Tooltip("Variación aleatoria del ritmo (0 = mecánico, 0.3 = humano).")]
    [Range(0f, 0.6f)]
    public float timingJitter = 0.3f;

    [Tooltip("No suena al mostrar espacios ni saltos de línea.")]
    public bool silentOnSpaces = true;

    public float startDelay = 0f;

    [Tooltip("Empieza a escribir automáticamente al activarse el objeto.")]
    public bool playOnEnable = true;

    public UnityEvent onFinished;

    private Coroutine typingRoutine;
    private int lastLength;

    // =================================================
    // CICLO DE VIDA
    // =================================================

    private void OnEnable()
    {
        if (mode == Mode.InputField)
        {
            if (inputField != null)
            {
                lastLength = inputField.text.Length;

                inputField.onValueChanged.AddListener(
                    HandleValueChanged
                );
            }
        }
        else if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(
                HandleValueChanged
            );
        }

        StopTyping();
    }

    // =================================================
    // INPUT FIELD
    // =================================================

    private void HandleValueChanged(string value)
    {
        if (audioManager == null)
        {
            return;
        }

        int length = value.Length;

        if (length > lastLength)
        {
            audioManager.PlayKeyClick();
        }
        else if (length < lastLength)
        {
            audioManager.PlayKeyBackspace();
        }

        lastLength = length;
    }

    // =================================================
    // TYPE OUT (API pública)
    // =================================================

    /// <summary>Escribe el texto que ya tiene targetText.</summary>
    public void Play()
    {
        if (targetText == null)
        {
            Debug.LogWarning(
                "KeyboardTypingEffect: falta asignar Target Text."
            );

            return;
        }

        StopTyping();

        typingRoutine =
            StartCoroutine(TypeRoutine());
    }

    /// <summary>Reemplaza el texto y lo escribe desde cero.</summary>
    public void Play(string newText)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = newText;

        Play();
    }

    /// <summary>Muestra todo el texto de golpe, sin sonido.</summary>
    public void Skip()
    {
        StopTyping();

        if (targetText != null)
        {
            targetText.maxVisibleCharacters =
                int.MaxValue;
        }
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }

    private IEnumerator TypeRoutine()
    {
        // Oculta todo antes de calcular el texto.
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        int total =
            targetText.textInfo.characterCount;

        if (startDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(startDelay);
        }

        float baseDelay =
            1f /
            Mathf.Max(
                1f,
                charactersPerSecond
            );

        for (int i = 0; i < total; i++)
        {
            targetText.maxVisibleCharacters = i + 1;

            char c =
                targetText.textInfo
                    .characterInfo[i]
                    .character;

            bool silent =
                silentOnSpaces &&
                char.IsWhiteSpace(c);

            if (!silent && audioManager != null)
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

        typingRoutine = null;

        onFinished?.Invoke();
    }
}
