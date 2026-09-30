using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using static System.MathF;
using static Stride.Core.Mathematics.MathUtil;
using static Stride.Core.Mathematics.Vector2;
using static Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// Ported https://blog.runevision.com/2026/03/fast-and-gorgeous-erosion-filter.html as is to c# based on the
/// GLSL implementation https://www.shadertoy.com/view/wXcfWn
/// Comments are his.
/// </summary>
public static partial class RunevisionTerrainErosion
{
    /*
    =====================================================================================

    Advanced terrain erosion filter based on stacked faded gullies,
    with controls for erosion strength, detail, ridge and crease rounding,
    and producing a ridge map output useful for e.g. water drainage.

    For more on the technique, see:
    https://www.youtube.com/watch?v=gsJHzBTPG0Y
    https://blog.runevision.com/2026/03/fast-and-gorgeous-erosion-filter.html

    This buffer has three parts:

     - Phacelle Nose function (used by the erosion function)
     - Erosion function
     - Demonstration

    For explanations of the erosion parameters, see the demonstration section.

    This erosion technique was originally derived from versions by
    Clay John (https://www.shadertoy.com/view/MtGcWh)
    and Fewes (https://www.shadertoy.com/view/7ljcRW)
    and my own cleaned up version (https://www.shadertoy.com/view/33cXW8),
    but at this point has little in common with them, apart from the high level concept.

    Also see "Mouse-Paint Eroded Mountains" variation with interactive heightmap.
    https://www.shadertoy.com/view/sf23W1

    The raymarched terrain rendering is largely based on Fewes' Shadertoy;
    see the Image buffer for more info on that.

    =====================================================================================
    */


    // -----------------------------------------------------------------------------
    // PHACELLE NOISE FUNCTION
    // -----------------------------------------------------------------------------

    // NOTE: Phacelle Noise depends on the 'hash' function defined in the Common tab.

    private const float TAU = 6.28318530717959f;

    // The Simple Phacelle Noise function produces a stripe pattern aligned with the input vector.
    // The name Phacelle is a portmanteau of phase and cell, since the function produces a phase by
    // interpolating cosine and sine waves from multiple cells.
    //  - p is the input point being evaluated.
    //  - normDir is the direction of the stripes at this point. It must be a normalized vector.
    //  - freq is the freqency of the stripes within each cell. It's best to keep it close to 1.0f, as
    //    high values will produce distortions and other artifacts.
    //  - offset is the phase offset of the stripes, where 1.0f is a full cycle.
    //  - normalization is the degree of normalization applied, between 0 and 1. With e.g. a value of
    //    0.4f, raw output with a magnitude below 0.6f won't get fully normalized to a magnitude of 1.0f.
    // Phacelle Noise function copyright (c) 2025 Rune Skovbo Johansen
    // This Source Code Form is subject to the terms of the Mozilla Public
    // License, v. 2.0f. If a copy of the MPL was not distributed with this
    // file, You can obtain one at https://mozilla.org/MPL/2.0f/.
    private static Vector4 PhacelleNoise(in Vector2 p, Vector2 normDir, float freq, float offset, float normalization)
    {
        // Get a vector orthogonal to the input direction, with a
        // magnitude proportional to the frequency of the stripes.
        Vector2 sideDir = normDir.YX() * new Vector2(-1.0f, 1.0f) * freq * TAU;
        offset *= TAU;

        // Iterate over 4x4 cells, calculating a stripe pattern for each and blending between them.
        // pInt is the integer part of the current coordinate p, pFrac is the remainder.
        //
        // o   o   o   o
        //
        // o   o   o   o
        //       p
        // o   i   o   o
        //
        // o   o   o   o
        //
        // p: current coordinate    i: integer part of p    o: grid points for 4x4 cells
        //
        Vector2 pInt = Floor(p);
        Vector2 pFrac = p - pInt;
        Vector2 phaseDir = new Vector2(0.0f);
        float weightSum = 0.0f;
        for (int i = -1; i <= 2; i++) {
            for (int j = -1; j <= 2; j++) {
                Vector2 gridOffset = new Vector2(i, j);

                // Calculate a cell point by starting off with a point in the integer grid.
                Vector2 gridPoint = pInt + gridOffset;

                // Calculate a random offset for the cell point between -0.5f and 0.5f on each axis.
                Vector2 randomOffset = hash(gridPoint) * 0.5f;

                // The final cell point (we don't store it) is the gridPoint plus the randomOffset.
                // Calculate a vector representing the input point relative to this cell point:
                // p - (gridPoint + randomOffset)
                // = (pFrac + pInt) - ((pInt + gridOffset) + randomOffset)
                // = pFrac + pInt - pInt - gridOffset - randomOffset
                // = pFrac - gridOffset - randomOffset
                Vector2 vectorFromCellPoint = pFrac - gridOffset - randomOffset;

                // Bell-shaped weight function which is 1 at dist 0 and nearly 0 at dist 1.5f.
                // Due to the random offsets of up to 0.5f, the closest a cell point not in the 4x4
                // grid can be to the current point p is 1.5f units away.
                float sqrDist = Dot(vectorFromCellPoint, vectorFromCellPoint);
                float weight = Exp(-sqrDist * 2.0f);
                // Subtract 0.01111 to make the function actually 0 at distance 1.5f, which avoids
                // some (very subtle) grid line artefacts.
                weight = Max(0.0f, weight - 0.01111f);

                // Keep track of the total sum of weights.
                weightSum += weight;

                // The waveInput is a gradient which increases in value along sideDir. Its rate of
                // change is the freq times tau, due to the multiplier pre-applied to sideDir.
                float waveInput = Dot(vectorFromCellPoint, sideDir) + offset;

                // Add this cell's cosine and sine wave contributions to the interpolated value.
                phaseDir += new Vector2(Cos(waveInput), Sin(waveInput)) * weight;
            }
        }

        // Get the raw interpolated value.
        Vector2 interpolated = phaseDir / weightSum;
        // Interpret the value as a vector whose Length represents the magnitude of both waves.
        float magnitude = Sqrt(Dot(interpolated, interpolated));
        // Apply a lower threshold to show small magnitudes we're going to fully normalize.
        magnitude = Max(1.0f - normalization, magnitude);
        // Return a vector containing the normalized cosine and sine waves, as well as the direction
        // vector, which can be multiplied onto the sine to get the derivatives of the cosine.
        var xy = interpolated / magnitude;
        var zw = sideDir;
        return new Vector4(xy.X, xy.Y, zw.X, zw.Y);
    }


