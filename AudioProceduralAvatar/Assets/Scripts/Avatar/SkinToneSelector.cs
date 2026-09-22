using UnityEngine;
using UnityEngine.UI;

namespace AudioProceduralAvatar.Avatar
{
    /// <summary>
    /// Selector continuo del tono de piel.
    ///
    /// El valor 0-1 se guarda en AvatarProfile como SkinTone.
    /// LeitmotivMappingConfig utiliza ese valor para determinar el tempo.
    /// </summary>
    public class SkinToneSelector : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private Image targetImage;

        [SerializeField]
        private SkinToneGradientAsset skinToneGradient;

        [SerializeField]
        private Image gradientBarImage;

        [SerializeField]
        private Image gradientSelectorImage;

        [SerializeField]
        private string attributeName = "SkinTone";


        public float CurrentValue { get; private set; } = 0.5f;

        public string AttributeName =>
            attributeName;


        private void Awake()
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;

                slider.onValueChanged.AddListener(
                    OnSliderChanged
                );

                OnSliderChanged(
                    slider.value
                );
            }
            else
            {
                Debug.LogWarning(
                    "SkinToneSelector: falta asignar Slider."
                );
            }


            if (gradientBarImage != null)
            {
                gradientBarImage.sprite =
                    GenerateGradientSprite();
            }
        }


        /// <summary>
        /// Fija el valor programáticamente (ej. para el preview en vivo de
        /// un preset secreto). Si hay slider, actualizarlo dispara
        /// OnSliderChanged solo; si no hay slider, actualiza directo.
        /// </summary>
        public void SetValue(float value)
        {
            float clamped = Mathf.Clamp01(value);

            if (slider != null)
            {
                slider.value = clamped; // dispara OnSliderChanged vía el listener
            }
            else
            {
                OnSliderChanged(clamped);
            }
        }


        private void OnSliderChanged(
            float value)
        {
            CurrentValue =
                Mathf.Clamp01(value);


            if (targetImage != null && skinToneGradient != null)
            {
                targetImage.color =
                    skinToneGradient.Evaluate(
                        CurrentValue
                    );
            }
        }


        private Sprite GenerateGradientSprite()
        {
            if (skinToneGradient == null)
            {
                Debug.LogWarning(
                    "SkinToneSelector: falta asignar Skin Tone Gradient."
                );
                return null;
            }

            const int width = 256;

            var texture =
                new Texture2D(
                    width,
                    1,
                    TextureFormat.RGBA32,
                    false
                )
                {
                    wrapMode =
                        TextureWrapMode.Clamp
                };


            for (int x = 0; x < width; x++)
            {
                float t =
                    x /
                    (float)(width - 1);

                texture.SetPixel(
                    x,
                    0,
                    skinToneGradient.Evaluate(t)
                );
            }


            texture.Apply();


            return Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    width,
                    1
                ),
                new Vector2(
                    0.5f,
                    0.5f
                )
            );
        }
    }
}
