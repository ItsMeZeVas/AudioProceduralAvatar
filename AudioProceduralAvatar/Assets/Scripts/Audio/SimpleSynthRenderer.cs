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
        private float masterVolume = 0.70f;

        [Range(0.7f, 1.5f)]
        [SerializeField]
        private float limiterStrength = 0.85f;

        [Header("CAPAS AUTOMÁTICAS")]

        [Range(0f, 1f)]
        [SerializeField]
        private float bassMasterLevel = 0f; //0.42f;

        [Range(0f, 1f)]
        [SerializeField]
        private float textureMasterLevel = 0f; //0.24f;

        [Range(0f, 1f)]
        [SerializeField]
        private float percussionMasterLevel = 0f; //0.20f;

        // Se cachea: AudioSettings solo es seguro desde el hilo principal,
        // y OnAudioFilterRead corre en el hilo de audio.
        private int _sampleRate = 48000;

        private float _gainSmoothCoef = 0.0004f;

        private int SampleRate => _sampleRate;

        // Sample rate con el que se renderiza (para escribir el WAV con el mismo).
        public int RenderSampleRate => _sampleRate;

        // Protege el estado compartido entre hilo principal y hilo de audio.
        private readonly object _lock = new object();

        // Pool de voces: evita crear objetos (GC) dentro del hilo de audio.
        private readonly Stack<VoiceState> _voicePool =
            new Stack<VoiceState>();

        // Ganancia suavizada de la normalización (evita escalones = clicks).
        private float _normGain = 1f;

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

            // Se resuelve en el hilo principal, no en el de audio.
            public InstrumentPreset Preset;
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

            public double LifeEndSeconds;
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

            RefreshAudioConfig();

            AudioSettings.OnAudioConfigurationChanged +=
                OnAudioConfigurationChanged;

            for (int i = 0; i < 48; i++)
                _voicePool.Push(new VoiceState());

            BuildBuiltInPresets();

            source.Play();
        }

        private void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -=
                OnAudioConfigurationChanged;
        }

        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            RefreshAudioConfig();
        }

        private void RefreshAudioConfig()
        {
            _sampleRate = AudioSettings.outputSampleRate;

            // Suavizado de ~50 ms para la ganancia de normalización.
            _gainSmoothCoef =
                1f - Mathf.Exp(-1f / (0.05f * _sampleRate));
        }

        // ========================================================
        // CREAR INSTRUMENTOS
        // ========================================================

        private void BuildBuiltInPresets()
        {
            // ========================================================
            // PLUCKS
            // ========================================================

            CreateBuiltIn(
                "warm_pluck",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.72f, 0.22f, 5f,
                0.38f, 0.12f, 7600f,
                0.012f, 0.16f, 0.50f, 0.18f
            );

            CreateBuiltIn(
                "bright_pluck",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.70f, 0.30f, 7f,
                0.45f, 0.16f, 10500f,
                0.008f, 0.13f, 0.42f, 0.16f
            );

            // ========================================================
            // CAMPANAS / CAJAS
            // ========================================================

            CreateBuiltIn(
                "soft_bell",
                WaveformType.Sine,
                WaveformType.Sine,
                0.68f, 0.40f, 9f,
                0.60f, 0.20f, 12000f,
                0.005f, 0.30f, 0.22f, 0.40f
            );

            CreateBuiltIn(
                "bell_low",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.62f, 0.30f, -5f,
                0.52f, 0.16f, 7000f,
                0.008f, 0.35f, 0.20f, 0.55f
            );

            CreateBuiltIn(
                "chime",
                WaveformType.Sine,
                WaveformType.Sine,
                0.58f, 0.55f, 14f,
                0.72f, 0.12f, 15000f,
                0.002f, 0.35f, 0.18f, 0.65f
            );

            CreateBuiltIn(
                "music_box",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.64f, 0.42f, 12f,
                0.65f, 0.12f, 11500f,
                0.004f, 0.16f, 0.30f, 0.35f
            );

            CreateBuiltIn(
                "kalimba",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.65f, 0.32f, 8f,
                0.58f, 0.15f, 9800f,
                0.003f, 0.12f, 0.30f, 0.30f
            );

            CreateBuiltIn(
                "marimba",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.70f, 0.25f, -2f,
                0.48f, 0.18f, 6500f,
                0.004f, 0.11f, 0.35f, 0.22f
            );

            CreateBuiltIn(
                "celesta",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.62f, 0.45f, 10f,
                0.64f, 0.10f, 14000f,
                0.004f, 0.22f, 0.28f, 0.48f
            );

            // ========================================================
            // ARPA
            // ========================================================

            CreateBuiltIn(
                "harp",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.68f, 0.20f, -3f,
                0.35f, 0.08f, 9200f,
                0.006f, 0.20f, 0.32f, 0.38f
            );

            // ========================================================
            // PIANOS
            // ========================================================

            CreateBuiltIn(
                "acoustic_piano",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.74f, 0.28f, 3f,
                0.50f, 0.18f, 6800f,
                0.006f, 0.20f, 0.45f, 0.30f
            );

            CreateBuiltIn(
                "electric_piano",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.70f, 0.35f, 5f,
                0.44f, 0.10f, 7600f,
                0.012f, 0.24f, 0.55f, 0.42f
            );

            // ========================================================
            // CUERDAS
            // ========================================================

            CreateBuiltIn(
                "violin",
                WaveformType.Sawtooth,
                WaveformType.Sine,
                0.58f, 0.32f, 6f,
                0.48f, 0.08f, 5200f,
                0.08f, 0.25f, 0.78f, 0.45f
            );

            CreateBuiltIn(
                "cello",
                WaveformType.Sawtooth,
                WaveformType.Triangle,
                0.62f, 0.28f, -4f,
                0.42f, 0.10f, 3900f,
                0.10f, 0.28f, 0.82f, 0.50f
            );

            CreateBuiltIn(
                "string_ensemble",
                WaveformType.Sawtooth,
                WaveformType.Sine,
                0.55f, 0.38f, 4f,
                0.52f, 0.06f, 4600f,
                0.16f, 0.30f, 0.82f, 0.65f
            );

            // ========================================================
            // VIENTOS
            // ========================================================

            CreateBuiltIn(
                "flute",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.60f, 0.18f, 2f,
                0.25f, 0.05f, 8200f,
                0.08f, 0.18f, 0.75f, 0.35f
            );

            CreateBuiltIn(
                "clarinet",
                WaveformType.Square,
                WaveformType.Sine,
                0.56f, 0.25f, -2f,
                0.38f, 0.12f, 4800f,
                0.04f, 0.18f, 0.72f, 0.32f
            );

            // ========================================================
            // ÓRGANO
            // ========================================================

            CreateBuiltIn(
                "organ",
                WaveformType.Sine,
                WaveformType.Square,
                0.58f, 0.30f, 0f,
                0.50f, 0.04f, 6200f,
                0.08f, 0.15f, 0.90f, 0.25f
            );

            // ========================================================
            // PADS
            // ========================================================

            CreateBuiltIn(
                "warm_pad",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.62f, 0.34f, -4f,
                0.30f, 0.08f, 5200f,
                0.12f, 0.30f, 0.80f, 0.55f
            );

            CreateBuiltIn(
                "choir_pad",
                WaveformType.Sine,
                WaveformType.Sawtooth,
                0.48f, 0.20f, 5f,
                0.34f, 0.06f, 4300f,
                0.20f, 0.35f, 0.82f, 0.75f
            );

            CreateBuiltIn(
                "dream_pad",
                WaveformType.Sine,
                WaveformType.Sine,
                0.50f, 0.55f, 8f,
                0.42f, 0.05f, 7000f,
                0.22f, 0.40f, 0.78f, 0.90f
            );

            // ========================================================
            // SYNTH
            // ========================================================

            CreateBuiltIn(
                "synth_lead",
                WaveformType.Sawtooth,
                WaveformType.Sine,
                0.62f, 0.25f, 7f,
                0.62f, 0.18f, 7600f,
                0.015f, 0.16f, 0.55f, 0.22f
            );

            CreateBuiltIn(
                "soft_synth",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.60f, 0.35f, 4f,
                0.42f, 0.10f, 6800f,
                0.02f, 0.18f, 0.65f, 0.30f
            );

            // ========================================================
            // BAJOS
            // ========================================================

            CreateBuiltIn(
                "deep_bass",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.76f, 0.18f, -3f,
                0.25f, 0.10f, 4200f,
                0.015f, 0.18f, 0.72f, 0.20f
            );

            CreateBuiltIn(
                "sub_bass",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.78f, 0.15f, -5f,
                0.18f, 0.06f, 2500f,
                0.012f, 0.15f, 0.80f, 0.18f
            );

            CreateBuiltIn(
                "upright_bass",
                WaveformType.Triangle,
                WaveformType.Sine,
                0.68f, 0.20f, -2f,
                0.28f, 0.08f, 3500f,
                0.025f, 0.20f, 0.68f, 0.25f
            );

            CreateBuiltIn(
                "pluck_bass",
                WaveformType.Square,
                WaveformType.Triangle,
                0.68f, 0.28f, -4f,
                0.38f, 0.12f, 3900f,
                0.008f, 0.14f, 0.50f, 0.18f
            );

            // ========================================================
            // TEXTURAS
            // ========================================================

            CreateBuiltIn(
                "air_texture",
                WaveformType.Sine,
                WaveformType.Sawtooth,
                0.48f, 0.12f, 4f,
                0.30f, 0.08f, 6800f,
                0.18f, 0.30f, 0.72f, 0.70f
            );

            CreateBuiltIn(
                "warm_texture",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.44f, 0.32f, -3f,
                0.24f, 0.06f, 4300f,
                0.20f, 0.35f, 0.72f, 0.65f
            );

            CreateBuiltIn(
                "dark_texture",
                WaveformType.Sawtooth,
                WaveformType.Sine,
                0.40f, 0.18f, -6f,
                0.45f, 0.15f, 2800f,
                0.22f, 0.30f, 0.70f, 0.70f
            );

            CreateBuiltIn(
                "shimmer",
                WaveformType.Sine,
                WaveformType.Sine,
                0.42f, 0.65f, 16f,
                0.72f, 0.06f, 15000f,
                0.16f, 0.35f, 0.65f, 0.80f
            );

            // ========================================================
            // PERCUSIÓN
            // ========================================================

            CreateBuiltIn(
                "soft_percussion",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.58f, 0.18f, 0f,
                0.20f, 0.18f, 8500f,
                0.001f, 0.045f, 0.05f, 0.09f
            );

            CreateBuiltIn(
                "kick_soft",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.70f, 0.10f, -8f,
                0.10f, 0.20f, 3000f,
                0.001f, 0.06f, 0.03f, 0.08f
            );

            CreateBuiltIn(
                "snare_soft",
                WaveformType.Sawtooth,
                WaveformType.Sine,
                0.42f, 0.30f, 3f,
                0.50f, 0.22f, 9000f,
                0.001f, 0.04f, 0.03f, 0.08f
            );

            CreateBuiltIn(
                "shaker",
                WaveformType.Sawtooth,
                WaveformType.Square,
                0.30f, 0.25f, 12f,
                0.60f, 0.25f, 12000f,
                0.001f, 0.025f, 0.02f, 0.05f
            );

            CreateBuiltIn(
                "timpani",
                WaveformType.Sine,
                WaveformType.Triangle,
                0.62f, 0.20f, -6f,
                0.22f, 0.16f, 3200f,
                0.008f, 0.16f, 0.35f, 0.35f
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
            // Puede llamarse antes de Awake (objeto inactivo) o tras un cambio
            // de dispositivo: asegurar sample rate y presets internos.
            RefreshAudioConfig();

            if (_builtInPresets.Count == 0)
                BuildBuiltInPresets();

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
                    GetReleaseSeconds(data, preset) + 0.01f;

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

            float offlineGain = 1f;

            // Resolver el preset de cada evento UNA vez (antes se buscaba
            // por cada muestra y por cada evento: millones de búsquedas).
            InstrumentPreset[] presetCache =
                new InstrumentPreset[events.Count];

            for (int k = 0; k < events.Count; k++)
                presetCache[k] = FindPreset(events[k].PresetId);

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
                        presetCache[v];

                    if (preset == null)
                        continue;

                    double life =
                        (end - start) +
                        GetReleaseSeconds(data, preset) +
                        0.01;

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
                        activeCount,
                        ref offlineGain
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

            double secondsPerBeat =
                60.0 /
                Mathf.Max(1f, data.TempoBpm);

            // Todo lo pesado se hace fuera del lock y fuera del hilo de audio.
            List<ScheduledEvent> events =
                BuildScheduledEvents(data);

            foreach (ScheduledEvent e in events)
                e.Preset = FindPreset(e.PresetId);

            lock (_lock)
            {
                ReleaseAllVoices();

                _current = data;
                _secondsPerBeat = secondsPerBeat;
                _clockSeconds = 0.0;
                _nextEventIndex = 0;
                _normGain = 1f;

                _scheduledEvents.Clear();
                _scheduledEvents.AddRange(events);

                _hasCurrent = true;
            }
        }

        // ========================================================
        // DETENER
        // ========================================================

        public void Stop()
        {
            lock (_lock)
            {
                _hasCurrent = false;

                ReleaseAllVoices();

                _scheduledEvents.Clear();

                _nextEventIndex = 0;
            }
        }

        // Debe llamarse dentro del lock.
        private void ReleaseAllVoices()
        {
            for (int i = 0; i < _activeVoices.Count; i++)
                _voicePool.Push(_activeVoices[i]);

            _activeVoices.Clear();
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
            // Este AudioSource no tiene clip: Unity NO garantiza que "data"
            // venga en silencio. Si salimos sin escribir (antes de la primera
            // reproducción, tras Stop(), etc.) el buffer puede conservar
            // contenido viejo y repetirse en bucle = zumbido/crackling de
            // fondo. Siempre partimos de silencio.
            Array.Clear(data, 0, data.Length);

            lock (_lock)
            {
                if (!_hasCurrent ||
                    _scheduledEvents.Count == 0)
                {
                    return;
                }

                LeitmotivData leitmotiv = _current;

                int sampleRate = _sampleRate;

                int frames = data.Length / channels;

                for (int frame = 0; frame < frames; frame++)
                {
                    int i = frame * channels;

                    double sampleTime =
                        _clockSeconds +
                        (double)frame / sampleRate;

                    // --------------------------------------------
                    // ACTIVAR EVENTOS
                    // --------------------------------------------

                    while (
                        _nextEventIndex < _scheduledEvents.Count &&
                        _scheduledEvents[_nextEventIndex].Note.StartBeat *
                        _secondsPerBeat <= sampleTime)
                    {
                        ScheduledEvent scheduled =
                            _scheduledEvents[_nextEventIndex];

                        _nextEventIndex++;

                        if (scheduled.Preset == null)
                            continue;

                        VoiceState voice =
                            _voicePool.Count > 0
                                ? _voicePool.Pop()
                                : new VoiceState();

                        voice.Note = scheduled.Note;
                        voice.Preset = scheduled.Preset;
                        voice.Level = scheduled.Level;
                        voice.MidiOverride = scheduled.MidiOverride;

                        voice.StartSeconds =
                            scheduled.Note.StartBeat *
                            _secondsPerBeat;

                        voice.EndSeconds =
                            (scheduled.Note.StartBeat +
                             scheduled.Note.DurationBeats) *
                            _secondsPerBeat;

                        // El release real (el mismo que usa el envelope).
                        voice.LifeEndSeconds =
                            voice.EndSeconds +
                            GetReleaseSeconds(leitmotiv, voice.Preset) +
                            0.01;

                        voice.Phase1 = 0.0;
                        voice.Phase2 = 0.0;
                        voice.FilterState = 0f;

                        _activeVoices.Add(voice);
                    }

                    float mixed = 0f;

                    int activeCount = 0;

                    // --------------------------------------------
                    // VOCES
                    // --------------------------------------------

                    for (int v = _activeVoices.Count - 1; v >= 0; v--)
                    {
                        VoiceState voice = _activeVoices[v];

                        if (sampleTime > voice.LifeEndSeconds)
                        {
                            _activeVoices.RemoveAt(v);
                            _voicePool.Push(voice);
                            continue;
                        }

                        double elapsed =
                            sampleTime - voice.StartSeconds;

                        double duration =
                            voice.EndSeconds - voice.StartSeconds;

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
                            activeCount,
                            ref _normGain
                        );

                    mixed = ApplyMasterLimiter(mixed);

                    // Una sola muestra NaN/Inf suena como un chasquido fuerte.
                    if (float.IsNaN(mixed) || float.IsInfinity(mixed))
                        mixed = 0f;

                    for (int c = 0; c < channels; c++)
                        data[i + c] = mixed;
                }

                _clockSeconds += (double)frames / sampleRate;
            }
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
                Mathf.Sin((float)phase1);

            float osc2 =
                Mathf.Sin((float)phase2);

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

            /*float drive =
                1f +
                preset.Saturation *
                2f;

            raw =
                SoftClip(
                    raw *
                    drive
                );*/

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

        // La ganancia se suaviza: antes cambiaba de golpe cada vez que
        // entraba o salía una voz, creando escalones audibles (clicks).
        private float NormalizeMix(
            float mixed,
            int activeVoices,
            ref float gainState)
        {
            float target =
                1f / Mathf.Max(1f, Mathf.Sqrt(activeVoices));

            gainState +=
                (target - gainState) * _gainSmoothCoef;

            return mixed * gainState;
        }

        // ========================================================
        // LIMITADOR
        // ========================================================

        // Limitador suave (tanh): ganancia ~masterVolume en señal baja y
        // techo de 0.9. El Clamp anterior recortaba la onda = distorsión.
        private float ApplyMasterLimiter(float sample)
        {
            const float ceiling = 0.9f;

            return
                ceiling *
                (float)Math.Tanh(
                    sample * masterVolume / ceiling
                );
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

        // Release efectivo en segundos. TIENE que ser el mismo valor que usa
        // ComputeEnvelope, si no la voz se corta antes de llegar a 0 (click).
        private static float GetReleaseSeconds(
            LeitmotivData data,
            InstrumentPreset preset)
        {
            float release =
                data.HasMappedEnvelope
                    ? data.Release
                    : (preset != null ? preset.Release : 0.2f);

            return Mathf.Clamp(release, 0.03f, 1f);
        }

        private static float EnvelopeBody(
            double t,
            float attack,
            float decay,
            float sustain)
        {
            if (t < attack)
                return (float)(t / attack);

            double afterAttack = t - attack;

            if (afterAttack < decay)
            {
                return Mathf.Lerp(
                    1f,
                    sustain,
                    (float)(afterAttack / decay)
                );
            }

            return sustain;
        }

        private static float ComputeEnvelope(
            double elapsed,
            double duration,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            attack = Mathf.Clamp(attack, 0.005f, 1f);
            decay = Mathf.Clamp(decay, 0.01f, 1f);
            sustain = Mathf.Clamp(sustain, 0.08f, 1f);
            release = Mathf.Clamp(release, 0.03f, 1f);

            // ATTACK / DECAY / SUSTAIN
            if (elapsed < duration)
                return EnvelopeBody(elapsed, attack, decay, sustain);

            // RELEASE: parte del nivel REAL que tenía la nota al soltarse.
            // Antes, si la nota era más corta que attack+decay, el release
            // arrancaba desde "sustain" y el volumen daba un salto.
            double afterNote = elapsed - duration;

            if (afterNote >= release)
                return 0f;

            float levelAtNoteOff =
                EnvelopeBody(duration, attack, decay, sustain);

            return levelAtNoteOff *
                   (1f - (float)(afterNote / release));
        }
    }
}