// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Convertor
{
    internal class BreakContinueVisitor : ShaderWalker
    {
        /// <summary>
        /// the logger
        /// </summary>
        private ParsingResult parserResult;

        /// <summary>
        /// the keyword to look after
        /// </summary>
        private string keyword;

        /// <summary>
        /// list of the "scopes" ie. where a break/continue test has to be performed
        /// </summary>
        private List<List<Statement>> scopeList = new List<List<Statement>>();

        /// <summary>
        /// current stack of "scopes"
        /// </summary>
        private Stack<Statement> containerStack = new Stack<Statement>();

        public BreakContinueVisitor()
            : base(false, true)
        {
            parserResult = new ParsingResult();
        }

        public bool Run(ForStatement forStatement, Variable breakFlag, string keywordName, ParsingResult logger)
        {
            keyword = keywordName;

            VisitDynamic(forStatement.Body);

            if (logger != null)
                parserResult.CopyTo(logger);

            if (parserResult.HasErrors)
                return false;

            TransformBreaks(breakFlag);
            
            return scopeList.Count > 0;
        }
        
        public override void Visit(KeywordExpression expression)
        {
            if (expression.Name.Text == keyword)
            {
                var list = new List<Statement>(containerStack);
                list.Reverse();
                if (ParentNode is ExpressionStatement)
                    list.Add(ParentNode as ExpressionStatement);
                else
                    parserResult.Error("{0} keyword detected, but outside of an ExpressionStatement. It is impossible to unroll the loop", expression.Span, keyword);
                
                scopeList.Add(list);
            }
        }

        public override void Visit(BlockStatement blockStatement)
        {
            containerStack.Push(blockStatement);
            base.Visit(blockStatement);
            containerStack.Pop();
        }

        public override void Visit(WhileStatement whileStatement) { }
        
        public override void Visit(ForStatement forStatement) { }

        public override void Visit(StatementList statementList)
        {
            containerStack.Push(statementList);
            base.Visit(statementList);
            containerStack.Pop();
        }

        public override void Visit(IfStatement ifStatement)
        {
            containerStack.Push(ifStatement);
            base.Visit(ifStatement);
            containerStack.Pop();
        }

        /// <summary>
        /// Inserts the break variable in the flow of the loop
        /// </summary>
        /// <param name="breakFlag">the break variable</param>
        protected void TransformBreaks(Variable breakFlag)
        {
            var breakTest = new UnaryExpression(UnaryOperator.LogicalNot, new VariableReferenceExpression(breakFlag));
            scopeList.Reverse();
            foreach (var breakScope in scopeList)
            {
                for (int i = 0; i < breakScope.Count - 1; ++i)
                {
                    var currentScope = breakScope[i];
                    var nextScope = breakScope[i+1];

                    if (currentScope is StatementList)
                    {
                        var typedScope = currentScope as StatementList;
                        var index = typedScope.Statements.IndexOf(nextScope);
                        if (index == -1)
                        {
                            parserResult.Error("unable to find the next scope when replacing break/continue", nextScope.Span);
                            break;
                        }

                        var testBlock = new IfStatement();
                        testBlock.Condition = breakTest;
                        var thenBlock = new StatementList();
                        for (int j = index + 1; j < typedScope.Statements.Count; ++j)
                            thenBlock.Add(typedScope.Statements[j]);
                        testBlock.Then = thenBlock;

                        typedScope.Statements.RemoveRange(index + 1, typedScope.Statements.Count - index - 1);
                        if (typedScope.Statements.Count > 0 && i != breakScope.Count - 2) // do not add the statements behind the break/continue
                            typedScope.Statements.Add(testBlock);
                    }
                }

                var last = breakScope.LastOrDefault() as ExpressionStatement;
                if (last != null)
                    last.Expression = new AssignmentExpression(AssignmentOperator.Default, new VariableReferenceExpression(breakFlag), new LiteralExpression(true));
            }
        }
    }
}
