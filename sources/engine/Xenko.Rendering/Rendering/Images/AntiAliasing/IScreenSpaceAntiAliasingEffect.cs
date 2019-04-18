// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Common interface for a screen-space anti-aliasing effect
    /// </summary>
    public interface IScreenSpaceAntiAliasingEffect : IImageEffect
    {
        bool NeedRangeDecompress { get; }
        bool RequiresVelocityBuffer { get; }
    }
}
