using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Motor de decisión musical. Recibe AvatarAttributes, entrega LeitmotivData.
    /// No reproduce audio — eso lo hace un IMusicRenderer.
    ///
    /// Las reglas de mapeo (Trait->Escala, Accesorio->Tempo, Ropa->Instrumento)
    /// NO están aquí — viven en un asset LeitmotivMappingConfig editable desde
    /// el Inspector de Unity, para que Diseño pueda ajustarlas sin tocar código.
    /// Si no se asigna un config, se usan valores por defecto razonables
    /// (para que el pipeline nunca se rompa por falta de configuración).
    /// </summary>
    public class LeitmotivGenerator : MonoBehaviour
    {
        [Tooltip("Si se deja vacío, se usan valores por defecto internos.")]
        [SerializeField] private LeitmotivMappingConfig mappingConfig;

        [Header("Longitud del motivo (independiente del config, es del algoritmo)")]
        [SerializeField] private int noteCount = 6;

        public LeitmotivData Generate(AvatarAttributes attrs)
        {
            int minRoot = mappingConfig != null ? mappingConfig.MinRootMidi : 48;
            int maxRoot = mappingConfig != null ? mappingConfig.MaxRootMidi : 60;

            var data = new LeitmotivData
            {
                OwnerAvatarName = attrs.AvatarName,
                Scale = mappingConfig != null
                    ? mappingConfig.GetScale(attrs.Trait)
                    : DefaultScaleFallback(attrs.Trait),
                RootNoteMidi = MapColorToRoot(attrs.AccentColor, minRoot, maxRoot),
                TempoBpm = mappingConfig != null
                    ? mappingConfig.GetTempo(attrs.Accessory)
                    : 100f,
                InstrumentHint = mappingConfig != null
                    ? mappingConfig.GetInstrumentPresetId(attrs.Clothing)
                    : "pluck",
                Notes = GenerateNotes(attrs)
            };
            return data;
        }

        // El color sí se queda como cálculo continuo (hue -> tónica) en vez de
        // lista editable: son infinitos colores posibles, no categorías discretas.
        private int MapColorToRoot(Color color, int minRoot, int maxRoot)
        {
            Color.RGBToHSV(color, out float hue, out _, out _);
            int range = maxRoot - minRoot;
            return minRoot + Mathf.RoundToInt(hue * range);
        }

        // Solo se usa si no hay LeitmotivMappingConfig asignado (fallback de emergencia).
        private MusicalScale DefaultScaleFallback(CharacterTrait trait)
        {
            switch (trait)
            {
                case CharacterTrait.Alegre: return MusicalScale.Mayor;
                case CharacterTrait.Energico: return MusicalScale.Pentatonica;
                case CharacterTrait.Serio: return MusicalScale.MenorNatural;
                case CharacterTrait.Misterioso: return MusicalScale.Dorico;
                default: return MusicalScale.Mayor;
            }
        }

        // TODO: sustituir por una generación con reglas melódicas reales
        // (evitar saltos grandes repetidos, resolver hacia la tónica, etc.)
        // Por ahora: patrón determinista simple para poder probar el pipeline.
        private List<NoteEvent> GenerateNotes(AvatarAttributes attrs)
        {
            var notes = new List<NoteEvent>();
            int seed = attrs.AvatarName.GetHashCode();
            var rnd = new System.Random(seed);

            float beat = 0f;
            for (int i = 0; i < noteCount; i++)
            {
                notes.Add(new NoteEvent
                {
                    ScaleDegree = rnd.Next(0, 5), // grados 0-4 de la escala
                    StartBeat = beat,
                    DurationBeats = 0.5f,
                    Velocity = 0.8f
                });
                beat += 0.5f;
            }
            return notes;
        }
    }
}
