// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;

using Irony.Parsing;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A State type.
    /// </summary>
    public partial class TextureType : ObjectType
    {
        #region Constants and Fields

        /// <summary>
        /// A Texture
        /// </summary>
        public static readonly TextureType Texture = new TextureType("texture");

        /// <summary>
        /// A Texture1D.
        /// </summary>
        public static readonly TextureType Texture1D = new TextureType("Texture1D", "texture1D");

        /// <summary>
        /// A Texture1DArray.
        /// </summary>
        public static readonly TextureType Texture1DArray = new TextureType("Texture1DArray", "texture1DArray");

        /// <summary>
        /// A Texture2D
        /// </summary>
        public static readonly TextureType Texture2D = new TextureType("Texture2D", "texture2D");

        /// <summary>
        /// A Texture2DArray.
        /// </summary>
        public static readonly TextureType Texture2DArray = new TextureType("Texture2DArray", "texture2DArray");

        /// <summary>
        /// A Texture3D.
        /// </summary>
        public static readonly TextureType Texture3D = new TextureType("Texture3D", "texture3D");

        /// <summary>
        /// An TextureCube.
        /// </summary>
        public static readonly TextureType TextureCube = new TextureType("TextureCube", "textureCube");

        private static readonly TextureType[] TextureTypes = new[] { Texture, Texture1D, Texture1DArray, Texture2D, Texture2DArray, Texture3D, TextureCube };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureType"/> class.
        /// </summary>
        public TextureType()
        {
            IsBuiltIn = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public TextureType(string name, params string[] altNames)
            : base(name, altNames)
        {
            IsBuiltIn = true;
        }

        /// <inheritdoc/>
        public bool Equals(TextureType other)
        {
            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return Equals(obj as TextureType);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(TextureType left, TextureType right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(TextureType left, TextureType right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Parses the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static TextureType Parse(string name)
        {
            return TextureTypes.FirstOrDefault(textureType => CultureInfo.InvariantCulture.CompareInfo.Compare(name, textureType.Name.Text, CompareOptions.None) == 0);
        }
    }
}
