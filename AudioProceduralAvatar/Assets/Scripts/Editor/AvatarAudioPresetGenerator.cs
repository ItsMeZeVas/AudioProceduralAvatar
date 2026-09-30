#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Crea automáticamente los presets de audio de la interfaz
/// y los asigna al AvatarAudioManager seleccionado.
/// </summary>
public static class AvatarAudioPresetGenerator
{
    private const string FolderPath =
        "Assets/AudioPresets/Generated UI";

    // ============================================================
    // MENU DE UNITY
    // ============================================================

    [MenuItem("Avatar Audio/Generar todos los presets UI")]
    public static void GenerateAll()
    {
        CreateFolderIfNeeded();

        AvatarAudioManager manager =
            Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<AvatarAudioManager>()
                : null;

        // ========================================================
        // TECLADO
        // ========================================================

        AudioPreset keyboardTyping =
            CreateOrUpdatePreset(
                "UI_KeyboardTyping",
                AvatarSoundType.KeyboardTyping,
                ConfigureKeyboardTyping
            );

        AudioPreset keyboardBackspace =
            CreateOrUpdatePreset(
                "UI_KeyboardBackspace",
                AvatarSoundType.KeyboardBackspace,
                ConfigureKeyboardBackspace
            );

        // Preset general por compatibilidad
        AudioPreset keyboard =
            CreateOrUpdatePreset(
                "UI_Keyboard",
                AvatarSoundType.KeyboardTyping,
                ConfigureKeyboardTyping
            );

        // ========================================================
        // ELIMINAR
        // ========================================================

        AudioPreset delete =
            CreateOrUpdatePreset(
                "UI_Delete",
                AvatarSoundType.Delete,
                ConfigureDelete
            );

        // ========================================================
        // SLIDER DE COLOR
        // ========================================================

        AudioPreset colorSlider =
            CreateOrUpdatePreset(
                "UI_ColorSlider",
                AvatarSoundType.ColorSlider,
                ConfigureColorSlider
            );

        // ========================================================
        // PAGINAS
        // ========================================================

        AudioPreset pagePrevious =
            CreateOrUpdatePreset(
                "UI_PagePrevious",
                AvatarSoundType.PagePrevious,
                ConfigurePagePrevious
            );

        AudioPreset pageNext =
            CreateOrUpdatePreset(
                "UI_PageNext",
                AvatarSoundType.PageNext,
                ConfigurePageNext
            );

        // ========================================================
        // QR EXITOSO
        // ========================================================

        AudioPreset qrSuccess =
            CreateOrUpdatePreset(
                "UI_QRSuccess",
                AvatarSoundType.QRSuccess,
                ConfigureQRSuccess
            );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ========================================================
        // ASIGNAR AL AVATAR AUDIO MANAGER
        // ========================================================

        if (manager != null)
        {
            AssignPresetsToManager(
                manager,
                keyboardTyping,
                keyboardBackspace,
                keyboard,
                delete,
                colorSlider,
                pagePrevious,
                pageNext,
                qrSuccess
            );
        }

        AssetDatabase.SaveAssets();

        Selection.activeObject =
            manager != null
                ? manager.gameObject
                : keyboardTyping;

        EditorUtility.DisplayDialog(
            "Audio UI generado",
            "Se crearon/configuraron todos los presets de interfaz:\n\n" +
            "• Keyboard Typing\n" +
            "• Keyboard Backspace\n" +
            "• Delete\n" +
            "• Color Slider\n" +
            "• Page Previous\n" +
            "• Page Next\n" +
            "• QR Success\n\n" +
            "También fueron asignados automáticamente al AvatarAudioManager.",
            "Perfecto"
        );
    }

    // ============================================================
    // CREAR / ACTUALIZAR PRESET
    // ============================================================

    private static AudioPreset CreateOrUpdatePreset(
        string fileName,
        AvatarSoundType soundType,
        System.Action<AudioPreset> configure
    )
    {
        string path =
            FolderPath +
            "/" +
            fileName +
            ".asset";

        AudioPreset preset =
            AssetDatabase.LoadAssetAtPath<AudioPreset>(
                path
            );

        if (preset == null)
        {
            preset =
                ScriptableObject.CreateInstance<AudioPreset>();

            AssetDatabase.CreateAsset(
                preset,
                path
            );
        }

        preset.soundType =
            soundType;

        configure(preset);

        EditorUtility.SetDirty(preset);

        return preset;
    }

    // ============================================================
    // CONFIGURACION BASE
    // ============================================================

