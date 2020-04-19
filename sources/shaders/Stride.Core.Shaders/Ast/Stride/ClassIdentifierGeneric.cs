// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast.Stride
{
    public partial class ClassIdentifierGeneric : Identifier
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ClassIdentifierGeneric" /> class.
        /// </summary>
        public ClassIdentifierGeneric()
        {
            Generics = new List<Variable>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the path.
        /// </summary>
        /// <value>
        ///   The path.
        /// </value>
        public List<Variable> Generics { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the separator.
        /// </summary>
        public string Separator
        {
            get
            {
                return ",";
            }
        }

        public bool Equals(ClassIdentifierGeneric other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && (Generics.Count != other.Generics.Count);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as ClassIdentifierGeneric);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Generics.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Generics;
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

            return string.Format(IsSpecialReference ? "{0}<{1}{2}>" : "{0}{1}{2}", Text, string.Join(Separator, Generics), ranks);
        }

        #endregion
    }
}
