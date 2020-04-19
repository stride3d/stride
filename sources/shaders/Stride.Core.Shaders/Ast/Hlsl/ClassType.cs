// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Definition of a class.
    /// </summary>
    public partial class ClassType : ObjectType, IDeclaration, IScopeContainer, IGenerics
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ClassType" /> class.
        /// </summary>
        public ClassType()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public ClassType(string name)
            : base(name)
        {
            BaseClasses = new List<TypeName>();
            Members = new List<Node>();
            GenericParameters = new List<TypeBase>();
            GenericArguments = new List<TypeBase>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the base classes.
        /// </summary>
        /// <value>
        ///   The base classes.
        /// </value>
        public List<TypeName> BaseClasses { get; set; }

        /// <inheritdoc/>
        public List<TypeBase> GenericParameters { get; set; }

        /// <inheritdoc/>
        public List<TypeBase> GenericArguments { get; set; }

        /// <summary>
        ///   Gets or sets the members.
        /// </summary>
        /// <value>
        ///   The members.
        /// </value>
        public List<Node> Members { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            ChildrenList.AddRange(BaseClasses);
            ChildrenList.AddRange(Members);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var bases = new StringBuilder();
            foreach (var baseClass in BaseClasses)
            {
                bases.Append(" : ").Append(baseClass);
            }
            var generics = new StringBuilder();
            if (GenericParameters.Count > 0)
            {
                generics.Append("<");
                for (int i = 0; i < GenericParameters.Count; i++)
                {
                    var genericArgument = GenericArguments.Count == GenericParameters.Count ? GenericArguments[i] : GenericParameters[i];
                    if (i > 0) generics.Append(", ");
                    generics.Append(genericArgument);
                }
                generics.Append(">");
            }

            return string.Format("class {0}{1}{2} {{...}}", Name, generics, bases);
        }

        /// <inheritdoc/>
        public bool Equals(ClassType other)
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
            return Equals(obj as ClassType);
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
        public static bool operator ==(ClassType left, ClassType right)
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
        public static bool operator !=(ClassType left, ClassType right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
