// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Rendering
{
    /// <summary>
    /// Keys used by transformations.
    /// </summary>
    public static partial class CameraKeys
    {
        /// <summary>
        /// Camera focus distance
        /// </summary>
        public static readonly ValueParameterKey<float> FocusDistance = ParameterKeys.NewValue(0.0f);

        public static Vector2 ZProjectionACalculate(float nearClipPlane, float farClipPlane)
        {
            // Formuluas to retro project a non-linear zdepth in the range [0.0 - 1.0] to a linear-depth in view space.

            // (0) ZProjection: A + B / z = depth in the range [0.0-1.0]
            // (1) A + B / NearClipPlane = 0.0
            // (2) A + B / FarClipPlane = 1.0
            //
            // From (1): A = -B / NearClipPlane
            // From (2) and (1): B = (-FarClipPlane * NearClipPlane) / (FarClipPlane - NearClipPlane)
            //
            // From (0) z = B / (depth - A)
            return new Vector2(farClipPlane / (farClipPlane - nearClipPlane), (-farClipPlane * nearClipPlane) / (farClipPlane - nearClipPlane));
        }
    }
}
