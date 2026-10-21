using UnityEngine;
using AudioProceduralAvatar.Persistence;

// Toma la foto de cuerpo completo del avatar en la escena de personalizacion,
// la pone sobre un fondo predeterminado y la guarda como {guid}.png.
// Usa el AvatarCapture que ya existe SIN modificarlo: solo lo llama, y deja la
// camara como estaba (para recortar el fondo pone transparencia un instante).
// Archivo NUEVO.
public class AvatarFullBodyPngSaver : MonoBehaviour
{
    [Header("Captura")]
    [Tooltip("El AvatarCapture de la escena de personalizacion.")]
    public AvatarCapture capture;

    [Header("Fondo predeterminado")]
    [Tooltip("Opcional. Imagen de fondo (en el Inspector de la textura: Read/Write Enabled). Si queda vacio, se usa el degradado.")]
    public Texture2D backgroundImage;
    public Color topColor = new Color32(238, 241, 248, 255);
    public Color bottomColor = new Color32(200, 210, 255, 255);

    // Llamar justo despues de crear/guardar el avatar, con el avatar visible en pantalla.
    // Sincrono: cuando devuelve, el PNG ya esta en disco. true si se guardo.
    public bool SaveFor(string avatarId)
    {
        if (string.IsNullOrEmpty(avatarId))
        {
            Debug.LogWarning("[AvatarFullBodyPngSaver] avatarId vacio.");
            return false;
        }
        if (capture == null || capture.avatarCamera == null)
        {
            Debug.LogWarning("[AvatarFullBodyPngSaver] Falta asignar capture (o su avatarCamera).");
            return false;
        }

        Camera cam = capture.avatarCamera;
        CameraClearFlags previousFlags = cam.clearFlags;
        Color previousBackground = cam.backgroundColor;

        Sprite sprite = null;
        try
        {
            // Fondo transparente solo durante la foto, para poder poner el nuestro.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            sprite = capture.CaptureAvatar();
        }
        finally
        {
            cam.clearFlags = previousFlags;
            cam.backgroundColor = previousBackground;
        }

        if (sprite == null || sprite.texture == null)
        {
            Debug.LogWarning("[AvatarFullBodyPngSaver] La captura no devolvio nada.");
            return false;
        }

        Texture2D shot = sprite.texture;
        bool saved = false;
        try
        {
            Color32[] fg = shot.GetPixels32();

            Color32[] bg = null;
            int bgW = 0, bgH = 0;
            if (backgroundImage != null)
            {
                if (backgroundImage.isReadable)
                {
                    bg = backgroundImage.GetPixels32();
                    bgW = backgroundImage.width;
                    bgH = backgroundImage.height;
                }
                else
                {
                    Debug.LogWarning("[AvatarFullBodyPngSaver] backgroundImage no es legible (activa Read/Write en su import). Se usa el degradado.");
                }
            }

            Color32[] merged = AvatarPngBackground.Composite(
                fg, shot.width, shot.height, bg, bgW, bgH, topColor, bottomColor);

            var result = new Texture2D(shot.width, shot.height, TextureFormat.RGBA32, false);
            result.SetPixels32(merged);
            result.Apply();
            byte[] png = result.EncodeToPNG();
            Destroy(result);

            saved = AvatarImageStorage.Save(avatarId, png);
        }
        finally
        {
            // CaptureAvatar crea una textura y un sprite nuevos en cada llamada: se liberan.
            Destroy(sprite);
            Destroy(shot);
        }

        return saved;
    }
}
