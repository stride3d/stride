// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Shaders.Parser.Mixins
{
    internal class StreamFieldVisitor : ShaderRewriter
    {
        private Variable typeInference = null;

        private Expression arrayIndex;

        public StreamFieldVisitor(Variable variable, Expression index = null)
            : base(false, false)
        {
            typeInference = variable;
            arrayIndex = index;
        }

        public Expression Run(Expression expression)
        {
            return (Expression)VisitDynamic(expression);
        }

        private Expression ProcessExpression(Expression expression)
        {
            if (expression.TypeInference.TargetType != null && expression.TypeInference.TargetType.IsStreamsType())
            {
                var mre = new MemberReferenceExpression(expression, typeInference.Name) { TypeInference = { Declaration = typeInference, TargetType = typeInference.Type.ResolveType() } };
                if (arrayIndex == null)
                    return mre;
                else
                {
                    var ire = new IndexerExpression(mre, arrayIndex);
                    return ire;
                }
            }

            return expression;
        }

        public override Node Visit(VariableReferenceExpression variableReferenceExpression)
        {
            var expression = (Expression)base.Visit(variableReferenceExpression);
            return ProcessExpression(expression);
        }

        public override Node Visit(MemberReferenceExpression memberReferenceExpression)
        {
            var expression = (Expression)base.Visit(memberReferenceExpression);
            return ProcessExpression(expression);
        }

        public override Node Visit(IndexerExpression indexerExpression)
        {
            var expression = (Expression)base.Visit(indexerExpression);
            return ProcessExpression(expression);
        }
    }
}
