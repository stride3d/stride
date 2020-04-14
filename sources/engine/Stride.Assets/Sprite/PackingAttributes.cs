// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Assets.Textures.Packing;

namespace Stride.Assets.Sprite
{
    /// <summary>
    /// Specify how the sprite should be packed into atlas
    /// </summary>
    [DataContract]
    public class PackingAttributes
    {
        /// <summary>
        /// Creates a default instance of packing attributes.
        /// </summary>
        public PackingAttributes()
        {
            Enabled = true;
            PackingAlgorithm = TexturePackingMethod.Best;

            AllowMultipacking = true;
            AllowRotations = true;

            BorderSize = 2;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to generate texture atlas
        /// </summary>
        /// <userdoc>Pack the sprites into atlas textures</userdoc>
        [DataMember(0)]
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets MaxRects rectangles placement algorithm
        /// </summary>
        /// <userdoc>The algorithm used to pack the sprites into atlas</userdoc>
        [DataMemberIgnore] // hide this for the moment.
        [DefaultValue(TexturePackingMethod.Best)]
        public TexturePackingMethod PackingAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the use of Multipack atlas mode which allows more than one texture atlas to fit all given textures
        /// </summary>
        /// <userdoc>If the sprites don't fit into a single atlas, generate several atlas textures</userdoc>
        [DataMember(20)]
        [DefaultValue(true)]
        public bool AllowMultipacking { get; set; }

        /// <summary>
        /// Gets or sets whether or not to use Rotation for images inside atlas texture
        /// </summary>
        /// <userdoc>Allow the packer to rotate the sprites by 90 degrees if it can reduce the size of the atlas</userdoc>
        [DataMember(30)]
        [DefaultValue(true)]
        public bool AllowRotations { get; set; }

        /// <summary>
        /// Gets or sets the size of the border for sprites inside atlas texture
        /// </summary>
        /// <userdoc>The size in pixels of the border around the sprites. 
        /// Note that having a space between sprites (a border) is important to prevent various atlassing side effects from occurring.</userdoc>
        [DataMember(50)]
        [DefaultValue(2)]
        public int BorderSize { get; set; }
    }
}
