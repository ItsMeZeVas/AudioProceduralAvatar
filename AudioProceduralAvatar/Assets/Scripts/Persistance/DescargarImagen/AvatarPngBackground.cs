using UnityEngine;

namespace AudioProceduralAvatar.Persistence
{
    // Pone el avatar (con transparencia) sobre un fondo predeterminado: una imagen
    // si se da, o si no un degradado vertical. Funciona con arreglos de pixeles,
    // sin tocar texturas: archivo NUEVO, sin dependencias de otros scripts.
    // Convencion de Unity: la fila 0 de los arreglos es la de ABAJO.
    public static class AvatarPngBackground
    {
        public static Color32[] Composite(
            Color32[] fg, int w, int h,
            Color32[] bg, int bgW, int bgH,
            Color32 top, Color32 bottom)
        {
            var result = new Color32[w * h];
            bool useImage = bg != null && bgW > 0 && bgH > 0 && bg.Length >= bgW * bgH;

            for (int y = 0; y < h; y++)
            {
                float v = h > 1 ? y / (float)(h - 1) : 0f; // 0 = abajo, 1 = arriba

                for (int x = 0; x < w; x++)
                {
                    float u = w > 1 ? x / (float)(w - 1) : 0f;

                    float br, bgG, bb;
                    if (useImage)
                        SampleBilinear(bg, bgW, bgH, u, v, out br, out bgG, out bb);
                    else
                    {
                        br = bottom.r + (top.r - bottom.r) * v;
                        bgG = bottom.g + (top.g - bottom.g) * v;
                        bb = bottom.b + (top.b - bottom.b) * v;
                    }

                    Color32 f = fg[y * w + x];
                    float a = f.a / 255f;
                    result[y * w + x] = new Color32(
                        ToByte(f.r * a + br * (1f - a)),
                        ToByte(f.g * a + bgG * (1f - a)),
                        ToByte(f.b * a + bb * (1f - a)),
                        255);
                }
            }
            return result;
        }

        private static void SampleBilinear(Color32[] px, int w, int h, float u, float v,
            out float r, out float g, out float b)
        {
            float fx = u * (w - 1), fy = v * (h - 1);
            int x0 = (int)fx, y0 = (int)fy;
            int x1 = x0 + 1 < w ? x0 + 1 : x0;
            int y1 = y0 + 1 < h ? y0 + 1 : y0;
            float tx = fx - x0, ty = fy - y0;

            Color32 c00 = px[y0 * w + x0], c10 = px[y0 * w + x1];
            Color32 c01 = px[y1 * w + x0], c11 = px[y1 * w + x1];

            r = Lerp(Lerp(c00.r, c10.r, tx), Lerp(c01.r, c11.r, tx), ty);
            g = Lerp(Lerp(c00.g, c10.g, tx), Lerp(c01.g, c11.g, tx), ty);
            b = Lerp(Lerp(c00.b, c10.b, tx), Lerp(c01.b, c11.b, tx), ty);
        }

        private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        private static byte ToByte(float v)
        {
            int i = (int)(v + 0.5f);
            return (byte)(i < 0 ? 0 : (i > 255 ? 255 : i));
        }
    }
}
