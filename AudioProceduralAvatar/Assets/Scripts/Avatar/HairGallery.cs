using UnityEngine;
using UnityEngine.UI;

public class HairGallery : MonoBehaviour
{
    public GameObject hairButtonPrefab; // prefab del recuadro (Button + Image)
    public Transform content;           // el "Content" del Scroll View
    public HairData[] hairOptions;      // lista de cabellos disponibles
    public AvatarHairController avatarController;

    void Start()
    {
        foreach (var hair in hairOptions)
        {
            GameObject btnObj = Instantiate(hairButtonPrefab, content);
            btnObj.GetComponent<Image>().sprite = hair.thumbnail;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => avatarController.SetHair(hair));
        }
    }
}

[System.Serializable]
public class HairData
{
    public string nombre;
    public Sprite thumbnail;   // la miniatura que se ve en la galería
    public Sprite hairSprite;  // el sprite que se pone en el personaje (si es 2D)
    // o public GameObject hairPrefab; si es 3D
}