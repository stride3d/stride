// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Analysis
{
    internal class CastHelper
    {
        /// <summary>
        /// Check if convert is necessary
        /// </summary>
        /// <param name="leftType">Type of the left.</param>
        /// <param name="rightType">Type of the right.</param>
        /// <returns>True if a cast is necessary between this two types</returns>
        public static bool NeedConvertForBinary(TypeBase leftType, TypeBase rightType)
        {
            return leftType != null && rightType != null && leftType != rightType
                   &&
                   !((leftType is ScalarType && (rightType is VectorType || rightType is MatrixType) && TypeBase.GetBaseType(rightType) == leftType)
                     || (rightType is ScalarType && (leftType is VectorType || leftType is MatrixType) && TypeBase.GetBaseType(leftType) == rightType));
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="isBooleanOperator">if set to <c>true</c> [is boolean operator].</param>
        /// <returns>
        /// The implicit conversion between between to two types
        /// </returns>
        public static TypeBase GetBinaryImplicitConversionType(TypeBase left, TypeBase right, bool isBooleanOperator)
        {
            if (left is MatrixType && right is MatrixType)
            {
                var leftMatrix = (MatrixType)left;
                var rightMatrix = (MatrixType)right;

                var leftDimension1 = leftMatrix.RowCount;
                var leftDimension2 = leftMatrix.ColumnCount;
                var rightDimension1 = rightMatrix.RowCount;
                var rightDimension2 = rightMatrix.ColumnCount;

                if (!((leftDimension1 >= rightDimension1 && leftDimension2 >= rightDimension2) || (leftDimension1 <= rightDimension1 && leftDimension2 <= rightDimension2)))
                {
                    return null;
                }

                var type = isBooleanOperator ? ScalarType.Bool : GetBinaryImplicitScalarConversionType(leftMatrix.Type.ResolveType(), rightMatrix.Type.ResolveType());
                if (type != null)
                    return new MatrixType(type, Math.Min(leftDimension1, rightDimension1), Math.Min(leftDimension2, rightDimension2));

                return null;
            }

            // Swap to handle next case with same code (always put bigger type on the left)
            // Works for Vector*Matrix, Scalar*Matrix and Scalar*Vector
            if (right is MatrixType || (!(left is MatrixType) && !(left is VectorType)))
            {
                var temp = left;
                left = right;
                right = temp;
            }

            if (left is MatrixType && right is VectorType)
            {
                var leftMatrix = (MatrixType)left;
                var rightVector = (VectorType)right;

                var leftDimension1 = leftMatrix.RowCount;
                var leftDimension2 = leftMatrix.ColumnCount;
                var rightDimension1 = rightVector.Dimension;

                // Matrix must have at least one dimension (row or column count == 1)
                if (leftDimension1 != 1 && leftDimension2 != 1)
                {
                    return null;
                }

                var type = isBooleanOperator ? ScalarType.Bool : GetBinaryImplicitScalarConversionType(leftMatrix.Type.ResolveType(), rightVector.Type.ResolveType());
                if (type != null)
                    return new VectorType(type, Math.Min(Math.Max(leftDimension1, leftDimension2), rightDimension1));
                return null;
            }

            if (left is MatrixType)
            {
                var leftMatrix = (MatrixType)left;

                var leftDimension1 = leftMatrix.RowCount;
                var leftDimension2 = leftMatrix.ColumnCount;

                var type = isBooleanOperator ? ScalarType.Bool : GetBinaryImplicitScalarConversionType(leftMatrix.Type.ResolveType(), right);
                if (type != null)
                    return new MatrixType(type, leftDimension1, leftDimension2);
                return null;
            }

            if (left is VectorType)
            {
                var leftVector = (VectorType)left;
                var leftDimension1 = leftVector.Dimension;

                var rightDimension1 = right is VectorType ? ((VectorType)right).Dimension : 1;

                var rightBaseType = right is VectorType ? ((VectorType)right).Type.ResolveType() : right;

                int dimension = Math.Min(leftDimension1, rightDimension1);
                if (rightDimension1 == 1 || leftDimension1 == 1)
                    dimension = Math.Max(leftDimension1, rightDimension1);

                var type = isBooleanOperator ? ScalarType.Bool : GetBinaryImplicitScalarConversionType(leftVector.Type.ResolveType(), rightBaseType);
                if (type != null) return new VectorType(type, dimension);
                return null;
            }

            return (isBooleanOperator) ? ScalarType.Bool : GetBinaryImplicitScalarConversionType(left, right);
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion in case of a multiplication.
        /// </summary>
        /// <param name="left">the left.</param>
        /// <param name="right">the right.</param>
        /// <returns>The implicit conversion between between to two types</returns>
        public static TypeBase GetMultiplyImplicitConversionType(TypeBase left, TypeBase right)
        {
            if ((left is VectorType || left is MatrixType) && right is ScalarType)
                return GetMultiplyImplicitConversionType(right, left);

            if (left is ScalarType)
            {
                TypeBase componentType = null;
                if (right is VectorType) { componentType = (right as VectorType).Type; }
                else if (right is MatrixType) { componentType = (right as MatrixType).Type; }

                if (componentType != null)
                {
                    ScalarType resultComponentType = null;
                    if (left == ScalarType.Double || componentType == ScalarType.Double)
                        resultComponentType = ScalarType.Double;
                    else if (left == ScalarType.Float || componentType == ScalarType.Float)
                        resultComponentType = ScalarType.Float;
                    else if (left == ScalarType.Half || componentType == ScalarType.Half)
                        resultComponentType = ScalarType.Half;
                    else if (left == ScalarType.Int || componentType == ScalarType.Int)
                        resultComponentType = ScalarType.Int;
                    else if (left == ScalarType.UInt || componentType == ScalarType.UInt)
                        resultComponentType = ScalarType.UInt;

                    if (resultComponentType != null)
                    {
                        if (right is VectorType)
                            return new VectorType(resultComponentType, (right as VectorType).Dimension);

                        if (right is MatrixType)
                            return new MatrixType(resultComponentType, (right as MatrixType).RowCount, (right as MatrixType).ColumnCount);
                    }
                }
            }

            return GetBinaryImplicitConversionType(left, right, false);
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion in case of a division
        /// </summary>
        /// <param name="left">the left.</param>
        /// <param name="right">the right.</param>
        /// <returns>The implicit conversion between between to two types</returns>
        public static TypeBase GetDivideImplicitConversionType(TypeBase left, TypeBase right)
        {
            if (right is ScalarType)
            {
                TypeBase componentType = null;
                if (left is VectorType) { componentType = (left as VectorType).Type; }
                else if (left is MatrixType) { componentType = (left as MatrixType).Type; }

                if (componentType != null)
                {
                    ScalarType resultComponentType = null;
                    if (left == ScalarType.Double || componentType == ScalarType.Double)
                        resultComponentType = ScalarType.Double;
                    else if (left == ScalarType.Float || componentType == ScalarType.Float)
                        resultComponentType = ScalarType.Float;
                    else if (left == ScalarType.Half || componentType == ScalarType.Half)
                        resultComponentType = ScalarType.Half;
                    else if (left == ScalarType.Int || componentType == ScalarType.Int)
                        resultComponentType = ScalarType.Int;
                    else if (left == ScalarType.UInt || componentType == ScalarType.UInt)
                        resultComponentType = ScalarType.UInt;

                    if (resultComponentType != null)
                    {
                        if (left is VectorType)
                            return new VectorType(resultComponentType, (left as VectorType).Dimension);

                        if (left is MatrixType)
                            return new MatrixType(resultComponentType, (left as MatrixType).RowCount, (left as MatrixType).ColumnCount);
                    }
                }
            }

            return GetBinaryImplicitConversionType(left, right, false);
        }

        /// <summary>
        /// Gets the type of the binary implicit scalar conversion.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The implicit conversion between the two scalar types
        /// </returns>
        public static ScalarType GetBinaryImplicitScalarConversionType(TypeBase left, TypeBase right)
        {
            if (left is ScalarType && right is ScalarType)
            {
                if (left == right)
                    return (ScalarType)left;

                foreach (var type in new[] { ScalarType.Double, ScalarType.Float, ScalarType.Half, ScalarType.UInt, ScalarType.Int, ScalarType.Bool })
                {
                    if (left == type || right == type)
                        return type;
                }
            }
            return null;
        }
    }
}
