// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Assets.Textures
{
    public interface ITextureType
    {
        bool IsSRgb(ColorSpace colorSpaceReference);

        bool ColorKeyEnabled { get; }

        Color ColorKeyColor { get; }

        AlphaFormat Alpha { get; }

        bool PremultiplyAlpha { get; }

        TextureHint Hint { get; }
    }
}