    // -----------------------------------------------------------------------------
    // EROSION FUNCTION
    // -----------------------------------------------------------------------------

    // First a few utility functions.

    private static float pow_inv(float t, float power)
    {
        // Flip, raise to the specified power, and flip back.
        return 1.0f - Pow(1.0f - Clamp01(t), power);
    }

    private static float ease_out(float t)
    {
        // Flip by subtracting from one.
        float v = 1.0f - Clamp01(t);
        // Raise to a power of two and flip back.
        return 1.0f - v * v;
    }

    private static float smooth_start(float t, float smoothing)
    {
        if (t >= smoothing)
            return t - 0.5f * smoothing;
        return 0.5f * t * t / smoothing;
    }

    private static Vector2 safe_normalize(Vector2 n)
    {
 	    // A div-by-zero-safe replacement for normalize.
        float l = Length(n);
	    return (Abs(l) > 1e-10) ? (n / l) : n;
    }

    // Advanced Terrain Erosion Filter copyright (c) 2025 Rune Skovbo Johansen
    // This Source Code Form is subject to the terms of the Mozilla Public
    // License, v. 2.0f. If a copy of the MPL was not distributed with this
    // file, You can obtain one at https://mozilla.org/MPL/2.0f/.
    private static Vector4 ErosionFilter(
        // Input parameters that vary per pixel.
        in Vector2 p, Vector3 heightAndSlope, float fadeTarget,
        // Stylistic parameters that may vary per pixel.
        float strength, float gullyWeight, float detail, Vector4 rounding, Vector4 onset, Vector2 assumedSlope,
        // Scale related parameters that do not support variation per pixel.
        float scale, int octaves, float lacunarity,
        // Other parameters.
        float gain, float cellScale, float normalization,
        // Output parameters.
        out float ridgeMap, out float debug
    )
    {
        strength *= scale;
        fadeTarget = Clamp(fadeTarget, -1.0f, 1.0f);

        Vector3 inputHeightAndSlope = heightAndSlope;
        float freq = 1.0f / (scale * cellScale);
        float slopeLength = Max(Length(heightAndSlope.YZ()), 1e-10f);
        float magnitude = 0.0f;
        float roundingMult = 1.0f;

        float roundingForInput = Lerp(rounding.Y, rounding.X, Clamp01(fadeTarget + 0.5f)) * rounding.Z;
        // The combined accumulating mask, based first on initial slope, and later on slope of each octave too.
        float combiMask = ease_out(smooth_start(slopeLength * onset.X, roundingForInput * onset.X));

        // Initialize the ridgeMap fadeTarget and mask.
        float ridgeMapCombiMask = ease_out(slopeLength * onset.Z);
        float ridgeMapFadeTarget = fadeTarget;

        // Deteriming the strength of the initial slope used for gully directions
        // based on the specified Lerp of the actual slope and an assumed slope.
        Vector2 gullySlope = Lerp(heightAndSlope.YZ(), heightAndSlope.YZ() / slopeLength * assumedSlope.X, assumedSlope.Y);

        for (int i = 0; i < octaves; i++) {
            // Calculate and add gullies to the height and slope.
            Vector4 phacelle = PhacelleNoise(p * freq, safe_normalize(gullySlope), cellScale, 0.25f, normalization);
            Vector2 phacelleZW = new Vector2(phacelle.Z, phacelle.W);
            // Multiply with freq since p was multiplied with freq.
            // Negate since we use slope directions that point down.
            phacelleZW *= -freq;
            // Amount of slope as value from 0 to 1.
            float sloping = Abs(phacelle.Y);

            // Add non-masked, normalized slope to gullySlope, for use by subsequent octaves.
            // It's normalized to use the steepest part of the sine wave everywhere.
            gullySlope += Sign(phacelle.Y) * phacelleZW * strength * gullyWeight;

            // Handle height offset and approximate output slope.

            // Gullies has height offset (from -1 to 1) in x and derivative in yz.
            Vector3 gullies = new Vector3(phacelle.X, phacelle.Y * phacelleZW.X, phacelle.Y * phacelleZW.Y);
            // Fade gullies towards fadeTarget based on combiMask.
            Vector3 fadedGullies = Lerp(new Vector3(fadeTarget, 0.0f, 0.0f), gullies * gullyWeight, combiMask);
            // Apply height offset and derivative (slope) according to strength of current octave.
            heightAndSlope += fadedGullies * strength;
            magnitude += strength;

            // Update fadeTarget to include the new octave.
            fadeTarget = fadedGullies.X;

            // Update the mask to include the new octave.
            float roundingForOctave = Lerp(rounding.Y, rounding.X, Clamp01(phacelle.X + 0.5f)) * roundingMult;
            float newMask = ease_out(smooth_start(sloping * onset.Y, roundingForOctave * onset.Y));
            combiMask = pow_inv(combiMask, detail) * newMask;

            // Update the ridgeMap fadeTarget and mask.
            ridgeMapFadeTarget = Lerp(ridgeMapFadeTarget, gullies.X, ridgeMapCombiMask);
            float newRidgeMapMask = ease_out(sloping * onset.W);
            ridgeMapCombiMask = ridgeMapCombiMask * newRidgeMapMask;

            // Prepare the next octave.
            strength *= gain;
            freq *= lacunarity;
            roundingMult *= rounding.W;
        }

        ridgeMap = ridgeMapFadeTarget * (1.0f - ridgeMapCombiMask);
        debug = fadeTarget;

        Vector3 heightAndSlopeDelta = heightAndSlope - inputHeightAndSlope;
        return new Vector4(heightAndSlopeDelta, magnitude);
    }


