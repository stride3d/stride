// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Parser;

namespace Stride.Core.Shaders.Analysis
{
    public class CastAnalysis : Analysis.AnalysisBase
    {
        public CastAnalysis(ParsingResult result) : base(result)
        {
        }

        public override Node Visit(UnaryExpression expression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(expression);

            var unaryType = expression.TypeInference.TargetType;
            var inputType = expression.Expression.TypeInference.TargetType;
            if (unaryType == null || inputType == null)
                return expression;

            if (unaryType == ScalarType.Bool && inputType != ScalarType.Bool && expression.Operator == UnaryOperator.LogicalNot)
                expression.Expression = new MethodInvocationExpression(new TypeReferenceExpression(ScalarType.Bool), expression.Expression) { TypeInference = { TargetType = ScalarType.Bool } };
            return expression;
        }

        public override Node Visit(BinaryExpression expression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(expression);

            var leftType = expression.Left.TypeInference.TargetType;
            var rightType = expression.Right.TypeInference.TargetType;
            var returnType = expression.TypeInference.ExpectedType ?? expression.TypeInference.TargetType;

            bool isNumericOperator = true;

            switch (expression.Operator)
            {
                case BinaryOperator.LogicalAnd:
                case BinaryOperator.LogicalOr:
                    isNumericOperator = false;
                    returnType = GetBinaryImplicitConversionType(expression.Span, leftType, rightType, true);
                    expression.TypeInference.TargetType = returnType;
                    break;
                case BinaryOperator.Less:
                case BinaryOperator.LessEqual:
                case BinaryOperator.Greater:
                case BinaryOperator.GreaterEqual:
                case BinaryOperator.Equality:
                case BinaryOperator.Inequality:
                    isNumericOperator = false;
                    returnType = GetBinaryImplicitConversionType(expression.Span, leftType, rightType, false);

                    TypeBase resultType = ScalarType.Bool;
                    if (returnType is VectorType)
                    {
                        resultType = new VectorType(ScalarType.Bool, ((VectorType)returnType).Dimension);
                    }
                    else if (returnType is MatrixType)
                    {
                        var matrixType = (MatrixType)returnType;
                        resultType = new MatrixType(ScalarType.Bool, matrixType.RowCount, matrixType.ColumnCount);
                    }
                    expression.TypeInference.TargetType = resultType;
                    break;
            }

            if (returnType != null)
            {
                if (returnType == ScalarType.Bool && isNumericOperator)
                {
                    var typeToCheck = leftType ?? rightType;
                    if (typeToCheck != null)
                        return ConvertExpressionToBool(expression, typeToCheck);
                } 
            }

            if (!isNumericOperator || CastHelper.NeedConvertForBinary(leftType, returnType))
                expression.Left = Cast(leftType, returnType, expression.Left);
            if (!isNumericOperator || CastHelper.NeedConvertForBinary(rightType, returnType))
                expression.Right = Cast(rightType, returnType, expression.Right);

            return expression;
        }

        private Expression ConvertExpressionToBool(Expression expression, TypeBase typeToCheck)
        {
            if (typeToCheck != ScalarType.Bool)
                return new MethodInvocationExpression(new TypeReferenceExpression(ScalarType.Bool), expression);
            return expression;
        }


        public override Node Visit(IfStatement ifStatement)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(ifStatement);

            var conditionType = ifStatement.Condition.TypeInference.TargetType;
            if (!(ifStatement.Condition is BinaryExpression || ifStatement.Condition is UnaryExpression))
            {
                ifStatement.Condition = ConvertExpressionToBool(ifStatement.Condition, conditionType);
            }

            return ifStatement;
        }

        public override Node Visit(ConditionalExpression conditionalExpression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(conditionalExpression);

            var leftType = conditionalExpression.Left.TypeInference.TargetType;
            var rightType = conditionalExpression.Right.TypeInference.TargetType;

            if (leftType == null || (leftType is ScalarType && !(rightType is ScalarType)))
            {
                conditionalExpression.Left = Cast(leftType, rightType, conditionalExpression.Left);
            }
            else
            {
                conditionalExpression.Right = Cast(rightType, leftType, conditionalExpression.Right);
            }

            return conditionalExpression;
        }


        private Expression Cast(TypeBase fromType, TypeBase toType, Expression expression)
        {
            if (fromType != null && toType != null)
            {
                if (fromType != toType)
                {
                    var castMethod = new MethodInvocationExpression(new TypeReferenceExpression(toType));
                    castMethod.Arguments.Add(expression);
                    expression = castMethod;
                    expression.TypeInference.TargetType = toType;
                }
            }

            return expression;
        }

        public override Node Visit(ReturnStatement returnStatement)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(returnStatement);

            if (returnStatement.Value != null)
            {
                var expressionType = returnStatement.Value.TypeInference.TargetType;
                if (expressionType != null)
                    returnStatement.Value = Cast(expressionType, returnStatement.Value.TypeInference.ExpectedType ?? expressionType, returnStatement.Value);
            }

            return returnStatement;
        }

        public override Node Visit(Variable variable)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(variable);

            if (variable.InitialValue != null)
            {
                var expressionType = variable.InitialValue.TypeInference.TargetType;
                if (!(expressionType is ObjectType))
                {
                    variable.InitialValue = Cast(expressionType, variable.Type.ResolveType(), variable.InitialValue);
                }
            }

            return variable;
        }

        public override Node Visit(AssignmentExpression expression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(expression);

            var expressionType = expression.Target.TypeInference.TargetType;
            var targetType = expression.Target.TypeInference.ExpectedType ?? expressionType;
            expression.Value = Cast(expression.Value.TypeInference.TargetType, targetType, expression.Value);

            return expression;
        }

        public override Node Visit(IndexerExpression expression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(expression);
            
            var indexerType = expression.Index.TypeInference.TargetType;
            if (indexerType != null)
            {
                var baseType = TypeBase.GetBaseType(indexerType);
                if (baseType == ScalarType.Float || baseType == ScalarType.Double)
                    expression.Index = Cast(indexerType, ScalarType.Int, expression.Index);
            }

            return expression;
        }

        public override Node Visit(MethodInvocationExpression expression)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(expression);

            // Add appropriate cast for all arguments
            for (int i = 0; i < expression.Arguments.Count; ++i)
            {
                var argument = expression.Arguments[i];
                var targetType = argument.TypeInference.TargetType;
                if (targetType != null && !(targetType is ObjectType))
                    expression.Arguments[i] = Cast(targetType, argument.TypeInference.ExpectedType ?? targetType, argument);
            }

            return expression;
        }

        /// <inheritdoc/>
        public override void Run()
        {
            Visit(ParsingResult.Shader);
        }
    }
}
