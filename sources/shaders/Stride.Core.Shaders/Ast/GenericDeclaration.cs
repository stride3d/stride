// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A generic declaration. This is used internally to identify a generic declaration.
    /// </summary>
    public partial class GenericDeclaration : Node, IDeclaration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDeclaration"/> class.
        /// </summary>
        public GenericDeclaration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDeclaration"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="holder">The holder.</param>
        /// <param name="index">The index.</param>
        /// <param name="isUsingBase">if set to <c>true</c> [is using base].</param>
        public GenericDeclaration(Identifier name, IGenerics holder, int index, bool isUsingBase)
        {
            Name = name;
            Holder = holder;
            Index = index;
            IsUsingBase = isUsingBase;
        }

        /// <summary>
        /// Gets or sets the name of this declaration
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        /// Gets or sets the holder.
        /// </summary>
        /// <value>
        /// The holder.
        /// </value>
        public IGenerics Holder { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is using base.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is using base; otherwise, <c>false</c>.
        /// </value>
        public bool IsUsingBase { get; set; }

        public bool Equals(GenericDeclaration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name) && Equals(other.Holder, Holder) && other.Index == Index && other.IsUsingBase.Equals(IsUsingBase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(GenericDeclaration)) return false;
            return Equals((GenericDeclaration)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Name.GetHashCode();
                result = (result * 397) ^ Holder.GetHashCode();
                result = (result * 397) ^ Index;
                result = (result * 397) ^ IsUsingBase.GetHashCode();
                return result;
            }
        }
    }
}