    // -----------------------------------------------------------------------------
    // DEMONSTRATION
    // -----------------------------------------------------------------------------

    // Used for the height map.
    public static Vector3 FractalNoise(Vector2 p, float freq, int octaves, float lacunarity, float gain)
    {
        Vector3 n = new Vector3(0.0f);
        float nf = freq;
        float na = 1.0f;
        for (int i = 0; i < octaves; i++) {
            n += noised(p * nf) * na * new Vector3(1.0f, nf, nf);
            na *= gain;
            nf *= lacunarity;
        }
        return n;
    }

    // Used for tree coverage on the height map.
    private static float GetTreesAmount(float height, float normalY, float occlusion, float ridgeMap, Settings settings)
    {
        return ((
            SmoothStep(
                settings.GrassHeight + 0.05f,
                settings.GrassHeight + 0.01f,
                height + 0.01f + (occlusion - 0.8f) * 0.05f
            )
            * SmoothStep(
                0.0f,
                0.4f,
                occlusion
            )
            * SmoothStep(0.95f, 1.0f, normalY)
            * SmoothStep(-1.4f, 0.0f, ridgeMap)
            * (settings.Water ?
                SmoothStep(
                    settings.WaterHeight + 0.000f,
                    settings.WaterHeight + 0.007f,
                    height
                )
                :
                1
            )
        ) - 0.5f) / 0.6f;
    }

