using UnityEngine;
using AudioProceduralAvatar.Avatar;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// SOLO PARA PRUEBAS. Genera un avatar hardcodeado al presionar Play (o
    /// Espacio) para validar que Generator + Renderer suenan de verdad, sin
    /// esperar a que exista la UI de personalización.
    ///
    /// Cómo usarlo:
    /// 1. Crea un GameObject vacío en la escena, ponle LeitmotivGenerator,
    ///    SimpleSynthRenderer, y este script.
    /// 2. Asigna un LeitmotivMappingConfig al Generator (opcional, tiene fallback).
    /// 3. Asigna al menos un InstrumentPreset a la lista "Presets" del Renderer,
    ///    y uno como "Fallback Preset".
    /// 4. Dale Play. Presiona Espacio para generar un avatar nuevo random
    ///    y escuchar su leitmotiv.
    /// </summary>
    [RequireComponent(typeof(LeitmotivGenerator))]
    [RequireComponent(typeof(SimpleSynthRenderer))]
    public class LeitmotivDemo : MonoBehaviour
    {
        private LeitmotivGenerator _generator;
        private SimpleSynthRenderer _renderer;

        private void Awake()
        {
            _generator = GetComponent<LeitmotivGenerator>();
            _renderer = GetComponent<SimpleSynthRenderer>();
        }

        private void Start()
        {
            PlayRandomAvatar();
        }

        private void Update()
        {
            if (SpacePressedThisFrame())
                PlayRandomAvatar();
        }

        private static bool SpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private void PlayRandomAvatar()
        {
            var attrs = new AvatarAttributes
            {
                AvatarName = "Demo_" + Random.Range(0, 10000),
                Clothing = (ClothingType)Random.Range(0, System.Enum.GetValues(typeof(ClothingType)).Length),
                AccentColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f),
                Accessory = (AccessoryType)Random.Range(0, System.Enum.GetValues(typeof(AccessoryType)).Length),
                Trait = (CharacterTrait)Random.Range(0, System.Enum.GetValues(typeof(CharacterTrait)).Length),
            };

            var leitmotiv = _generator.Generate(attrs);
            _renderer.PlayLeitmotiv(leitmotiv);

            Debug.Log($"[LeitmotivDemo] {attrs.AvatarName} | {attrs.Clothing}/{attrs.Accessory}/{attrs.Trait} " +
                      $"-> Scale={leitmotiv.Scale} Root={leitmotiv.RootNoteMidi} Tempo={leitmotiv.TempoBpm} Instrument={leitmotiv.InstrumentHint}");
        }
    }
}
