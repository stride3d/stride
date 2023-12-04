// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Class representing a texture in the new Assimp's material stack.
    /// </summary>
    public class StackTexture : StackElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackTexture"/> class.
        /// </summary>
        /// <param name="texturePath">The texture path.</param>
        /// <param name="channel">The uv channel used by the texture.</param>
        /// <param name="mappingModeU">The U mapping mode.</param>
        /// <param name="mappingModeV">The V mapping mode.</param>
        /// <param name="alpha">The alpha of the node.</param>
        /// <param name="blend">The blending coefficient of the node.</param>
        /// <param name="flags">The flags of the node.</param>
        public StackTexture(string texturePath, int channel, MappingMode mappingModeU, MappingMode mappingModeV, float alpha = 1.0f, float blend = 1.0F, int flags = 0)
            : base(alpha, blend, flags, StackElementType.Texture)
        {
            TexturePath = texturePath;
            Channel = channel;
            MappingModeU = mappingModeU;
            MappingModeV = mappingModeV;
        }
        /// <summary>
        /// Gets the texture path.
        /// </summary>
        /// <value>
        /// The texture path.
        /// </value>
        public string TexturePath { get; private set; }
        /// <summary>
        /// Gets the uv channel.
        /// </summary>
        /// <value>
        /// The uv channel.
        /// </value>
        public int Channel { get; private set; }
        /// <summary>
        /// Gets the U mapping mode.
        /// </summary>
        /// <value>
        /// The U mapping mode.
        /// </value>
        public MappingMode MappingModeU { get; private set; }
        /// <summary>
        /// Gets the Vmapping mode.
        /// </summary>
        /// <value>
        /// The V mapping mode.
        /// </value>
        public MappingMode MappingModeV { get; private set; }
    }
}
