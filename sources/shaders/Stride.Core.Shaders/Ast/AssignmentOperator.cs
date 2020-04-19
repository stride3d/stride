// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Assignment operator used in assignment expression (a = b) or statements (a = b;)
    /// </summary>
    public enum AssignmentOperator
    {
        /// <summary>
        ///   Operator =
        /// </summary>
        Default, 

        /// <summary>
        ///   Operator +=
        /// </summary>
        Addition, 

        /// <summary>
        ///   Operator -=
        /// </summary>
        Subtraction, 

        /// <summary>
        ///   Operator *=
        /// </summary>
        Multiplication, 

        /// <summary>
        ///   Operator /=
        /// </summary>
        Division, 

        /// <summary>
        ///   Operator %=
        /// </summary>
        Modulo, 

        /// <summary>
        ///   Operator &amp;=
        /// </summary>
        BitwiseAnd, 

        /// <summary>
        ///   Operator |=
        /// </summary>
        BitwiseOr, 

        /// <summary>
        ///   Operator ^=
        /// </summary>
        BitwiseXor, 

        /// <summary>
        ///   Operator &lt;&lt;=
        /// </summary>
        BitwiseShiftLeft, 

        /// <summary>
        ///   Operator >>=
        /// </summary>
        BitwiseShiftRight
    }

    /// <summary>
    /// Helper for <see cref="AssignmentOperator"/>.
    /// </summary>
    public static class AssignmentOperatorHelper
    {
        #region Public Methods

        /// <summary>
        /// Converts from operator to string
        /// </summary>
        /// <param name="assignmentOperator">
        /// The assignment operator.
        /// </param>
        /// <returns>
        /// A string representation of an assignment operator
        /// </returns>
        public static string ConvertToString(this AssignmentOperator assignmentOperator)
        {
            switch (assignmentOperator)
            {
                case AssignmentOperator.Default:
                    return "=";
                case AssignmentOperator.Addition:
                    return "+=";
                case AssignmentOperator.Subtraction:
                    return "-=";
                case AssignmentOperator.Multiplication:
                    return "*=";
                case AssignmentOperator.Division:
                    return "/=";
                case AssignmentOperator.Modulo:
                    return "%=";
                case AssignmentOperator.BitwiseAnd:
                    return "&=";
                case AssignmentOperator.BitwiseOr:
                    return "|=";
                case AssignmentOperator.BitwiseXor:
                    return "^=";
                case AssignmentOperator.BitwiseShiftLeft:
                    return "<<=";
                case AssignmentOperator.BitwiseShiftRight:
                    return ">>=";
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
        /// An assignment operator
        /// </returns>
        public static AssignmentOperator FromString(string operatorStr)
        {
            if (operatorStr == "=")
            {
                return AssignmentOperator.Default;
            }

            if (operatorStr == "+=")
            {
                return AssignmentOperator.Addition;
            }

            if (operatorStr == "-=")
            {
                return AssignmentOperator.Subtraction;
            }

            if (operatorStr == "*=")
            {
                return AssignmentOperator.Multiplication;
            }

            if (operatorStr == "/=")
            {
                return AssignmentOperator.Division;
            }

            if (operatorStr == "%=")
            {
                return AssignmentOperator.Modulo;
            }

            if (operatorStr == "&=")
            {
                return AssignmentOperator.BitwiseAnd;
            }

            if (operatorStr == "|=")
            {
                return AssignmentOperator.BitwiseOr;
            }

            if (operatorStr == "^=")
            {
                return AssignmentOperator.BitwiseXor;
            }

            if (operatorStr == "<<=")
            {
                return AssignmentOperator.BitwiseShiftLeft;
            }

            if (operatorStr == ">>=")
            {
                return AssignmentOperator.BitwiseShiftRight;
            }

            throw new ArgumentException(string.Format("Invalid assigment operator [{0}]", operatorStr));
        }

        #endregion
    }
}
