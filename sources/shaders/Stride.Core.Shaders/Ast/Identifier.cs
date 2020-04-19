// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// An identifier.
    /// </summary>
    public partial class Identifier : Node
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Identifier" /> class.
        /// </summary>
        public Identifier()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identifier"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public Identifier(string name)
        {
            Text = name;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether this instance has indices.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has indices; otherwise, <c>false</c>.
        /// </value>
        public bool HasIndices
        {
            get
            {
                return Indices != null && Indices.Count > 0;
            }
        }
        
        /// <summary>
        ///   Gets or sets the indices.
        /// </summary>
        /// <value>
        ///   The indices.
        /// </value>
        /// <remarks>
        /// This property can be null.
        /// </remarks>
        public List<Expression> Indices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a special reference using &lt; &gt;
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is special reference; otherwise, <c>false</c>.
        /// </value>
        public bool IsSpecialReference { get; set; }

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value>
        ///   The name.
        /// </value>
        public string Text { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// true if equals to other.
        /// </returns>
        public bool Equals(Identifier other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.Text, this.Text) && other.IsSpecialReference.Equals(this.IsSpecialReference);
        }

        /// <inheritdoc />
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
            var other = obj as Identifier;
            if (other == null)
                return false;

            return Equals(other.Text, Text) && other.IsSpecialReference == IsSpecialReference;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.Text != null ? this.Text.GetHashCode() : 0;
                result = (result * 397) ^ this.IsSpecialReference.GetHashCode();
                return result;
            }
        }

        /// <summary>
        ///   Returns a <see cref = "System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///   A <see cref = "System.String" /> that represents this instance.
        /// </returns>
        /// <inheritdoc />
        public override string ToString()
        {
            var ranks = new StringBuilder();
            if (Indices != null)
            {
                foreach (var expression in Indices)
                {
                    ranks.Append("[").Append(expression).Append("]");
                }
            }

            return string.Format(IsSpecialReference ? "<{0}{1}>" : "{0}{1}", this.Text, ranks);
        }

        #endregion

        #region Operators

        /// <summary>
        ///   Implements the operator ==.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator ==(Identifier left, Identifier right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref = "Identifier" /> to <see cref = "System.String" />.
        /// </summary>
        /// <param name = "identifier">The identifier.</param>
        /// <returns>
        ///   The result of the conversion.
        /// </returns>
        public static implicit operator string(Identifier identifier)
        {
            return identifier.ToString();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Identifier"/>.
        /// </summary>
        /// <param name="identifierName">Name of the identifier.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Identifier(string identifierName)
        {
            return new Identifier(identifierName);
        }

        /// <summary>
        ///   Implements the operator !=.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator !=(Identifier left, Identifier right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
