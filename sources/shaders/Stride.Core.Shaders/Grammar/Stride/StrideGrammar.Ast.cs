// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;

using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Grammar.Stride
{
    public partial class StrideGrammar
    {
        private static void CreateStreamsType(ParsingContext context, ParseTreeNode parseNode)
        {
            var nextNode = parseNode;
            while (nextNode.Token == null)
            {
                nextNode = nextNode.ChildNodes[0];
            }

            var value = StreamsType.Parse(nextNode.Token.Text);
            parseNode.AstNode = value;
        }

        private static void CreateShaderBlockAst(ParsingContext context, ParseTreeNode node)
        {
            //   [0]        [1]               [2]             [3]             [4]              [5]
            // "class" + type_name + shader_class_base_type.Star() + "{" + scope_declaration.Star() + "}";
            var value = Ast<ShaderClassType>(node);

            ParseClassGenerics((Identifier)node.ChildNodes[1].AstNode, value);
            value.BaseClasses.AddRange((List<ShaderTypeName>)node.ChildNodes[2].AstNode);
            FillListFromNodes(node.ChildNodes[4].ChildNodes, value.Members);
        }

        protected static void ParseClassGenerics(Identifier input, ShaderClassType dest)
        {
            // Parse generic identifier and convert it to simple identifier by adding contraint to the class type
            var genericIdentifier = input as ClassIdentifierGeneric;
            if (genericIdentifier != null)
            {
                foreach (var genericIdentifierItem in genericIdentifier.Generics)
                {
                    dest.ShaderGenerics.Add(genericIdentifierItem);
                }
                input = new Identifier(input.Text) { Span = genericIdentifier.Span };
            }
            dest.Name = input;
        }

        private static void CreateClassIdentifierGenericAst(ParsingContext context, ParseTreeNode node)
        {
            // identifier_generic.Rule = 
            //       [0]      [1]                  [2]                  [3]
            //   identifier 
            // | identifier + "<" + class_identifier_generic_parameter_list + ">";
            var identifier = (Identifier)node.ChildNodes[0].AstNode;

            if (node.ChildNodes.Count == 4)
            {
                var value = Ast<ClassIdentifierGeneric>(node);
                value.IsSpecialReference = true;
                value.Text = identifier.Text;
                value.Generics.AddRange((List<Variable>)node.ChildNodes[2].AstNode);
            }
            else
            {
                node.AstNode = identifier;
            }
        }

        protected override void CreateStorageQualifier(ParsingContext context, ParseTreeNode node)
        {
            var qualifier = AstCompositeEnum<Qualifier>(node);

            if (node.ChildNodes.Count == 1)
            {
                qualifier = StrideStorageQualifier.Parse(node.ChildNodes[0].Token.Text);
                qualifier.Span = SpanConverter.Convert(node.Span);
            }

            // Use Hlsl Storage Qualifiers to parse the qualifier
            node.AstNode = qualifier;
        }

        private static void CreateSemanticTypeAst(ParsingContext context, ParseTreeNode node)
        {
            Ast<SemanticType>(node);
        }

        private static void CreateLinkTypeAst(ParsingContext context, ParseTreeNode node)
        {
            Ast<LinkType>(node);
        }

        private static void CreateStreamNameAst(ParsingContext context, ParseTreeNode node)
        {
            Ast<MemberName>(node);
        }

        private static void CreateVarTypeAst(ParsingContext context, ParseTreeNode node)
        {
            Ast<VarType>(node);
        }

        private static void CreateForEachStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ForEachStatement>(node);

            //// for_statement.Rule = _
            ////               [0]      [1]    [2]        [3]           [4]          [5]      [6]       [7]
            ////   Keyword("foreach") + "(" + type + identifier + Keyword("in") + expression + ")" + statement;
            value.Variable = new Variable { Type = (TypeBase)node.ChildNodes[2].AstNode, Name = (Identifier)node.ChildNodes[3].AstNode };
            value.Collection = (Expression)node.ChildNodes[5].AstNode;
            value.Body = (Statement)node.ChildNodes[7].AstNode;
        }

        protected override void CreateIdentifierSubGenericAst(ParsingContext context, ParseTreeNode node)
        {
            base.CreateIdentifierSubGenericAst(context, node);
            if ( node.AstNode is Literal)
            {
                node.AstNode = new LiteralIdentifier((Literal)node.AstNode);
            }
            else if (node.AstNode is TypeBase)
            {
                node.AstNode = new TypeIdentifier((TypeBase)node.AstNode);
            }
        }

        protected override void CreateConstantBufferTypeAst(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = StrideConstantBufferType.Parse(node.ChildNodes[0].Token.Text);
        }

        protected void CreateClassIdentifierSubGenericAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<Variable>(node);
            
            //// class_identifier_sub_generic.Rule =
            ////  [0]       [1]
            //// type + identifier;
            
            value.Name = (Identifier)node.ChildNodes[1].AstNode;
            value.Type = (TypeBase)node.ChildNodes[0].AstNode;
            
            /*base.CreateIdentifierSubGenericAst(context, node);
            if (node.AstNode is Literal)
            {
                node.AstNode = new LiteralIdentifier((Literal)node.AstNode);
            }
            else if (node.AstNode is TypeBase)
            {
                node.AstNode = new TypeIdentifier((TypeBase)node.AstNode);
            }*/
        }

        private static void CreateClassTypeAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ShaderTypeName>(node);
            value.Name = (Identifier)node.ChildNodes[0].AstNode;
        }

        protected static void CreateShaderClassBaseTypeAst(ParsingContext context, ParseTreeNode node)
        {
            ////           [0]
            //// (":" + shader_type_name).Q();
            if (node.ChildNodes[0].ChildNodes.Count == 1)
                node.AstNode = node.ChildNodes[0].ChildNodes[0].AstNode;
            else
                node.AstNode = new List<ShaderTypeName>();
        }

        private static void CreateParametersAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ParametersBlock>(node);
            //                           [0]                        [1]              [2]            [3]
            // params_block.Rule = attribute_qualifier_pre + Keyword("params") + identifier + block_statement;

            // value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
            value.Name = (Identifier)node.ChildNodes[2].AstNode;
            value.Body = (BlockStatement)node.ChildNodes[3].AstNode;
        }

        private static void CreateEffectBlockAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<EffectBlock>(node);
            //                               [0]                        [1]                      [2]             [3]             [4]           
            // shader_block.Rule = attribute_qualifier_pre + Keyword("partial").Opt() + Keyword("shader") + identifier_raw + block_statement;

            // value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
            value.IsPartial = node.ChildNodes[1].ChildNodes.Count == 1;
            value.Name = (Identifier)node.ChildNodes[3].AstNode;
            value.Body = (BlockStatement)node.ChildNodes[4].AstNode;
        }

        private static void CreateMixinStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<MixinStatement>(node);
            //                                [0]               [1]              [2]        [3]
            //mixin_statement.Rule =   Keyword("mixin") + Keyword("compose") + expression + ";"
            //                       | Keyword("mixin") + Keyword("remove") + expression + ";"
            //                       | Keyword("mixin") + Keyword("macro") + expression + ";"
            //                       | Keyword("mixin") + Keyword("mixin") + expression + ";"
            //                       | Keyword("mixin") + Keyword("clone") + ";"
            //                       | Keyword("mixin") + expression + ";";

            value.Value = (node.ChildNodes[1].AstNode as Expression);
            if (value.Value == null)
            {
                var typeName = node.ChildNodes[1].Term.Name;
                MixinStatementType type;
                Enum.TryParse(typeName, true, out type);
                value.Type = type;

                if (type != MixinStatementType.Clone)
                {
                    value.Value = (Expression)node.ChildNodes[2].AstNode;
                }
            }
        }

        private static void CreateUsingStatement(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<UsingStatement>(node);

            //                               [0]                 [1]           
            //using_statement.Rule = Keyword("using") + identifier_or_dot + ";";
            value.Name = (Identifier)node.ChildNodes[1].AstNode;
        }

        private static void CreateUsingParametersStatement(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<UsingParametersStatement>(node);

            //                                     [0]                 [1]              [2]          [3]
            //using_params_statement.Rule =   Keyword("using") + Keyword("params") + expression + ";"
            //                              | Keyword("using") + Keyword("params") + expression + block_statement;
            value.Name = (Expression)node.ChildNodes[2].AstNode;
            if (node.ChildNodes.Count == 4)
            {
                value.Body = (BlockStatement)node.ChildNodes[3].AstNode;
            }
        }

        private static void CreateEnumBlockAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<EnumType>(node);
            //                        [0]                      [1]              [2]          [3]       [4]
            //enum_block.Rule = attribute_qualifier_pre + Keyword("enum") + identifier_raw + "{" + enum_item_list + "}";
            value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
            value.Name = (Identifier)node.ChildNodes[2].AstNode;
            value.Values.AddRange((List<Expression>)node.ChildNodes[4].AstNode);
        }

        private static void CreateEnumItemAst(ParsingContext context, ParseTreeNode node)
        {
            //                    [0]       [1]      [2]
            //enum_item.Rule = identifier + "=" + expression
            //                 | identifier;
            if (node.ChildNodes.Count == 1)
            {
                var value = Ast<VariableReferenceExpression>(node);
                value.Name = (Identifier)node.ChildNodes[0].AstNode;
            }
            else
            {
                var value = Ast<AssignmentExpression>(node);
                value.Target = new VariableReferenceExpression((Identifier)node.ChildNodes[0].AstNode);
                value.Operator = AssignmentOperator.Default;
                value.Value = (Expression)node.ChildNodes[2].AstNode;
            }
        }

        private static void CreateForEachParamsStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ForEachStatement>(node);
            ////                                         [0]          [1]         [2]                  [3]               [4]      [5]
            //// foreach_params_statement.Rule = Keyword("foreach") + "(" + Keyword("params") + conditional_expression + ")" + statement;
            value.Collection = (Expression)node.ChildNodes[3].AstNode;
            value.Body = (Statement)node.ChildNodes[5].AstNode;
        }

        private static void CreateNamespaceBlockAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<NamespaceBlock>(node);
            ////                                  [0]                [1]                [2]       
            //// namespace_block.Rule = Keyword("namespace") + identifier_or_dot + toplevel_declaration_block;
            value.Name = (Identifier)node.ChildNodes[1].AstNode;
            value.Body = (List<Node>)node.ChildNodes[2].AstNode;
        }

        private static void CreateDeclarationBlockAst(ParsingContext context, ParseTreeNode node)
        {
            //                                   [0]               [1]
            // toplevel_declaration_block.Rule = "{" + toplevel_declaration_list + "}";
            node.AstNode = (List<Node>)node.ChildNodes[1].AstNode;
        }

        private static void CreateConstantBufferNameAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<Identifier>(node);
            value.Text = string.Join(".", node.ChildNodes.Select(x => ((Identifier)x.AstNode).Text));
        }
    }
}