    public static Sample Heightmap(Vector2 p, Settings settings, SampleHeight heightSampler)
    {
        // ------------------------------------------------------------------------
        // Erosion parameters.
        // ------------------------------------------------------------------------

        // The scale of the erosion effect, affecting it both horizontally and vertically.
        float EROSION_SCALE = settings.Erosion.Scale;
        //AnimateLoHi(EROSION_SCALE, 0.08, 0.25, time - 7.0f);

        // The strength of the erosion effect, affecting the magnitude of all octaves,
        // and indirectly affecting the directions of the gullies as a result.
        float EROSION_STRENGTH = settings.Erosion.Strength;
        //AnimateLoHi(EROSION_STRENGTH, 0.01, 0.10, time - 1.0f);

        // The magnitude of the gullies as a weight value from 0 to 1.
        // A value of 0 can sharpen peaks and valleys but feature virtually no gullies.
        // A value of 1 produces full gullies but may leave peaks and valleys rounded.
        // Adjusting erosion gully weight while inversely adjusting erosion scale can be
        // used to control the sharpness of peaks and valleys while leaving gully
        // magnitudes largely untocuhed.
        float EROSION_GULLY_WEIGHT = settings.Erosion.GullyWeight;

        // The overall detail of the erosion. Lower values restrict the effect of higher
        // frequency gullies to steeper slopes.
        float EROSION_DETAIL = settings.Erosion.Detail;
        //AnimateLoHi(EROSION_DETAIL, 3.0f, 0.7f, time - 13.0f);

        /*float ridgeRounding = 0.1f;
        float creaseRounding = 0.0f;
        AnimateWaveTo(creaseRounding, 1.0f, time - 19.0f);
        AnimateWaveTo(ridgeRounding, 1.0f, time - 21.0f);
        AnimateWaveTo(creaseRounding, 0.0f, time - 23.0f);
        AnimateWaveTo(ridgeRounding, 0.1f, time - 25.0f);*/
        // Separate rounding control of ridges and creases.
        //  x: Rounding of ridges.
        //  y: Rounding of creases.
        //  z: Multiplier applied to the initial height function.
        //     E.g. if the height function has noise of 5 times lower frequency
        //     than the largest gullies, a value of 0.2f can compensate for that.
        //  w: Multiplier applied to each subsequent gully octave after the first.
        //     Setting it to the same value as the erosion lacunarity will produce
        //     consistent rounding of all octaves.
        Vector4 EROSION_ROUNDING = settings.Erosion.Rounding;

        // Control over how far away from ridges/creases the erosion takes effect.
        //  x: Onset used on the initial height function.
        //  y: Onset used on each gully octave.
        //  z: RidgeMap-specific onset used on the initial height function.
        //  w: RidgeMap-specific onset used on each gully octave.
        Vector4 EROSION_ONSET = settings.Erosion.Onset;

        // Control over the assumed slope of the initial height function.
        // In practise, assuming a slope can work better than using the input slope,
        // since the final terrain can be shaped quite differently than the input.
        //  x: An assumed slope value to override the actual slope.
        //  y: The amount (from 0 to 1) to override the actual slope.
        Vector2 EROSION_ASSUMED_SLOPE = settings.Erosion.AssumedSlope;

        // Gullies are based on stripes within Voronoi-like cells in the Phacelle noise
        // function. The cell scale parameter controls the sizes of the cells relative
        // to the overall erosion scale, while keeping the stripe widths unaffected.
        // Values close to 1 usually produce good results. Smaller values produce more
        // grainy gullies while larger values produce longer unbroken gullies, but too
        // large values produce chaotic curved gullies that are not aligned with the
        // slopes. Value changes can cause abrupt changes in output, especially far away
        // from the origin, so this parameter is not well suited for animation or for
        // modulation by other functions.
        float EROSION_CELL_SCALE = settings.Erosion.CellScale;
        // The degree of normalization applied in the Phacelle noise, between 0 and 1.
        // The erosion filter depends on a certain consistency in magnitude of the
        // Phacelle output. However, high values can create loopy results where ridges
        // and creases meet up at a point, which produces unnatural looking results.
        float EROSION_NORMALIZATION = settings.Erosion.Normalization;

        // Control over the erosion octaves, with each successive octave layering
        // smaller gullies onto the terrain.
        int EROSION_OCTAVES = settings.Erosion.Octaves;
        // The lacunarity controls the frequency (the inverse
        // horizontal scale) of each octave relative to the last.
        float EROSION_LACUNARITY = settings.Erosion.Lacunarity;
        // The gain controls the magnitude (the vertical
        // scale) of each octave relative to the last.
        float EROSION_GAIN = settings.Erosion.Gain;


        // ------------------------------------------------------------------------
        // Terrain parameters not used in the erosion function itself.
        // ------------------------------------------------------------------------

        // Control over whether the erosion effect raises or lowers the terrain.
        //  x: An offset value between -1 and 1, where a value of -1 only lowers, while
        //     1 only raises. The offset is proportional to the erosion strength
        //     parameter, so if that parameter is the same for the entire terrain, the
        //     effect of the height offset will move the entire terrain surface up or
        //     down by the same emount.
        //  y: A value between 0 and 1 which is the degree to which the offset value is
        //     replaced by the negated erosion fade target value. This has the effect
        //     of only raising at valleys and only lowering at peaks, which, due to how
        //     the erosion filter works, has the effect of largely preserving the minima
        //     and maxima of the terrain.
        Vector2 TERRAIN_HEIGHT_OFFSET = settings.TerrainHeightOffset;


        // ------------------------------------------------------------------------
        // Logic for whether erosion is enabled or not.
        // ------------------------------------------------------------------------

        bool erosion = true;

        // Toggle erosion with Enter key toggle.
        /*if (texelFetch(iChannel1, iVector2(13, 2), 0).X > 0.0f)
            erosion = false;*/

        /*#ifdef COMPARISON_SLIDER
            // Animated slider that displays terrain with/without erosion.
            if (1.0f - p.Y > 0.5f - Cos(iTime))
                erosion = false;
        #endif*/


        // ------------------------------------------------------------------------
        // Heightmap implementation.
        // ------------------------------------------------------------------------

        heightSampler(p, out float height, out Vector2 normal, out float fadeTarget);
        var n = new Vector3(height, normal.X, normal.Y);

        // Store erosion in h (x : height delta, yz : slope delta, w : magnitude).
        // The output ridge map is -1 on creases and 1 on ridges.
        // The output debug value can be set to various values inside the erosion function.
        float ridgeMap, debug;
        Vector4 h = ErosionFilter(
            p, n, fadeTarget,
            EROSION_STRENGTH, EROSION_GULLY_WEIGHT, EROSION_DETAIL,
            EROSION_ROUNDING, EROSION_ONSET, EROSION_ASSUMED_SLOPE,
            EROSION_SCALE, EROSION_OCTAVES, EROSION_LACUNARITY,
            EROSION_GAIN, EROSION_CELL_SCALE, EROSION_NORMALIZATION,
            out ridgeMap, out debug);

        if (!erosion) {
            h = new Vector4(0.0f);
            ridgeMap = 1.0f;
        }

        // Offset according to the height offset parameter by multiplying it with the magnitude.
        float offset = Lerp(TERRAIN_HEIGHT_OFFSET.X, -fadeTarget, TERRAIN_HEIGHT_OFFSET.Y) * h.W;
        float eroded = n.X + h.X + offset;

        // Add trees to terrain.
        float trees = -1.0f;
        if (settings.Trees)
        {
            Vector2 deriv = n.YZ() + new Vector2(h.Y, h.Z);
            float normalY = 1.0f / Sqrt(1.0f + Dot(deriv, deriv));
            float treesAmount = GetTreesAmount(eroded, normalY, h.X / h.W + 0.5f, ridgeMap, settings);
            trees = (1.0f - Pow(noised((p + new Vector2(0.5f)) * 200.0f).X * 0.5f + 0.5f, 2.0f) - 1.0f + 1.0f * treesAmount) * 1.5f;
            if (trees > 0.0f)
            {
                eroded += trees / 300.0f;
            }
        }

        Sample sample;
        sample.Height = eroded;
        sample.Erosion = Clamp01(h.X / h.W * 0.5f + 0.5f);
        sample.Ridge = Clamp01(ridgeMap * 0.5f + 0.5f);
        sample.Tree = Clamp01(trees * 0.5f + 0.5f);
        sample.Debug = Clamp01(debug * 0.5f + 0.5f);
        return sample;
    }

