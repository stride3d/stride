// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Graphics
{
    public partial struct TextureDescription
    {
        /// <summary>
        /// Creates a new <see cref="TextureDescription" /> with a single mipmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New3D(int width, int height, int depth, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New3D(width, height, depth, false, format, textureFlags, usage);
        }

        /// <summary>
        /// Creates a new <see cref="TextureDescription" />.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int &gt;=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New3D(int width, int height, int depth, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New3D(width, height, depth, format, textureFlags, mipCount, usage);
        }

        private static TextureDescription New3D(int width, int height, int depth, PixelFormat format, TextureFlags flags, int mipCount, GraphicsResourceUsage usage)
        {
            var desc = new TextureDescription()
            {
                Width = width,
                Height = height,
                Depth = depth,
                Flags = flags,
                Format = format,
                MipLevels = Texture.CalculateMipMapCount(mipCount, width, height, depth),
                Usage = Texture.GetUsageWithFlags(usage, flags),
                ArraySize = 1,
                Dimension = TextureDimension.Texture3D,
                MultisampleCount = MultisampleCount.None,
            };

            return desc;
        } 
    }
}
