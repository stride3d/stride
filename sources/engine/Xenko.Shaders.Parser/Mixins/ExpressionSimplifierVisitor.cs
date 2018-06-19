// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Shaders.Parser.Mixins
{
    internal class ExpressionSimplifierVisitor : ShaderWalker
    {
        private readonly ExpressionEvaluator evaluator;

        public ExpressionSimplifierVisitor()
            : base(true, true)
        {
            evaluator = new ExpressionEvaluator();
        }

        public void Run(Shader shader)
        {
            Visit(shader);
        }

        public override void Visit(StatementList statementList)
        {
            for (int i = 0; i < statementList.Count; i++)
            {
                var statement = statementList[i];
                var ifStatement = statement as IfStatement;
                if (ifStatement != null)
                {
                    var result = evaluator.Evaluate(ifStatement.Condition);
                    if (result.HasErrors)
                    {
                        continue;
                    }
                    statementList[i] = result.Value == 1.0 ? ifStatement.Then : ifStatement.Else;
                    if (statementList[i] == null)
                    {
                        statementList.RemoveAt(i);
                    }
                    i--;
                }
            }

            base.Visit(statementList);
        }
    }
}
