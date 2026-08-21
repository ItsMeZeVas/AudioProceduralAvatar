using System.Collections.Generic;
using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Implementación de IMusicRenderer: sintetiza el leitmotiv con
    /// osciladores simples + envolvente ADSR, sin FMOD ni assets de audio.
    ///
    /// El TIMBRE (forma de onda, ADSR, volumen) sale de InstrumentPreset
    /// assets asignados en la lista "presets" del Inspector — Diseño puede
    /// agregar/editar presets sin tocar este script. Este código solo sabe
    /// "cómo tocar" el preset que le llega en cada nota.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SimpleSynthRenderer : MonoBehaviour, IMusicRenderer
    {
        [Tooltip("Todos los timbres disponibles. El InstrumentHint de LeitmotivData debe matchear el PresetId de alguno de estos.")]
        [SerializeField] private List<InstrumentPreset> presets = new();

        [SerializeField] private InstrumentPreset fallbackPreset;

        private const int SampleRate = 44100;

        private LeitmotivData? _current;
        private InstrumentPreset _activePreset;
        private double _clockSeconds;
        private double _secondsPerBeat;

        // Estado de envolvente por nota activa (para no cortar clicks al superponerse)
        private class VoiceState
        {
            public NoteEvent Note;
            public double StartSeconds;
            public double EndSeconds; // start + duración musical (antes de release)
            public double Phase;      // fase del oscilador, en radianes
        }
        private readonly List<VoiceState> _activeVoices = new();
        private int _nextNoteIndex;

        private void Awake()
        {
            var source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.Play(); // necesario para que Unity invoque OnAudioFilterRead continuamente
        }

        public void PlayLeitmotiv(LeitmotivData data)
        {
            _current = data;
            _activePreset = FindPreset(data.InstrumentHint) ?? fallbackPreset;
            _secondsPerBeat = 60.0 / data.TempoBpm;
            _clockSeconds = 0.0;
            _nextNoteIndex = 0;
            _activeVoices.Clear();

            if (_activePreset == null)
            {
                Debug.LogWarning($"SimpleSynthRenderer: no hay InstrumentPreset para '{data.InstrumentHint}' ni fallback asignado. No se reproducirá nada.");
            }
        }

        public void Stop()
        {
            _current = null;
            _activeVoices.Clear();
        }

        private InstrumentPreset FindPreset(string presetId)
        {
            foreach (var p in presets)
                if (p != null && p.PresetId == presetId) return p;
            return null;
        }

        // Hilo de audio de Unity. Debe ser rápido y sin allocations por sample.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_current == null || _activePreset == null)
            {
                return; // silencio
            }

            var leitmotiv = _current.Value;
            var notes = leitmotiv.Notes;

            for (int i = 0; i < data.Length; i += channels)
            {
                double sampleTime = _clockSeconds + (double)(i / channels) / SampleRate;

                // Activar nuevas notas cuyo StartBeat ya llegó
                while (_nextNoteIndex < notes.Count &&
                       notes[_nextNoteIndex].StartBeat * _secondsPerBeat <= sampleTime)
                {
                    var n = notes[_nextNoteIndex];
                    _activeVoices.Add(new VoiceState
                    {
                        Note = n,
                        StartSeconds = n.StartBeat * _secondsPerBeat,
                        EndSeconds = (n.StartBeat + n.DurationBeats) * _secondsPerBeat,
                        Phase = 0.0
                    });
                    _nextNoteIndex++;
                }

                float mixed = 0f;
                for (int v = _activeVoices.Count - 1; v >= 0; v--)
                {
                    var voice = _activeVoices[v];
                    double noteElapsed = sampleTime - voice.StartSeconds;
                    double totalLife = (voice.EndSeconds - voice.StartSeconds) + _activePreset.Release;

                    if (noteElapsed > totalLife)
                    {
                        _activeVoices.RemoveAt(v); // nota terminada, incluyendo release
                        continue;
                    }

                    int midiNote = MusicTheory.DegreeToMidiNote(leitmotiv.Scale, leitmotiv.RootNoteMidi, voice.Note.ScaleDegree);
                    float freq = MusicTheory.MidiToFrequency(midiNote);

                    voice.Phase += 2.0 * Mathf.PI * freq / SampleRate;
                    if (voice.Phase > 2.0 * Mathf.PI) voice.Phase -= 2.0 * Mathf.PI;

                    float raw = Oscillate(_activePreset.Waveform, voice.Phase);
                    float envelope = ComputeEnvelope(noteElapsed, voice.EndSeconds - voice.StartSeconds, _activePreset);

                    mixed += raw * envelope * _activePreset.Volume * voice.Note.Velocity;
                }

                // Mezcla simple: clamp para evitar clipping duro con varias voces
                mixed = Mathf.Clamp(mixed, -1f, 1f);

                for (int c = 0; c < channels; c++)
                    data[i + c] = mixed;
            }

            _clockSeconds += (double)data.Length / channels / SampleRate;
        }

        private static float Oscillate(WaveformType waveform, double phase)
        {
            switch (waveform)
            {
                case WaveformType.Sine:
                    return Mathf.Sin((float)phase);
                case WaveformType.Square:
                    return Mathf.Sin((float)phase) >= 0f ? 1f : -1f;
                case WaveformType.Sawtooth:
                    return (float)((phase / System.Math.PI) - 1.0);
                case WaveformType.Triangle:
                    return (float)(2.0 / System.Math.PI * System.Math.Asin(System.Math.Sin(phase)));
                default:
                    return 0f;
            }
        }

        // ADSR clásico: Attack sube a 1, Decay baja a Sustain, se mantiene en
        // Sustain hasta que termina la duración musical de la nota, luego Release baja a 0.
        private static float ComputeEnvelope(double elapsed, double noteDurationSeconds, InstrumentPreset preset)
        {
            if (elapsed < preset.Attack)
                return (float)(elapsed / preset.Attack);

            double afterAttack = elapsed - preset.Attack;
            if (afterAttack < preset.Decay)
            {
                float t = (float)(afterAttack / preset.Decay);
                return Mathf.Lerp(1f, preset.Sustain, t);
            }

            if (elapsed < noteDurationSeconds)
                return preset.Sustain;

            double afterNote = elapsed - noteDurationSeconds;
            if (afterNote < preset.Release)
            {
                float t = (float)(afterNote / preset.Release);
                return Mathf.Lerp(preset.Sustain, 0f, t);
            }

            return 0f;
        }
    }
}
