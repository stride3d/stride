// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Definition of a class.
    /// </summary>
    public partial class InterfaceType : ObjectType, IDeclaration, IGenerics
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "InterfaceType" /> class.
        /// </summary>
        public InterfaceType()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public InterfaceType(string name)
            : base(name)
        {
            Methods = new List<MethodDeclaration>();
            GenericParameters = new List<TypeBase>();
            GenericArguments = new List<TypeBase>();
        }

        #endregion

        #region Public Properties

        /// <inheritdoc/>
        public List<TypeBase> GenericParameters { get; set; }

        /// <inheritdoc/>
        public List<TypeBase> GenericArguments { get; set; }

        /// <summary>
        /// Gets or sets the methods.
        /// </summary>
        /// <value>
        /// The methods.
        /// </value>
        public List<MethodDeclaration> Methods { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            ChildrenList.AddRange(Methods);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("interface {0} {{...}}", Name);
        }

        /// <inheritdoc/>
        public bool Equals(InterfaceType other)
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
            return Equals(obj as InterfaceType);
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
        public static bool operator ==(InterfaceType left, InterfaceType right)
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
        public static bool operator !=(InterfaceType left, InterfaceType right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
