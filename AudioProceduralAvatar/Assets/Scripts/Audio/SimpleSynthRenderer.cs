using UnityEngine;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Implementación inicial de IMusicRenderer: sintetiza el leitmotiv con
    /// osciladores simples vía OnAudioFilterRead, sin FMOD ni assets externos.
    /// Sirve para validar el pipeline completo (atributos -> música -> sonido)
    /// antes de invertir tiempo en integrar FMOD.
    ///
    /// TODO (Desarrollo): esta es la pieza más "esqueleto" de todas —
    /// hoy solo deja la interfaz y el enganche a Unity listos.
    /// Falta implementar la generación real de la onda por nota.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SimpleSynthRenderer : MonoBehaviour, IMusicRenderer
    {
        private LeitmotivData? _current;
        private float _playbackClock;
        private const int SampleRate = 44100;

        public void PlayLeitmotiv(LeitmotivData data)
        {
            _current = data;
            _playbackClock = 0f;
            // TODO: activar generación (ver OnAudioFilterRead)
        }

        public void Stop()
        {
            _current = null;
        }

        // Se ejecuta en el hilo de audio de Unity. Aquí es donde, nota por
        // nota, se escribiría la forma de onda (seno/diente de sierra/etc.)
        // según ScaleDegree -> frecuencia real, usando RootNoteMidi + Scale.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_current == null) return;

            // TODO: por cada sample, calcular en qué NoteEvent estamos
            // (según _playbackClock y TempoBpm) y sintetizar su frecuencia.
            // Placeholder: silencio, para que compile y el pipeline sea probable.

            _playbackClock += (float)data.Length / channels / SampleRate;
        }
    }
}
