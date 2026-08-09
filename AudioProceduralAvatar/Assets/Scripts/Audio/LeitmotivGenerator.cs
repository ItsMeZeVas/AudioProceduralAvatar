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

        // TODO: sustituir por una generación con reglas melódicas reales
        // (evitar saltos grandes repetidos, resolver hacia la tónica, etc.)
        // Por ahora: patrón determinista simple para poder probar el pipeline.
        private List<NoteEvent> GenerateNotes(AvatarProfile profile)
        {
            var notes = new List<NoteEvent>();
            string seedSource = !string.IsNullOrEmpty(profile.Id) ? profile.Id : profile.AvatarName;
            var rnd = new System.Random(seedSource.GetHashCode());

            float beat = 0f;
            for (int i = 0; i < noteCount; i++)
            {
                notes.Add(new NoteEvent
                {
                    ScaleDegree = rnd.Next(0, 5), // grados 0-4 de la escala
                    StartBeat = beat,
                    DurationBeats = 0.5f,
                    Velocity = 0.8f
                });
                beat += 0.5f;
            }
            return notes;
        }
    }
}
