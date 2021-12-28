// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Grammar.Hlsl
{
    public partial class HlslGrammar
    {
        #region Public Methods

        #endregion

        #region Methods

        protected static void CreateListFromNode<T>(ParsingContext context, ParseTreeNode node)
        {
            var list = new List<T>();
            FillListFromNodes(node.ChildNodes, list);
            node.AstNode = list;
        }

        private static void FillListFromNodes<TItem>(IEnumerable<ParseTreeNode> nodes, List<TItem> items)
        {
            foreach (var childNode in nodes)
            {
                items.Add((TItem)childNode.AstNode);
            }
        }

        private NonTerminal CreateScalarTerminal<T>(T scalarType) where T : ScalarType, new()
        {
            return new NonTerminal(
                scalarType.Name,
                (context, node) =>
                {
                    var value = Ast<T>(node);
                    value.Name = new Identifier(scalarType.Name) { Span = SpanConverter.Convert(node.Span) };
                    value.Type = scalarType.Type;
                }) { Rule = Keyword(scalarType.Name) };
        }

        #endregion
    }
}
