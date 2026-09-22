using UnityEngine;

// Gradiente de tono de piel compartido entre SkinToneSelector (escena de
// personalización) y AvatarThumbnailSlot (miniaturas del álbum), para que
// ambos usen siempre el mismo gradiente sin duplicarlo a mano y sin riesgo
// de que se desincronicen si lo cambian después.
[CreateAssetMenu(fileName = "SkinToneGradient", menuName = "Avatar/Skin Tone Gradient")]
public class SkinToneGradientAsset : ScriptableObject
{
    public Gradient gradient;

    public Color Evaluate(float t) => gradient.Evaluate(Mathf.Clamp01(t));
}
