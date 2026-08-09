using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Motor de decisión musical. Recibe un AvatarProfile real (capas +
    /// nombre + código), entrega LeitmotivData. No reproduce audio — eso lo
    /// hace un IMusicRenderer.
    ///
    /// Todas las reglas de mapeo viven fuera de esta clase: escala/tempo/
    /// instrumento en LeitmotivMappingConfig (editable por Diseño), y la
    /// tónica en un RootNoteStrategy intercambiable (sin decidir aún).
    /// </summary>
    public class LeitmotivGenerator : MonoBehaviour
    {
        [SerializeField] private LeitmotivMappingConfig mappingConfig;

        [Tooltip("Fuente de la tónica. Si se deja vacío, se usa un hash determinista interno (ver FallbackHashRoot).")]
        [SerializeField] private RootNoteStrategy rootNoteStrategy;

        [Header("Longitud del motivo (independiente del config, es del algoritmo)")]
        [SerializeField] private int noteCount = 6;

        public LeitmotivData Generate(AvatarProfile profile)
        {
            int minRoot = mappingConfig != null ? mappingConfig.MinRootMidi : 48;
            int maxRoot = mappingConfig != null ? mappingConfig.MaxRootMidi : 60;

            var data = new LeitmotivData
            {
                OwnerAvatarName = profile.AvatarName,
                Scale = mappingConfig != null ? mappingConfig.GetScale(profile) : MusicalScale.Mayor,
                RootNoteMidi = rootNoteStrategy != null
                    ? rootNoteStrategy.GetRootMidi(profile, minRoot, maxRoot)
                    : FallbackHashRoot(profile, minRoot, maxRoot),
                TempoBpm = mappingConfig != null ? mappingConfig.GetTempo(profile) : 100f,
                InstrumentHint = mappingConfig != null ? mappingConfig.GetInstrumentPresetId(profile) : "pluck",
                Notes = GenerateNotes(profile)
            };
            return data;
        }

        // Solo se usa si no hay RootNoteStrategy asignado (fallback de emergencia).
        private int FallbackHashRoot(AvatarProfile profile, int minRoot, int maxRoot)
        {
            int hash = !string.IsNullOrEmpty(profile.Id) ? profile.Id.GetHashCode() : profile.AvatarName.GetHashCode();
            int range = Mathf.Max(1, maxRoot - minRoot);
            return minRoot + Mathf.Abs(hash) % (range + 1);
        }

        // Contorno melódico con reglas reales: caminata acotada en vez de
        // saltos totalmente al azar (así suena como una frase, no como
        // ruido), termina siempre en la tónica (grado 0) para dar sensación
        // de resolución/cierre, y varía ritmo + acento por nota.
        private List<NoteEvent> GenerateNotes(AvatarProfile profile)
        {
            var notes = new List<NoteEvent>();
            string seedSource = !string.IsNullOrEmpty(profile.Id) ? profile.Id : profile.AvatarName;
            var rnd = new System.Random(seedSource.GetHashCode());

            int[] stepChoices = { -2, -1, -1, 0, 1, 1, 2 };
            int currentDegree = 0;
            float beat = 0f;

            for (int i = 0; i < noteCount; i++)
            {
                bool isLastNote = i == noteCount - 1;
                int degree = isLastNote ? 0 : currentDegree; // resolución final a la tónica

                float duration = PickDuration(rnd);
                float velocity = (i % 2 == 0) ? 0.85f : 0.65f; // acento simple en notas pares

                notes.Add(new NoteEvent
                {
                    ScaleDegree = degree,
                    StartBeat = beat,
                    DurationBeats = duration,
                    Velocity = velocity
                });

                beat += duration;

                if (!isLastNote)
                {
                    int step = stepChoices[rnd.Next(stepChoices.Length)];
                    currentDegree = Mathf.Clamp(currentDegree + step, -1, 7); // evita saltos de registro extremos
                }
            }

            return notes;
        }

        // Duraciones ponderadas: más corcheas/negras que redondas, para que
        // un motivo tan corto no se sienta arrastrado.
        private static float PickDuration(System.Random rnd)
        {
            int roll = rnd.Next(100);
            if (roll < 45) return 0.5f;   // negra
            if (roll < 70) return 0.25f;  // corchea
            if (roll < 90) return 0.75f;  // negra con puntillo
            return 1f;                    // redonda corta (poco frecuente)
        }
    }
}
