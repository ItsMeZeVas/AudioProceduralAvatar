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
        private Gradient skinToneGradient;

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


        private void OnSliderChanged(
            float value)
        {
            CurrentValue =
                Mathf.Clamp01(value);


            if (targetImage != null)
            {
                targetImage.color =
                    skinToneGradient.Evaluate(
                        CurrentValue
                    );
            }
        }


        private Sprite GenerateGradientSprite()
        {
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