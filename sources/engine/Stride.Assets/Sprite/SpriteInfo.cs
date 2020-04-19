// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Sprite
{
    /// <summary>
    /// This class contains all the information to describe one sprite.
    /// </summary>
    [DataContract("SpriteInfo")]
    public class SpriteInfo
    {
        /// <summary>
        /// Gets or sets the source file of this 
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The path to the file containing the image data.
        /// </userdoc>
        [DataMember(0)]
        [DefaultValue(null)]
        [SourceFileMember(false)]
        public UFile Source;

        /// <summary>
        /// Gets or sets the name of the sprite.
        /// </summary>
        /// <userdoc>
        /// The name of the sprite instance.
        /// </userdoc>
        [DataMember(10)]
        public string Name;

        /// <summary>
        /// The rectangle specifying the region of the texture to use.
        /// </summary>
        /// <userdoc>
        /// The rectangle specifying the sprite region in the source file.
        /// </userdoc>
        [DataMember(20)]
        public Rectangle TextureRegion;

        /// <summary>
        /// The number of pixels representing a unit of 1 in the scene.
        /// </summary>
        /// <userdoc>
        /// The number of pixels representing a unit of 1 in the scene.
        /// </userdoc>
        [DataMember(25)]
        [DefaultValue(100)]
        public float PixelsPerUnit;

        /// <summary>
        /// Gets or sets the rotation to apply to the texture region when rendering the sprite
        /// </summary>
        /// <userdoc>
        /// The orientation of the sprite in the source file.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(ImageOrientation.AsIs)]
        public ImageOrientation Orientation { get; set; }

        /// <summary>
        /// The position of the center of the sprite in pixels.
        /// </summary>
        /// <userdoc>
        /// The position of the center of the sprite in pixels. 
        /// Depending on the value of 'CenterFromMiddle', it is the offset from the top/left corner or the middle of the sprite.
        /// </userdoc>
        [DataMember(40)]
        public Vector2 Center;

        /// <summary>
        /// Gets or sets the value indicating position provided to <see cref="Center"/> is from the middle of the sprite region or from the left/top corner.
        /// </summary>
        /// <userdoc>
        /// If enabled, the value in Center represents the offset of the sprite center from the middle of the sprite
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(true)]
        public bool CenterFromMiddle { get; set; }

        /// <summary>
        /// Gets or sets the size of the non-stretchable borders of the sprite.
        /// </summary>
        /// <userdoc>
        /// The size in pixels of the non-stretchable parts of the sprite.
        /// The part sizes are organized as follows: X->Left, Y->Top, Z->Right, W->Bottom.
        /// </userdoc>
        [DataMember(60)]
        public Vector4 Borders { get; set; }

        /// <summary>
        /// Gets or sets atlas border mode in X axis for images inside atlas texture
        /// </summary>
        /// <usderdoc>The method used to color the sprite outside its texture region along the X axis. 
        /// This information is essentially used during texture packing to avoid artifacts at sprite borders.</usderdoc>
        [DataMember(100)]
        [DefaultValue(TextureAddressMode.Clamp)]
        public TextureAddressMode BorderModeU { get; set; }

        /// <summary>
        /// Gets or sets atlas border mode in Y axis for images inside atlas texture
        /// </summary>
        /// <usderdoc>The method used to color the sprite outside its texture region along the Y axis. 
        /// This information is essentially used during texture packing to avoid artifacts at sprite borders.</usderdoc>
        [DataMember(110)]
        [DefaultValue(TextureAddressMode.Clamp)]
        public TextureAddressMode BorderModeV { get; set; }

        /// <summary>
        /// Gets or sets atlas border color for images inside atlas texture where Border mode is used in BorderModeU/V
        /// </summary>
        /// <usderdoc>The color used for this sprite outside of its texture region. 
        /// This parameter is used only when either 'BorderModeU' or 'BorderModeV' is set to 'Border'.</usderdoc>
        [DataMember(120)]
        public Color BorderColor { get; set; }

        /// <summary>
        /// Creates an empty instance of SpriteInfo
        /// </summary>
        public SpriteInfo()
        {
            PixelsPerUnit = 100;
            CenterFromMiddle = true;
            BorderModeU = TextureAddressMode.Clamp;
            BorderModeV = TextureAddressMode.Clamp;
            BorderColor = Color.Transparent;
        }
    }
}
