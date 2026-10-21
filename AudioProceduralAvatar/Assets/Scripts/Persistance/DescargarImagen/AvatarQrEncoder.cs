using System;
using System.Text;

namespace AudioProceduralAvatar.Persistence
{
    // Generador de QR minimo y autocontenido, SIN dependencias de Unity ni de internet.
    // Modo byte (UTF-8), correccion de errores nivel M, versiones 1 a 10 (hasta 213 bytes).
    // Basado en el algoritmo estandar (ISO/IEC 18004), con la misma estructura que el
    // generador de referencia de Nayuki (MIT).
    public static class AvatarQrEncoder
    {
        // Por version (1..10), nivel M: { bloques cortos, datos por bloque corto,
        // bloques largos, datos por bloque largo, bytes ECC por bloque }
        private static readonly int[][] Blocks =
        {
            null,
            new[] { 1, 16, 0, 0, 10 },
            new[] { 1, 28, 0, 0, 16 },
            new[] { 1, 44, 0, 0, 26 },
            new[] { 2, 32, 0, 0, 18 },
            new[] { 2, 43, 0, 0, 24 },
            new[] { 4, 27, 0, 0, 16 },
            new[] { 4, 31, 0, 0, 18 },
            new[] { 2, 38, 2, 39, 22 },
            new[] { 3, 36, 2, 37, 22 },
            new[] { 4, 43, 1, 44, 26 },
        };

        private static readonly int[][] AlignPos =
        {
            null,
            new int[0],
            new[] { 6, 18 },
            new[] { 6, 22 },
            new[] { 6, 26 },
            new[] { 6, 30 },
            new[] { 6, 34 },
            new[] { 6, 22, 38 },
            new[] { 6, 24, 42 },
            new[] { 6, 26, 46 },
            new[] { 6, 28, 50 },
        };

        public const int MaxBytes = 213;

        // Devuelve la matriz [fila, columna]; true = modulo oscuro. null si el texto no cabe.
        public static bool[,] Encode(string text)
        {
            if (text == null) return null;
            byte[] data = Encoding.UTF8.GetBytes(text);

            int version = 0;
            for (int v = 1; v <= 10; v++)
            {
                int[] b = Blocks[v];
                int capacityBits = (b[0] * b[1] + b[2] * b[3]) * 8;
                int countBits = v <= 9 ? 8 : 16;
                if (4 + countBits + data.Length * 8 <= capacityBits) { version = v; break; }
            }
            if (version == 0) return null;

            int[] info = Blocks[version];
            int totalData = info[0] * info[1] + info[2] * info[3];

            // ---- 1) Flujo de bits ----
            var bits = new System.Collections.Generic.List<bool>();
            AppendBits(bits, 0x4, 4); // modo byte
            AppendBits(bits, data.Length, version <= 9 ? 8 : 16);
            foreach (byte x in data) AppendBits(bits, x, 8);
            int capacity = totalData * 8;
            AppendBits(bits, 0, Math.Min(4, capacity - bits.Count)); // terminador
            while (bits.Count % 8 != 0) bits.Add(false);
            for (int pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11) AppendBits(bits, pad, 8);

            var dataCodewords = new byte[totalData];
            for (int i = 0; i < bits.Count; i++)
                if (bits[i]) dataCodewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

            // ---- 2) Reed-Solomon e intercalado ----
            byte[] all = AddEccAndInterleave(dataCodewords, info);

            // ---- 3) Dibujo ----
            int size = version * 4 + 17;
            var modules = new bool[size, size];
            var isFunction = new bool[size, size];
            DrawFunctionPatterns(modules, isFunction, size, version);
            DrawCodewords(modules, isFunction, size, all);

            int bestMask = 0;
            int minPenalty = int.MaxValue;
            for (int m = 0; m < 8; m++)
            {
                ApplyMask(modules, isFunction, size, m);
                DrawFormatBits(modules, isFunction, size, m);
                int p = Penalty(modules, size);
                if (p < minPenalty) { minPenalty = p; bestMask = m; }
                ApplyMask(modules, isFunction, size, m); // deshace
            }
            ApplyMask(modules, isFunction, size, bestMask);
            DrawFormatBits(modules, isFunction, size, bestMask);
            return modules;
        }

