// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Graphics
{
    /// <summary>
    /// A sprite.
    /// </summary>
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<Sprite>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Sprite>), Profile = "Content")]
    public class Sprite
    {
        public const int DefaultPixelsPerUnit = 100;

        private ImageOrientation orientation;
        private Vector2 sizeInPixels;
        private Vector2 pixelsPerUnit;
        
        internal RectangleF RegionInternal;
        internal Vector4 BordersInternal;
        internal Vector2 SizeInternal;

        internal event EventHandler<EventArgs> BorderChanged;
        internal event EventHandler<EventArgs> SizeChanged;

        /// <summary>
        /// Create an instance of <see cref="Sprite"/> with a unique random name.
        /// </summary>
        public Sprite()
            : this(Guid.NewGuid().ToString(), null)
        {
        }

        /// <summary>
        /// Creates an empty <see cref="Sprite"/> having the provided name.
        /// </summary>
        /// <param name="fragmentName">Name of the fragment</param>
        public Sprite(string fragmentName)
            : this(fragmentName, null)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Sprite"/> from the provided <see cref="Texture"/>.
        /// A unique Id is set as name and the <see cref="Region"/> is initialized to the size of the whole texture.
        /// </summary>
        /// <param name="texture">The texture to use as texture</param>
        public Sprite(Texture texture)
            : this(Guid.NewGuid().ToString(), texture)
        {
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> having the provided texture and name.
        /// The region size is initialized with the whole size of the texture.
        /// </summary>
        /// <param name="fragmentName">The name of the sprite</param>
        /// <param name="texture">The texture to use as texture</param>
        public Sprite(string fragmentName, Texture texture)
        {
            Name = fragmentName;
            PixelsPerUnit = new Vector2(DefaultPixelsPerUnit);
            IsTransparent = true;
            
            Texture = texture;
            if (texture != null)
            {
                var isFullTexture = texture.ViewType == ViewType.Full;
                var fullQualitySize = texture.FullQualitySize;
                var width = isFullTexture ? fullQualitySize.Width : texture.ViewWidth;
                var height = isFullTexture ? fullQualitySize.Height : texture.ViewHeight;
                Region = new Rectangle(0, 0, width, height);
                Center = new Vector2(Region.Width / 2, Region.Height / 2);
            }
        }

        /// <summary>
        /// Gets or sets the name of the image fragment.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The texture in which the image is contained
        /// </summary>
        public Texture Texture { get; set; }

        /// <summary>
        /// The position of the center of the image in pixels.
        /// </summary>
        public Vector2 Center { get; set; }

        /// <summary>
        /// The rectangle specifying the region of the texture to use as fragment.
        /// </summary>
        public RectangleF Region
        {
            get { return RegionInternal; }
            set
            {
                RegionInternal = value;
                UpdateSizes();
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the fragment contains transparent regions.
        /// </summary>
        public bool IsTransparent { get; set; }

        /// <summary>
        /// Gets or sets the rotation to apply to the texture region when rendering the <see cref="Sprite"/>
        /// </summary>
        public virtual ImageOrientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                UpdateSizes();
            }
        }
        
        /// <summary>
        /// Gets or sets size of the unstretchable borders of source sprite in pixels.
        /// </summary>
        /// <remarks>Borders size are ordered as follows X->Left, Y->Top, Z->Right, W->Bottom.</remarks>
        public Vector4 Borders
        {
            get { return BordersInternal; }
            set
            {
                if (value == BordersInternal)
                    return;

                BordersInternal = value;
                HasBorders = BordersInternal.Length() > MathUtil.ZeroTolerance;

                BorderChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the value indicating if the image has unstretchable borders.
        /// </summary>
        public bool HasBorders { get; private set; }

        /// <summary>
        /// Gets the size of the sprite in scene units.
        /// Note that the orientation of the image is taken into account in this calculation.
        /// </summary>
        public Vector2 Size
        {
            get { return SizeInternal; }
        }

        /// <summary>
        /// Gets the size of the sprite in pixels. 
        /// Note that the orientation of the image is taken into account in this calculation.
        /// </summary>
        public Vector2 SizeInPixels
        {
            get { return sizeInPixels; }
            private set
            {
                if (value == sizeInPixels)
                    return;

                sizeInPixels = value;

                SizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the pixels per scene unit of the sprite.
        /// </summary>
        /// <remarks>The value is clamped to a strictly positive value.</remarks>
        public Vector2 PixelsPerUnit
        {
            get { return pixelsPerUnit; }
            set
            {
                if (pixelsPerUnit == value)
                    return;

                pixelsPerUnit = value;
                pixelsPerUnit.X = Math.Max(MathUtil.ZeroTolerance, pixelsPerUnit.X);
                pixelsPerUnit.Y = Math.Max(MathUtil.ZeroTolerance, pixelsPerUnit.Y);
                UpdateSizes();
            }
        }

        private void UpdateSizes()
        {
            var pixelSize = new Vector2(RegionInternal.Width, RegionInternal.Height);
            SizeInternal = new Vector2(pixelSize.X / pixelsPerUnit.X, pixelSize.Y / pixelsPerUnit.Y);
            if (orientation == ImageOrientation.Rotated90)
            {
                Utilities.Swap(ref pixelSize.X, ref pixelSize.Y);
                Utilities.Swap(ref SizeInternal.X, ref SizeInternal.Y);
            }

            SizeInPixels = pixelSize;
        }

        public override string ToString()
        {
            var textureName = Texture != null ? Texture.Name : "''";
            return Name + ", Texture: " + textureName + ", Region: " + Region;
        }

        /// <summary>
        /// Clone the current sprite.
        /// </summary>
        /// <returns>A new instance of the current sprite.</returns>
        public Sprite Clone()
        {
            return (Sprite)MemberwiseClone();
        }
    }
}
