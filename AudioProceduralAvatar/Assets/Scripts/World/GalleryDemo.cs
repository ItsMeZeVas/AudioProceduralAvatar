using UnityEngine;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;

namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// SOLO PARA PRUEBAS. Crea varios avatares random al iniciar y los manda
    /// a AvatarGalleryManager, para poder caminar por la galería y ver/clickear
    /// avatares sin esperar a que exista la UI de personalización real.
    /// Requiere un LeitmotivGenerator en la escena (puede ser el mismo
    /// GameObject u otro).
    /// </summary>
    public class GalleryDemo : MonoBehaviour
    {
        [SerializeField] private LeitmotivGenerator generator;
        [Tooltip("Con la capacidad por defecto de 6 avatares/plano, 14 crea automáticamente un tercer plano — útil para probar ese comportamiento.")]
        [SerializeField] private int avatarsToCreate = 14;

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
                var attrs = RandomAttributes(i);
                var leitmotiv = generator.Generate(attrs);
                AvatarGalleryManager.Instance.CreateAndSpawn(attrs, leitmotiv);
            }
        }

        private AvatarAttributes RandomAttributes(int index)
        {
            return new AvatarAttributes
            {
                AvatarName = "Avatar_" + index,
                Clothing = (ClothingType)Random.Range(0, System.Enum.GetValues(typeof(ClothingType)).Length),
                AccentColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f),
                Accessory = (AccessoryType)Random.Range(0, System.Enum.GetValues(typeof(AccessoryType)).Length),
                Trait = (CharacterTrait)Random.Range(0, System.Enum.GetValues(typeof(CharacterTrait)).Length),
            };
        }
    }
}
