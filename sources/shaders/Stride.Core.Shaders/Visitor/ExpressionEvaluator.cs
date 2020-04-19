// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// An expression evaluator.
    /// </summary>
    public class ExpressionEvaluator : ShaderWalker
    {
        private static readonly List<string> hlslScalarTypeNames =
            new List<string>
                {
                    "bool",
                    "int",
                    "uint",
                    "dword",
                    "half",
                    "float",
                    "double",
                    "min16float",
                    "min10float",
                    "min16int",
                    "min12int",
                    "min16uint"
                };

        private readonly Stack<double> values;

        private ExpressionResult result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
        /// </summary>
        public ExpressionEvaluator() : base(false, false)
        {
            values = new Stack<double>();
        }

        /// <summary>
        /// Evaluates the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>Result of the expression evaluated</returns>
        public ExpressionResult Evaluate(Expression expression)
        {
            values.Clear();
            result = new ExpressionResult();

            // Small optim, if LiteralExpression, we perform a direct eval.
            var literalExpression = expression as LiteralExpression;
            if (literalExpression != null)
            {
                Visit(literalExpression);
            }
            else
            {
                VisitDynamic(expression);
            }

            if (values.Count == 1)
                result.Value = values.Pop();
            else
            {
                result.Error("Cannot evaluate expression {0}", expression.Span, expression);
            }
            return result;
        }

        public override void DefaultVisit(Node node)
        {
            var expression = node as Expression;
            if (expression != null)
            {
                if (!(expression is BinaryExpression || expression is MethodInvocationExpression || expression is VariableReferenceExpression || expression is LiteralExpression || expression is ParenthesizedExpression || expression is UnaryExpression))
                    result.Error("Expression evaluation [{0}] is not supported", expression.Span, expression);
            }
        }

        /// <inheritdoc/>
        public override void Visit(BinaryExpression binaryExpression)
        {
            base.Visit( binaryExpression);

            if (values.Count < 2)
            {
                return;
            }

            var rightValue = values.Pop();
            var leftValue = values.Pop();

            var resultValue = 0.0;

            switch (binaryExpression.Operator)
            {
                case BinaryOperator.Plus:
                    resultValue = leftValue + rightValue;
                    break;
                case BinaryOperator.Minus:
                    resultValue = leftValue - rightValue;
                    break;
                case BinaryOperator.Multiply:
                    resultValue = leftValue*rightValue;
                    break;
                case BinaryOperator.Divide:
                    resultValue = leftValue/rightValue;
                    break;
                case BinaryOperator.Modulo:
                    resultValue = leftValue%rightValue;
                    break;
                case BinaryOperator.LeftShift:
                    resultValue = (int) leftValue << (int) rightValue;
                    break;
                case BinaryOperator.RightShift:
                    resultValue = (int) leftValue >> (int) rightValue;
                    break;
                case BinaryOperator.BitwiseOr:
                    resultValue = ((int) leftValue) | ((int) rightValue);
                    break;
                case BinaryOperator.BitwiseAnd:
                    resultValue = ((int) leftValue) & ((int) rightValue);
                    break;
                case BinaryOperator.BitwiseXor:
                    resultValue = ((int) leftValue) ^ ((int) rightValue);
                    break;
                case BinaryOperator.LogicalAnd:
                    resultValue = leftValue != 0.0 && rightValue != 0.0 ? 1.0 : 0.0;
                    break;
                case BinaryOperator.LogicalOr:
                    resultValue = leftValue != 0.0 || rightValue != 0.0 ? 1.0 : 0.0;
                    break;
                case BinaryOperator.GreaterEqual:
                    resultValue = leftValue >= rightValue ? 1.0 : 0.0;
                    break;
                case BinaryOperator.Greater:
                    resultValue = leftValue > rightValue ? 1.0 : 0.0;
                    break;
                case BinaryOperator.Less:
                    resultValue = leftValue < rightValue ? 1.0 : 0.0;
                    break;
                case BinaryOperator.LessEqual:
                    resultValue = leftValue <= rightValue ? 1.0 : 0.0;
                    break;
                case BinaryOperator.Equality:
                    resultValue = leftValue == rightValue ? 1.0 : 0.0;
                    break;
                case BinaryOperator.Inequality:
                    resultValue = leftValue != rightValue ? 1.0 : 0.0;
                    break;
                default:
                    result.Error("Binary operator [{0}] is not supported", binaryExpression.Span, binaryExpression);
                    break;
            }

            values.Push(resultValue);
        }

        /// <inheritdoc/>
        public override void Visit(MethodInvocationExpression methodInvocationExpression)
        {
            if (methodInvocationExpression.Target is TypeReferenceExpression)
            {
                var methodName = (methodInvocationExpression.Target as TypeReferenceExpression).Type.Name.Text;
                if (hlslScalarTypeNames.Contains(methodName))
                {
                    var evaluator = new ExpressionEvaluator();
                    var subResult = evaluator.Evaluate(methodInvocationExpression.Arguments[0]);
                    values.Push(subResult.Value);
                    return;
                }
            }

            result.Error("Method invocation expression evaluation [{0}] is not supported", methodInvocationExpression.Span, methodInvocationExpression);
        }

        /// <inheritdoc/>
        public override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);

            var variableDeclaration = variableReferenceExpression.TypeInference.Declaration as Variable;
            if (variableDeclaration == null)
            {
                result.Error("Unable to find variable [{0}]", variableReferenceExpression.Span, variableReferenceExpression);
            }
            else if (variableDeclaration.InitialValue == null || !variableDeclaration.Qualifiers.Contains(StorageQualifier.Const))
            {
                result.Error("Variable [{0}] used in expression is not constant", variableReferenceExpression.Span, variableDeclaration);
            }
            else
            {
                var evaluator = new ExpressionEvaluator();
                var subResult = evaluator.Evaluate(variableDeclaration.InitialValue);
                subResult.CopyTo(result);

                if (subResult.HasErrors)
                {
                    values.Push(0.0);
                }
                else
                {
                    values.Push(subResult.Value);
                }
            }
        }

        /// <inheritdoc/>
        public override void Visit(LiteralExpression literalExpression)
        {
            try
            {
                var value = Convert.ToDouble(literalExpression.Literal.Value, CultureInfo.InvariantCulture);
                values.Push(value);
            }
            catch (Exception)
            {
                result.Error("Unable to convert value [{0}] to double", literalExpression.Span, literalExpression.Literal.Value);
            }
        }

        /// <inheritdoc/>
        public override void Visit(UnaryExpression unaryExpression)
        {
            base.Visit(unaryExpression);

            if (values.Count == 0)
            {
                return;
            }

            var value = values.Pop();

            switch (unaryExpression.Operator)
            {
                case UnaryOperator.Plus:
                    values.Push(value);
                    break;
                case UnaryOperator.Minus:
                    values.Push(-value);
                    break;
                case UnaryOperator.PreIncrement:
                case UnaryOperator.PostIncrement:
                    // TODO Pre/Post increment/decrement are not correctly handled
                    value++;
                    values.Push(value);
                    break;
                case UnaryOperator.PreDecrement:
                case UnaryOperator.PostDecrement:
                    value--;
                    values.Push(value);
                    break;
                case UnaryOperator.LogicalNot:
                    values.Push(value == 0.0 ? 1.0 : 0.0);
                    break;
                default:
                    result.Error("Unary operator [{0}] is not supported", unaryExpression.Span, unaryExpression);
                    values.Push(0);
                    break;
            }
        }
    }
}
