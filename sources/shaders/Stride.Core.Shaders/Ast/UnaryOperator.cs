// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Unary operator used in all binary expressions (except assignment expression).
    /// </summary>
    public enum UnaryOperator
    {
        /// <summary>
        /// Logical not operator "!"
        /// </summary>
        LogicalNot,

        /// <summary>
        /// Bitwise not operator "~"
        /// </summary>
        BitwiseNot,

        /// <summary>
        /// Minus operator "-"
        /// </summary>
        Minus,

        /// <summary>
        /// Plus operator "+"
        /// </summary>
        Plus,

        /// <summary>
        /// Pre-decrement operator "--"
        /// </summary>
        PreDecrement,

        /// <summary>
        /// Pre-inscrment operator "++"
        /// </summary>
        PreIncrement,

        /// <summary>
        /// Post-decrement operator "--"
        /// </summary>
        PostDecrement,

        /// <summary>
        /// Post-increment operator "++"
        /// </summary>
        PostIncrement,
    }

    /// <summary>
    /// Helper for <see cref="UnaryOperator"/>.
    /// </summary>
    public static class UnaryOperatorHelper
    {
        /// <summary>
        /// Converts from string an operator. For post and pre operators, only working for pre.
        /// </summary>
        /// <param name="operatorStr">The operator text.</param>
        /// <exception cref="ArgumentException">If operatorStr is invalid</exception>
        /// <returns>An unary operator</returns>
        public static UnaryOperator FromString(string operatorStr)
        {
            if (operatorStr == "!")
                return UnaryOperator.LogicalNot;
            if (operatorStr == "~")
                return UnaryOperator.BitwiseNot;
            if (operatorStr == "-")
                return UnaryOperator.Minus;
            if (operatorStr == "+")
                return UnaryOperator.Plus;
            if (operatorStr == "--")
                return UnaryOperator.PreDecrement;
            if (operatorStr == "++")
                return UnaryOperator.PreIncrement;
            throw new ArgumentException(string.Format("Invalid unary operator [{0}]", operatorStr));
        }

        /// <summary>
        /// Determines whether [is post fix] [the specified unary operator].
        /// </summary>
        /// <param name="unaryOperator">The unary operator.</param>
        /// <returns>
        ///   <c>true</c> if [is post fix] [the specified unary operator]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPostFix(this UnaryOperator unaryOperator)
        {
            return unaryOperator == UnaryOperator.PostIncrement || unaryOperator == UnaryOperator.PostDecrement;
        }

        /// <summary>
        /// Converts from operator to string
        /// </summary>
        /// <param name="unaryOperator">The unary operator.</param>
        /// <returns>
        /// A string representation of an unary operator
        /// </returns>
        public static string ConvertToString(this UnaryOperator unaryOperator)
        {
            switch (unaryOperator)
            {
                case UnaryOperator.LogicalNot:
                    return "!";
                case UnaryOperator.BitwiseNot:
                    return "~";
                case UnaryOperator.Minus:
                    return "-";
                case UnaryOperator.Plus:
                    return "+";
                case UnaryOperator.PreDecrement:
                case UnaryOperator.PostDecrement:
                    return "--";
                case UnaryOperator.PreIncrement:
                case UnaryOperator.PostIncrement:
                    return "++";
            }
            return string.Empty;
        }
    }
}
