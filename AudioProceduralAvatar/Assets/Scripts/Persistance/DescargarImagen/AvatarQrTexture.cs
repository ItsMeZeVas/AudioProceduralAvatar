using UnityEngine;

namespace AudioProceduralAvatar.Persistence
{
    // Convierte un texto en una textura de QR, 100% local (sin internet).
    // Archivo NUEVO. Devuelve null si el texto no cabe en el QR.
    // La textura es responsabilidad de quien la pide: destruirla con Destroy()
    // cuando ya no se use, para no acumular memoria.
    public static class AvatarQrTexture
    {
        public static Texture2D Create(string text, int pixelsPerModule = 8, int quietModules = 4)
        {
            bool[,] modules = AvatarQrEncoder.Encode(text);
            if (modules == null) return null;

            pixelsPerModule = Mathf.Max(1, pixelsPerModule);
            quietModules = Mathf.Max(0, quietModules);

            int n = modules.GetLength(0);
            int side = (n + 2 * quietModules) * pixelsPerModule;

            var light = new Color32(255, 255, 255, 255);
            var dark = new Color32(0, 0, 0, 255);

            var pixels = new Color32[side * side];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = light;

            for (int row = 0; row < n; row++)
            {
                // La fila 0 del QR va ARRIBA; en una Texture2D la fila 0 es ABAJO.
                int texRow = n - 1 - row;
                for (int col = 0; col < n; col++)
                {
                    if (!modules[row, col]) continue;

                    int x0 = (col + quietModules) * pixelsPerModule;
                    int y0 = (texRow + quietModules) * pixelsPerModule;
                    for (int dy = 0; dy < pixelsPerModule; dy++)
                    {
                        int offset = (y0 + dy) * side + x0;
                        for (int dx = 0; dx < pixelsPerModule; dx++)
                            pixels[offset + dx] = dark;
                    }
                }
            }

            var tex = new Texture2D(side, side, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
