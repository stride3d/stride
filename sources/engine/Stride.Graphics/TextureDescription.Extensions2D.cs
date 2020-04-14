// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    public partial struct TextureDescription
    {
        /// <summary>
        /// Creates a new <see cref="TextureDescription" /> with a single mipmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, TextureOptions textureOptions = TextureOptions.None)
        {
            return New2D(width, height, false, format, textureFlags, arraySize, usage, MultisampleCount.None, textureOptions);
        }

        /// <summary>
        /// Creates a new <see cref="TextureDescription" />.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int &gt;=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="multisampleCount">The multisample count.</param>
        /// <returns>A new instance of <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New2D(int width, int height, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, MultisampleCount multisampleCount = MultisampleCount.None, TextureOptions textureOptions = TextureOptions.None)
        {
            return New2D(width, height, format, textureFlags, mipCount, arraySize, usage, multisampleCount, textureOptions);
        }

        private static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags, int mipCount, int arraySize, GraphicsResourceUsage usage, MultisampleCount multisampleCount, TextureOptions textureOptions = TextureOptions.None)
        {
            if ((textureFlags & TextureFlags.UnorderedAccess) != 0)
                usage = GraphicsResourceUsage.Default;

            var desc = new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                Depth = 1,
                ArraySize = arraySize,
                MultisampleCount = multisampleCount,
                Flags = textureFlags,
                Format = format,
                MipLevels = Texture.CalculateMipMapCount(mipCount, width, height),
                Usage = Texture.GetUsageWithFlags(usage, textureFlags),
                Options = textureOptions
            };
            return desc;
        }
    }
}
