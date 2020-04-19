// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Base class for all generic types.
    /// </summary>
    public abstract partial class GenericBaseType : TypeBase
    {
        #region Constructors and Destructors

        public GenericBaseType() : this(null, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericBaseType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="parameterCount">
        /// The parameter count.
        /// </param>
        public GenericBaseType(string name, int parameterCount)
            : base(name)
        {
            ParameterTypes = new List<Type>();
            Parameters = new List<Node>();
            for (int i = 0; i < parameterCount; i++)
            {
                Parameters.Add(null);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Name).Append("<");
            for (int i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.Append(parameter is TypeBase ? ((TypeBase)parameter).Name : parameter);
            }

            builder.Append(">");

            return builder.ToString();
        }

        /// <summary>
        ///   Gets or sets the parameter types.
        /// </summary>
        /// <value>
        ///   The parameter types.
        /// </value>
        [DataMemberIgnore] // By default don't store it, unless derived class are overriding this member
        [VisitorIgnore]
        public virtual List<Type> ParameterTypes { get; set; }

        /// <summary>
        ///   Gets or sets the parameters.
        /// </summary>
        /// <value>
        ///   The parameters.
        /// </value>
        [DataMemberIgnore] // By default don't store it, unless derived class are overriding this member
        [VisitorIgnore]
        public virtual List<Node> Parameters { get; set; }

        public virtual TypeBase ToNonGenericType(SourceSpan? span = null)
        {
            return this;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="GenericBaseType"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GenericBaseType other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            //return base.Equals(other) && ParameterTypes.SequenceEqual(other.ParameterTypes) && Parameters.SequenceEqual(other.Parameters);
            return base.Equals(other) && Parameters.SequenceEqual(other.Parameters);
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

            return Equals(obj as GenericBaseType);
        }

        /// <summary>
        /// Gets the child nodes.
        /// </summary>
        /// <returns>
        /// </returns>
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            foreach (var parameter in Parameters)
            {
                if (parameter != null)
                {
                    ChildrenList.Add(parameter);
                }
            }

            return ChildrenList;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode() * 397;
            foreach (var parameter in Parameters)
            {
                hashCode = (hashCode * 397) ^ (parameter != null ? parameter.GetHashCode() : 0);
            }

            return hashCode;
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
        public static bool operator ==(GenericBaseType left, GenericBaseType right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Implements the operator !=.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator !=(GenericBaseType left, GenericBaseType right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
