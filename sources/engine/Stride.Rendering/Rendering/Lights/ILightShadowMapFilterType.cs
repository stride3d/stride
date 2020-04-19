// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Common interface of a shadowmap filter.
    /// </summary>
    public interface ILightShadowMapFilterType
    {
        bool RequiresCustomBuffer();
    }
}
