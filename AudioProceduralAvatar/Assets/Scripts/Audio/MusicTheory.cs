using System.Collections.Generic;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Funciones puras de teoría musical. No depende de Unity ni de MonoBehaviour
    /// a propósito, para poder testearla aislada si hace falta.
    /// </summary>
    public static class MusicTheory
    {
        // Intervalos en semitonos desde la tónica, para cada escala soportada.
        private static readonly Dictionary<MusicalScale, int[]> ScaleIntervals = new()
        {
            { MusicalScale.Mayor,        new[] { 0, 2, 4, 5, 7, 9, 11 } },
            { MusicalScale.MenorNatural, new[] { 0, 2, 3, 5, 7, 8, 10 } },
            { MusicalScale.Pentatonica,  new[] { 0, 2, 4, 7, 9 } },
            { MusicalScale.Dorico,       new[] { 0, 2, 3, 5, 7, 9, 10 } },
            { MusicalScale.Lidio,        new[] { 0, 2, 4, 6, 7, 9, 11 } },
        };

        /// <summary>
        /// Convierte un grado de escala (puede ser mayor que el tamaño de la
        /// escala, o negativo) a semitonos desde la tónica, manejando el
        /// salto de octava automáticamente.
        /// </summary>
        public static int DegreeToSemitone(MusicalScale scale, int degree)
        {
            var intervals = ScaleIntervals[scale];
            int len = intervals.Length;

            int octave = FloorDiv(degree, len);
            int index = degree - octave * len; // siempre en [0, len)

            return intervals[index] + octave * 12;
        }

        public static int DegreeToMidiNote(MusicalScale scale, int rootMidi, int degree)
        {
            return rootMidi + DegreeToSemitone(scale, degree);
        }

        public static float MidiToFrequency(int midiNote)
        {
            // A4 (MIDI 69) = 440 Hz
            return 440f * Mathf_Pow2((midiNote - 69) / 12f);
        }

        private static float Mathf_Pow2(float exponent)
        {
            return (float)System.Math.Pow(2.0, exponent);
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
            return q;
        }
    }
}
