using UnityEngine;
using AudioProceduralAvatar.Audio;
using AudioProceduralAvatar.Persistence;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Al iniciar la escena de galería, carga todos los avatares guardados
    /// en disco (uno por cada persona que le dio "Crear avatar" en la
    /// personalización) y los coloca en el mundo.
    ///
    /// El leitmotiv se REGENERA aquí a partir del AvatarProfile — no se
    /// guarda en el JSON. Como la generación es determinista, el resultado
    /// es siempre el mismo mientras la configuración (LeitmotivMappingConfig,
    /// RootNoteStrategy) sea la misma en ambas escenas. Esto evita duplicar
    /// datos musicales y mantiene una sola fuente de verdad: el perfil.
    /// </summary>
    public class AvatarGalleryLoader : MonoBehaviour
    {
        [SerializeField] private LeitmotivGenerator generator;

        private void Start()
        {
            if (generator == null || AvatarGalleryManager.Instance == null)
            {
                Debug.LogWarning("AvatarGalleryLoader: falta el Generator o el AvatarGalleryManager en la escena.");
                return;
            }

            foreach (var id in AvatarJsonStorage.GetAllAvatarIds())
            {
                var profile = AvatarJsonStorage.Load(id);
                if (profile == null) continue;

                var leitmotiv = generator.Generate(profile);

                Sprite capturedSprite = null;
                var texture = AvatarJsonStorage.LoadImage(id);
                if (texture != null)
                {
                    capturedSprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                AvatarGalleryManager.Instance.CreateAndSpawn(profile, leitmotiv, capturedSprite);
            }
        }
    }
}
