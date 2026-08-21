using UnityEngine;
using UnityEngine.UI;

public class AvatarHairController : MonoBehaviour
{
    public Image hairSlotOnAvatar; // el Image en el canvas donde está el personaje

    public void SetHair(HairData hair)
    {
        hairSlotOnAvatar.sprite = hair.hairSprite;
    }
}
