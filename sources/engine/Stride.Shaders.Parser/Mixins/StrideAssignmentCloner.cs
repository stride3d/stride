// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Mixins
{
    /// <summary>
    /// Class used to clone an expression without the references it may contain
    /// </summary>
    internal static class StrideAssignmentCloner
    {
        public static Expression Run(Expression expression)
        {
            return Clone(expression);
        }

        private static Expression Clone(Expression expression)
        {
            if (expression is ArrayInitializerExpression)
                return Clone((ArrayInitializerExpression)expression);
            if (expression is BinaryExpression)
                return Clone((BinaryExpression)expression);
            if (expression is ConditionalExpression)
                return Clone((ConditionalExpression)expression);
            if (expression is EmptyExpression)
                return Clone((EmptyExpression)expression);
            if (expression is ExpressionList)
                return Clone((ExpressionList)expression);
            if (expression is IndexerExpression)
                return Clone((IndexerExpression)expression);
            if (expression is KeywordExpression)
                return Clone((KeywordExpression)expression);
            if (expression is LiteralExpression)
                return Clone((LiteralExpression)expression);
            if (expression is MemberReferenceExpression)
                return Clone((MemberReferenceExpression)expression);
            if (expression is MethodInvocationExpression)
                return Clone((MethodInvocationExpression)expression);
            if (expression is ParenthesizedExpression)
                return Clone((ParenthesizedExpression)expression);
            if (expression is TypeReferenceExpression)
                return Clone((TypeReferenceExpression)expression);
            if (expression is UnaryExpression)
                return Clone((UnaryExpression)expression);
            if (expression is VariableReferenceExpression)
                return Clone((VariableReferenceExpression)expression);
            return null;
        }

        private static ArrayInitializerExpression Clone(ArrayInitializerExpression expression)
        {
            var aie = new ArrayInitializerExpression();
            foreach (var item in expression.Items)
                aie.Items.Add(Clone(item));
            return aie;
        }

        private static BinaryExpression Clone(BinaryExpression expression)
        {
            return new BinaryExpression(expression.Operator, Clone(expression.Left), Clone(expression.Right));
        }

        private static ConditionalExpression Clone(ConditionalExpression expression)
        {
            return new ConditionalExpression(Clone(expression.Condition), Clone(expression.Left), Clone(expression.Right));
        }

        private static EmptyExpression Clone(EmptyExpression expression)
        {
            return expression;
        }

        private static ExpressionList Clone(ExpressionList expression)
        {
            var parameters = new Expression[expression.Count];
            for (int i = 0; i < expression.Count; ++i)
                parameters[i] = Clone(expression[i]);
            return new ExpressionList(parameters);
        }

        private static IndexerExpression Clone(IndexerExpression expression)
        {
            var ire = new IndexerExpression(Clone(expression.Target), Clone(expression.Index));
            if (expression.TypeInference.TargetType != null && expression.TypeInference.TargetType.IsStreamsType())
                ire.TypeInference.TargetType = expression.TypeInference.TargetType;
            return ire;
        }

        private static KeywordExpression Clone(KeywordExpression expression)
        {
            return expression;
        }

        private static LiteralExpression Clone(LiteralExpression expression)
        {
            return expression;
        }

        private static MemberReferenceExpression Clone(MemberReferenceExpression expression)
        {
            var mre = new MemberReferenceExpression(Clone(expression.Target), expression.Member);
            if (expression.TypeInference.TargetType != null && expression.TypeInference.TargetType.IsStreamsType())
                mre.TypeInference.TargetType = expression.TypeInference.TargetType;
            return mre;
        }

        private static MethodInvocationExpression Clone(MethodInvocationExpression expression)
        {
            var parameters = new Expression[expression.Arguments.Count];
            for (int i = 0; i < expression.Arguments.Count; ++i)
                parameters[i] = Clone(expression.Arguments[i]);
            return new MethodInvocationExpression(Clone(expression.Target), parameters);
        }

        private static ParenthesizedExpression Clone(ParenthesizedExpression expression)
        {
            return new ParenthesizedExpression(Clone(expression.Content));
        }

        private static TypeReferenceExpression Clone(TypeReferenceExpression expression)
        {
            return expression;
        }

        private static UnaryExpression Clone(UnaryExpression expression)
        {
            return new UnaryExpression(expression.Operator, Clone(expression.Expression));
        }

        private static VariableReferenceExpression Clone(VariableReferenceExpression expression)
        {
            var vre = new VariableReferenceExpression(expression.Name);
            if (expression.TypeInference.TargetType != null && expression.TypeInference.TargetType.IsStreamsType())
                vre.TypeInference.TargetType = expression.TypeInference.TargetType;
            return vre;
        }
    }
}
