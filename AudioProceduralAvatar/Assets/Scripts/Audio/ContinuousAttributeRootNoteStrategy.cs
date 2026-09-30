using UnityEngine;
using AudioProceduralAvatar.Avatar;

namespace AudioProceduralAvatar.Audio
{
    [CreateAssetMenu(
        fileName = "ContinuousAttributeRootNoteStrategy",
        menuName =
            "AudioProceduralAvatar/Root Note Strategy/By Continuous Attribute"
    )]
    public class ContinuousAttributeRootNoteStrategy
        : RootNoteStrategy
    {
        [Tooltip(
            "Nombre del atributo continuo."
        )]
        public string AttributeName =
            "SkinTone";

        public bool Invert = false;

        [Range(0f, 1f)]
        public float FallbackValue = 0.5f;

        public override int GetRootMidi(
            AvatarProfile profile,
            int minRoot,
            int maxRoot)
        {
            if (profile == null)
            {
                return Mathf.Clamp(
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            minRoot,
                            maxRoot,
                            FallbackValue
                        )
                    ),
                    minRoot,
                    maxRoot
                );
            }

            float value =
                profile.GetContinuousValue(
                    AttributeName,
                    FallbackValue
                );

            value =
                Mathf.Clamp01(
                    value
                );

            if (Invert)
                value = 1f - value;

            int result =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        minRoot,
                        maxRoot,
                        value
                    )
                );

            return Mathf.Clamp(
                result,
                minRoot,
                maxRoot
            );
        }
    }
}