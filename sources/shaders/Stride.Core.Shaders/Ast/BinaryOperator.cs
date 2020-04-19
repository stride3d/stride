// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Binary operator used in all binary expressions (except assignment expression).
    /// </summary>
    public enum BinaryOperator
    {
        /// <summary>
        ///   No operator defined.
        /// </summary>
        None, 

        /// <summary>
        ///   Logical And operator "&amp;&amp;"
        /// </summary>
        LogicalAnd, 

        /// <summary>
        ///   Logical Or operator "||"
        /// </summary>
        LogicalOr, 

        /// <summary>
        ///   Bitwise And operator "&amp;"
        /// </summary>
        BitwiseAnd, 

        /// <summary>
        ///   Bitwise Or operator "|"
        /// </summary>
        BitwiseOr, 

        /// <summary>
        ///   Bitwise Xor operator "^"
        /// </summary>
        BitwiseXor, 

        /// <summary>
        ///   Left shift operator "&lt;&lt;"
        /// </summary>
        LeftShift, 

        /// <summary>
        ///   Right shift operator "&gt;&gt;"
        /// </summary>
        RightShift, 

        /// <summary>
        ///   Minus operator "-"
        /// </summary>
        Minus, 

        /// <summary>
        ///   Plus operator "+"
        /// </summary>
        Plus, 

        /// <summary>
        ///   Multiply operator "*"
        /// </summary>
        Multiply, 

        /// <summary>
        ///   Divide operator "/"
        /// </summary>
        Divide, 

        /// <summary>
        ///   Modulo operator "%"
        /// </summary>
        Modulo, 

        /// <summary>
        ///   Less than operator "&lt;"
        /// </summary>
        Less, 

        /// <summary>
        ///   Less or equal operator "&lt;="
        /// </summary>
        LessEqual, 

        /// <summary>
        ///   Greater operator "&gt;"
        /// </summary>
        Greater, 

        /// <summary>
        ///   Greater or equal operator "&gt;="
        /// </summary>
        GreaterEqual, 

        /// <summary>
        ///   Equality operator "=="
        /// </summary>
        Equality, 

        /// <summary>
        ///   Inequality operator "!="
        /// </summary>
        Inequality, 
    }

    /// <summary>
    /// Helper for <see cref="BinaryOperator"/>.
    /// </summary>
    public static class BinaryOperatorHelper
    {
        #region Public Methods

        /// <summary>
        /// Converts from operator to string
        /// </summary>
        /// <param name="binaryOperator">
        /// The binary operator.
        /// </param>
        /// <returns>
        /// A string representation of an binary operator
        /// </returns>
        public static string ConvertToString(this BinaryOperator binaryOperator)
        {
            switch (binaryOperator)
            {
                case BinaryOperator.LogicalAnd:
                    return "&&";
                case BinaryOperator.LogicalOr:
                    return "||";
                case BinaryOperator.BitwiseAnd:
                    return "&";
                case BinaryOperator.BitwiseOr:
                    return "|";
                case BinaryOperator.BitwiseXor:
                    return "^";
                case BinaryOperator.LeftShift:
                    return "<<";
                case BinaryOperator.RightShift:
                    return ">>";
                case BinaryOperator.Minus:
                    return "-";
                case BinaryOperator.Plus:
                    return "+";
                case BinaryOperator.Multiply:
                    return "*";
                case BinaryOperator.Divide:
                    return "/";
                case BinaryOperator.Modulo:
                    return "%";
                case BinaryOperator.Less:
                    return "<";
                case BinaryOperator.LessEqual:
                    return "<=";
                case BinaryOperator.Greater:
                    return ">";
                case BinaryOperator.GreaterEqual:
                    return ">=";
                case BinaryOperator.Equality:
                    return "==";
                case BinaryOperator.Inequality:
                    return "!=";
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts from string an operator.
        /// </summary>
        /// <param name="operatorStr">
        /// The operator text.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If operatorStr is invalid
        /// </exception>
        /// <returns>
        /// An binary operator
        /// </returns>
        public static BinaryOperator FromString(string operatorStr)
        {
            if (operatorStr == "&&")
            {
                return BinaryOperator.LogicalAnd;
            }

            if (operatorStr == "||")
            {
                return BinaryOperator.LogicalOr;
            }

            if (operatorStr == "&")
            {
                return BinaryOperator.BitwiseAnd;
            }

            if (operatorStr == "|")
            {
                return BinaryOperator.BitwiseOr;
            }

            if (operatorStr == "^")
            {
                return BinaryOperator.BitwiseXor;
            }

            if (operatorStr == "<<")
            {
                return BinaryOperator.LeftShift;
            }

            if (operatorStr == ">>")
            {
                return BinaryOperator.RightShift;
            }

            if (operatorStr == "-")
            {
                return BinaryOperator.Minus;
            }

            if (operatorStr == "+")
            {
                return BinaryOperator.Plus;
            }

            if (operatorStr == "*")
            {
                return BinaryOperator.Multiply;
            }

            if (operatorStr == "/")
            {
                return BinaryOperator.Divide;
            }

            if (operatorStr == "%")
            {
                return BinaryOperator.Modulo;
            }

            if (operatorStr == "<")
            {
                return BinaryOperator.Less;
            }

            if (operatorStr == "<=")
            {
                return BinaryOperator.LessEqual;
            }

            if (operatorStr == ">")
            {
                return BinaryOperator.Greater;
            }

            if (operatorStr == ">=")
            {
                return BinaryOperator.GreaterEqual;
            }

            if (operatorStr == "==")
            {
                return BinaryOperator.Equality;
            }

            if (operatorStr == "!=")
            {
                return BinaryOperator.Inequality;
            }

            throw new ArgumentException(string.Format("Invalid binary operator [{0}]", operatorStr));
        }

        #endregion
    }
}