        // ================= Datos =================

        private static void AppendBits(System.Collections.Generic.List<bool> list, int value, int count)
        {
            for (int i = count - 1; i >= 0; i--) list.Add(((value >> i) & 1) != 0);
        }

        private static byte[] AddEccAndInterleave(byte[] data, int[] info)
        {
            int nShort = info[0], lenShort = info[1], nLong = info[2], lenLong = info[3], eccLen = info[4];
            int nBlocks = nShort + nLong;
            byte[] divisor = RsDivisor(eccLen);

            var blockData = new byte[nBlocks][];
            var blockEcc = new byte[nBlocks][];
            int k = 0;
            for (int i = 0; i < nBlocks; i++)
            {
                int len = i < nShort ? lenShort : lenLong;
                blockData[i] = new byte[len];
                Array.Copy(data, k, blockData[i], 0, len);
                k += len;
                blockEcc[i] = RsRemainder(blockData[i], divisor);
            }

            var result = new byte[data.Length + nBlocks * eccLen];
            int pos = 0;
            int maxLen = nLong > 0 ? lenLong : lenShort;
            for (int i = 0; i < maxLen; i++)
                for (int b = 0; b < nBlocks; b++)
                    if (i < blockData[b].Length) result[pos++] = blockData[b][i];
            for (int i = 0; i < eccLen; i++)
                for (int b = 0; b < nBlocks; b++)
                    result[pos++] = blockEcc[b][i];
            return result;
        }

