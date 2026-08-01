using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Motor de decisión musical. Recibe AvatarAttributes, entrega LeitmotivData.
    /// No reproduce audio — eso lo hace un IMusicRenderer.
    ///
    /// TODO (Diseño + Desarrollo, semana 1-2): cerrar las reglas reales de mapeo.
    /// Las de abajo son un punto de partida deliberadamente simple para tener
    /// el pipeline vertical funcionando cuanto antes; se refinan después.
    /// </summary>
    public class LeitmotivGenerator : MonoBehaviour
    {
        [Header("Rango de tonalidad permitido (evita que suene 'random')")]
        [SerializeField] private int minRootMidi = 48; // C3
        [SerializeField] private int maxRootMidi = 60; // C4

        [Header("Longitud del motivo")]
        [SerializeField] private int noteCount = 6;

        public LeitmotivData Generate(AvatarAttributes attrs)
        {
            var data = new LeitmotivData
            {
                OwnerAvatarName = attrs.AvatarName,
                Scale = MapTraitToScale(attrs.Trait),
                RootNoteMidi = MapColorToRoot(attrs.AccentColor),
                TempoBpm = MapAccessoryToTempo(attrs.Accessory),
                InstrumentHint = MapClothingToInstrument(attrs.Clothing),
                Notes = GenerateNotes(attrs)
            };
            return data;
        }

        // TODO: regla real pendiente de definir con Diseño.
        // Idea de partida: trait "Alegre"/"Energico" -> escalas mayores/pentatónica,
        // "Serio"/"Misterioso" -> escalas menores/dórico.
        private MusicalScale MapTraitToScale(CharacterTrait trait)
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

        // TODO: mapear el color (hue) a tónica dentro del rango permitido,
        // en vez de hash directo — así colores "cercanos" dan tónicas cercanas.
        private int MapColorToRoot(Color color)
        {
            Color.RGBToHSV(color, out float hue, out _, out _);
            int range = maxRootMidi - minRootMidi;
            return minRootMidi + Mathf.RoundToInt(hue * range);
        }

        // TODO: definir tempos concretos por accesorio con Diseño.
        private float MapAccessoryToTempo(AccessoryType accessory)
        {
            switch (accessory)
            {
                case AccessoryType.Sombrero: return 100f;
                case AccessoryType.Lentes: return 110f;
                case AccessoryType.Collar: return 90f;
                case AccessoryType.Mochila: return 120f;
                default: return 100f;
            }
        }

        // TODO: el string debe corresponder a un preset real una vez exista
        // el SimpleSynthRenderer (o un evento de FMOD, en el futuro).
        private string MapClothingToInstrument(ClothingType clothing)
        {
            switch (clothing)
            {
                case ClothingType.Casual: return "pluck";
                case ClothingType.Formal: return "pad";
                case ClothingType.Deportivo: return "bass";
                case ClothingType.Fantasia: return "bell";
                default: return "pluck";
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
