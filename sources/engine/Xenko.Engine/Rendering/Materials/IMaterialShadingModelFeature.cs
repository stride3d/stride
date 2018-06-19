// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Base interface for a shading light dependent model material feature.
    /// </summary>
    public interface IMaterialShadingModelFeature : IMaterialFeature, IEquatable<IMaterialShadingModelFeature>
    {
    }
}