        private static byte[] RsDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    result[j] = (byte)GfMul(result[j], root);
                    if (j + 1 < degree) result[j] ^= result[j + 1];
                }
                root = GfMul(root, 0x02);
            }
            return result;
        }

        private static byte[] RsRemainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            foreach (byte b in data)
            {
                int factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (int i = 0; i < result.Length; i++) result[i] ^= (byte)GfMul(divisor[i], factor);
            }
            return result;
        }

        private static int GfMul(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return z & 0xFF;
        }

        // ================= Patrones fijos =================

        private static void SetFn(bool[,] m, bool[,] fn, int size, int x, int y, bool dark)
        {
            if (x < 0 || y < 0 || x >= size || y >= size) return;
            m[y, x] = dark;
            fn[y, x] = true;
        }

        private static void DrawFunctionPatterns(bool[,] m, bool[,] fn, int size, int version)
        {
            for (int i = 0; i < size; i++)
            {
                SetFn(m, fn, size, 6, i, i % 2 == 0);
                SetFn(m, fn, size, i, 6, i % 2 == 0);
            }

            DrawFinder(m, fn, size, 3, 3);
            DrawFinder(m, fn, size, size - 4, 3);
            DrawFinder(m, fn, size, 3, size - 4);

            int[] pos = AlignPos[version];
            int n = pos.Length;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if ((i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0)) continue;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                            SetFn(m, fn, size, pos[i] + dx, pos[j] + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                }
            }

            DrawFormatBits(m, fn, size, 0); // reserva el espacio
            DrawVersion(m, fn, size, version);
        }

        private static void DrawFinder(bool[,] m, bool[,] fn, int size, int cx, int cy)
        {
            for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFn(m, fn, size, cx + dx, cy + dy, dist != 2 && dist != 4);
                }
        }

        private static void DrawFormatBits(bool[,] m, bool[,] fn, int size, int mask)
        {
            int data = (0 << 3) | mask; // nivel M = 00
            int rem = data;
            for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            int bits = ((data << 10) | rem) ^ 0x5412;

            for (int i = 0; i <= 5; i++) SetFn(m, fn, size, 8, i, Bit(bits, i));
            SetFn(m, fn, size, 8, 7, Bit(bits, 6));
            SetFn(m, fn, size, 8, 8, Bit(bits, 7));
            SetFn(m, fn, size, 7, 8, Bit(bits, 8));
            for (int i = 9; i < 15; i++) SetFn(m, fn, size, 14 - i, 8, Bit(bits, i));

            for (int i = 0; i < 8; i++) SetFn(m, fn, size, size - 1 - i, 8, Bit(bits, i));
            for (int i = 8; i < 15; i++) SetFn(m, fn, size, 8, size - 15 + i, Bit(bits, i));
            SetFn(m, fn, size, 8, size - 8, true);
        }

        private static void DrawVersion(bool[,] m, bool[,] fn, int size, int version)
        {
            if (version < 7) return;
            int rem = version;
            for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            int bits = (version << 12) | rem;
            for (int i = 0; i < 18; i++)
            {
                bool bit = Bit(bits, i);
                int a = size - 11 + i % 3;
                int b = i / 3;
                SetFn(m, fn, size, a, b, bit);
                SetFn(m, fn, size, b, a, bit);
            }
        }

        private static bool Bit(int x, int i) { return ((x >> i) & 1) != 0; }

        // ================= Colocacion y mascara =================

        private static void DrawCodewords(bool[,] m, bool[,] fn, int size, byte[] data)
        {
            int i = 0;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < size; vert++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? size - 1 - vert : vert;
                        if (!fn[y, x] && i < data.Length * 8)
                        {
                            m[y, x] = Bit(data[i >> 3], 7 - (i & 7));
                            i++;
                        }
                    }
                }
            }
        }

        private static void ApplyMask(bool[,] m, bool[,] fn, int size, int mask)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool invert;
                    switch (mask)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        default: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    if (invert && !fn[y, x]) m[y, x] = !m[y, x];
                }
        }

        // ================= Penalizacion (elige la mascara mas legible) =================

        private static int Penalty(bool[,] m, int size)
        {
            int result = 0;

            // Regla 1: tiras de 5+ modulos iguales en filas y columnas
            for (int a = 0; a < size; a++)
            {
                for (int dir = 0; dir < 2; dir++)
                {
                    int run = 1;
                    for (int b = 1; b < size; b++)
                    {
                        bool cur = dir == 0 ? m[a, b] : m[b, a];
                        bool prev = dir == 0 ? m[a, b - 1] : m[b - 1, a];
                        if (cur == prev) { run++; }
                        else { if (run >= 5) result += 3 + (run - 5); run = 1; }
                    }
                    if (run >= 5) result += 3 + (run - 5);
                }
            }

            // Regla 2: bloques de 2x2 del mismo color
            for (int y = 0; y < size - 1; y++)
                for (int x = 0; x < size - 1; x++)
                    if (m[y, x] == m[y, x + 1] && m[y, x] == m[y + 1, x] && m[y, x] == m[y + 1, x + 1])
                        result += 3;

            // Regla 3: patron tipo "finder" (1:1:3:1:1) con 4 claros a un lado
            bool[] p1 = { true, false, true, true, true, false, true, false, false, false, false };
            bool[] p2 = { false, false, false, false, true, false, true, true, true, false, true };
            for (int a = 0; a < size; a++)
                for (int b = 0; b <= size - 11; b++)
                    for (int dir = 0; dir < 2; dir++)
                    {
                        bool match1 = true, match2 = true;
                        for (int k = 0; k < 11 && (match1 || match2); k++)
                        {
                            bool v = dir == 0 ? m[a, b + k] : m[b + k, a];
                            if (v != p1[k]) match1 = false;
                            if (v != p2[k]) match2 = false;
                        }
                        if (match1) result += 40;
                        if (match2) result += 40;
                    }

            // Regla 4: balance entre oscuros y claros
            int dark = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (m[y, x]) dark++;
            int total = size * size;
            int kk = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            result += kk * 10;

            return result;
        }
    }
}