    /// <summary>
    /// Returns the height and normals at a given point
    /// </summary>
    /// <param name="normalizedPoint">The coordinate to sample at</param>
    /// <param name="height">The height of this sample at <see cref="normalizedPoint"/> coordinate</param>
    /// <param name="normal">The normal of the sample at <see cref="normalizedPoint"/> coordinate</param>
    /// <param name="fadeTarget">The erosion mask in [-1,+1] range</param>
    public delegate void SampleHeight(Vector2 normalizedPoint, out float height, out Vector2 normal, out float fadeTarget);
}

public static partial class RunevisionTerrainErosion
{
    [DataContract]
    public record Settings
    {
        /// <summary>
        /// Control over whether the erosion effect raises or lowers the terrain.
        ///  x: An offset value between -1 and 1, where a value of -1 only lowers, while
        ///     1 only raises. The offset is proportional to the erosion strength
        ///     parameter, so if that parameter is the same for the entire terrain, the
        ///     effect of the height offset will move the entire terrain surface up or
        ///     down by the same emount.
        ///  y: A value between 0 and 1 which is the degree to which the offset value is
        ///     replaced by the negated erosion fade target value. This has the effect
        ///     of only raising at valleys and only lowering at peaks, which, due to how
        ///     the erosion filter works, has the effect of largely preserving the minima
        ///     and maxima of the terrain.
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
            ///  x: Rounding of ridges.
            ///  y: Rounding of creases.
            ///  z: Multiplier applied to the initial height function.
            ///     E.g. if the height function has noise of 5 times lower frequency
            ///     than the largest gullies, a value of 0.2f can compensate for that.
            ///  w: Multiplier applied to each subsequent gully octave after the first.
            ///     Setting it to the same value as the erosion lacunarity will produce
            ///     consistent rounding of all octaves.
            /// </summary>
            public Vector4 Rounding = new Vector4(0.1f, 0.0f, 0.1f, 2.0f);

