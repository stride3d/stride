using System;
using System.Buffers;

namespace Stride.Graphics.Font
{
    internal sealed partial class RuntimeSignedDistanceFieldSpriteFont
    {
        /// <summary>
        /// Bitmap-based SDF generation fallback, packed into RGB so median(R,G,B) = SDF value.
        /// </summary>
        private static unsafe CharacterBitmapRgba BuildSdfRgbFromCoverage(byte[] src, int srcW, int srcH, int srcPitch, int pad, int pixelRange, DistanceEncodeParams enc)
        {
            int w = srcW + pad * 2;
            int h = srcH + pad * 2;

            var inside = new bool[w * h];

            for (int y = 0; y < srcH; y++)
            {
                int dstRow = (y + pad) * w + pad;
                int srcRow = y * srcPitch;

                for (int x = 0; x < srcW; x++)
                {
                    inside[dstRow + x] = src[srcRow + x] >= 128;
                }
            }

            var distToOutsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: false, distToOutsideSq);

            var distToInsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: true, distToInsideSq);

            var bmp = new CharacterBitmapRgba(w, h);
            byte* dst = (byte*)bmp.Buffer;

            float scale = enc.Scale / Math.Max(1, pixelRange);

            float bias = enc.Bias;
            for (int y = 0; y < h; y++)
            {
                byte* row = dst + y * bmp.Pitch;
                int baseIdx = y * w;

                for (int x = 0; x < w; x++)
                {
                    int i = baseIdx + x;
                    float dOut = MathF.Sqrt(distToOutsideSq[i]);
                    float dIn = MathF.Sqrt(distToInsideSq[i]);
                    float signed = dOut - dIn;

                    float encoded = Math.Clamp(bias + signed * scale, 0f, 1f);
                    byte b = (byte)(encoded * 255f + 0.5f);

                    int o = x * 4;
                    row[o + 0] = b;
                    row[o + 1] = b;
                    row[o + 2] = b;
                    row[o + 3] = 255;
                }
            }

            return bmp;
        }
        /// <summary>
        /// Computes squared distances to the nearest feature pixels using Felzenszwalb/Huttenlocher EDT.
        /// </summary>
        private static void ComputeEdtSquared(int w, int h, bool[] inside, bool featureIsInside, float[] outDistSq)
        {
            const float INF = 1e20f;
            int maxDim = Math.Max(w, h);

            // Rent buffers from the shared pool instead of 'new'
            float[] tmp = ArrayPool<float>.Shared.Rent(w * h);
            float[] f = ArrayPool<float>.Shared.Rent(maxDim);
            float[] d = ArrayPool<float>.Shared.Rent(maxDim);
            int[] v = ArrayPool<int>.Shared.Rent(maxDim);
            float[] z = ArrayPool<float>.Shared.Rent(maxDim + 1);

            try
            {
                // Stage 1: vertical transform
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bool isFeature = (inside[y * w + x] == featureIsInside);
                        f[y] = isFeature ? 0f : INF;
                    }

                    DistanceTransform1D(f, h, d, v, z);

                    for (int y = 0; y < h; y++)
                        tmp[y * w + x] = d[y];
                }

                // Stage 2: horizontal transform
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                        f[x] = tmp[row + x];

                    DistanceTransform1D(f, w, d, v, z);

                    for (int x = 0; x < w; x++)
                        outDistSq[row + x] = d[x];
                }
            }
            finally
            {
                // ALWAYS return the arrays so they can be reused
                ArrayPool<float>.Shared.Return(tmp);
                ArrayPool<float>.Shared.Return(f);
                ArrayPool<float>.Shared.Return(d);
                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
            }
        }

        /// <summary>
        /// 1D squared distance transform using lower envelope of parabolas.
        /// Produces d[i] = min_j ((i - j)^2 + f[j]).
        /// </summary>
        private static void DistanceTransform1D(float[] f, int n, float[] d, int[] v, float[] z)
        {
            int k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;

            for (int q = 1; q < n; q++)
            {
                float s;
                while (true)
                {
                    int p = v[k];
                    // intersection of parabolas from p and q
                    s = ((f[q] + q * q) - (f[p] + p * p)) / (2f * (q - p));

                    if (s > z[k]) break;
                    k--;
                }

                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (int q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                int p = v[k];
                float dx = q - p;
                d[q] = dx * dx + f[p];
            }
        }
    }
}
