// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Shaders.Visitor;

namespace Xenko.Core.Shaders.Ast
{
    /// <summary>
    /// An expression.
    /// </summary>
    public abstract partial class Expression : Node, ITypeInferencer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Expression"/> class.
        /// </summary>
        protected Expression()
        {
            TypeInference = new TypeInference();
        }

        /// <summary>
        /// Gets or sets the type reference.
        /// </summary>
        /// <value>
        /// The type reference.
        /// </value>
        public TypeInference TypeInference { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Empty;
        }

        public bool Equals(Expression other)
        {
            return !ReferenceEquals(null, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Expression)) return false;
            return Equals((Expression)obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(Expression left, Expression right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Expression left, Expression right)
        {
            return !Equals(left, right);
        }
    }
}
