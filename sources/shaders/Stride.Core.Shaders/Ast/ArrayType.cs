// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Array type.
    /// </summary>
    public partial class ArrayType : TypeBase
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ArrayType" /> class.
        /// </summary>
        public ArrayType() : base("$array")
        {
            Dimensions = new List<Expression>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayType"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="dimensions">The dimensions.</param>
        public ArrayType(TypeBase type, params Expression[] dimensions) : base("$array")
        {
            Type = type;
            Dimensions = new List<Expression>(); 
            Dimensions.AddRange(dimensions);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the dimensions.
        /// </summary>
        /// <value>
        ///   The dimensions.
        /// </value>
        public List<Expression> Dimensions { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var indices = new StringBuilder();
            foreach (var index in Dimensions)
            {
                indices.Append("[").Append(index).Append("]");
            }

            return string.Format("{0}{1}", Type, indices);
        }

        /// <summary>
        ///   Gets or sets the type.
        /// </summary>
        /// <value>
        ///   The type.
        /// </value>
        public TypeBase Type { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is dimension empty.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is dimension empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsDimensionEmpty
        {
            get { return Dimensions.Count == 1 && Dimensions[0] is EmptyExpression; }
        }

        #endregion

        #region Public Methods

        public bool Equals(ArrayType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if ( !base.Equals(other) || !Equals(other.Type.ResolveType(), Type.ResolveType()) || other.Dimensions.Count != Dimensions.Count) return false;

            // Check that dimensions are all equals
            return !Dimensions.Where((t, i) => t != other.Dimensions[i]).Any();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as ArrayType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ (Dimensions != null ? Dimensions.GetHashCode() : 0);
                result = (result * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(ArrayType left, ArrayType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ArrayType left, ArrayType right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Dimensions;
        }

        // TODO Implements equals for array types

        #endregion
    }
}
