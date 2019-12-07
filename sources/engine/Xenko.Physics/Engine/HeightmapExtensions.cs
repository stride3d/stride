// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Graphics;

namespace Xenko.Physics
{
    public static class HeightmapExtensions
    {
        public static bool IsValidSize([NotNull] this Heightmap heightmap)
        {
            if (heightmap == null) throw new ArgumentNullException(nameof(heightmap));

            return heightmap.Size.X >= 2 && heightmap.Size.Y >= 2;
        }

        public static Texture CreateTexture([NotNull] this Heightmap heightmap, GraphicsDevice device)
        {
            if (heightmap == null) throw new ArgumentNullException(nameof(heightmap));

            if (device == null || !heightmap.IsValidSize())
            {
                return null;
            }

            switch (heightmap.HeightType)
            {
                case HeightfieldTypes.Float:
                    return Texture.New2D(device, heightmap.Size.X, heightmap.Size.Y, PixelFormat.R32_Float, heightmap.Floats);

                case HeightfieldTypes.Short:
                    return Texture.New2D(device, heightmap.Size.X, heightmap.Size.Y, PixelFormat.R16_SNorm, heightmap.Shorts);

                case HeightfieldTypes.Byte:
                    return Texture.New2D(device, heightmap.Size.X, heightmap.Size.Y, PixelFormat.R8_UNorm, heightmap.Bytes);

                default:
                    return null;
            }
        }
    }
}
