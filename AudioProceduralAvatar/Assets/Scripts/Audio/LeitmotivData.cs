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

        [Range(0f, 1f)]
        public float Velocity;
    }

    /// <summary>
    /// Información musical completa generada para un avatar.
    ///
    /// El struct SIEMPRE es válido.
    /// Notes nunca debe quedar en null.
    /// </summary>
    [Serializable]
    public struct LeitmotivData
    {
        public string OwnerAvatarName;

        public MusicalScale Scale;

        public int RootNoteMidi;

        public float TempoBpm;

        // Instrumento principal.
        public string InstrumentHint;

        // Instrumentos adicionales.
        public string BassInstrumentHint;
        public string TextureInstrumentHint;
        public string PercussionInstrumentHint;

        public List<NoteEvent> Notes;

        // Variación determinista.
        [Range(0f, 1f)]
        public float TimbreVariation;

        // ---------------------------------------------------------
        // TIMBRE
        // ---------------------------------------------------------

        [Range(0f, 1f)]
        public float TimbreBrightness;

        [Range(0f, 1f)]
        public float HarmonicAmount;

        [Range(0f, 1f)]
        public float Warmth;

        // ---------------------------------------------------------
        // ADSR
        // ---------------------------------------------------------

        public bool HasMappedEnvelope;

        [Range(0f, 1f)]
        public float Attack;

        [Range(0f, 1f)]
        public float Decay;

        [Range(0f, 1f)]
        public float Sustain;

        [Range(0f, 1f)]
        public float Release;

        // ---------------------------------------------------------
        // RITMO
        // ---------------------------------------------------------

        public RhythmPattern Rhythm;

        // ---------------------------------------------------------
        // DINÁMICA
        // ---------------------------------------------------------

        [Range(0.65f, 1.20f)]
        public float DynamicMultiplier;

        // ---------------------------------------------------------
        // CAPAS ADICIONALES
        // ---------------------------------------------------------

        [Range(0f, 1f)]
        public float BassLevel;

        [Range(0f, 1f)]
        public float TextureLevel;

        [Range(0f, 1f)]
        public float PercussionLevel;

        [Range(0f, 1f)]
        public float Articulation;
    }
}