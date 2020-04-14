// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;
using StorageQualifier = Stride.Core.Shaders.Ast.StorageQualifier;

namespace Stride.Core.Shaders.Grammar
{
    /// <summary>
    /// Methods used to create the Abstract Syntax Tree..
    /// </summary>
    public abstract partial class ShaderGrammar
    {
        #region Methods

        /// <summary>
        /// The ast.
        /// </summary>
        /// <param name="node">
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        protected static T Ast<T>(ParseTreeNode node) where T : class, new()
        {
            T value = new T();
            object obj = value;
            node.AstNode = value;
            if (value is Node)
            {
                ((Node)obj).Span = SpanConverter.Convert(node.Span);
            }

            return value;
        }

        /// <summary>
        /// The ast composite enum.
        /// </summary>
        /// <param name="node">
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        protected static T AstCompositeEnum<T>(ParseTreeNode node) where T : CompositeEnum, new()
        {
            var value = Ast<T>(node);
            value.Key = string.Empty;
            value.Values.Add(value);
            return value;
        }

        /// <summary>
        /// The collect qualifiers.
        /// </summary>
        /// <param name="node">
        /// </param>
        /// <returns>
        /// </returns>
        protected static Qualifier CollectQualifiers(ParseTreeNode node)
        {
            var value = Qualifier.None;
            foreach (var subNode in node.ChildNodes)
            {
                value |= (Qualifier)subNode.AstNode;
            }

            if (value != Qualifier.None)
            {
                value.Span = SpanConverter.Convert(node.Span);
            }

            return value;
        }

        /// <summary>
        /// The create array initializer expression ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateArrayInitializerExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            // [0]        [1]           [3]
            // "{" + initializer_list + "}";
            var value = Ast<ArrayInitializerExpression>(node);
            value.Items = (List<Expression>)node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// The create assignement expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAssignementExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // [0]          [1]         [2]
            // expression + operator + expression
            var value = Ast<AssignmentExpression>(node);

            value.Target = GetExpression(node.ChildNodes[0]);
            value.Operator = (AssignmentOperator)node.ChildNodes[1].AstNode;
            value.Value = GetExpression(node.ChildNodes[2]);
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>An expression</returns>
        protected static Expression GetExpression(ParseTreeNode node)
        {
            if (node.AstNode is Identifier)
            {
                return new VariableReferenceExpression((Identifier)node.AstNode) { Span = SpanConverter.Convert(node.Span) };
            }

            return (Expression)node.AstNode;
        }

        /// <summary>
        /// The create assignment operator.
        /// </summary>
        /// <param name="parsingContext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAssignmentOperator(ParsingContext parsingContext, ParseTreeNode node)
        {
            node.AstNode = AssignmentOperatorHelper.FromString(node.ChildNodes[0].Token.Text);
        }

        /// <summary>
        /// The create binary expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateBinaryExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // [0]          [1]         [2]
            // expression + operator + expression
            var value = Ast<BinaryExpression>(node);

            value.Left = (Expression)node.ChildNodes[0].AstNode;
            value.Operator = BinaryOperatorHelper.FromString(node.ChildNodes[1].Token.Text);
            value.Right = (Expression)node.ChildNodes[2].AstNode;
        }

        /// <summary>
        /// The create block statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateBlockStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<BlockStatement>(node);

