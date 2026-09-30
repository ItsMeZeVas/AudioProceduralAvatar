using System.Collections.Generic;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Catálogo centralizado de los instrumentos procedurales
    /// disponibles para el sistema de leitmotiv.
    /// </summary>
    public static class InstrumentPresetCatalog
    {
        public static readonly string[] BuiltInIds =
        {
            // Melódicos / plucks
            "warm_pluck",
            "bright_pluck",
            "soft_bell",
            "glass_lead",
            "music_box",
            "kalimba",
            "marimba",
            "celesta",
            "harp",

            // Cuerdas
            "violin",
            "cello",
            "string_ensemble",

            // Vientos
            "flute",
            "clarinet",

            // Teclas
            "acoustic_piano",
            "electric_piano",
            "organ",

            // Pads / sintetizadores
            "warm_pad",
            "choir_pad",
            "dream_pad",
            "synth_lead",
            "soft_synth",

            // Bajos
            "deep_bass",
            "sub_bass",
            "upright_bass",
            "pluck_bass",

            // Texturas
            "air_texture",
            "warm_texture",
            "dark_texture",
            "shimmer",

            // Percusión
            "soft_percussion",
            "kick_soft",
            "snare_soft",
            "shaker",
            "timpani",

            // Campanas
            "chime",
            "bell_low"
        };

        public static bool ContainsBuiltIn(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < BuiltInIds.Length; i++)
            {
                if (BuiltInIds[i] == id)
                    return true;
            }

            return false;
        }

        public static List<string> GetBuiltInList()
        {
            return new List<string>(BuiltInIds);
        }
    }
}