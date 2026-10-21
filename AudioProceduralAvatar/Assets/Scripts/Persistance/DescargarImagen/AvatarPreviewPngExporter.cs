using UnityEngine;

// Toma una foto con fondo transparente de lo que ve la camara de vista previa
// del panel de detalle (la que solo renderiza el layer del rig del avatar) y
// la devuelve como bytes PNG. Archivo NUEVO: no modifica AvatarCapture ni nada
// existente; renderiza a una RenderTexture temporal y deja la camara como estaba.
public class AvatarPreviewPngExporter : MonoBehaviour
{
    [Tooltip("La camara dedicada de la vista previa del panel (la que ve solo el layer AvatarPreview).")]
    public Camera previewCamera;

    [Tooltip("Si la camara ya tiene una RenderTexture asignada, se usa su tamano (para respetar el encuadre) multiplicado por la escala.")]
    public bool matchPreviewAspect = true;

    [Range(1f, 4f)]
    [Tooltip("Multiplica el tamano de la vista previa para que la imagen descargada salga mas nitida.")]
    public float resolutionScale = 2f;

    [Tooltip("Solo se usan si la camara no tiene RenderTexture o matchPreviewAspect esta apagado.")]
    public int fallbackWidth = 800;
    public int fallbackHeight = 1000;

    public byte[] CapturePng()
    {
        if (previewCamera == null)
        {
            Debug.LogWarning("[AvatarPreviewPngExporter] Falta previewCamera.");
            return null;
        }

        RenderTexture previousTarget = previewCamera.targetTexture;
        CameraClearFlags previousFlags = previewCamera.clearFlags;
        Color previousBackground = previewCamera.backgroundColor;
        RenderTexture previousActive = RenderTexture.active;

        int w = fallbackWidth;
        int h = fallbackHeight;
        if (matchPreviewAspect && previousTarget != null)
        {
            w = Mathf.RoundToInt(previousTarget.width * resolutionScale);
            h = Mathf.RoundToInt(previousTarget.height * resolutionScale);
        }
        w = Mathf.Clamp(w, 64, 4096);
        h = Mathf.Clamp(h, 64, 4096);

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = null;
        byte[] png = null;

        try
        {
            previewCamera.targetTexture = rt;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.Render();

            RenderTexture.active = rt;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            png = tex.EncodeToPNG();
        }
        finally
        {
            previewCamera.targetTexture = previousTarget;
            previewCamera.clearFlags = previousFlags;
            previewCamera.backgroundColor = previousBackground;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
            if (tex != null) Destroy(tex);
        }

        return png;
    }
}
