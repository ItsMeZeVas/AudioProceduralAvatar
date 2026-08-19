using UnityEngine;
using UnityEngine.UI;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Selector CONTINUO de tono de piel: un Slider mueve el color de un
    /// sprite (blanco por defecto) a través de un degradado que Diseño
    /// define en el Inspector (Gradient), en vez de ciclar entre sprites
    /// discretos como hacen las demás capas (Body/Head/Hair).
    ///
    /// CÓMO ARMARLO:
    /// 1. En la UI de personalización, agrega un Slider (rango 0 a 1) y una
    ///    Image con tu sprite blanco.
    /// 2. Agrega este componente a cualquier GameObject, asigna Slider y
    ///    Target Image.
    /// 3. En "Skin Tone Gradient", agrega los color stops del degradado que
    ///    quieran (Diseño decide los tonos exactos — clic en el gradiente
    ///    del Inspector para editarlo).
    /// 4. Asigna este componente en el campo "Skin Tone Selector" del
    ///    AvatarCreationController.
    /// </summary>
    public class SkinToneSelector : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image targetImage;

        [Tooltip("Degradado de tonos de piel, de un extremo a otro. Edítalo con los color stops del Inspector.")]
        [SerializeField] private Gradient skinToneGradient;

        [Tooltip("Opcional: la Image de fondo del Slider (Background). Si se asigna, el script genera automáticamente una textura con el degradado y se la pone ahí — así no hace falta crear la imagen a mano en otro programa.")]
        [SerializeField] private Image gradientBarImage;

        [Tooltip("Opcional: la Image de fondo del Selector (Background). Si se asigna, el script genera automáticamente una textura con el degradado y se la pone ahí — así no hace falta crear la imagen a mano en otro programa.")]
        [SerializeField] private Image gradientSelectorImage;

        [Tooltip("Nombre con el que este atributo se guarda en el AvatarProfile. Debe coincidir con el que uses en ContinuousAttributeRootNoteStrategy.")]
        [SerializeField] private string attributeName = "SkinTone";

        public float CurrentValue { get; private set; } = 0.5f;
        public string AttributeName => attributeName;

        private void Awake()
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderChanged);
                OnSliderChanged(slider.value);
            }
            else
            {
                Debug.LogWarning("SkinToneSelector: falta asignar 'Slider' en el Inspector.");
            }

            if (gradientBarImage != null)
                gradientBarImage.sprite = GenerateGradientSprite();
        }

        private void OnSliderChanged(float value)
        {
            CurrentValue = value;

            if (targetImage != null)
                targetImage.color = skinToneGradient.Evaluate(value);
        }

        // Genera una textura horizontal de 256px muestreando el mismo
        // Gradient que se usa para calcular el color real — una sola
        // fuente de verdad para los colores, nada duplicado a mano.
        private Sprite GenerateGradientSprite()
        {
            const int width = 256;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                texture.SetPixel(x, 0, skinToneGradient.Evaluate(t));
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