    private static void ResetPreset(
        AudioPreset p
    )
    {
        p.volume = 1f;

        // OSC A
        p.oscillatorA = WaveType.Sine;
        p.oscillatorASemitones = 0f;
        p.oscillatorADb = -5f;

        // OSC B
        p.oscillatorB = WaveType.Triangle;
        p.oscillatorBSemitones = 12f;
        p.oscillatorBDb = -13f;

        // OSC C
        p.oscillatorC = WaveType.Square;
        p.oscillatorCSemitones = 19f;
        p.oscillatorCDb = -33f;

        // OSC D
        p.useOscillatorD = false;
        p.oscillatorD = WaveType.Triangle;
        p.oscillatorDSemitones = 0f;

        // PHASE
        p.usePhaseModulation = false;
        p.phaseModAmount = 0f;
        p.phaseModCToB = 0f;
        p.phaseModDToA = 0f;

        // ADSR
        p.attack = 0.005f;
        p.decay = 0.06f;
        p.sustain = 0.1f;
        p.release = 0.04f;

        // ENV
        p.env2.attack = 0.001f;
        p.env2.decay = 0.03f;
        p.env2.sustain = 0f;
        p.env2.release = 0.02f;

        p.env3.attack = 0.001f;
        p.env3.decay = 0.03f;
        p.env3.sustain = 0f;
        p.env3.release = 0.02f;

        // LFO
        p.lfo1.wave = WaveType.Sine;
        p.lfo1.rateHz = 8f;

        p.lfo2.wave = WaveType.Sine;
        p.lfo2.rateHz = 5f;

        p.randomizeLfoPhase = false;

        // AM
        p.ampModEnv3ToA = 0f;
        p.ampModLfo1ToC = 0f;
        p.ampModMainToD = 0f;

        // PITCH
        p.pitchEnvelope = false;
        p.pitchAmount = 0f;
        p.pitchAttackLevel = 0f;
        p.pitchEndAmount = 0f;
        p.pitchAttack = 0.005f;
        p.pitchDecay = 0.04f;

        // FILTER
        p.useFilter = true;
        p.filterCutoff = 2200f;
        p.filterResonance = 0.15f;
        p.resonantFilter = false;

        // FILTER MODULATION
        p.filterModMain = 0f;
        p.filterModEnv2 = 0f;
        p.filterModEnv3 = 0f;
        p.filterModLfo1 = 0f;
        p.filterModLfo2 = 0f;
        p.filterModOctaves = 1f;

        // EQ
        p.useEQ = false;
        p.lowShelfDb = 0f;
        p.lowMidDb = 0f;
        p.highMidDb = 0f;
    }

    // ============================================================
    // KEYBOARD TYPING
    // ============================================================

    private static void ConfigureKeyboardTyping(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.55f;

        p.oscillatorA =
            WaveType.Triangle;

        p.oscillatorASemitones =
            0f;

        p.oscillatorADb =
            -4f;

        p.oscillatorB =
            WaveType.Sine;

        p.oscillatorBSemitones =
            12f;

        p.oscillatorBDb =
            -16f;

        p.oscillatorC =
            WaveType.Sine;

        p.oscillatorCSemitones =
            19f;

        p.oscillatorCDb =
            -32f;

        p.attack = 0.002f;
        p.decay = 0.035f;
        p.sustain = 0.02f;
        p.release = 0.025f;

        // Más grave y menos chillón
        p.filterCutoff =
            1700f;

        p.filterResonance =
            0.05f;

        p.useEQ = true;
        p.lowShelfDb = 1.5f;
    }

    // ============================================================
    // KEYBOARD BACKSPACE
    // ============================================================

    private static void ConfigureKeyboardBackspace(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.50f;

        p.oscillatorA =
            WaveType.Triangle;

        p.oscillatorASemitones =
            -5f;

        p.oscillatorADb =
            -3f;

        p.oscillatorB =
            WaveType.Sine;

        p.oscillatorBSemitones =
            5f;

        p.oscillatorBDb =
            -15f;

        p.oscillatorC =
            WaveType.Sine;

        p.oscillatorCSemitones =
            12f;

        p.oscillatorCDb =
            -34f;

        p.attack = 0.002f;
        p.decay = 0.05f;
        p.sustain = 0.01f;
        p.release = 0.035f;

        p.filterCutoff =
            1300f;

        p.filterResonance =
            0.04f;

        p.useEQ = true;
        p.lowShelfDb = 2f;
    }

    // ============================================================
    // DELETE
    // ============================================================

    private static void ConfigureDelete(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.65f;

        p.oscillatorA =
            WaveType.Triangle;

        p.oscillatorASemitones =
            -7f;

        p.oscillatorADb =
            -3f;

        p.oscillatorB =
            WaveType.Sine;

        p.oscillatorBSemitones =
            -12f;

        p.oscillatorBDb =
            -12f;

        p.attack = 0.003f;
        p.decay = 0.09f;
        p.sustain = 0f;
        p.release = 0.05f;

        p.filterCutoff =
            1200f;

        p.filterResonance =
            0.12f;
    }

    // ============================================================
    // COLOR SLIDER
    // ============================================================