            /// <summary>
            /// Control over how far away from ridges/creases the erosion takes effect.
            ///  x: Onset used on the initial height function.
            ///  y: Onset used on each gully octave.
            ///  z: RidgeMap-specific onset used on the initial height function.
            ///  w: RidgeMap-specific onset used on each gully octave.
            /// </summary>
            public Vector4 Onset = new Vector4(1.25f, 1.25f, 2.8f, 1.5f);

            /// <summary>
            /// Control over the assumed slope of the initial height function.
            /// In practise, assuming a slope can work better than using the input slope,
            /// since the final terrain can be shaped quite differently than the input.
            ///  x: An assumed slope value to override the actual slope.
            ///  y: The amount (from 0 to 1) to override the actual slope.
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
        return value - Floor(value);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
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
        Vector2 i = Floor(p);
        Vector2 f = p - i;

        Vector2 u = f * f * f * (f * (f * new Vector2(6.0f) - new Vector2(15.0f)) + new Vector2(10.0f));
        Vector2 du = 30.0f * f * f * (f * (f - new Vector2(2.0f)) + new Vector2(1.0f));

        Vector2 ga = hash(i + new Vector2(0.0f, 0.0f));
        Vector2 gb = hash(i + new Vector2(1.0f, 0.0f));
        Vector2 gc = hash(i + new Vector2(0.0f, 1.0f));
        Vector2 gd = hash(i + new Vector2(1.0f, 1.0f));

        float va = Dot(ga, f - new Vector2(0.0f, 0.0f));
        float vb = Dot(gb, f - new Vector2(1.0f, 0.0f));
        float vc = Dot(gc, f - new Vector2(0.0f, 1.0f));
        float vd = Dot(gd, f - new Vector2(1.0f, 1.0f));

        var x = va + u.X * (vb - va) + u.Y * (vc - va) + u.X * u.Y * (va - vb - vc + vd);
        var yz = ga + u.X * (gb - ga) + u.Y * (gc - ga) + u.X * u.Y * (ga - gb - gc + gd) +
                 du * (u.YX() * (va - vb - vc + vd) + new Vector2(vb, vc) - new Vector2(va));

        return new Vector3(x, yz.X, yz.Y);
    }
}
