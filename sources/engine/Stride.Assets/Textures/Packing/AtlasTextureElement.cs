// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Globalization;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// This represent an element of the atlas texture.
    /// </summary>
    public class AtlasTextureElement
    {
        /// <summary>
        /// The name of the atlas element.
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets or sets CPU-resource texture
        /// </summary>
        public Image Texture;

        /// <summary>
        /// The region of the atlas element in its original texture.
        /// </summary>
        public RotableRectangle SourceRegion;

        /// <summary>
        /// The region of the atlas element in the output atlas texture (it includes the border size!).
        /// </summary>
        public RotableRectangle DestinationRegion;

        /// <summary>
        /// The size of the border around the atlas elements
        /// </summary>
        public int BorderSize;

        /// <summary>
        /// Gets or sets border modes in X axis which applies specific TextureAddressMode in the border of each texture element in a given size of border
        /// </summary>
        public TextureAddressMode BorderModeU;

        /// <summary>
        /// Gets or sets border modes in Y axis which applies specific TextureAddressMode in the border of each texture element in a given size of border
        /// </summary>
        public TextureAddressMode BorderModeV;

        /// <summary>
        /// Gets or sets Border color when BorderModeU is set to Border mode
        /// </summary>
        public Color BorderColor;

        /// <summary>
        /// Create an empty atlas texture element.
        /// </summary>
        public AtlasTextureElement() :
            this(null, null, new RotableRectangle(), 0, TextureAddressMode.Wrap, TextureAddressMode.Wrap)
        {
        }

        /// <summary>
        /// Create an atlas texture element that contains all the information from the source texture.
        /// </summary>
        /// <param name="name">The reference name of the element</param>
        /// <param name="texture"></param>
        /// <param name="sourceRegion">The region of the element in the source texture</param>
        /// <param name="borderSize">The size of the border around the element in the output atlas</param>
        /// <param name="borderModeU">The border mode along the U axis</param>
        /// <param name="borderModeV">The border mode along the V axis</param>
        /// <param name="borderColor">The color of the border</param>
        public AtlasTextureElement(string name, Image texture, RotableRectangle sourceRegion, int borderSize, TextureAddressMode borderModeU, TextureAddressMode borderModeV, Color? borderColor = null)
        {
            Name = name;
            Texture = texture;
            SourceRegion = sourceRegion;
            BorderSize = borderSize;
            BorderModeU = borderModeU;
            BorderModeV = borderModeV;
            BorderColor = borderColor ?? Color.Transparent;
        }

        /// <summary>
        /// Clone the current element.
        /// </summary>
        /// <returns>A copy of the current element</returns>
        public AtlasTextureElement Clone()
        {
            return (AtlasTextureElement)MemberwiseClone();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Name: {0} Source: {1} Destination:{2}", Name, SourceRegion, DestinationRegion);
        }
    }
}
