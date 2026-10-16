// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering.Tessellation
{
    public class TessellationKeys
    {
        /// <summary>
        /// Desired maximum triangle size in screen space during tessellation.
        /// </summary>
        public static readonly ValueParameterKey<float> DesiredTriangleSize = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue(12.0f), typeof(TessellationKeys), "Tessellation.DesiredTriangleSize");

        /// <summary>
        /// The intensity of the smoothing for PN-based tessellation.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> SmoothingMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(TessellationKeys), "Tessellation.SmoothingMap");
        public static readonly ValueParameterKey<float> SmoothingValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(TessellationKeys), "Tessellation.SmoothingValue");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TessellationKeys).TypeHandle);
        }
    }
}
