using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SimpleSynthRenderer
        : MonoBehaviour, IMusicRenderer
    {
        [Header("PRESETS PERSONALIZADOS")]

        [SerializeField]
        private List<InstrumentPreset> presets =
            new List<InstrumentPreset>();

        [Header("PRESET FALLBACK")]

        [SerializeField]
        private InstrumentPreset fallbackPreset;

        [Header("MEZCLA GENERAL")]

        [Range(0.65f, 1.15f)]
        [SerializeField]
        private float masterVolume = 0.92f;

        [Range(0.7f, 1.5f)]
        [SerializeField]
        private float limiterStrength = 1.10f;

        [Header("CAPAS AUTOMÁTICAS")]

        [Range(0f, 1f)]
        [SerializeField]
        private float bassMasterLevel = 0.42f;

        [Range(0f, 1f)]
        [SerializeField]
        private float textureMasterLevel = 0.24f;

        [Range(0f, 1f)]
        [SerializeField]
        private float percussionMasterLevel = 0.20f;

        private const int SampleRate = 44100;

        private LeitmotivData _current;

        private bool _hasCurrent;

        private double _clockSeconds;

        private double _secondsPerBeat;

        // ========================================================
        // EVENTOS
        // ========================================================

        private class ScheduledEvent
        {
            public NoteEvent Note;

            public string PresetId;

            public float Level;

            public int MidiOverride = -1;
        }

        private class VoiceState
        {
            public NoteEvent Note;

            public InstrumentPreset Preset;

            public float Level;

            public int MidiOverride;

            public double StartSeconds;

            public double EndSeconds;

            public double Phase1;

            public double Phase2;

            public float FilterState;
        }

        private readonly List<ScheduledEvent>
            _scheduledEvents =
                new List<ScheduledEvent>();

        private readonly List<VoiceState>
            _activeVoices =
                new List<VoiceState>();

        private int _nextEventIndex;

        // ========================================================
        // PRESETS INTERNOS
        // ========================================================

        private readonly Dictionary<string, InstrumentPreset>
            _builtInPresets =
                new Dictionary<string, InstrumentPreset>();

        // ========================================================
        // AWAKE
        // ========================================================

        private void Awake()
        {
            AudioSource source =
                GetComponent<AudioSource>();

            source.playOnAwake = false;

            BuildBuiltInPresets();

            source.Play();
        }

        // ========================================================
        // CREAR INSTRUMENTOS
        // ========================================================

        private void BuildBuiltInPresets()
        {
            CreateBuiltIn(
                "warm_pluck",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.72f,
                0.22f,
                5f,
                0.38f,
                0.12f,
                7600f,
                0.012f,
                0.16f,
                0.50f,
                0.18f
            );

            CreateBuiltIn(
                "bright_pluck",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.70f,
                0.30f,
                7f,
                0.45f,
                0.16f,
                10500f,
                0.008f,
                0.13f,
                0.42f,
                0.16f
            );

            CreateBuiltIn(
                "soft_bell",
                WaveformType.Sine,
                WaveformType.Sine,
                0.68f,
                0.40f,
                9f,
                0.60f,
                0.20f,
                12000f,
                0.005f,
                0.30f,
                0.22f,
                0.40f
            );

            CreateBuiltIn(
                "warm_pad",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.62f,
                0.34f,
                -4f,
                0.30f,
                0.08f,
                5200f,
                0.12f,
                0.30f,
                0.80f,
                0.55f
            );

            CreateBuiltIn(
                "glass_lead",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.68f,
                0.28f,
                11f,
                0.50f,
                0.15f,
                9800f,
                0.015f,
                0.20f,
                0.50f,
                0.25f
            );

            CreateBuiltIn(
                "deep_bass",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.76f,
                0.18f,
                -3f,
                0.25f,
                0.10f,
                4200f,
                0.015f,
                0.18f,
                0.72f,
                0.20f
            );

            CreateBuiltIn(
                "air_texture",
                WaveformType.Sine,
                WaveformType.Sawtooth,
                0.48f,
                0.12f,
                4f,
                0.30f,
                0.08f,
                6800f,
                0.18f,
                0.30f,
                0.72f,
                0.70f
            );

            CreateBuiltIn(
                "soft_percussion",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.58f,
                0.18f,
                0f,
                0.20f,
                0.18f,
                8500f,
                0.001f,
                0.045f,
                0.05f,
                0.09f
            );
        }

        private void CreateBuiltIn(
            string id,
            WaveformType primary,
            WaveformType secondary,
            float volume,
            float secondaryMix,
            float detune,
            float harmonic,
            float saturation,
            float cutoff,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            if (_builtInPresets.ContainsKey(id))
                return;

            InstrumentPreset preset =
                InstrumentPreset.CreateRuntime(
                    id,
                    primary,
                    secondary,
                    volume,
                    secondaryMix,
                    detune,
                    harmonic,
                    saturation,
                    cutoff,
                    attack,
                    decay,
                    sustain,
                    release
                );

            _builtInPresets.Add(
                id,
                preset
            );
        }

        // ========================================================
        // EXPORTAR
        // ========================================================

        public float[] RenderOffline(
            LeitmotivData data)
        {
            List<ScheduledEvent> events =
                BuildScheduledEvents(data);

            if (events.Count == 0)
                return Array.Empty<float>();

            double secondsPerBeat =
                60.0 /
                Mathf.Max(
                    1f,
                    data.TempoBpm
                );

            double totalSeconds =
                0.2;

            foreach (ScheduledEvent e in events)
            {
                double start =
                    e.Note.StartBeat *
                    secondsPerBeat;

                double end =
                    (
                        e.Note.StartBeat +
                        e.Note.DurationBeats
                    ) *
                    secondsPerBeat;

                InstrumentPreset preset =
                    FindPreset(e.PresetId);

                float release =
                    preset != null
                        ? preset.Release
                        : 0.2f;

                totalSeconds =
                    Math.Max(
                        totalSeconds,
                        end + release
                    );
            }

            int totalSamples =
                Mathf.CeilToInt(
                    (float)(
                        totalSeconds *
                        SampleRate
                    )
                );

            float[] buffer =
                new float[
                    totalSamples
                ];

            double[] phase1 =
                new double[
                    events.Count
                ];

            double[] phase2 =
                new double[
                    events.Count
                ];

            float[] filter =
                new float[
                    events.Count
                ];

            for (
                int i = 0;
                i < totalSamples;
                i++)
            {
                double time =
                    (double)i /
                    SampleRate;

                float mixed = 0f;

                int activeCount = 0;

                for (
                    int v = 0;
                    v < events.Count;
                    v++)
                {
                    ScheduledEvent e =
                        events[v];

                    double start =
                        e.Note.StartBeat *
                        secondsPerBeat;

                    double end =
                        (
                            e.Note.StartBeat +
                            e.Note.DurationBeats
                        ) *
                        secondsPerBeat;

                    double elapsed =
                        time - start;

                    if (elapsed < 0)
                        continue;

                    InstrumentPreset preset =
                        FindPreset(
                            e.PresetId
                        );

                    if (preset == null)
                        continue;

                    double life =
                        (
                            end -
                            start
                        ) +
                        preset.Release;

                    if (elapsed > life)
                        continue;

                    activeCount++;

                    mixed +=
                        RenderVoice(
                            data,
                            e.Note,
                            preset,
                            e.Level,
                            e.MidiOverride,
                            elapsed,
                            end - start,
                            ref phase1[v],
                            ref phase2[v],
                            ref filter[v]
                        );
                }

                mixed =
                    NormalizeMix(
                        mixed,
                        activeCount
                    );

                buffer[i] =
                    ApplyMasterLimiter(
                        mixed
                    );
            }

            return buffer;
        }

        // ========================================================
        // REPRODUCIR
        // ========================================================

        public void PlayLeitmotiv(
            LeitmotivData data)
        {
            // Garantizar siempre una lista válida.
            if (data.Notes == null)
            {
                data.Notes =
                    new List<NoteEvent>();
            }

            _current = data;

            _hasCurrent = true;

            _secondsPerBeat =
                60.0 /
                Mathf.Max(
                    1f,
                    data.TempoBpm
                );

            _clockSeconds = 0.0;

            _nextEventIndex = 0;

            _activeVoices.Clear();

            _scheduledEvents.Clear();

            _scheduledEvents.AddRange(
                BuildScheduledEvents(data)
            );
        }

        // ========================================================
        // DETENER
        // ========================================================

        public void Stop()
        {
            _hasCurrent = false;

            _activeVoices.Clear();

            _scheduledEvents.Clear();

            _nextEventIndex = 0;
        }

        // ========================================================
        // CREAR CAPAS MUSICALES
        // ========================================================

        private List<ScheduledEvent>
            BuildScheduledEvents(
                LeitmotivData data)
        {
            List<ScheduledEvent> events =
                new List<ScheduledEvent>();

            // ----------------------------------------------------
            // MELODÍA PRINCIPAL
            // ----------------------------------------------------

            if (data.Notes != null)
            {
                foreach (NoteEvent note in data.Notes)
                {
                    events.Add(
                        new ScheduledEvent
                        {
                            Note = note,

                            PresetId =
                                data.InstrumentHint,

                            Level = 1f,

                            MidiOverride = -1
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // DURACIÓN TOTAL
            // ----------------------------------------------------

            float totalBeats = 4f;

            foreach (NoteEvent note in data.Notes)
            {
                totalBeats =
                    Mathf.Max(
                        totalBeats,
                        note.StartBeat +
                        note.DurationBeats
                    );
            }

            // ----------------------------------------------------
            // BAJO
            // ----------------------------------------------------

            float bassLevel =
                Mathf.Clamp01(
                    data.BassLevel *
                    bassMasterLevel
                );

            if (bassLevel > 0.01f)
            {
                float beatStep =
                    data.Rhythm ==
                    RhythmPattern.Short
                        ? 1f
                        : 2f;

                for (
                    float beat = 0f;
                    beat < totalBeats;
                    beat += beatStep)
                {
                    NoteEvent bassNote =
                        new NoteEvent
                        {
                            ScaleDegree =
                                (Mathf.RoundToInt(
                                    beat /
                                    beatStep
                                ) %
                                2 == 0)
                                    ? 0
                                    : 4,

                            StartBeat =
                                beat,

                            DurationBeats =
                                beatStep *
                                0.82f,

                            Velocity =
                                0.72f
                        };

                    events.Add(
                        new ScheduledEvent
                        {
                            Note = bassNote,

                            PresetId =
                                data.BassInstrumentHint,

                            Level =
                                bassLevel,

                            MidiOverride = -1
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // TEXTURA
            // ----------------------------------------------------

            float textureLevel =
                Mathf.Clamp01(
                    data.TextureLevel *
                    textureMasterLevel
                );

            if (textureLevel > 0.01f)
            {
                for (
                    float beat = 0f;
                    beat < totalBeats;
                    beat += 4f)
                {
                    NoteEvent textureNote =
                        new NoteEvent
                        {
                            ScaleDegree = 4,

                            StartBeat =
                                beat,

                            DurationBeats =
                                Mathf.Min(
                                    3.5f,
                                    totalBeats - beat
                                ),

                            Velocity =
                                0.45f
                        };

                    events.Add(
                        new ScheduledEvent
                        {
                            Note =
                                textureNote,

                            PresetId =
                                data.TextureInstrumentHint,

                            Level =
                                textureLevel,

                            MidiOverride = -1
                        }
                    );
                }
            }

            // ----------------------------------------------------
            // PERCUSIÓN
            // ----------------------------------------------------

            float percussionLevel =
                Mathf.Clamp01(
                    data.PercussionLevel *
                    percussionMasterLevel
                );

            if (percussionLevel > 0.01f)
            {
                float step =
                    data.Rhythm ==
                    RhythmPattern.Short
                        ? 0.5f
                        : 1f;

                for (
                    float beat = 0f;
                    beat < totalBeats;
                    beat += step)
                {
                    bool accent =
                        Mathf.Abs(
                            beat % 2f
                        ) < 0.001f;

                    NoteEvent percussion =
                        new NoteEvent
                        {
                            ScaleDegree = 0,

                            StartBeat =
                                beat,

                            DurationBeats =
                                0.08f,

                            Velocity =
                                accent
                                    ? 0.72f
                                    : 0.48f
                        };

                    events.Add(
                        new ScheduledEvent
                        {
                            Note =
                                percussion,

                            PresetId =
                                data.PercussionInstrumentHint,

                            Level =
                                percussionLevel,

                            // MIDI 42 aproximadamente.
                            MidiOverride = 42
                        }
                    );
                }
            }

            events.Sort(
                (a, b) =>
                    a.Note.StartBeat.CompareTo(
                        b.Note.StartBeat
                    )
            );

            return events;
        }

        // ========================================================
        // AUDIO THREAD
        // ========================================================

        private void OnAudioFilterRead(
            float[] data,
            int channels)
        {
            if (!_hasCurrent ||
                _scheduledEvents.Count == 0)
            {
                return;
            }

            LeitmotivData leitmotiv =
                _current;

            for (
                int i = 0;
                i < data.Length;
                i += channels)
            {
                double sampleTime =
                    _clockSeconds +
                    (
                        double)(i / channels) /
                        SampleRate;

                // ------------------------------------------------
                // ACTIVAR EVENTOS
                // ------------------------------------------------

                while (
                    _nextEventIndex <
                    _scheduledEvents.Count &&
                    _scheduledEvents[
                        _nextEventIndex
                    ].Note.StartBeat *
                    _secondsPerBeat
                    <= sampleTime)
                {
                    ScheduledEvent scheduled =
                        _scheduledEvents[
                            _nextEventIndex
                        ];

                    InstrumentPreset preset =
                        FindPreset(
                            scheduled.PresetId
                        );

                    if (preset != null)
                    {
                        _activeVoices.Add(
                            new VoiceState
                            {
                                Note =
                                    scheduled.Note,

                                Preset =
                                    preset,

                                Level =
                                    scheduled.Level,

                                MidiOverride =
                                    scheduled.MidiOverride,

                                StartSeconds =
                                    scheduled.Note.StartBeat *
                                    _secondsPerBeat,

                                EndSeconds =
                                    (
                                        scheduled.Note.StartBeat +
                                        scheduled.Note.DurationBeats
                                    ) *
                                    _secondsPerBeat,

                                Phase1 = 0.0,

                                Phase2 = 0.0,

                                FilterState = 0f
                            }
                        );
                    }

                    _nextEventIndex++;
                }

                float mixed = 0f;

                int activeCount = 0;

                // ------------------------------------------------
                // VOCES
                // ------------------------------------------------

                for (
                    int v =
                        _activeVoices.Count - 1;
                    v >= 0;
                    v--)
                {
                    VoiceState voice =
                        _activeVoices[v];

                    double elapsed =
                        sampleTime -
                        voice.StartSeconds;

                    double duration =
                        voice.EndSeconds -
                        voice.StartSeconds;

                    double life =
                        duration +
                        voice.Preset.Release;

                    if (elapsed > life)
                    {
                        _activeVoices.RemoveAt(v);

                        continue;
                    }

                    activeCount++;

                    mixed +=
                        RenderVoice(
                            leitmotiv,
                            voice.Note,
                            voice.Preset,
                            voice.Level,
                            voice.MidiOverride,
                            elapsed,
                            duration,
                            ref voice.Phase1,
                            ref voice.Phase2,
                            ref voice.FilterState
                        );
                }

                mixed =
                    NormalizeMix(
                        mixed,
                        activeCount
                    );

                mixed =
                    ApplyMasterLimiter(
                        mixed
                    );

                for (
                    int c = 0;
                    c < channels;
                    c++)
                {
                    data[i + c] =
                        mixed;
                }
            }

            _clockSeconds +=
                (double)data.Length /
                channels /
                SampleRate;
        }

        // ========================================================
        // VOZ
        // ========================================================

        private float RenderVoice(
            LeitmotivData data,
            NoteEvent note,
            InstrumentPreset preset,
            float level,
            int midiOverride,
            double elapsed,
            double duration,
            ref double phase1,
            ref double phase2,
            ref float filterState)
        {
            float attack =
                data.HasMappedEnvelope
                    ? data.Attack
                    : preset.Attack;

            float decay =
                data.HasMappedEnvelope
                    ? data.Decay
                    : preset.Decay;

            float sustain =
                data.HasMappedEnvelope
                    ? data.Sustain
                    : preset.Sustain;

            float release =
                data.HasMappedEnvelope
                    ? data.Release
                    : preset.Release;

            float envelope =
                ComputeEnvelope(
                    elapsed,
                    duration,
                    attack,
                    decay,
                    sustain,
                    release
                );

            if (envelope <= 0f)
                return 0f;

            // ----------------------------------------------------
            // FRECUENCIA
            // ----------------------------------------------------

            int midiNote;

            if (midiOverride >= 0)
            {
                midiNote =
                    midiOverride;
            }
            else
            {
                midiNote =
                    MusicTheory
                        .DegreeToMidiNote(
                            data.Scale,
                            data.RootNoteMidi,
                            note.ScaleDegree
                        );
            }

            float frequency =
                MusicTheory.MidiToFrequency(
                    midiNote
                );

            // ----------------------------------------------------
            // DETUNE
            // ----------------------------------------------------

            float detune =
                preset.DetuneCents;

            detune +=
                (
                    data.TimbreVariation -
                    0.5f
                ) * 5f;

            float frequency2 =
                frequency *
                Mathf.Pow(
                    2f,
                    detune /
                    1200f
                );

            // ----------------------------------------------------
            // OSCILADORES
            // ----------------------------------------------------

            phase1 +=
                2.0 *
                Mathf.PI *
                frequency /
                SampleRate;

            phase2 +=
                2.0 *
                Mathf.PI *
                frequency2 /
                SampleRate;

            phase1 =
                WrapPhase(
                    phase1
                );

            phase2 =
                WrapPhase(
                    phase2
                );

            float osc1 =
                Oscillate(
                    preset.Waveform,
                    phase1
                );

            float osc2 =
                Oscillate(
                    preset.SecondaryWaveform,
                    phase2
                );

            float secondaryMix =
                Mathf.Clamp01(
                    preset.SecondaryMix *
                    Mathf.Lerp(
                        0.75f,
                        1.25f,
                        data.HarmonicAmount
                    )
                );

            float raw =
                Mathf.Lerp(
                    osc1,
                    osc2,
                    secondaryMix
                );

            // ----------------------------------------------------
            // FILTRO
            // ----------------------------------------------------

            float brightness =
                Mathf.Lerp(
                    0.72f,
                    1.30f,
                    data.TimbreBrightness
                );

            float warmth =
                Mathf.Lerp(
                    0.85f,
                    1.10f,
                    data.Warmth
                );

            float cutoff =
                preset.FilterCutoffHz *
                brightness *
                warmth;

            cutoff =
                Mathf.Clamp(
                    cutoff,
                    300f,
                    16000f
                );

            float alpha =
                1f -
                Mathf.Exp(
                    -2f *
                    Mathf.PI *
                    cutoff /
                    SampleRate
                );

            filterState +=
                alpha *
                (raw - filterState);

            raw =
                filterState;

            // ----------------------------------------------------
            // SATURACIÓN SUAVE
            // ----------------------------------------------------

            float drive =
                1f +
                preset.Saturation *
                2f;

            raw =
                SoftClip(
                    raw *
                    drive
                );

            // ----------------------------------------------------
            // DINÁMICA
            // ----------------------------------------------------

            float dynamic =
                Mathf.Clamp(
                    data.DynamicMultiplier,
                    0.72f,
                    1.18f
                );

            float velocity =
                Mathf.Clamp(
                    note.Velocity,
                    0.55f,
                    1f
                );

            float output =
                raw *
                envelope *
                preset.Volume *
                velocity *
                level *
                dynamic;

            return output;
        }

        // ========================================================
        // NORMALIZACIÓN
        // ========================================================

        private float NormalizeMix(
            float mixed,
            int activeVoices)
        {
            if (activeVoices <= 1)
                return mixed;

            float divisor =
                Mathf.Sqrt(
                    activeVoices
                );

            return
                mixed /
                Mathf.Max(
                    1f,
                    divisor
                );
        }

        // ========================================================
        // LIMITADOR
        // ========================================================

        private float ApplyMasterLimiter(
            float sample)
        {
            sample *=
                masterVolume;

            float strength =
                Mathf.Max(
                    0.7f,
                    limiterStrength
                );

            float normalized =
                SoftClip(
                    sample *
                    strength
                );

            // Máximo real.
            normalized =
                Mathf.Clamp(
                    normalized,
                    -0.94f,
                    0.94f
                );

            return normalized;
        }

        private static float SoftClip(
            float value)
        {
            return
                (float)
                System.Math.Tanh(
                    value
                );
        }

        // ========================================================
        // PRESET
        // ========================================================

        private InstrumentPreset FindPreset(
            string presetId)
        {
            if (!string.IsNullOrEmpty(presetId))
            {
                foreach (
                    InstrumentPreset preset
                    in presets)
                {
                    if (preset != null &&
                        preset.PresetId ==
                        presetId)
                    {
                        return preset;
                    }
                }

                if (
                    _builtInPresets.TryGetValue(
                        presetId,
                        out InstrumentPreset builtIn))
                {
                    return builtIn;
                }
            }

            if (fallbackPreset != null)
                return fallbackPreset;

            if (
                _builtInPresets.TryGetValue(
                    "warm_pluck",
                    out InstrumentPreset defaultPreset))
            {
                return defaultPreset;
            }

            return null;
        }

        // ========================================================
        // OSCILADORES
        // ========================================================

        private static float Oscillate(
            WaveformType waveform,
            double phase)
        {
            switch (waveform)
            {
                case WaveformType.Sine:

                    return Mathf.Sin(
                        (float)phase
                    );

                case WaveformType.Square:

                    return Mathf.Sin(
                        (float)phase
                    ) >= 0f
                        ? 1f
                        : -1f;

                case WaveformType.Sawtooth:

                    return
                        (float)(
                            phase /
                            System.Math.PI
                            - 1.0
                        );

                case WaveformType.Triangle:

                    return
                        (float)(
                            2.0 /
                            System.Math.PI *
                            System.Math.Asin(
                                System.Math.Sin(
                                    phase
                                )
                            )
                        );

                default:

                    return 0f;
            }
        }

        private static double WrapPhase(
            double phase)
        {
            double twoPi =
                2.0 *
                Math.PI;

            while (phase >= twoPi)
                phase -= twoPi;

            while (phase < 0)
                phase += twoPi;

            return phase;
        }

        // ========================================================
        // ADSR
        // ========================================================

        private static float ComputeEnvelope(
            double elapsed,
            double duration,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            attack =
                Mathf.Clamp(
                    attack,
                    0.005f,
                    1f
                );

            decay =
                Mathf.Clamp(
                    decay,
                    0.01f,
                    1f
                );

            release =
                Mathf.Clamp(
                    release,
                    0.03f,
                    1f
                );

            sustain =
                Mathf.Clamp(
                    sustain,
                    0.08f,
                    1f
                );

            // ATTACK
            if (elapsed < attack)
            {
                return Mathf.Clamp01(
                    (float)(
                        elapsed /
                        attack
                    )
                );
            }

            // DECAY
            double afterAttack =
                elapsed -
                attack;

            if (afterAttack < decay)
            {
                float t =
                    Mathf.Clamp01(
                        (float)(
                            afterAttack /
                            decay
                        )
                    );

                return Mathf.Lerp(
                    1f,
                    sustain,
                    t
                );
            }

            // SUSTAIN
            if (elapsed < duration)
            {
                return sustain;
            }

            // RELEASE
            double afterNote =
                elapsed -
                duration;

            if (afterNote < release)
            {
                float t =
                    Mathf.Clamp01(
                        (float)(
                            afterNote /
                            release
                        )
                    );

                return Mathf.Lerp(
                    sustain,
                    0f,
                    t
                );
            }

            return 0f;
        }
    }
}