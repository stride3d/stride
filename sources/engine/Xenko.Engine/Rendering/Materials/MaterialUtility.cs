// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Class MaterialUtility.
    /// </summary>
    internal static class MaterialUtility
    {
        public const string BackgroundCompositionName = "color1";

        public const string ForegroundCompositionName = "color2";

        public static int GetTextureIndex(TextureCoordinate texcoord)
        {
            switch (texcoord)
            {
                case TextureCoordinate.Texcoord0:
                    return 0;
                case TextureCoordinate.Texcoord1:
                    return 1;
                case TextureCoordinate.Texcoord2:
                    return 2;
                case TextureCoordinate.Texcoord3:
                    return 3;
                case TextureCoordinate.Texcoord4:
                    return 4;
                case TextureCoordinate.Texcoord5:
                    return 5;
                case TextureCoordinate.Texcoord6:
                    return 6;
                case TextureCoordinate.Texcoord7:
                    return 7;
                case TextureCoordinate.Texcoord8:
                    return 8;
                case TextureCoordinate.Texcoord9:
                    return 9;
                case TextureCoordinate.TexcoordNone:
                default:
                    throw new ArgumentOutOfRangeException("texcoord");
            }
        }

        public static string GetAsShaderString(ColorChannel channel)
        {
            return channel.ToString().ToLowerInvariant();
        }

        public static string GetAsShaderString(Vector2 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float2({0}, {1})", v.X, v.Y);
        }

        public static string GetAsShaderString(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float3({0}, {1}, {2})", v.X, v.Y, v.Z);
        }

        public static string GetAsShaderString(Vector4 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", v.X, v.Y, v.Z, v.W);
        }

        public static string GetAsShaderString(Color4 c)
        {
            return string.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", c.R, c.G, c.B, c.A);
        }

        public static string GetAsShaderString(float f)
        {
            return string.Format(CultureInfo.InvariantCulture, "float4({0}, {0}, {0}, {0})", f);
        }

        public static string GetAsShaderString(object obj)
        {
            return obj.ToString();
        }

        /// <summary>
        /// Build a encapsulating ShaderMixinSource if necessary.
        /// </summary>
        /// <param name="shaderSource">The input ShaderSource.</param>
        /// <returns>A ShaderMixinSource</returns>
        public static ShaderMixinSource GetShaderMixinSource(ShaderSource shaderSource)
        {
            if (shaderSource is ShaderClassSource)
            {
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add((ShaderClassSource)shaderSource);
                return mixin;
            }
            if (shaderSource is ShaderMixinSource)
                return (ShaderMixinSource)shaderSource;

            return null;
        }

        /// <summary>
        /// Get the ParameterKey of generic sampler.
        /// </summary>
        /// <param name="i">The id of the texture.</param>
        /// <returns>The corresponding ParameterKey.</returns>
        public static ObjectParameterKey<SamplerState> GetDefaultSamplerKey(int i)
        {
            switch (i)
            {
                case 0:
                    return TexturingKeys.Sampler0;
                case 1:
                    return TexturingKeys.Sampler1;
                case 2:
                    return TexturingKeys.Sampler2;
                case 3:
                    return TexturingKeys.Sampler3;
                case 4:
                    return TexturingKeys.Sampler4;
                case 5:
                    return TexturingKeys.Sampler5;
                case 6:
                    return TexturingKeys.Sampler6;
                case 7:
                    return TexturingKeys.Sampler7;
                case 8:
                    return TexturingKeys.Sampler8;
                case 9:
                    return TexturingKeys.Sampler9;
                default:
                    throw new ArgumentOutOfRangeException("Asked for " + i + " but no more than 10 default textures are currently supported");
            }
        }

        /// <summary>
        /// Clamps <see cref="ComputeColors.ComputeFloat"/> value within a specified range [min; max].
        /// </summary>
        /// <param name="key">Input scalar.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public static void ClampFloat([NotNull] this IComputeScalar key, float min, float max)
        {
            var asFloat = key as ComputeColors.ComputeFloat;
            if (asFloat != null)
                asFloat.Value = MathUtil.Clamp(asFloat.Value, min, max);
        }

        /// <summary>
        /// Clamps <see cref="ComputeColors.ComputeFloat4"/> value within a specified range [min; max].
        /// </summary>
        /// <param name="key">Input scalar.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public static void ClampFloat4([NotNull] this IComputeColor key, ref Vector4 min, ref Vector4 max)
        {
            var asFloat4 = key as ComputeColors.ComputeFloat4;
            if (asFloat4 != null)
                asFloat4.Value = Vector4.Clamp(asFloat4.Value, min, max);
        }
    }
}
