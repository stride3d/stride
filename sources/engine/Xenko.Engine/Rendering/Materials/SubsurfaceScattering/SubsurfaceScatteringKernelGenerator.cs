// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// This class generates transmittance profiles and screen space scattering kernels for Separable Subsurface Scattering.
    /// </summary>
    public class SubsurfaceScatteringKernelGenerator
    {
        private static float GaussianComponent(float variance, float r, float falloff)
        {
            float rr = r / (0.001f + falloff);
            return (float)Math.Exp((-(rr * rr)) / (2.0f * variance)) / (2.0f * 3.14f * variance);
        }

        private static Vector3 Gaussian(float variance, float r, Color3 falloff) // Based on "SeparableSSS::gaussian()".
        {
            // We use a falloff to modulate the shape of the profile. Big falloffs
            // spreads the shape making it wider, while small falloffs make it
            // narrower.
            return new Vector3(GaussianComponent(variance, r, falloff.R),
                               GaussianComponent(variance, r, falloff.G),
                               GaussianComponent(variance, r, falloff.B));
        }

        private static Vector3 Profile(float r, Color3 falloff) // Based on "SeparableSSS::profile()".
        {
            // We used the red channel of the original skin profile defined in
            // [d'Eon07] for all three channels. We noticed it can be used for green
            // and blue channels (scaled using the falloff parameter) without
            // introducing noticeable differences and allowing for total control over
            // the profile. For example, it allows to create blue SSS gradients, which
            // could be useful in case of rendering blue creatures.
            return // 0.233f * Gaussian(0.0064f, r, falloff) + // We consider this one to be directly bounced light, accounted by the strength parameter (see @STRENGTH)
                   0.100f * Gaussian(0.0484f, r, falloff) +
                   0.118f * Gaussian(0.187f, r, falloff) +
                   0.113f * Gaussian(0.567f, r, falloff) +
                   0.358f * Gaussian(1.99f, r, falloff) +
                   0.078f * Gaussian(7.41f, r, falloff);
        }

        private static Vector4 GaussianTransmittanceProfile(float weight, float variance, Vector3 falloff)
        {
            // Whole formula: exp(-(r * r) / adjustedFalloffSquaredTwoVariance) / twoPiVariance * weight;

            Vector3 adjustedFalloff = new Vector3(0.001f) + falloff;
            Vector3 adjustedFalloffSquared = adjustedFalloff * adjustedFalloff;

            float twoVariance = 2.0f * variance;
            float twoPiVariance = 2.0f * 3.14f * variance;

            Vector3 adjustedFalloffSquaredTwoVariance = adjustedFalloffSquared * twoVariance;

            Vector3 reciprocalAdjustedFalloffSquaredTwoVariance = 1.0f / adjustedFalloffSquaredTwoVariance;
            float reciprocalTwoPiVarianceWeight = 1.0f / twoPiVariance * weight;

            // Used in the shader like this:
            // Used like "profile[i].w * exp(-(d * d) * profile[i].xyz)".

            return new Vector4(reciprocalAdjustedFalloffSquaredTwoVariance, // inner factor
                               reciprocalTwoPiVarianceWeight); // outer factor
        }

        public static Vector4[] CalculateTransmittanceProfile(Vector3 falloff) // Based on "SeparableSSS::profile()".
        {
            // We used the red channel of the original skin profile defined in
            // [d'Eon07] for all three channels. We noticed it can be used for green
            // and blue channels (scaled using the falloff parameter) without
            // introducing noticeable differences and allowing for total control over
            // the profile. For example, it allows to create blue SSS gradients, which
            // could be useful in case of rendering blue creatures.

            // Used like "profile[i].w * exp(-(d * d) * profile[i].xyz)".

            Vector4[] profile = new Vector4[6];
            profile[0] = GaussianTransmittanceProfile(0.233f, 0.0064f, falloff); // We consider this one to be directly bounced light, accounted by the strength parameter (see @STRENGTH)
            profile[1] = GaussianTransmittanceProfile(0.100f, 0.0484f, falloff);
            profile[2] = GaussianTransmittanceProfile(0.118f, 0.187f, falloff);
            profile[3] = GaussianTransmittanceProfile(0.113f, 0.567f, falloff);
            profile[4] = GaussianTransmittanceProfile(0.358f, 1.99f, falloff);
            profile[5] = GaussianTransmittanceProfile(0.078f, 7.41f, falloff);

            return (profile);
        }

        public static Vector4[] CalculateScatteringKernel(int sampleCount, Color3 strength, Color3 falloff) // Based on "SeparableSSS::calculateKernel()".
        {
            // sampleCount: number of samples of the kernel convolution.

            // @STRENGTH
            // strength: This parameter specifies the how much of the diffuse light gets into
            //           the skin, and thus gets modified by the SSS mechanism.
            //
            //           It can be seen as a per-channel mix factor between the original
            //           image, and the SSS-filtered image.

            float range = sampleCount > 20 ? 3.0f : 2.0f;
            const float exponent = 2.0f;

            Vector4[] kernel = new Vector4[sampleCount];

            // Calculate the offsets:
            float step = 2.0f * range / (sampleCount - 1);
            for (int i = 0; i < sampleCount; i++)
            {
                float o = -range + i * step;
                float sign = o < 0.0f ? -1.0f : 1.0f;
                kernel[i].W = range * sign * (float)(Math.Abs(Math.Pow(o, exponent)) / Math.Pow(range, exponent));
            }

            // Calculate the weights:
            for (int i = 0; i < sampleCount; i++)
            {
                float w0 = i > 0 ? Math.Abs(kernel[i].W - kernel[i - 1].W) : 0.0f;
                float w1 = i < sampleCount - 1 ? Math.Abs(kernel[i].W - kernel[i + 1].W) : 0.0f;
                float area = (w0 + w1) / 2.0f;
                Vector3 t1 = area * Profile(kernel[i].W, falloff);
                kernel[i].X = t1.X;
                kernel[i].Y = t1.Y;
                kernel[i].Z = t1.Z;
            }

            // We want the offset 0.0 to come first:
            Vector4 t2 = kernel[sampleCount / 2];
            for (int i = sampleCount / 2; i > 0; i--)
            {
                kernel[i] = kernel[i - 1];
            }
            kernel[0] = t2;

            // Calculate the sum of the weights, we will need to normalize them below:
            Vector3 sum = new Vector3(0.0f, 0.0f, 0.0f);
            for (int i = 0; i < sampleCount; i++)
            {
                sum.X += kernel[i].X;
                sum.Y += kernel[i].Y;
                sum.Z += kernel[i].Z;
            }

            // Normalize the weights:
            for (int i = 0; i < sampleCount; i++)
            {
                kernel[i].X /= sum.X;
                kernel[i].Y /= sum.Y;
                kernel[i].Z /= sum.Z;
            }

            // Tweak them using the desired strength. The first one is:
            //     lerp(1.0, kernel[0].rgb, strength)
            kernel[0].X = (1.0f - strength.R) * 1.0f + strength.R * kernel[0].X;
            kernel[0].Y = (1.0f - strength.G) * 1.0f + strength.G * kernel[0].Y;
            kernel[0].Z = (1.0f - strength.B) * 1.0f + strength.B * kernel[0].Z;

            // The others:
            //     lerp(0.0, kernel[0].rgb, strength)
            for (int i = 1; i < sampleCount; i++)
            {
                kernel[i].X *= strength.R;
                kernel[i].Y *= strength.G;
                kernel[i].Z *= strength.B;

                kernel[i].W /= 3.0f;   // Divide by three so we don't have to do it in the pixel shader.       // TODO: Must this depend on SCALE? The original implementation doesn't take it into account, although it seems logical.
            }

            return kernel;
        }
    }
}
