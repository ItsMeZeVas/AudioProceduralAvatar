using System.Collections.Generic;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Sintetizador procedural simple.
    ///
    /// InstrumentPreset:
    ///     Waveform
    ///     Volume
    ///     ADSR base
    ///
    /// LeitmotivMappingConfig:
    ///     Instrumento -> Eyes
    ///     ADSR       -> UpperBody
    ///     Dinámica   -> Accessories
    ///
    /// El resultado se sintetiza directamente con C# y Unity.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SimpleSynthRenderer
        : MonoBehaviour, IMusicRenderer
    {
        [Tooltip(
            "Todos los InstrumentPreset disponibles."
        )]
        [SerializeField]
        private List<InstrumentPreset> presets = new();

        [SerializeField]
        private InstrumentPreset fallbackPreset;

        private const int SampleRate = 44100;

        private LeitmotivData? _current;

        private InstrumentPreset _activePreset;

        private double _clockSeconds;

        private double _secondsPerBeat;

        private class VoiceState
        {
            public NoteEvent Note;

            public double StartSeconds;

            public double EndSeconds;

            public double Phase;
        }

        private readonly List<VoiceState>
            _activeVoices = new();

        private int _nextNoteIndex;


        private void Awake()
        {
            var source =
                GetComponent<AudioSource>();

            source.playOnAwake = false;

            source.Play();
        }

        // ========================================================
        // EXPORTAR
        // ========================================================

        public float[] RenderOffline(LeitmotivData data)
        {
            var preset = FindPreset(data.InstrumentHint) ?? fallbackPreset;
            if (preset == null)
            {
                Debug.LogWarning($"SimpleSynthRenderer: no existe InstrumentPreset para '{data.InstrumentHint}' y tampoco hay fallback. No se puede exportar.");
                return System.Array.Empty<float>();
            }

            double secondsPerBeat = 60.0 / Mathf.Max(1f, data.TempoBpm);
            float attack = data.HasMappedEnvelope ? data.Attack : preset.Attack;
            float decay = data.HasMappedEnvelope ? data.Decay : preset.Decay;
            float sustain = data.HasMappedEnvelope ? data.Sustain : preset.Sustain;
            float release = data.HasMappedEnvelope ? data.Release : preset.Release;

            double totalSeconds = 0.1;
            var voices = new System.Collections.Generic.List<(NoteEvent note, double start, double end)>();
            foreach (var note in data.Notes)
            {
                double start = note.StartBeat * secondsPerBeat;
                double end = (note.StartBeat + note.DurationBeats) * secondsPerBeat;
                voices.Add((note, start, end));
                if (end + release > totalSeconds) totalSeconds = end + release;
            }

            int totalSamples = Mathf.CeilToInt((float)(totalSeconds * SampleRate));
            float[] buffer = new float[totalSamples];
            var phase = new double[voices.Count];

            for (int i = 0; i < totalSamples; i++)
            {
                double sampleTime = (double)i / SampleRate;
                float mixed = 0f;

                for (int v = 0; v < voices.Count; v++)
                {
                    var (note, start, end) = voices[v];
                    double noteDuration = end - start;
                    double noteElapsed = sampleTime - start;
                    if (noteElapsed < 0 || noteElapsed > noteDuration + release) continue;

                    int midiNote = MusicTheory.DegreeToMidiNote(data.Scale, data.RootNoteMidi, note.ScaleDegree);
                    float frequency = MusicTheory.MidiToFrequency(midiNote);

                    phase[v] += 2.0 * Mathf.PI * frequency / SampleRate;
                    if (phase[v] > 2.0 * Mathf.PI) phase[v] -= 2.0 * Mathf.PI;

                    float raw = Oscillate(preset.Waveform, phase[v]);
                    float envelope = ComputeEnvelope(noteElapsed, noteDuration, attack, decay, sustain, release);
                    float dynamic = Mathf.Clamp01(data.DynamicMultiplier);
                    mixed += raw * envelope * preset.Volume * note.Velocity * dynamic;
                }

                buffer[i] = Mathf.Clamp(mixed, -1f, 1f);
            }

            return buffer;
        }

        // ========================================================
        // REPRODUCIR
        // ========================================================

        public void PlayLeitmotiv(
            LeitmotivData data)
        {
            _current = data;

            _activePreset =
                FindPreset(
                    data.InstrumentHint
                ) ?? fallbackPreset;


            _secondsPerBeat =
                60.0 /
                Mathf.Max(
                    1f,
                    data.TempoBpm
                );


            _clockSeconds = 0.0;

            _nextNoteIndex = 0;

            _activeVoices.Clear();


            if (_activePreset == null)
            {
                Debug.LogWarning(
                    $"SimpleSynthRenderer: no existe InstrumentPreset para '{data.InstrumentHint}' y tampoco hay fallback."
                );
            }
        }


        // ========================================================
        // DETENER
        // ========================================================

        public void Stop()
        {
            _current = null;

            _activeVoices.Clear();
        }


        // ========================================================
        // BUSCAR INSTRUMENTO
        // ========================================================

        private InstrumentPreset FindPreset(
            string presetId)
        {
            foreach (var p in presets)
            {
                if (p != null &&
                    p.PresetId == presetId)
                {
                    return p;
                }
            }

            return null;
        }


        // ========================================================
        // AUDIO THREAD
        // ========================================================

        private void OnAudioFilterRead(
            float[] data,
            int channels)
        {
            if (_current == null ||
                _activePreset == null)
            {
                return;
            }


            var leitmotiv =
                _current.Value;

            var notes =
                leitmotiv.Notes;


            for (
                int i = 0;
                i < data.Length;
                i += channels)
            {
                double sampleTime =
                    _clockSeconds +
                    (double)(i / channels) /
                    SampleRate;


                // ------------------------------------------------
                // ACTIVAR NOTAS
                // ------------------------------------------------

                while (
                    _nextNoteIndex <
                    notes.Count &&
                    notes[_nextNoteIndex]
                        .StartBeat *
                        _secondsPerBeat
                        <= sampleTime)
                {
                    var n =
                        notes[_nextNoteIndex];


                    _activeVoices.Add(
                        new VoiceState
                        {
                            Note = n,

                            StartSeconds =
                                n.StartBeat *
                                _secondsPerBeat,

                            EndSeconds =
                                (
                                    n.StartBeat +
                                    n.DurationBeats
                                ) *
                                _secondsPerBeat,

                            Phase = 0.0
                        }
                    );


                    _nextNoteIndex++;
                }


                float mixed = 0f;


                // ------------------------------------------------
                // VOCES
                // ------------------------------------------------

                for (
                    int v =
                        _activeVoices.Count - 1;
                    v >= 0;
                    v--)
                {
                    var voice =
                        _activeVoices[v];


                    double noteElapsed =
                        sampleTime -
                        voice.StartSeconds;


                    // ------------------------------------------------
                    // ADSR
                    // ------------------------------------------------

                    float attack =
                        leitmotiv.HasMappedEnvelope
                            ? leitmotiv.Attack
                            : _activePreset.Attack;

                    float decay =
                        leitmotiv.HasMappedEnvelope
                            ? leitmotiv.Decay
                            : _activePreset.Decay;

                    float sustain =
                        leitmotiv.HasMappedEnvelope
                            ? leitmotiv.Sustain
                            : _activePreset.Sustain;

                    float release =
                        leitmotiv.HasMappedEnvelope
                            ? leitmotiv.Release
                            : _activePreset.Release;


                    double noteDuration =
                        voice.EndSeconds -
                        voice.StartSeconds;


                    double totalLife =
                        noteDuration +
                        release;


                    if (noteElapsed > totalLife)
                    {
                        _activeVoices.RemoveAt(v);

                        continue;
                    }


                    // ------------------------------------------------
                    // FRECUENCIA
                    // ------------------------------------------------

                    int midiNote =
                        MusicTheory.DegreeToMidiNote(
                            leitmotiv.Scale,
                            leitmotiv.RootNoteMidi,
                            voice.Note.ScaleDegree
                        );


                    float frequency =
                        MusicTheory.MidiToFrequency(
                            midiNote
                        );


                    // ------------------------------------------------
                    // OSCILADOR
                    // ------------------------------------------------

                    voice.Phase +=
                        2.0 *
                        Mathf.PI *
                        frequency /
                        SampleRate;


                    if (
                        voice.Phase >
                        2.0 * Mathf.PI)
                    {
                        voice.Phase -=
                            2.0 * Mathf.PI;
                    }


                    float raw =
                        Oscillate(
                            _activePreset.Waveform,
                            voice.Phase
                        );


                    // ------------------------------------------------
                    // ENVOLVENTE
                    // ------------------------------------------------

                    float envelope =
                        ComputeEnvelope(
                            noteElapsed,
                            noteDuration,
                            attack,
                            decay,
                            sustain,
                            release
                        );


                    // ------------------------------------------------
                    // DINÁMICA
                    // ------------------------------------------------

                    float dynamic =
                        Mathf.Clamp01(
                            leitmotiv.DynamicMultiplier
                        );


                    mixed +=
                        raw *
                        envelope *
                        _activePreset.Volume *
                        voice.Note.Velocity *
                        dynamic;
                }


                // Evitar clipping
                mixed =
                    Mathf.Clamp(
                        mixed,
                        -1f,
                        1f
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

                    return (float)(
                        phase /
                        System.Math.PI
                        - 1.0
                    );


                case WaveformType.Triangle:

                    return (float)(
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


        // ========================================================
        // ADSR
        // ========================================================

        private static float ComputeEnvelope(
            double elapsed,
            double noteDurationSeconds,
            float attack,
            float decay,
            float sustain,
            float release)
        {
            // Evitamos divisiones entre cero
            attack =
                Mathf.Max(
                    0.0001f,
                    attack
                );

            decay =
                Mathf.Max(
                    0.0001f,
                    decay
                );

            release =
                Mathf.Max(
                    0.0001f,
                    release
                );


            // ATTACK

            if (elapsed < attack)
            {
                return (float)(
                    elapsed / attack
                );
            }


            // DECAY

            double afterAttack =
                elapsed - attack;


            if (afterAttack < decay)
            {
                float t =
                    (float)(
                        afterAttack /
                        decay
                    );


                return Mathf.Lerp(
                    1f,
                    sustain,
                    t
                );
            }


            // SUSTAIN

            if (
                elapsed <
                noteDurationSeconds)
            {
                return sustain;
            }


            // RELEASE

            double afterNote =
                elapsed -
                noteDurationSeconds;


            if (afterNote < release)
            {
                float t =
                    (float)(
                        afterNote /
                        release
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
