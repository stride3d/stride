// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Base type for all types.
    /// </summary>
    public abstract partial class TypeBase : Node, IAttributes, ITypeInferencer, IQualifiers
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "TypeBase" /> class.
        /// </summary>
        protected TypeBase() : this((Identifier)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeBase"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        protected TypeBase(string name)
            : this(name != null ? new Identifier(name) : null)
        {
            Name = name != null ? new Identifier(name) : null;
            Attributes = new List<AttributeBase>();
            Qualifiers = Qualifier.None;
            TypeInference = new TypeInference();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeBase"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        protected TypeBase(Identifier name)
        {
            Name = name;
            Attributes = new List<AttributeBase>();
            Qualifiers = Qualifier.None;
            TypeInference = new TypeInference();
        }
        
        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the attributes.
        /// </summary>
        /// <value>
        ///   The attributes.
        /// </value>
        public List<AttributeBase> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the resolved reference.
        /// </summary>
        /// <value>
        /// The resolved reference.
        /// </value>
        public TypeInference TypeInference { get; set; }

        /// <summary>
        ///   Gets or sets the type name.
        /// </summary>
        /// <value>
        ///   The type name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        ///   Gets or sets the qualifiers.
        /// </summary>
        /// <value>
        ///   The qualifiers.
        /// </value>
        public Qualifier Qualifiers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is built in.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is built in; otherwise, <c>false</c>.
        /// </value>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Resolves the type.
        /// </summary>
        /// <returns>
        /// The resolved type.
        /// </returns>
        public virtual TypeBase ResolveType()
        {
            return TypeInference.TargetType ?? this;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a type based on a new base type. If type is a matrix or vector, then the base type is changed to match the newBaseType.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="newBaseType">New type of the base.</param>
        /// <returns>A new type</returns>
        public static TypeBase CreateWithBaseType(TypeBase type, ScalarType newBaseType)
        {
            if (type is MatrixType)
                return new MatrixType(newBaseType, ((MatrixType)type).RowCount, ((MatrixType)type).ColumnCount);

            if (type is VectorType)
                return new VectorType(newBaseType, ((VectorType)type).Dimension);

            return newBaseType;
        }

        public static TypeBase GetBaseType(TypeBase type)
        {
            if (type is MatrixType) return ((MatrixType)type).Type;
            if (type is VectorType) return ((VectorType)type).Type;
            return type;
        }

        public static bool HasDimensions(TypeBase typeDeclaration)
        {
            return (typeDeclaration is ScalarType) || (typeDeclaration is VectorType) || (typeDeclaration is MatrixType);
        }

        public static int GetDimensionSize(TypeBase typeDeclaration, int dimension)
        {
            if (typeDeclaration is VectorType)
            {
                if (dimension != 1) return 1;
                return ((VectorType)typeDeclaration).Dimension;
            }

            if (typeDeclaration is MatrixType)
            {
                var matrixType = (MatrixType)typeDeclaration;
                return dimension == 0 ? matrixType.RowCount : matrixType.ColumnCount;
            }

            return 1;
        }

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="TypeBase"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(TypeBase other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.Name, Name);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="System.Object"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

            if (!typeof(TypeBase).GetTypeInfo().IsAssignableFrom(obj.GetType().GetTypeInfo()))
            {
                return false;
            }

            return Equals((TypeBase)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name?.ToString();
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
        public static bool operator ==(TypeBase left, TypeBase right)
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
        public static bool operator !=(TypeBase left, TypeBase right)
        {
            return !Equals(left, right);
        }

        #endregion

        /// <summary>
        ///   Scalar void. TODO this is not a scalar!
        /// </summary>
        public static readonly ScalarType Void = new ScalarType("void", typeof(void));

        /// <summary>
        ///   Scalar void. TODO this is not a scalar!
        /// </summary>
        public static readonly ScalarType String = new ScalarType("string", typeof(string));
    }
}
