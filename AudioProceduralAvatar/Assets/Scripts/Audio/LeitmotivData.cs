using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    public enum MusicalScale
    {
        Mayor,
        MenorNatural,
        Pentatonica,
        Dorico,
        Lidio
    }

    /// <summary>
    /// Patrones rítmicos disponibles para el leitmotiv.
    /// La ropa inferior determina cuál se utiliza.
    /// </summary>
    public enum RhythmPattern
    {
        Balanced,
        Short,
        Long,
        Syncopated
    }

    [Serializable]
    public struct NoteEvent
    {
        public int ScaleDegree;
        public float StartBeat;
        public float DurationBeats;

        /// <summary>
        /// Intensidad individual de la nota.
        /// </summary>
        public float Velocity;
    }

    /// <summary>
    /// Resultado completo del LeitmotivGenerator.
    /// Contiene qué se toca y los parámetros musicales
    /// determinados por el avatar.
    /// </summary>
    [Serializable]
    public struct LeitmotivData
    {
        public string OwnerAvatarName;

        public MusicalScale Scale;

        public int RootNoteMidi;

        public float TempoBpm;

        public string InstrumentHint;

        public List<NoteEvent> Notes;

        [Tooltip(
            "0-1. Variación determinista del timbre."
        )]
        public float TimbreVariation;

        // ---------------------------------------------------------
        // ADSR
        // ---------------------------------------------------------

        /// <summary>
        /// Indica si la ropa superior ha definido un ADSR propio.
        /// Si es false, SimpleSynthRenderer utiliza el ADSR del InstrumentPreset.
        /// </summary>
        public bool HasMappedEnvelope;

        public float Attack;
        public float Decay;
        public float Sustain;
        public float Release;

        // ---------------------------------------------------------
        // RITMO
        // ---------------------------------------------------------

        /// <summary>
        /// Patrón rítmico seleccionado por la ropa inferior.
        /// </summary>
        public RhythmPattern Rhythm;

        // ---------------------------------------------------------
        // DINÁMICA
        // ---------------------------------------------------------

        /// <summary>
        /// Multiplicador general de dinámica.
        /// Los accesorios determinan este valor.
        /// </summary>
        public float DynamicMultiplier;
    }
}