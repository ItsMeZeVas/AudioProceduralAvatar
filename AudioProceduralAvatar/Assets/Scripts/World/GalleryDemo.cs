using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// SOLO PARA PRUEBAS. Crea varios AvatarProfile random al iniciar y los
    /// manda a AvatarGalleryManager, para poder caminar por la galería sin
    /// esperar a que haya avatares reales guardados en disco.
    /// Para probar el flujo REAL (personalización -> JSON -> galería), usa
    /// AvatarGalleryLoader en vez de este script.
    /// </summary>
    public class GalleryDemo : MonoBehaviour
    {
        [SerializeField] private LeitmotivGenerator generator;
        [Tooltip("Con la capacidad por defecto de 6 avatares/plano, 14 crea automáticamente un tercer plano — útil para probar ese comportamiento.")]
        [SerializeField] private int avatarsToCreate = 14;
        [SerializeField] private string[] layerNames = { "Body", "Head", "Hair" };
        [SerializeField] private int spritesPerLayer = 3;

        private void Start()
        {
            if (generator == null)
            {
                Debug.LogWarning("GalleryDemo: falta asignar un LeitmotivGenerator.");
                return;
            }
            if (AvatarGalleryManager.Instance == null)
            {
                Debug.LogWarning("GalleryDemo: no hay AvatarGalleryManager en la escena.");
                return;
            }

            for (int i = 0; i < avatarsToCreate; i++)
            {
                var profile = RandomProfile(i);
                var leitmotiv = generator.Generate(profile);
                AvatarGalleryManager.Instance.CreateAndSpawn(profile, leitmotiv);
            }
        }

        private AvatarProfile RandomProfile(int index)
        {
            var profile = new AvatarProfile
            {
                Id = System.Guid.NewGuid().ToString(),
                AvatarName = "Avatar_" + index,
                Layers = new List<LayerSelection>()
            };

            foreach (var layerName in layerNames)
                profile.Layers.Add(new LayerSelection { LayerName = layerName, SpriteIndex = Random.Range(0, spritesPerLayer) });

            return profile;
        }
    }
}
