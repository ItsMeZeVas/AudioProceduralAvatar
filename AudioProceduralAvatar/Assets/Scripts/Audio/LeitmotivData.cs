using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    public enum MusicalScale { Mayor, MenorNatural, Pentatonica, Dorico, Lidio }

    [Serializable]
    public struct NoteEvent
    {
        public int ScaleDegree;   // grado dentro de la escala (0 = tónica), no nota MIDI directa
        public float StartBeat;   // en beats, no en segundos (independiente del tempo)
        public float DurationBeats;
        public float Velocity;    // 0-1, intensidad/volumen relativo
    }

    /// <summary>
    /// Resultado de LeitmotivGenerator: "qué se toca". No sabe nada de cómo
    /// se sintetiza ni de FMOD/osciladores — eso es responsabilidad de IMusicRenderer.
    /// </summary>
    [Serializable]
    public struct LeitmotivData
    {
        public string OwnerAvatarName;
        public MusicalScale Scale;
        public int RootNoteMidi;      // nota raíz (tónica) en notación MIDI
        public float TempoBpm;
        public string InstrumentHint; // ej: "pluck", "pad", "bass" — el renderer decide el timbre real
        public List<NoteEvent> Notes;

        [Tooltip("0-1, determinista a partir de las capas/atributos del avatar. Ajusta ligeramente el timbre (desafine, brillo) para que avatares con el mismo instrumento no suenen idénticos.")]
        public float TimbreVariation;
    }
}
