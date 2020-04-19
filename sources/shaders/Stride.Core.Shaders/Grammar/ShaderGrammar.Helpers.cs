// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Grammar
{
    public abstract partial class ShaderGrammar
    {
        #region Public Methods

        protected KeyTerm Keyword(string term, bool isCaseInsensitive = false)
        {
            var keyTerm = ToTerm(term);
            keyTerm.AstNodeConfig = new TokenInfo { TokenCategory = TokenCategory.Keyword, IsCaseInsensitive =  isCaseInsensitive};
            return keyTerm;
        }

        protected KeyTerm TypeName(string term)
        {
            var keyTerm = ToTerm(term);
            keyTerm.AstNodeConfig = new TokenInfo { TokenCategory = TokenCategory.Typename};
            return keyTerm;
        }

        protected void Term(Terminal terminal, TokenCategory category, TokenType type)
        {
            var config = (TokenInfo)terminal.AstNodeConfig;
            if (config == null)
            {
                config = new TokenInfo { TokenCategory = category };
                terminal.AstNodeConfig = config;
            }
            TokenTypeToTerminals.Add(type, terminal);
        }

        protected KeyTerm Op(string term, TokenType type)
        {
            var keyTerm = ToTerm(term);
            keyTerm.AstNodeConfig = new TokenInfo { TokenCategory = TokenCategory.Operator};
            TokenTypeToTerminals.Add(type, keyTerm);
            return keyTerm;
        }

        protected KeyTerm Punc(string term, TokenType type)
        {
            var keyTerm = ToTerm(term);
            keyTerm.AstNodeConfig = new TokenInfo { TokenCategory = TokenCategory.Puntuation };
            TokenTypeToTerminals.Add(type, keyTerm);
            return keyTerm;
        }

        #endregion

        #region Methods

        protected static T GetOptional<T>(ParseTreeNode node) where T : class
        {
            if (node.ChildNodes.Count == 1) return (T)node.ChildNodes[0].AstNode;
            return null;
        }

        protected static T CreateEnumFlags<T>(T initialValue, IEnumerable<ParseTreeNode> enumValues) where T : CompositeEnum, new()
        {
            T value = initialValue;
            foreach (var storageClassItem in enumValues)
            {
                value = CompositeEnum.OperatorOr(value, (T)storageClassItem.AstNode);
            }

            return value;
        }

        protected BnfExpression CreateRuleFromObjectTypes(params ObjectType[] types)
        {
            return CreateRuleFromObjectTypes(types.AsEnumerable());
        }

        protected BnfExpression CreateRuleFromObjectTypes<T>(IEnumerable<T> types) where T : ObjectType
        {
            BnfExpression rule = null;

            foreach (var type in types)
            {
                if (rule == null)
                {
                    rule = TypeName(type.Name);
                }
                else
                {
                    rule |= TypeName(type.Name);
                }

                // Add alternative names as well
                foreach (var alternativeName in type.AlternativeNames)
                    rule |= TypeName(alternativeName);
            }
            return rule;
        }
        
        private static void CreateListFromNode<T>(ParsingContext context, ParseTreeNode node)
        {
            var list = new List<T>();
            FillListFromNodes(node.ChildNodes, list);
            node.AstNode = list;
        }

        protected static void FillListFromNodes<TItem>(IEnumerable<ParseTreeNode> nodes, IList<TItem> items)
        {
            foreach (var childNode in nodes)
            {
                if (childNode.AstNode != null)
                    items.Add((TItem)childNode.AstNode);
            }
        }

        protected static void FillTokenText(ParseTreeNode node, StringBuilder builder)
        {
            if (node.Token != null) builder.Append(node.Token.Text);

            foreach (var subNode in node.ChildNodes)
            {
                FillTokenText(subNode, builder);
            }
        }

        protected static string GetTokenText(ParseTreeNode node)
        {
            var builder = new StringBuilder();
            FillTokenText(node, builder);
            return builder.ToString();
        }

        protected static string ParseStringFromNode(ParseTreeNode node)
        {
            while (node.ChildNodes.Count == 1)
            {
                node = node.ChildNodes[0];
            }

            return (string)node.AstNode;
        }

        protected static NonTerminal T(string name)
        {
            return new NonTerminal(name);
        }

        protected static NonTerminal T(string name, AstNodeCreator nodeCreator)
        {
            return new NonTerminal(name, nodeCreator);
        }

        protected static NonTerminal TT(string name)
        {
            var nonTerminal = T(name);
            nonTerminal.Flags = TermFlags.IsTransient | TermFlags.NoAstNode;
            return nonTerminal;
        }
        #endregion
    }
}
