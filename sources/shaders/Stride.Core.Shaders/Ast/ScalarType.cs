// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Scalar type
    /// </summary>
    public partial class ScalarType : TypeBase
    {
        #region Constants and Fields

        /// <summary>
        ///   Scalar bool.
        /// </summary>
        public static readonly ScalarType Bool = new ScalarType("bool", typeof(bool));

        /// <summary>
        ///   Scalar double.
        /// </summary>
        public static readonly ScalarType Double = new ScalarType("double", typeof(double));

        /// <summary>
        ///   Sclar float.
        /// </summary>
        public static readonly ScalarType Float = new ScalarType("float", typeof(float));

        /// <summary>
        ///   Scalar half.
        /// </summary>
        public static readonly ScalarType Half = new ScalarType("half");

        /// <summary>
        ///   Scalar int.
        /// </summary>
        public static readonly ScalarType Int = new ScalarType("int", typeof(int));

        /// <summary>
        ///   Scalar unsigned int.
        /// </summary>
        public static readonly ScalarType UInt = new ScalarType("uint", typeof(uint));

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ScalarType" /> class.
        /// </summary>
        public ScalarType()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public ScalarType(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarType"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        public ScalarType(string name, Type type)
            : base(name)
        {
            Type = type;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the type.
        /// </summary>
        /// <value>
        ///   The type.
        /// </value>
        public Type Type { get; set; }

        /// <summary>
        /// Gets a boolean indicating if this scalar is an unsigned type.
        /// </summary>
        public bool IsUnsigned
        {
            get
            {
                return Type == typeof(uint);
            }
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
        /// <c>true</c> if the specified <see cref="ScalarType"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ScalarType other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && Equals(other.Type, Type);
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

            return Equals(obj as ScalarType);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether the specified type is a float/half/double.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is float/half/double; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFloat(TypeBase type)
        {
            return type == Float || type == Double || type == Half;
        }

        /// <summary>
        /// Determines whether the specified type is an integer.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is an integer; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInteger(TypeBase type)
        {
            return type == Int || type == UInt;
        }

        /// <summary>
        ///   Implements the operator ==.
        /// </summary>
        /// <param name = "left">The left.</param>
        /// <param name = "right">The right.</param>
        /// <returns>
        ///   The result of the operator.
        /// </returns>
        public static bool operator ==(ScalarType left, ScalarType right)
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
        public static bool operator !=(ScalarType left, ScalarType right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
