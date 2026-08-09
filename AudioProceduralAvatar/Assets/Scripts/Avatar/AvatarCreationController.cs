using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Pega los scripts de personalización que ya existen (AvatarCreator,
    /// AvatarData, AvatarCapture — sin namespace) con nuestro pipeline de
    /// leitmotiv y la persistencia en JSON.
    ///
    /// CÓMO CONECTARLO:
    /// 1. Agrega este componente a cualquier GameObject de la escena de
    ///    personalización (ej. el mismo "Managers").
    /// 2. Asigna en el Inspector: Avatar Creator, Avatar Data, y si ya está
    ///    listo, Avatar Capture (opcional — si no está armado el render de
    ///    captura todavía, se guarda sin imagen y no rompe nada).
    /// 3. Asigna un Leitmotiv Generator (agrégalo a este mismo GameObject si
    ///    no existe ya en la escena).
    /// 4. Verifica que "Layer Names" tenga los mismos nombres exactos que
    ///    usaste en los AvatarLayer del AvatarCreator (Body, Head, Hair).
    /// 5. En el botón "Crear avatar" de la UI (agrégalo si no existe), en su
    ///    OnClick() arrastra este GameObject y selecciona CreateAvatar().
    /// </summary>
    public class AvatarCreationController : MonoBehaviour
    {
        [Header("Referencias a los scripts existentes de personalización")]
        [SerializeField] private global::AvatarCreator avatarCreator;
        [SerializeField] private global::AvatarData avatarData;
        [Tooltip("Opcional. Si no está asignado, el avatar se guarda sin imagen (se usa un sprite placeholder en la galería).")]
        [SerializeField] private global::AvatarCapture avatarCapture;

        [Header("Nuestro pipeline")]
        [SerializeField] private LeitmotivGenerator leitmotivGenerator;

        [Tooltip("Debe coincidir EXACTO con los layerName configurados en el AvatarCreator.")]
        [SerializeField] private string[] layerNames = { "Body", "Head", "Hair" };

        /// <summary>Conectar al OnClick() del botón "Crear avatar".</summary>
        public void CreateAvatar()
        {
            if (avatarCreator == null || avatarData == null || leitmotivGenerator == null)
            {
                Debug.LogWarning("AvatarCreationController: faltan referencias por asignar en el Inspector.");
                return;
            }

            var profile = BuildProfile();
            var leitmotiv = leitmotivGenerator.Generate(profile);

            Texture2D capturedTexture = null;
            if (avatarCapture != null)
            {
                var sprite = avatarCapture.CaptureAvatar();
                if (sprite != null) capturedTexture = sprite.texture;
            }

            AvatarJsonStorage.Save(profile, capturedTexture);

            Debug.Log($"[AvatarCreationController] '{profile.AvatarName}' guardado (id {profile.Id}). " +
                      $"Leitmotiv: Scale={leitmotiv.Scale} Root={leitmotiv.RootNoteMidi} Tempo={leitmotiv.TempoBpm} Instrument={leitmotiv.InstrumentHint}");
        }

        private AvatarProfile BuildProfile()
        {
            var profile = new AvatarProfile
            {
                Id = System.Guid.NewGuid().ToString(),
                AvatarName = avatarData.GetAvatarName(),
                StudentCode = avatarData.GetStudentCode()
            };

            foreach (var layerName in layerNames)
            {
                int index = GetCurrentIndex(layerName);
                profile.Layers.Add(new LayerSelection { LayerName = layerName, SpriteIndex = index });
            }

            return profile;
        }

        private int GetCurrentIndex(string layerName)
        {
            foreach (var layer in avatarCreator.layers)
            {
                if (layer.layerName == layerName)
                    return layer.currentIndex;
            }
            return 0;
        }
    }
}
