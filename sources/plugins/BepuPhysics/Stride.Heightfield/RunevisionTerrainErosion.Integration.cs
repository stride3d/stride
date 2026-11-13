// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Heightfield;

public static partial class RunevisionTerrainErosion
{
    [DataContract]
    public record Settings
    {
        /// <summary>
        /// Control over whether the erosion effect raises or lowers the terrain.
        /// <list type="bullet">
        /// <item>
        ///  x: An offset value between -1 and 1, where a value of -1 only lowers, while
        ///     1 only raises. The offset is proportional to the erosion strength
        ///     parameter, so if that parameter is the same for the entire terrain, the
        ///     effect of the height offset will move the entire terrain surface up or
        ///     down by the same emount.
        /// </item>
        /// <item>
        ///  y: A value between 0 and 1 which is the degree to which the offset value is
        ///     replaced by the negated erosion fade target value. This has the effect
        ///     of only raising at valleys and only lowering at peaks, which, due to how
        ///     the erosion filter works, has the effect of largely preserving the minima
        ///     and maxima of the terrain.
        /// </item>
        /// </list>
        /// </summary>
        public Vector2 TerrainHeightOffset = new Vector2(-0.65f, 0.0f);

        public bool Water = false;

        public float WaterHeight = 0.36f;

        public bool Trees = false;

        public float GrassHeight = 0.465f;

        public ErosionParameters Erosion = new();

        [DataContract]
        public record struct ErosionParameters()
        {
            /// <summary>
            /// The scale of the erosion effect, affecting it both horizontally and vertically.
            /// </summary>
            [DataMemberRange(minimum: 0.08, maximum: 0.25, 0.01, 0.05, 4)]
            public float Scale = 0.15f;

            /// <summary>
            /// The strength of the erosion effect, affecting the magnitude of all octaves,
            /// and indirectly affecting the directions of the gullies as a result.
            /// </summary>
            [DataMemberRange(minimum: 0.01, maximum: 0.22, 0.01, 0.05, 4)]
            public float Strength = 0.22f;

            /// <summary>
            /// The magnitude of the gullies as a weight value from 0 to 1.
            /// A value of 0 can sharpen peaks and valleys but feature virtually no gullies.
            /// A value of 1 produces full gullies but may leave peaks and valleys rounded.
            /// Adjusting erosion gully weight while inversely adjusting erosion scale can be
            /// used to control the sharpness of peaks and valleys while leaving gully
            /// magnitudes largely untouched.
            /// </summary>
            [DataMemberRange(minimum: 0, maximum: 1, 0.1, 0.25, 4)]
            public float GullyWeight = 0.5f;

            /// <summary>
            /// The overall detail of the erosion. Lower values restrict the effect of higher
            /// frequency gullies to steeper slopes.
            /// </summary>
            [DataMemberRange(minimum: 0.7, maximum: 3, 0.1, 0.25, 3)]
            public float Detail = 1.5f;

            /// <summary>
            /// Separate rounding control of ridges and creases.
            /// <list type="bullet">
            /// <item>
            ///  x: Rounding of ridges.
            /// </item>
            /// <item>
            ///  y: Rounding of creases.
            /// </item>
            /// <item>
            ///  z: Multiplier applied to the initial height function.
            ///     E.g. if the height function has noise of 5 times lower frequency
            ///     than the largest gullies, a value of 0.2f can compensate for that.
            /// </item>
            /// <item>
            ///  w: Multiplier applied to each subsequent gully octave after the first.
            ///     Setting it to the same value as the erosion lacunarity will produce
            ///     consistent rounding of all octaves.
            /// </item>
            /// </list>
            /// </summary>
            public Vector4 Rounding = new Vector4(0.1f, 0.0f, 0.1f, 2.0f);

            /// <summary>
            /// Control over how far away from ridges/creases the erosion takes effect.
            /// <list type="bullet">
            /// <item>
            ///  x: Onset used on the initial height function.
            /// </item>
            /// <item>
            ///  y: Onset used on each gully octave.
            /// </item>
            /// <item>
            ///  z: RidgeMap-specific onset used on the initial height function.
            /// </item>
            /// <item>
            ///  w: RidgeMap-specific onset used on each gully octave.
            /// </item>
            /// </list>
            /// </summary>
            public Vector4 Onset = new Vector4(1.25f, 1.25f, 2.8f, 1.5f);

