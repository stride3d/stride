// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Matrix type.
    /// </summary>
    public partial class MatrixType : GenericBaseType
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "MatrixType" /> class.
        /// </summary>
        public MatrixType()
            : base("matrix", 3)
        {
            ParameterTypes.Add(typeof(TypeBase));
            ParameterTypes.Add(typeof(Literal));
            ParameterTypes.Add(typeof(Literal));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixType"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="rowCount">
        /// The row count.
        /// </param>
        /// <param name="columnCount">
        /// The column count.
        /// </param>
        public MatrixType(ScalarType type, int rowCount, int columnCount)
            : this()
        {
            Type = type;
            RowCount = rowCount;
            ColumnCount = columnCount;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the row count.
        /// </summary>
        /// <value>
        ///   The row count.
        /// </value>
        public int RowCount
        {
            get
            {
                return (int)((Literal)Parameters[1]).Value;
            }

            set
            {
                Parameters[1] = new Literal(value);
            }
        }

        /// <summary>
        ///   Gets or sets the column count.
        /// </summary>
        /// <value>
        ///   The column count.
        /// </value>
        public int ColumnCount
        {
            get
            {
                return (int)((Literal)Parameters[2]).Value;
            }

            set
            {
                Parameters[2] = new Literal(value);
            }
        }

        /// <summary>
        ///   Gets or sets the type.
        /// </summary>
        /// <value>
        ///   The type.
        /// </value>
        public TypeBase Type
        {
            get
            {
                return (TypeBase)Parameters[0];
            }

            set
            {
                Parameters[0] = value;
            }
        }

        #endregion

        public override TypeBase ToNonGenericType(SourceSpan? span = null)
        {
            var typeName = new TypeName();
            var name = string.Format("{0}{1}x{2}", Type.Name, RowCount, ColumnCount);
            typeName.Name = new Identifier(name);
            if (span.HasValue)
            {
                typeName.Span = span.Value;
                typeName.Name.Span = span.Value;
            };
            typeName.TypeInference.TargetType = this;
            return typeName;
        }

        /// <inheritdoc/>
        public bool Equals(MatrixType other)
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
            return Equals(obj as MatrixType);
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
        public static bool operator ==(MatrixType left, MatrixType right)
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
        public static bool operator !=(MatrixType left, MatrixType right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Index information.
        /// </summary>
        [DataContract]
        public struct Indexer
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Indexer"/> struct.
            /// </summary>
            /// <param name="row">The row.</param>
            /// <param name="column">The column.</param>
            public Indexer(int row, int column)
            {
                Row = row;
                Column = column;
            }

            /// <summary>
            /// The row number, zero-based index.
            /// </summary>
            public int Row;

            /// <summary>
            /// The column number, zero-based index.
            /// </summary>
            public int Column;
        }
    }
}
