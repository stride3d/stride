// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Materials
{
    public static partial class MaterialSurfaceSubsurfaceScatteringShadingKeys
    {
        public static readonly ObjectParameterKey<Vector4[]> ScatteringKernel = (ObjectParameterKey<Vector4[]>)ParameterKeys.Merge(ParameterKeys.NewObject<Vector4[]>(), typeof(MaterialSurfaceSubsurfaceScatteringShadingKeys), "MaterialSurfaceSubsurfaceScatteringShading.ScatteringKernel");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
