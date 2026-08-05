using UnityEngine;

public class AvatarCapture : MonoBehaviour
{
    public Camera avatarCamera;

    public RenderTexture renderTexture;

    public int width = 512;
    public int height = 512;

    public Sprite CaptureAvatar()
    {
        RenderTexture current = RenderTexture.active;

        avatarCamera.targetTexture = renderTexture;
        avatarCamera.Render();

        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        avatarCamera.targetTexture = null;
        RenderTexture.active = current;

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f),
            100f
        );
    }
}