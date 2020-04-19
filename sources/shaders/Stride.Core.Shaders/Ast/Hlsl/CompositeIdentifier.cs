// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A composite identifier.
    /// </summary>
    public abstract partial class CompositeIdentifier : Identifier
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CompositeIdentifier" /> class.
        /// </summary>
        public CompositeIdentifier()
        {
            Identifiers = new List<Identifier>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the path.
        /// </summary>
        /// <value>
        ///   The path.
        /// </value>
        public List<Identifier> Identifiers { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the separator.
        /// </summary>
        public abstract string Separator { get; }

        public bool Equals(CompositeIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && (Identifiers.Count != other.Identifiers.Count);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as CompositeIdentifier);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Identifiers.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Identifiers;
        }

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

            return string.Format(IsSpecialReference ? "<{0}{1}>" : "{0}{1}", string.Join(Separator, Identifiers), ranks);
        }

        #endregion
    }
}
