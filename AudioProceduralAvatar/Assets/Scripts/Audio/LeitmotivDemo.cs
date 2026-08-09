using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// SOLO PARA PRUEBAS. Genera un AvatarProfile random (capas Body/Head/Hair
    /// con índices al azar) al presionar Play o Espacio, para validar que
    /// Generator + Renderer suenan de verdad sin depender de la UI real.
    /// </summary>
    [RequireComponent(typeof(LeitmotivGenerator))]
    [RequireComponent(typeof(SimpleSynthRenderer))]
    public class LeitmotivDemo : MonoBehaviour
    {
        [SerializeField] private string[] layerNames = { "Body", "Head", "Hair" };
        [SerializeField] private int spritesPerLayer = 3;

        private LeitmotivGenerator _generator;
        private SimpleSynthRenderer _renderer;

        private void Awake()
        {
            _generator = GetComponent<LeitmotivGenerator>();
            _renderer = GetComponent<SimpleSynthRenderer>();
        }

        private void Start() => PlayRandomAvatar();

        private void Update()
        {
            if (SpacePressedThisFrame())
                PlayRandomAvatar();
        }

        private void PlayRandomAvatar()
        {
            var profile = new AvatarProfile
            {
                Id = System.Guid.NewGuid().ToString(),
                AvatarName = "Demo_" + Random.Range(0, 10000),
                Layers = new List<LayerSelection>()
            };

            foreach (var layerName in layerNames)
                profile.Layers.Add(new LayerSelection { LayerName = layerName, SpriteIndex = Random.Range(0, spritesPerLayer) });

            var leitmotiv = _generator.Generate(profile);
            _renderer.PlayLeitmotiv(leitmotiv);

            Debug.Log($"[LeitmotivDemo] {profile.AvatarName} -> Scale={leitmotiv.Scale} Root={leitmotiv.RootNoteMidi} Tempo={leitmotiv.TempoBpm} Instrument={leitmotiv.InstrumentHint}");
        }

        private static bool SpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
