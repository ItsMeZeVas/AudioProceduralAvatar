using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Filtro simple de texto prohibido. La lista de palabras se llena desde
    /// el Inspector (Create -> AudioProceduralAvatar -> Profanity Filter) —
    /// no está poblada por defecto a propósito, cada equipo/evento decide
    /// qué términos bloquear.
    ///
    /// Comparación insensible a mayúsculas y acentos (busca coincidencia de
    /// substring). Es una primera línea de defensa razonable para un evento
    /// en vivo, no un filtro lingüístico avanzado — ver limitaciones abajo.
    /// </summary>
    [CreateAssetMenu(fileName = "ProfanityFilter", menuName = "AudioProceduralAvatar/Profanity Filter")]
    public class ProfanityFilter : ScriptableObject
    {
        [Tooltip("Palabras o fragmentos prohibidos. No distingue mayúsculas ni acentos (ej. 'aaa' bloquea 'AAA' y 'ááá').")]
        public List<string> BannedWords = new();

        public bool ContainsProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            string normalizedText = Normalize(text);
            foreach (var word in BannedWords)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;
                if (normalizedText.Contains(Normalize(word))) return true;
            }
            return false;
        }

        private static string Normalize(string text)
        {
            string lower = text.ToLowerInvariant();
            string decomposed = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // LIMITACIÓN CONOCIDA: coincidencia por substring puede dar falsos
        // positivos (ej. una palabra prohibida corta que aparece dentro de
        // otra palabra válida). Para el volumen de un evento estudiantil
        // esto es aceptable; si da problemas, se puede refinar a coincidencia
        // por palabra completa más adelante sin cambiar la API pública.
    }
}
