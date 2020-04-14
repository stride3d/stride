// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser.Mixins
{
    internal class StrideReplaceAppend : ShaderRewriter
    {
        #region Private members

        /// <summary>
        /// List of append methods
        /// </summary>
        private HashSet<MethodInvocationExpression> appendMethodsList;

        /// <summary>
        /// List of output statements replacing the append method
        /// </summary>
        private List<Statement> outputStatements;

        /// <summary>
        /// Variable replacing the stream in the append function
        /// </summary>
        private VariableReferenceExpression outputVre;

        #endregion

        #region Constructor

        public StrideReplaceAppend(HashSet<MethodInvocationExpression> appendList, List<Statement> output, VariableReferenceExpression vre)
            : base(false, false)
        {
            appendMethodsList = appendList;
            outputStatements = output;
            outputVre = vre;
        }

        #endregion

        #region Public method

        public void Run(Node startNode)
        {
            VisitDynamic(startNode);
        }

        #endregion

        #region Protected method

        public override Node Visit(ExpressionStatement expressionStatement)
        {
            base.Visit(expressionStatement);

            if (appendMethodsList.Contains(expressionStatement.Expression))
            {
                var appendMethodCall = expressionStatement.Expression as MethodInvocationExpression;
                var blockStatement = new BlockStatement();
                blockStatement.Statements.AddRange(outputStatements);
                appendMethodCall.Arguments[0] = outputVre;
                blockStatement.Statements.Add(expressionStatement);
                return blockStatement;
            }
            return expressionStatement;
        }

        #endregion
    }
}