            /// <summary>
            /// Control over the assumed slope of the initial height function.
            /// In practise, assuming a slope can work better than using the input slope,
            /// since the final terrain can be shaped quite differently than the input.
            /// <list type="bullet">
            /// <item>
            ///  x: An assumed slope value to override the actual slope.
            /// </item>
            /// <item>
            ///  y: The amount (from 0 to 1) to override the actual slope.
            /// </item>
            /// </list>
            /// </summary>
            public Vector2 AssumedSlope = new Vector2(0.7f, 1.0f);

            /// <summary>
            /// Gullies are based on stripes within Voronoi-like cells in the Phacelle noise
            /// function. The cell scale parameter controls the sizes of the cells relative
            /// to the overall erosion scale, while keeping the stripe widths unaffected.
            /// Values close to 1 usually produce good results. Smaller values produce more
            /// grainy gullies while larger values produce longer unbroken gullies, but too
            /// large values produce chaotic curved gullies that are not aligned with the
            /// slopes. Value changes can cause abrupt changes in output, especially far away
            /// from the origin, so this parameter is not well suited for animation or for
            /// modulation by other functions.
            /// </summary>
            public float CellScale = 0.7f;

            /// <summary>
            /// The degree of normalization applied in the Phacelle noise, between 0 and 1.
            /// The erosion filter depends on a certain consistency in magnitude of the
            /// Phacelle output. However, high values can create loopy results where ridges
            /// and creases meet up at a point, which produces unnatural looking results.
            /// </summary>
            [DataMemberRange(minimum: 0, maximum: 1, 0.1, 0.25, 3)]
            public float Normalization = 0.5f;

            /// <summary>
            /// Control over the erosion octaves, with each successive octave layering
            /// smaller gullies onto the terrain.
            /// </summary>
            [DataMemberRange(minimum: 1, maximum: 20, 1, 5, 0)]
            public int Octaves = 5;

            /// <summary>
            /// The lacunarity controls the frequency (the inverse
            /// horizontal scale) of each octave relative to the last.
            /// </summary>
            public float Lacunarity = 2.0f;

            /// <summary>
            /// The gain controls the magnitude (the vertical
            /// scale) of each octave relative to the last.
            /// </summary>
            public float Gain = 0.5f;
        }
    }

    public struct Sample
    {
        public float Height;
        public float Erosion;
        public float Ridge;
        public float Tree;
        public float Debug;
    }

    private static float Fract(float value)
    {
        return value - System.MathF.Floor(value);
    }

    private static Vector2 Frac(Vector2 value)
    {
        return value - Vector2.Floor(value);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = MathUtil.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    private static float Length(Vector2 value)
    {
        return value.Length();
    }

    private static float Clamp01(float f)
    {
        return f > 1 ? 1 : f < 0 ? 0 : f;
    }

    private static Vector2 hash(Vector2 x)
    {
        Vector2 k = new Vector2(0.3183099f, 0.3678794f);
        x = x * k + k.YX();
        return new Vector2(-1.0f) + 2.0f * Frac(16.0f * k * Fract(x.X * x.Y * (x.X + x.Y)));
    }

    // Returns gradient noise (in x) and its derivatives (in yz).
    // From https://www.shadertoy.com/view/XdXBRH
    public static Vector3 noised(in Vector2 p)
    {
        Vector2 i = Vector2.Floor(p);
        Vector2 f = p - i;

        Vector2 u = f * f * f * (f * (f * new Vector2(6.0f) - new Vector2(15.0f)) + new Vector2(10.0f));
        Vector2 du = 30.0f * f * f * (f * (f - new Vector2(2.0f)) + new Vector2(1.0f));

        Vector2 ga = hash(i + new Vector2(0.0f, 0.0f));
        Vector2 gb = hash(i + new Vector2(1.0f, 0.0f));
        Vector2 gc = hash(i + new Vector2(0.0f, 1.0f));
        Vector2 gd = hash(i + new Vector2(1.0f, 1.0f));

        float va = Vector2.Dot(ga, f - new Vector2(0.0f, 0.0f));
        float vb = Vector2.Dot(gb, f - new Vector2(1.0f, 0.0f));
        float vc = Vector2.Dot(gc, f - new Vector2(0.0f, 1.0f));
        float vd = Vector2.Dot(gd, f - new Vector2(1.0f, 1.0f));

        var x = va + u.X * (vb - va) + u.Y * (vc - va) + u.X * u.Y * (va - vb - vc + vd);
        var yz = ga + u.X * (gb - ga) + u.Y * (gc - ga) + u.X * u.Y * (ga - gb - gc + gd) +
                 du * (u.YX() * (va - vb - vc + vd) + new Vector2(vb, vc) - new Vector2(va));

        return new Vector3(x, yz.X, yz.Y);
    }
}
