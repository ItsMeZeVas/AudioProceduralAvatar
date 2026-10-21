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
    /// Comparación insensible a mayúsculas y acentos. Por defecto coincide
    /// por PALABRA COMPLETA (ej. "pito" bloquea "pito" y "Hola pito", pero
    /// NO "Pepito" ni "Pitoniso"). Se puede cambiar a substring con
    /// MatchWholeWord = false desde el Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "ProfanityFilter", menuName = "AudioProceduralAvatar/Profanity Filter")]
    public class ProfanityFilter : ScriptableObject
    {
        [Tooltip("Palabras o frases prohibidas. No distingue mayúsculas ni acentos (ej. 'aaa' bloquea 'AAA' y 'ááá').")]
        public List<string> BannedWords = new();

        [Tooltip("Si está activo, solo bloquea palabras completas ('pito' no bloquea 'Pepito'). " +
                 "Si está desactivado, bloquea cualquier texto que contenga la palabra (substring).")]
        public bool MatchWholeWord = true;

        public bool ContainsProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            string normalizedText = Normalize(text);

            // Para palabra completa: dejamos solo letras/dígitos separados por
            // un espacio y rodeamos con espacios, así buscamos " palabra ".
            string paddedText = MatchWholeWord ? " " + ToWordsOnly(normalizedText) + " " : null;

            foreach (var word in BannedWords)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;

                string normalizedWord = Normalize(word);

                if (MatchWholeWord)
                {
                    string cleanWord = ToWordsOnly(normalizedWord);
                    if (cleanWord.Length == 0) continue;
                    if (paddedText.Contains(" " + cleanWord + " ")) return true;
                }
                else
                {
                    if (normalizedText.Contains(normalizedWord)) return true;
                }
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

        /// <summary>
        /// Reemplaza todo lo que no sea letra o dígito por un espacio y
        /// colapsa espacios repetidos ("Juan_pito-99" -> "juan pito 99").
        /// </summary>
        private static string ToWordsOnly(string text)
        {
            var sb = new StringBuilder();
            bool lastWasSpace = true;
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
                else if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            return sb.ToString().TrimEnd();
        }

        // LIMITACIÓN CONOCIDA: con palabra completa, variantes como "pitos"
        // o "pitazo" no se bloquean a menos que las agregues a la lista.
        // Si necesitas bloquear algo como substring solo para una palabra
        // específica, agrégala con sus variantes en BannedWords.
    }
}