    private static void ConfigureColorSlider(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.48f;

        p.oscillatorA =
            WaveType.Sine;

        p.oscillatorASemitones =
            0f;

        p.oscillatorADb =
            -5f;

        p.oscillatorB =
            WaveType.Triangle;

        p.oscillatorBSemitones =
            7f;

        p.oscillatorBDb =
            -14f;

        p.attack = 0.001f;
        p.decay = 0.025f;
        p.sustain = 0f;
        p.release = 0.018f;

        p.filterCutoff =
            2400f;

        p.filterResonance =
            0.03f;
    }

    // ============================================================
    // PAGINA ANTERIOR
    // ============================================================

    private static void ConfigurePagePrevious(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.55f;

        p.oscillatorA =
            WaveType.Triangle;

        p.oscillatorASemitones =
            -4f;

        p.oscillatorADb =
            -4f;

        p.oscillatorB =
            WaveType.Sine;

        p.oscillatorBSemitones =
            3f;

        p.oscillatorBDb =
            -13f;

        p.attack = 0.003f;
        p.decay = 0.07f;
        p.sustain = 0.08f;
        p.release = 0.045f;

        p.filterCutoff =
            1500f;
    }

    // ============================================================
    // PAGINA SIGUIENTE
    // ============================================================

    private static void ConfigurePageNext(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.55f;

        p.oscillatorA =
            WaveType.Triangle;

        p.oscillatorASemitones =
            0f;

        p.oscillatorADb =
            -4f;

        p.oscillatorB =
            WaveType.Sine;

        p.oscillatorBSemitones =
            7f;

        p.oscillatorBDb =
            -12f;

        p.attack = 0.003f;
        p.decay = 0.07f;
        p.sustain = 0.08f;
        p.release = 0.045f;

        p.filterCutoff =
            1900f;
    }

    // ============================================================
    // QR SUCCESS
    // ============================================================

    private static void ConfigureQRSuccess(
        AudioPreset p
    )
    {
        ResetPreset(p);

        p.volume = 0.70f;

        p.oscillatorA =
            WaveType.Sine;

        p.oscillatorASemitones =
            0f;

        p.oscillatorADb =
            -3f;

        p.oscillatorB =
            WaveType.Triangle;

        p.oscillatorBSemitones =
            12f;

        p.oscillatorBDb =
            -9f;

        p.oscillatorC =
            WaveType.Sine;

        p.oscillatorCSemitones =
            19f;

        p.oscillatorCDb =
            -18f;

        p.attack = 0.004f;
        p.decay = 0.08f;
        p.sustain = 0.25f;
        p.release = 0.11f;

        p.filterCutoff =
            3500f;

        p.filterResonance =
            0.08f;
    }

    // ============================================================
    // ASIGNAR AL MANAGER
    // ============================================================

    private static void AssignPresetsToManager(
        AvatarAudioManager manager,
        AudioPreset keyboardTyping,
        AudioPreset keyboardBackspace,
        AudioPreset keyboard,
        AudioPreset delete,
        AudioPreset colorSlider,
        AudioPreset pagePrevious,
        AudioPreset pageNext,
        AudioPreset qrSuccess
    )
    {
        SerializedObject so =
            new SerializedObject(manager);

        Assign(
            so,
            "keyboardTypingPreset",
            keyboardTyping
        );

        Assign(
            so,
            "keyboardBackspacePreset",
            keyboardBackspace
        );

        Assign(
            so,
            "keyboardPreset",
            keyboard
        );

        Assign(
            so,
            "deletePreset",
            delete
        );

        Assign(
            so,
            "colorSliderPreset",
            colorSlider
        );

        Assign(
            so,
            "pagePreviousPreset",
            pagePrevious
        );

        Assign(
            so,
            "pageNextPreset",
            pageNext
        );

        Assign(
            so,
            "qrSuccessPreset",
            qrSuccess
        );

        // ========================================================
        // CONFIGURACION DEL TECLADO
        // ========================================================

        SetFloat(
            so,
            "keyBaseFrequency",
            220f
        );

        SetFloat(
            so,
            "keyDuration",
            0.065f
        );

        SetFloat(
            so,
            "keyPitchVariation",
            0.8f
        );

        SetFloat(
            so,
            "keyboardVolume",
            0.65f
        );

        SetFloat(
            so,
            "backspaceVolume",
            0.60f
        );

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(manager);
    }

    // ============================================================
    // SERIALIZED HELPERS
    // ============================================================

    private static void Assign(
        SerializedObject so,
        string propertyName,
        AudioPreset preset
    )
    {
        SerializedProperty property =
            so.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue =
                preset;
        }
    }

    private static void SetFloat(
        SerializedObject so,
        string propertyName,
        float value
    )
    {
        SerializedProperty property =
            so.FindProperty(propertyName);

        if (property != null)
        {
            property.floatValue =
                value;
        }
    }

    // ============================================================
    // CREAR CARPETA
    // ============================================================

    private static void CreateFolderIfNeeded()
    {
        if (!AssetDatabase.IsValidFolder("Assets/AudioPresets"))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "AudioPresets"
            );
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder(
                "Assets/AudioPresets",
                "Generated UI"
            );
        }
    }
}

#endif