            // [0]         [1]           [2]
            // "{" + block_item.Star() + "}";
            FillListFromNodes(node.ChildNodes[1].ChildNodes, value.Statements);
        }

        /// <summary>
        /// The create case statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateCaseStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var caseStatement = Ast<CaseStatement>(node);

            //// switch_case_statement.Rule =
            ////       [0]       [1]                   
            ////      "case" + constant_expression + ":" 
            //// | _("default") + ":";
            if (node.ChildNodes.Count == 2)
            {
                caseStatement.Case = (Expression)node.ChildNodes[1].AstNode;
            }
        }

        /// <summary>
        /// The create conditional expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateConditionalExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<ConditionalExpression>(node);

            // [0]                     [1]      [2]                      [3]
            // logical_or_expression + "?" + expression + ":" + conditional_expression;
            value.Condition = (Expression)node.ChildNodes[0].AstNode;
            value.Left = (Expression)node.ChildNodes[2].AstNode;
            value.Right = (Expression)node.ChildNodes[3].AstNode;
        }

        /// <summary>
        /// The create declaration specifier.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateDeclarationSpecifier(ParsingContext context, ParseTreeNode node)
        {
            // declaration_specifiers = 
            // type
            // | storage_qualifier.Plus() + type;
            var storageQualifier = Qualifier.None;
            TypeBase typeBase;
            var i = 0;

            if (node.ChildNodes.Count == 2)
            {
                storageQualifier = CollectQualifiers(node.ChildNodes[0]);
                i++;
            }

            typeBase = (TypeBase)node.ChildNodes[i].AstNode;
            node.AstNode = new Tuple<Qualifier, TypeBase>(storageQualifier, typeBase);
        }

        /// <summary>
        /// The create declaration statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateDeclarationStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var declarationStatement = Ast<DeclarationStatement>(node);
            declarationStatement.Content = (Node)node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create do while statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateDoWhileStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<WhileStatement>(node);

            //// do_switch_statement.Rule =
            ////    [0]     [1]          [2]     [3]     [4] 
            //// _("do") + statement + "while" + "(" + expression + ")" + ";";
            value.Condition = (Expression)node.ChildNodes[4].AstNode;
            value.Statement = (Statement)node.ChildNodes[1].AstNode;
            value.IsDoWhile = true;
        }

        /// <summary>
        /// The create expression or empty ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateExpressionOrEmptyAst(ParsingContext context, ParseTreeNode node)
        {
            if (node.ChildNodes.Count == 1)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
            }

            if (node.AstNode == null)
            {
                Ast<EmptyExpression>(node);
            }
        }

        protected static void CreateEmptyStatementAst(ParsingContext context, ParseTreeNode node)
        {
            Ast<EmptyStatement>(node);
        }

        /// <summary>
        /// The create expression statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateExpressionStatementAst(ParsingContext context, ParseTreeNode node)
        {

            //// expression_statement.Rule = 
            //// empty_statement
            //// | expression + ";";
            if (node.ChildNodes[0].AstNode is EmptyStatement)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
                return;
            }

            var expressionStatement = Ast<ExpressionStatement>(node);
            if (node.ChildNodes[0].AstNode is Expression)
            {
                // Standard expression statement
                expressionStatement.Expression = (Expression)node.ChildNodes[0].AstNode;
            }
            else
            {
                // Expression statement like "break;" "continue;" "discard;"
                // Standard expression statement
                CreateIdentifierAst(context, node.ChildNodes[0]);
                expressionStatement.Expression = new KeywordExpression((Identifier)node.ChildNodes[0].AstNode) { Span = SpanConverter.Convert(node.Span) };
            }
        }

        /// <summary>
        /// The create for statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateForStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ForStatement>(node);

            //// for_statement.Rule = _
            ////      [0]     [1]               [2]                      [3]                [4]        [5]     [6]
            ////   _("for") + "(" + expression_statement     + expression.Q() + ";" + expression.Q() + ")" + statement
            //// | _("for") + "(" + variable_declaration_raw + expression.Q() + ";" + expression.Q() + ")" + statement;
            var start = (Node)node.ChildNodes[2].AstNode;
            if (start is Variable)
            {
                value.Start = new DeclarationStatement { Content = start, Span = start.Span };
            }
            else
            {
                value.Start = (Statement)start;
            }

            value.Condition = GetOptional<Expression>(node.ChildNodes[3]);
            value.Next = GetOptional<Expression>(node.ChildNodes[4]);
            value.Body = (Statement)node.ChildNodes[6].AstNode;
        }

        /// <summary>
        /// The create identifier ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var nextNode = node;

            // Get the deepest valid node (either an AstNode or a Token)
            while (nextNode.AstNode == null && nextNode.ChildNodes.Count > 0)
            {
                nextNode = nextNode.ChildNodes[0];
            }

            node.AstNode = nextNode.AstNode as Identifier;

            // Handle special names (sample, point...)
            if (node.AstNode == null)
            {
                var value = Ast<Identifier>(node);
                value.Text = nextNode.Token.Text;
            }
        }

        /// <summary>
        /// The create identifier indexable ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierIndexableAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // identifier + rankSpecifier.Star();
            var value = Ast<Identifier>(node);
            var identifier = (Identifier)node.ChildNodes[0].AstNode;
            value.Text = identifier.Text;
            value.Indices = new List<Expression>();
            FillListFromNodes(node.ChildNodes[1].ChildNodes, value.Indices);
        }

        /// <summary>
        /// The create identifier list ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierListAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var identifiers = Ast<List<Identifier>>(node);
            FillListFromNodes(node.ChildNodes, identifiers);
        }

        /// <summary>
        /// The create if statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIfStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<IfStatement>(node);

            //// if_statement.Rule =
            ////      [0]    [1]     [2]       [3]     [4]                             [5]      [6]
            ////  _("if") + "(" + expression + ")" + statement
            ////| _("if") + "(" + expression + ")" + statement + PreferShiftHere() + "else" + statement;
            value.Condition = (Expression)node.ChildNodes[2].AstNode;
            value.Then = (Statement)node.ChildNodes[4].AstNode;
            if (node.ChildNodes.Count == 7)
            {
                value.Else = (Statement)node.ChildNodes[6].AstNode;
            }
        }

        /// <summary>
        /// The create indexer expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIndexerExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<IndexerExpression>(node);

            // [0]                 [1]     
            // postfix_expression + array_indexer
            value.Target = (Expression)node.ChildNodes[0].AstNode;
            value.Index = (Expression)node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// The create literal ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateLiteralAst(ParsingContext context, ParseTreeNode node)
        {
            var literalValueNode = node.ChildNodes[0].AstNode;
            if (literalValueNode is Literal)
            {
                node.AstNode = literalValueNode;
            }
            else
            {
                var value = Ast<Literal>(node);
                value.Value = literalValueNode;
                value.Text = GetTokenText(node.ChildNodes[0]);
            }
        }

        /// <summary>
        /// The create literal expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateLiteralExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<LiteralExpression>(node);
            value.Literal = (Literal)node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create member reference expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMemberReferenceExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<MemberReferenceExpression>(node);

            // [0]             [1]      [2]
            // postfix_expression + "." + identifier
            value.Target = (Expression)node.ChildNodes[0].AstNode;
            value.Member = (Identifier)node.ChildNodes[2].AstNode;
        }

        /// <summary>
        /// The create method declaration ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMethodDeclarationAst(ParsingContext context, ParseTreeNode node)
        {
            //// method_declaration_raw + ";";
            node.AstNode = node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create method declaration raw ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMethodDeclarationRawAst(ParsingContext context, ParseTreeNode node)
        {
            //// method_declaration_raw.Rule = 
            ////        [0]                         [1]                 [2]               [3]
            //// attribute_qualifier_pre + declaration_specifiers + method_declarator + method_qualifier_post;
            var methodDeclaration = (MethodDeclaration)node.ChildNodes[2].AstNode;
            node.AstNode = methodDeclaration;
            methodDeclaration.Span = SpanConverter.Convert(node.Span);

            methodDeclaration.Attributes.AddRange((List<AttributeBase>)node.ChildNodes[0].AstNode);
            var declarationSpecifiers = (Tuple<Qualifier, TypeBase>)node.ChildNodes[1].AstNode;
            methodDeclaration.Qualifiers = declarationSpecifiers.Item1;
            methodDeclaration.ReturnType = declarationSpecifiers.Item2;
            methodDeclaration.Qualifiers |= (Qualifier)node.ChildNodes[3].AstNode;

            methodDeclaration.UpdateParameters();
        }

        /// <summary>
        /// The create method declarator ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMethodDeclaratorAst(ParsingContext context, ParseTreeNode node)
        {
            var methodDeclaration = Ast<MethodDeclaration>(node);

            //// method_declarator.Rule = 
            ////      [0]      [1]   [2]               [3]
            ////  identifier + "(" + parameter_list  + ")"
            ////| identifier + "(" + identifier_list + ")"
            ////| identifier + "(" + "void"          + ")"
            ////| identifier + "(" + ")";
            methodDeclaration.Name = (Identifier)node.ChildNodes[0].AstNode;
            if (node.ChildNodes.Count == 4)
            {
                if (node.ChildNodes[2].AstNode is List<Parameter>)
                {
                    methodDeclaration.Parameters = (List<Parameter>)node.ChildNodes[2].AstNode;
                }
                else if (node.ChildNodes[2].AstNode is List<Identifier>)
                {
                    var identifiers = (List<Identifier>)node.ChildNodes[2].AstNode;
                    foreach (var identifier in identifiers)
                    {
                        methodDeclaration.Parameters.Add(new Parameter() { Name = identifier, Span = identifier.Span });
                    }
                }
            }
        }

        /// <summary>
        /// The create method definition ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMethodDefinitionAst(ParsingContext context, ParseTreeNode node)
        {
            //// method_definition.Rule =
            ////       [0]                [1]         [2]              [3]
            //// method_declaration_raw + "{" + block_item.ListOpt() + "}" + semi_opt;
            var methodDefinition = Ast<MethodDefinition>(node);

            ((MethodDeclaration)node.ChildNodes[0].AstNode).CopyTo(methodDefinition);
            methodDefinition.UpdateParameters();

            // [0]         [1]           [2]
            // "{" + block_item.Star() + "}";
            FillListFromNodes(node.ChildNodes[2].ChildNodes, methodDefinition.Body);
        }

        /// <summary>
        /// The create method invoke expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateMethodInvokeExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<MethodInvocationExpression>(node);

            //// method_invoke_expression.Rule 
            // [0]       [1]                [2]
            // = identifier + "(" + argument_expression_list.Q() + ")"; 
            value.Target = GetExpression(node.ChildNodes[0]);

            var arguments = GetOptional<List<Expression>>(node.ChildNodes[2]);
            if (arguments != null)
            {
                value.Arguments = arguments;
            }
        }

        /// <summary>
        /// The create parameter ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected virtual void CreateParameterAst(ParsingContext context, ParseTreeNode node)
        {
            //// parameter_declaration.Rule = 
            ////          [0]                            [1]               [2]                   [3]                      [4]             
            //// attribute_qualifier_pre + parameter_qualifier_pre + parameter_type + indexable_identifier.Opt() + parameter_qualifier_post; 
            var parameter = Ast<Parameter>(node);

            parameter.Attributes.AddRange((List<AttributeBase>)node.ChildNodes[0].AstNode);
            parameter.Type = (TypeBase)node.ChildNodes[2].AstNode;
            parameter.Name = GetOptional<Identifier>(node.ChildNodes[3]);

            // If it is an identifier with indices, transform it to a plain identifier with an array type
            if (parameter.Name != null && parameter.Name.HasIndices)
            {
                // base type of the array will be added by variable declaration
                parameter.Type = new ArrayType { Dimensions = new List<Expression>(parameter.Name.Indices), Type = parameter.Type, Span = parameter.Name.Span };

                parameter.Name.Indices = null;
            }
        }

        /// <summary>
        /// The create parenthesized expression ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateParenthesizedExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ParenthesizedExpression>(node);

            // [0]        [1]                      [2]
            // "(" +   argument_expression_list  + ")"
            value.Content = (Expression)node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// The create postfix unary expression ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreatePostfixUnaryExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<UnaryExpression>(node);

            //// post_incr_decr_expression.Rule = 
            ////        [0]              [1]      
            //// postfix_expression + incr_or_decr;
            value.Expression = (Expression)node.ChildNodes[0].AstNode;
            var operatorText = node.ChildNodes[1].Token.Text;
            value.Operator = (operatorText == "++") ? UnaryOperator.PostIncrement : UnaryOperator.PostDecrement;
        }

        /// <summary>
        /// The create qualifiers.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateQualifiers(ParsingContext context, ParseTreeNode node)
        {
            var qualifier = node.ChildNodes.Count == 0 ? Qualifier.None : CollectQualifiers(node.ChildNodes[0]);
            if (qualifier != Qualifier.None)
            {
                qualifier.Span = SpanConverter.Convert(node.Span);
            }

            node.AstNode = qualifier;
        }

        protected static void CreateQualifiersAst(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = CollectQualifiers(node.ChildNodes[0]);
        }

        /// <summary>
        /// The create rank specifier ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateRankSpecifierAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // [0]      [1]       [2]
            // "[" + expression + "]";
            node.AstNode = node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// The create return statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateReturnStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<ReturnStatement>(node);

            //// _("return") + ";" | _("return") + expression + ";";
            if (node.ChildNodes.Count == 2)
            {
                value.Value = (Expression)node.ChildNodes[1].AstNode;
            }
        }

        /// <summary>
        /// The create shader ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateShaderAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<Shader>(node);
            // For top level node, embed it into a browsable node in order for Irony to browse it.
            node.AstNode = new IronyBrowsableNode(value);
            value.Declarations = (List<Node>)node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateStatementAst(ParsingContext context, ParseTreeNode node)
        {
            // statement.Rule =
            // attribute_list_opt + statement_raw;
            var value = (Statement)node.ChildNodes[1].AstNode;
            node.AstNode = value;
            value.Span = SpanConverter.Convert(node.Span);
            value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create structure ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateStructureAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<StructType>(node);

            //// struct_specifier.Rule = 
            ////     [0]         [1]          [2]          [3]
            ////  "struct" + identifier.Q() + "{" + variable_declaration.ListOpt() + "}"
            value.Name = GetOptional<Identifier>(node.ChildNodes[1]);
            FillListFromNodes(node.ChildNodes[3].ChildNodes, value.Fields);
        }

        /// <summary>
        /// The create switch cast group ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateSwitchCastGroupAst(ParsingContext context, ParseTreeNode node)
        {
            var group = Ast<SwitchCaseGroup>(node);

            //// switch_case_group.Rule = switch_case_statement.Plus() + statement.Plus();
            FillListFromNodes(node.ChildNodes[0].ChildNodes, group.Cases);
            FillListFromNodes(node.ChildNodes[1].ChildNodes, group.Statements);
        }

        /// <summary>
        /// The create switch statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateSwitchStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<SwitchStatement>(node);

            //// switch_statement.Rule =
            ////      [0]       [1]     [2]        [3]   [4]              [5]                 [6]
            ////  _("switch") + "(" + expression + ")" + "{" + switch_case_group.Star() + "}";
            value.Condition = (Expression)node.ChildNodes[2].AstNode;
            FillListFromNodes(node.ChildNodes[5].ChildNodes, value.Groups);
        }

        /// <summary>
        /// The create type name ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateTypeNameAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<TypeName>(node);
            value.Name = (Identifier)node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create type name from token ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateTypeNameFromTokenAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            CreateTypeFromTokenAst<TypeName>(parsingcontext, node);
        }

        protected static void CreateTypeFromTokenAst<T>(ParsingContext parsingcontext, ParseTreeNode node) where T : TypeBase, new()
        {
            if (node.ChildNodes.Count == 1 && node.ChildNodes[0].AstNode is T)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
            }
            else
            {
                var value = Ast<T>(node);
                var nextNode = node;
                while (nextNode.Token == null)
                {
                    nextNode = nextNode.ChildNodes[0];
                }

                value.Name = new Identifier(nextNode.Token.Text) { Span = SpanConverter.Convert(node.Span) };
            }
        }

        /// <summary>
        /// The create unary expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateUnaryExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<UnaryExpression>(node);

            //// unary_expression_raw.Rule = 
            ////        [0]              [1]         [0]              [1]       
            //// incr_or_decr + unary_expression | unary_operator + cast_expression;
            value.Operator = UnaryOperatorHelper.FromString(node.ChildNodes[0].Token.Text);
            value.Expression = (Expression)node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// Creates the variable group ast.
        /// </summary>
        /// <param name="parsingContext">
        /// The parsing context.
        /// </param>
        /// <param name="node">
        /// The tree node.
        /// </param>
        protected static void CreateVariableGroupAst(ParsingContext parsingContext, ParseTreeNode node)
        {
            // variable_group.Rule = 
            // [0]                                 [1]     
            // attribute_qualifier_pre + variable_group_raw

            // This is a type declaration and not a variable
            if (node.ChildNodes[1].AstNode is TypeBase)
            {
                var typeBase = (TypeBase)node.ChildNodes[1].AstNode;
                var attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
                typeBase.Attributes = attributes;
                node.AstNode = typeBase;
            }
            else
            {
                // else this is a standard variable declaration.
                var var = (Variable)node.ChildNodes[1].AstNode;
                var.Attributes.AddRange((List<AttributeBase>)node.ChildNodes[0].AstNode);
                node.AstNode = var;
            }
        }

        /// <summary>
        /// The create variable group raw ast.
        /// </summary>
        /// <param name="parsingContext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateVariableGroupRawAst(ParsingContext parsingContext, ParseTreeNode node)
        {
            var var = Ast<Variable>(node);

            //// [0]                             [1]          
            // declaration_specifiers + variable_declarator_list.Q()  + ";";
            var declarationSpecifiers = (Tuple<Qualifier, TypeBase>)node.ChildNodes[0].AstNode;
            var.Qualifiers = declarationSpecifiers.Item1;
            var.Type = declarationSpecifiers.Item2;

            var declarators = GetOptional<List<Variable>>(node.ChildNodes[1]);

            if (declarators != null)
            {
                var.SubVariables = declarators;

                // Update array type for sub variables
                foreach(var subVariable in declarators)
                {
                    if (subVariable.Type is ArrayType)
                        ((ArrayType)subVariable.Type).Type = var.Type;
                    else
                        subVariable.Type = var.Type;
                }
            }

            // If this is a variable group, check if we can transform it to a single variable declaration.
            if (var.IsGroup)
            {
                // If the variable is a single variable declaration, replace the group
                if (var.SubVariables.Count == 1)
                {
                    var subVariable = var.SubVariables[0];
                    subVariable.MergeFrom(var);
                    node.AstNode = subVariable;
                }
            } 
            else 
            {
                // If variable declarators is 0, check if this is a named struct
                var.Type.Qualifiers = var.Qualifiers;
                if (var.Type is StructType)
                {
                    node.AstNode = var.Type;
                }
                else if (var.Type is InterfaceType)
                {
                    node.AstNode = var.Type;
                }
                else if (var.Type is ClassType)
                {
                    node.AstNode = var.Type;
                }
                else
                {
                    parsingContext.AddParserError("Expecting identifier for variable declaration [{0}]", var.Type);
                }

                if (var.Type.Name == null)
                {
                    parsingContext.AddParserError("Cannot declare anonymous type at the top level");
                }
            }
        }

        /// <summary>
        /// The create while statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateWhileStatementAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<WhileStatement>(node);

            //// switch_statement.Rule =
            ////      [0]     [1]     [2]        [3]     [4] 
            //// _("while") + "(" + expression + ")" + statement;
            value.Condition = (Expression)node.ChildNodes[2].AstNode;
            value.Statement = (Statement)node.ChildNodes[4].AstNode;
        }

        /// <summary>
        /// The create float literal.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected void CreateFloatLiteral(ParsingContext context, ParseTreeNode node)
        {
            var literalFloat = Ast<Literal>(node);
            float value;
            var floatStr = node.Token.Text;
            bool isHalf = floatStr.EndsWith("h", StringComparison.CurrentCultureIgnoreCase); 

            // Remove postfix
            if (floatStr.EndsWith("d", StringComparison.CurrentCultureIgnoreCase) 
                || floatStr.EndsWith("f", StringComparison.CurrentCultureIgnoreCase)
                || isHalf)
            {
                floatStr = floatStr.Substring(0, floatStr.Length - 1);
            }

            if (!float.TryParse(
                floatStr, 
                NumberStyles.Float, 
                CultureInfo.InvariantCulture, 
                out value))
            {
                context.AddParserError("Unable to parse float [{0}]", node.Token.Text);
            }

            literalFloat.Value = value;
            literalFloat.Text = node.Token.Text;

            // Don't output half
            // TODO MOVE THIS TO GLSL WRITER
            if (isHalf)
            {
                literalFloat.Text = floatStr;
            }
        }

        /// <summary>
        /// The create integer literal.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected void CreateIntegerLiteral(ParsingContext context, ParseTreeNode node)
        {
            var literalInt = Ast<Literal>(node);
            int value = 0;
            bool isOctal = false;
            var intStr = node.Token.Text;
            var style = NumberStyles.Integer;

            // Remove post-fix
            if (intStr.EndsWith("l", StringComparison.CurrentCultureIgnoreCase))
            {
                intStr = intStr.Substring(0, intStr.Length - 1);
            }

            // Check for Hexa decimal
            if (intStr.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                intStr = intStr.Substring(2);
                style = NumberStyles.HexNumber;
            }
            else if (intStr.StartsWith("0") && intStr.Length > 1)
            {
                // Else parse Octal
                isOctal = true;
                try
                {
                    value = Convert.ToInt32(intStr, 8);
                }
                catch (FormatException)
                {
                    context.AddParserError("Unable to parse octal number [{0}]", node.Token.Text);
                }
            }

            // If on octal, parse regular hexa
            if (!isOctal && !int.TryParse(
                intStr, 
                style, 
                CultureInfo.InvariantCulture, 
                out value))
            {
                context.AddParserError("Unable to parse integer [{0}]", node.Token.Text);
            }

            literalInt.Value = value;
            literalInt.Text = node.Token.Text;
        }

        /// <summary>
        /// The create storage qualifier.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected virtual void CreateStorageQualifier(ParsingContext context, ParseTreeNode node)
        {
            var qualifier = Qualifier.None;
            if (node.ChildNodes.Count == 1)
            {
                qualifier = Shaders.Ast.StorageQualifier.Parse(node.ChildNodes[0].Token.Text);
                qualifier.Span = SpanConverter.Convert(node.Span);
            }

            node.AstNode = qualifier;
        }

        /// <summary>
        /// The create variable declarator ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected virtual void CreateVariableDeclaratorAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = (Variable)node.ChildNodes[0].AstNode;
            node.AstNode = value;

            //// variable_declarator.Rule = 
            ////          [0]               [1]       [2]
            ////  variable_declarator_raw
            ////| variable_declarator_raw + "=" + initializer;

            // Get initial value
            if (node.ChildNodes.Count == 3)
            {
                value.InitialValue = (Expression)node.ChildNodes[2].AstNode;
            }
        }

        /// <summary>
        /// The create variable declarator raw ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected virtual void CreateVariableDeclaratorRawAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<Variable>(node);

            //// variable_declarator_raw.Rule = 
            ////          [0]                                       [1]               
            ////  indexable_identifier_declarator + variable_declarator_qualifier_post
            var identifier = (Identifier)node.ChildNodes[0].AstNode;

            // Get modifiers
            value.Qualifiers = (Qualifier)node.ChildNodes[1].AstNode;

            value.Name = identifier;

            // If it is an identifier with indices, transform it to a plain identifier with an array type
            if (identifier.HasIndices)
            {
                // base type of the array will be added by variable declaration
                value.Type = new ArrayType { Dimensions = new List<Expression>(identifier.Indices), Span = identifier.Span };
                identifier.Indices = null;
            }
        }

        private static void CheckFieldDeclarationAst(ParsingContext context, ParseTreeNode node)
        {
            //// field_declaration.Rule = 
            ////         [0]            [1]                 [2]      
            //// field_qualifier_pre + type + variable_declarator_list + ";";
            // Check that field has a declarator

            // If field declaration is a type, then this is an error
            if (node.ChildNodes[0].AstNode is TypeBase)
            {
                var typeBase = (TypeBase)node.ChildNodes[0].AstNode;
                var baseTypeSpan = typeBase.Span;
                var location = ((Irony.Parsing.IBrowsableAstNode)typeBase).Location;
                location.Position += baseTypeSpan.Length;

                context.AddParserMessage(ParserErrorLevel.Error, location, "Field declaration must contain an identifier");
                return;
            }

            var var = (Variable)node.ChildNodes[0].AstNode;
            node.AstNode = var;

            // Check that declarator doesn't contain initial values
            foreach (var variableDeclarator in var.Instances())
            {
                if (variableDeclarator.InitialValue != null)
                {
                    context.AddParserMessage(
                        ParserErrorLevel.Error, ((Irony.Parsing.IBrowsableAstNode)variableDeclarator.InitialValue).Location, "Field declaration cannot contain an initial value");
                }
            }
        }

        private static void CreateVariableReferenceExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            //// variable_identifier.Rule = identifier;
            var value = Ast<VariableReferenceExpression>(node);

            value.Name = (Identifier)node.ChildNodes[0].AstNode;
        }
        #endregion

        private static void CreateTypeReferenceExpression(ParsingContext context, ParseTreeNode node)
        {
            //// variable_identifier.Rule = identifier;
            var value = Ast<TypeReferenceExpression>(node);

            value.Type = (TypeBase)node.ChildNodes[0].AstNode;
        }

        private static void CreateExpressionListAst(ParsingContext context, ParseTreeNode node)
        {
            //// expression.Rule = 
            ////      [0]                     [1]             [2]
            ////   assignment_expression 
            //// | expression               + "," + assignment_expression;

            if (node.ChildNodes.Count == 1)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
            }
            else
            {
                var value = Ast<ExpressionList>(node);
                FillListFromNodes(node.ChildNodes, value);
            }
        }
    }
}
