// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// An Object Type.
    /// </summary>
    public partial class ObjectType : TypeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectType"/> class.
        /// </summary>
        public ObjectType()
        {
            AlternativeNames = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectType"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="atlNames">The atl names.</param>
        public ObjectType(string name, params string[] atlNames)
            : base(name)
        {
            AlternativeNames = new List<string>();
            if (atlNames != null)
                AlternativeNames.AddRange(atlNames);
        }

        /// <summary>
        /// Gets or sets the alternatives.
        /// </summary>
        /// <value>
        /// The alternatives.
        /// </value>
        public List<string> AlternativeNames { get; set; }

        /// <inheritdoc/>
        public bool Equals(ObjectType other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.Name, Name) || AlternativeNames.Contains(other.Name);
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
            return Equals(obj as ObjectType);
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
        public static bool operator ==(ObjectType left, ObjectType right)
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
        public static bool operator !=(ObjectType left, ObjectType right)
        {
            return !Equals(left, right);
        }
    }
}
