// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

using GoldParser;
using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;
using ParameterQualifier = Stride.Core.Shaders.Ast.Hlsl.ParameterQualifier;
using StorageQualifier = Stride.Core.Shaders.Ast.Hlsl.StorageQualifier;

namespace Stride.Core.Shaders.Grammar.Hlsl
{
    /// <summary>
    /// Methods used to create the Abstract Syntax Tree..
    /// </summary>
    public partial class HlslGrammar
    {
        /// <summary>
        /// The create annotations ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAnnotationsAst(ParsingContext context, ParseTreeNode node)
        {
            var annotations = Ast<Ast.Hlsl.Annotations>(node);

            // [0]                 [1]                 [2]
            // "<" + variable_declaration_raw.ListOpt() + ">";
            FillListFromNodes(node.ChildNodes[1].ChildNodes, annotations.Variables);
        }

        /// <summary>
        /// The create annotations opt ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAnnotationsOptAst(ParsingContext context, ParseTreeNode node)
        {
            var values = GetOptional<Ast.Hlsl.Annotations>(node);
            node.AstNode = values;
            if (values == null)
            {
                Ast<Ast.Hlsl.Annotations>(node);
            }
        }

        /// <summary>
        /// The create asm ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAsmAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<AsmExpression>(node);

            //    [0] 
            // asm_block 
            value.Text = node.ChildNodes[0].Token.Text;
        }

        /// <summary>
        /// The create attribute ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateAttributeAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<AttributeDeclaration>(node);

            //// [0]       [1]      [2]         [3]      
            // "[" + identifier + "]"
            // | "[" + identifier + "(" + literal_list.Q() + ")" + "]";
            value.Name = (Identifier)node.ChildNodes[1].AstNode;

            if (node.ChildNodes.Count > 3)
            {
                if (node.ChildNodes[3].ChildNodes.Count > 0)
                {
                    FillListFromNodes(node.ChildNodes[3].ChildNodes[0].ChildNodes, value.Parameters);
                }
            }
        }

        /// <summary>
        /// The create cast expression ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateCastExpressionAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // [0]       [1]               [2]               [3]        [4]
            // "(" + type_for_cast + rank_specifier.Star() + ")" + cast_expression;
            var value = Ast<CastExpression>(node);

            var type = (TypeBase)node.ChildNodes[1].AstNode;

            if (node.ChildNodes[2].ChildNodes.Count > 0)
            {
                var arrayType = new ArrayType { Type = type, Span = SpanConverter.Convert(node.ChildNodes[2].Span) };
                FillListFromNodes(node.ChildNodes[2].ChildNodes, arrayType.Dimensions);
                type = arrayType;
            }

            value.Target = type;
            value.From = (Expression)node.ChildNodes[4].AstNode;
        }

        /// <summary>
        /// The create class base type ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateClassBaseTypeAst(ParsingContext context, ParseTreeNode node)
        {
            ////           [0]
            //// (":" + type_name).Q();
            if (node.ChildNodes[0].ChildNodes.Count == 1)
                node.AstNode = node.ChildNodes[0].ChildNodes[0].AstNode;
            else
                node.AstNode = new List<TypeName>();
        }

        /// <summary>
        /// The create class declaration ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateClassDeclarationAst(ParsingContext context, ParseTreeNode node)
        {
            // [0]         [1]               [2]             [3]             [4]              [5]
            // "class" + type_name + class_base_type.Star() + "{" + scope_declaration.Star() + "}";
            var value = Ast<ClassType>(node);

            // Parse generics
            ParseGenerics((Identifier)node.ChildNodes[1].AstNode, value);

            //FillListFromNodes(node.ChildNodes[2].ChildNodes, value.BaseClasses);
            value.BaseClasses.AddRange((List<TypeName>)node.ChildNodes[2].AstNode);
            FillListFromNodes(node.ChildNodes[4].ChildNodes, value.Members);
        }

        protected static void ParseGenerics<T>(Identifier input, T dest) where T : TypeBase, IGenerics
        {
            // Parse generic identifier and convert it to simple identifier by adding contraint to the class type
            var genericIdentifier = input as IdentifierGeneric;
            if (genericIdentifier != null)
            {
                foreach (var genericIdentifierItem in genericIdentifier.Identifiers)
                {
                    dest.GenericParameters.Add(new GenericParameterType(genericIdentifierItem));
                }
                input = new Identifier(input.Text) { Span = genericIdentifier.Span };
            }
            dest.Name = input;
        }

        /// <summary>
        /// The create compile expression ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateCompileExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            // [0]         [1]                     [2]
            // "compile" + identifier + simple_method_invoke_expression;
            var value = Ast<CompileExpression>(node);

            value.Profile = (Identifier)node.ChildNodes[1].AstNode;
            value.Function = (MethodInvocationExpression)node.ChildNodes[2].AstNode;
        }

        /// <summary>
        /// The create constant buffer ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateConstantBufferAst(ParsingContext context, ParseTreeNode node)
        {
            // [0]                        [1]                            [2]             [3]        [4]        [5]             [6]     
            // attribute_list_opt + constant_buffer_resource_type + identifier.Q() + register.Q() + "{" + declaration.Star() + "}" + semi_opt;
            var value = Ast<ConstantBuffer>(node);
            value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;

            value.Type = (ConstantBufferType)node.ChildNodes[1].AstNode;

            value.Name = GetOptional<Identifier>(node.ChildNodes[2]);
            value.Register = GetOptional<RegisterLocation>(node.ChildNodes[3]);

            FillListFromNodes(node.ChildNodes[5].ChildNodes, value.Members);
        }

        /// <summary>
        /// The create generic type ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        /// <typeparam name="T1">
        /// </typeparam>
        protected static void CreateGenericTypeAst<T1>(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<GenericType>(node);
            value.ParameterTypes.Add(typeof(T1));

            // [0]       [1]      [2]      [3]
            // keyword + "<" + type_name + ">"
            Identifier identifier = null;

            if (node.ChildNodes[0].AstNode is TypeBase)
            {
                identifier = ((TypeBase)node.ChildNodes[0].AstNode).Name;
            }

            if (identifier == null)
            {
                CreateIdentifierAst(parsingcontext, node.ChildNodes[0]);
                identifier = (Identifier)node.ChildNodes[0].AstNode;
            }

            value.Name = identifier;
            value.Parameters.Add((Node)node.ChildNodes[2].AstNode);
        }

        /// <summary>
        /// The create generic type ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        /// <typeparam name="T1">
        /// </typeparam>
        /// <typeparam name="T2">
        /// </typeparam>
        protected static void CreateGenericTypeAst<T1, T2>(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<GenericType>(node);
            value.ParameterTypes.Add(typeof(T1));
            value.ParameterTypes.Add(typeof(T2));

            // [0]          [1]      [2]             [3]
            // identifier + "<" + type_name + "," + value ">"
            Identifier identifier = null;

            if (node.ChildNodes[0].AstNode is TypeBase)
            {
                identifier = ((TypeBase)node.ChildNodes[0].AstNode).Name;
            }

            if (identifier == null)
            {
                CreateIdentifierAst(parsingcontext, node.ChildNodes[0]);
                identifier = (Identifier)node.ChildNodes[0].AstNode;
            }

            value.Name = identifier;

            value.Parameters.Add((Node)node.ChildNodes[2].AstNode);
            value.Parameters.Add((Node)node.ChildNodes[3].AstNode);
        }

        /// <summary>
        /// The create identifier composite list.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierCompositeList(ParsingContext context, ParseTreeNode node)
        {
            var values = Ast<List<Identifier>>(node);
            foreach (var subNode in node.ChildNodes)
            {
                values.Add(new Identifier(subNode.Token.Text) { Span = SpanConverter.Convert(subNode.Span) });
            }
        }

        /// <summary>
        /// The create identifier ns ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierNsAst(ParsingContext context, ParseTreeNode node)
        {
            // identifier_ns.Rule = 
            //       [0]        [1]            [2]
            // identifier_raw + "::" + identifier_ns_list;
            var value = Ast<IdentifierNs>(node);
            value.Identifiers = new List<Identifier>();
            value.Identifiers.Add((Identifier)node.ChildNodes[0].AstNode);
            value.Identifiers.AddRange((List<Identifier>)node.ChildNodes[2].AstNode);
        }

        /// <summary>
        /// The create identifier special reference ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateIdentifierSpecialReferenceAst(ParsingContext context, ParseTreeNode node)
        {
            // "<" + identifier + ">"
            CreateIdentifierAst(context, node.ChildNodes[1]);
            var value = (Identifier)node.ChildNodes[1].AstNode;
            value.IsSpecialReference = true;
            node.AstNode = value;
        }

        /// <summary>
        /// The create interface ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateInterfaceAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<InterfaceType>(node);

            //// interface_specifier.Rule = 
            ////      [0]        [1]       [2]            [3]
            //// "interface" + identifier + "{" + method_declaration.Star() + "}";

            // Parse generics
            ParseGenerics((Identifier)node.ChildNodes[1].AstNode, value);

            FillListFromNodes(node.ChildNodes[3].ChildNodes, value.Methods);
        }

        /// <summary>
        /// Creates the matrix ast.
        /// </summary>
        /// <param name="parsingContext">
        /// The parsing context.
        /// </param>
        /// <param name="node">
        /// The node.
        /// </param>
        protected static void CreateMatrixAst(ParsingContext parsingContext, ParseTreeNode node)
        {
            var matrixType = Ast<MatrixType>(node);

            ////    [0]       [1]     [2]            [3]            [4]     [5]
            // _("matrix") + "<" + scalars + "," + number + "," + number + ">"
            matrixType.Type = (TypeBase)node.ChildNodes[2].AstNode;
            matrixType.Parameters[1] = (Literal)node.ChildNodes[3].AstNode;
            matrixType.Parameters[2] = (Literal)node.ChildNodes[4].AstNode;
        }

        /// <summary>
        /// The create pack offset ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreatePackOffsetAst(ParsingContext context, ParseTreeNode node)
        {
            ////             [0]       [1]       [2]      [3]
            // _(":") + "packoffset" + "(" + identifier + ")";
            node.AstNode = new PackOffset()
                {
                   Value = (Identifier)node.ChildNodes[2].AstNode, Span = SpanConverter.Convert(node.Span) 
                };
        }

        /// <summary>
        /// The create pass ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreatePassAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<Pass>(node);

            //// pass_definition.Rule = 
            ////      [0]                     [1]               [2]                [3]      [4]           [5]               [6]      [7]
            //// attribute_list_opt + pass_keyword + identifier.Opt() + annotations.Opt() + "{" + pass_statement.ListOpt() + "}" + semi_opt;
            value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
            value.Name = GetOptional<Identifier>(node.ChildNodes[2]);

            // TODO HANDLE Annotations here
            // value.Annotations = (List<Annotations>)node.ChildNodes[3].AstNode;
            FillListFromNodes(node.ChildNodes[5].ChildNodes, value.Items);
        }

        /// <summary>
        /// The create pass statement ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreatePassStatementAst(ParsingContext context, ParseTreeNode node)
        {
            //// pass_statement.Rule = 
            ////  method_invoke_expression_simple + ";"
            ////| simple_assignment_expression_statement;
            node.AstNode = node.ChildNodes[0].AstNode;
        }

        /// <summary>
        /// The create register location ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateRegisterLocationAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<RegisterLocation>(node);

            ////            [0]        [1]      [2]                 [3]                [4]
            // _(":") + "register" + "(" + indexable_identifier + ")"
            // | _(":") + "register" + "(" + identifier + "," + indexable_identifier + ")";
            int index = 2;
            if (node.ChildNodes.Count == 5)
            {
                value.Profile = (Identifier)node.ChildNodes[index].AstNode;
                index++;
            }

            value.Register = (Identifier)node.ChildNodes[index].AstNode;
        }

        /// <summary>
        /// The create semantic ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateSemanticAst(ParsingContext context, ParseTreeNode node)
        {
            ////       [0]
            // ":" + identifier
            node.AstNode = new Semantic()
                {
                   Name = (Identifier)node.ChildNodes[0].AstNode, Span = SpanConverter.Convert(node.Span) 
                };
        }

        /// <summary>
        /// The create state expression ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateStateExpressionAst(ParsingContext context, ParseTreeNode node)
        {
            //// state_expression.Rule = 
            ////    [0]             [1]     
            //// state_type + state_initializer;
            var value = Ast<StateExpression>(node);
            value.StateType = (TypeBase)node.ChildNodes[0].AstNode;
            value.Initializer = (StateInitializer)node.ChildNodes[1].AstNode;
        }

        /// <summary>
        /// The create state values ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateStateValuesAst(ParsingContext context, ParseTreeNode node)
        {
            //// state_initializer.Rule = "{" + simple_assignment_expression_statement.Star() + "}";
            var stateValues = Ast<StateInitializer>(node);
            FillListFromNodes(node.ChildNodes[1].ChildNodes, stateValues.Items);
        }

        /// <summary>
        /// The create string literal ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateStringLiteralAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<Literal>(node);

            value.SubLiterals = new List<Literal>();

            //// string_literal.Rule = 
            //// string_literal_raw.List();
            var text = new StringBuilder();
            var textValue = new StringBuilder();
            foreach (var childNode in node.ChildNodes[0].ChildNodes)
            {
                var subLiteral = new Literal
                    {
                        Span = SpanConverter.Convert(childNode.Span), 
                        Value = childNode.AstNode, 
                        Text = childNode.Token.Text
                    };
                value.SubLiterals.Add(subLiteral);
                textValue.Append(subLiteral.Value);
            }

            text.Append("\"").Append(textValue).Append("\"");

            value.Value = textValue.ToString();
            value.Text = text.ToString();
        }

        /// <summary>
        /// The create technique ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateTechniqueAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<Technique>(node);

            //// technique_definition.Rule = 
            ////      [0]                     [1]               [2]                [3]           [4]           [5]              [6]      [7]
            //// attribute_list_opt + technique_keyword + identifier.Opt() + annotations_opt + "{" + pass_definition.List() + "}" + semi_opt;
            value.Type = (Identifier)node.ChildNodes[1].AstNode;
            value.Attributes = (List<AttributeBase>)node.ChildNodes[0].AstNode;
            value.Name = GetOptional<Identifier>(node.ChildNodes[2]);

            // TODO Handle annotations here
            // value.Annotations = (List<Annotations>)node.ChildNodes[3].AstNode;
            FillListFromNodes(node.ChildNodes[5].ChildNodes, value.Passes);
        }

        /// <summary>
        /// The create texture dms ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateTextureDMSAst(ParsingContext context, ParseTreeNode node)
        {
            //// texture_generic_dms_type.Rule = 
            ////           [0]                 [1]              [2]        [3]     [3]    [4]
            ////  texture_dms_type_profile_5 + "<" + scalars_and_vectors + ">"
            ////| texture_dms_type_profile_5 + "<" + scalars_and_vectors + "," + number + ">";
            if (node.ChildNodes.Count == 4)
            {
                CreateGenericTypeAst<TypeBase>(context, node);
            }
            else
            {
                CreateGenericTypeAst<TypeBase, Literal>(context, node);
            }
        }

        /// <summary>
        /// The create typedef ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateTypedefAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            var value = Ast<Typedef>(node);

            // [0]             [1]                     [2]                   [3]
            // _("typedef") + typedefable_type + variable_declarator_list + ";"
            // | _("typedef") + storage_class_specifier + typedefable_type + variable_declarator_list + ";";
            int indexType = 1;
            if (node.ChildNodes.Count == 4)
            {
                value.Qualifiers = CreateEnumFlags(Qualifier.None, node.ChildNodes[0].ChildNodes);
                indexType++;
            }

            value.Type = (TypeBase)node.ChildNodes[indexType].AstNode;
            var identifierList = (List<Identifier>)node.ChildNodes[indexType + 1].AstNode;

            var declarators = new List<Typedef>(identifierList.Count);

            // Create declarators for typedefs
            foreach (var identifier in identifierList)
            {
                var declarator = new Typedef(value.Type)
                    {
                       Span = identifier.Span, 
                       Name = identifier 
                    };

                if (identifier.HasIndices)
                {
                    var arrayType = new ArrayType { Type = declarator.Type, Span = identifier.Span };
                    arrayType.Dimensions.AddRange(identifier.Indices);
                    declarator.Type = arrayType;
                    identifier.Indices.Clear();
                }

                declarators.Add(declarator);
            }

            if (declarators.Count == 1)
            {
                value.Type = declarators[0].Type;
                value.Name = declarators[0].Name;
            }
            else
            {
                value.SubDeclarators = declarators;
            }
        }

        /// <summary>
        /// The create variable declarator qualifier post ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected static void CreateVariableDeclaratorQualifierPostAst(ParsingContext context, ParseTreeNode node)
        {
            // Empty
            // | semantic
            // | semantic + packoffset + register.Star()
            // | semantic + register.Plus()
            // | packoffset + register.Star()
            // | register.Plus();
            var qualifiers = AstCompositeEnum<Qualifier>(node);

            foreach (var childNode in node.ChildNodes)
            {
                // semantic or packoffset
                if (childNode.AstNode is Semantic)
                {
                    qualifiers |= (Semantic)childNode.AstNode;
                }
                else if (childNode.AstNode is PackOffset)
                {
                    qualifiers |= (PackOffset)childNode.AstNode;
                }
                else
                {
                    qualifiers = CreateEnumFlags(qualifiers, childNode.ChildNodes);
                }
            }

            // Pass a local object to be used by the variable_declarator
            node.AstNode = qualifiers;
        }

        /// <summary>
        /// Creates the vector ast.
        /// </summary>
        /// <param name="parsingContext">
        /// The parsing context.
        /// </param>
        /// <param name="node">
        /// The node.
        /// </param>
        protected static void CreateVectorAst(ParsingContext parsingContext, ParseTreeNode node)
        {
            var vectorType = Ast<VectorType>(node);

            ////    [0]      [1]     [2]            [3]     [4]
            // _("vector") + "<" + scalars + "," + number + ">"
            vectorType.Type = (TypeBase)node.ChildNodes[2].AstNode;
            vectorType.Parameters[1] = (Literal)node.ChildNodes[3].AstNode;
        }

        /// <summary>
        /// The create parameter ast.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected override void CreateParameterAst(ParsingContext context, ParseTreeNode node)
        {
            base.CreateParameterAst(context, node);

            //// parameter_declaration.Rule = 
            ////          [0]                            [1]               [2]                   [3]                      [4]             
            //// attribute_qualifier_pre + parameter_qualifier_pre + parameter_type + indexable_identifier.Opt() + parameter_qualifier_post; 
            var parameter = (Parameter)node.AstNode;

            parameter.Qualifiers = (Qualifier)node.ChildNodes[1].AstNode;

            var postQualifier = (Tuple<Expression, Qualifier>)node.ChildNodes[4].AstNode;
            parameter.InitialValue = postQualifier.Item1;
            parameter.Qualifiers |= postQualifier.Item2;
        }

        private void CreateParameterQualifierPost(ParsingContext context, ParseTreeNode node)
        {
            ////                                       [0]                [1]             [2]
            //// parameter_qualifier_post.Rule = semantic_list_opt 
            ////                               |        "="          + initializer + semantic_list_opt;

            Tuple<Expression, Qualifier> postQualifier;

            if (node.ChildNodes.Count == 3)
            {
                postQualifier = new Tuple<Expression, Qualifier>((Expression)node.ChildNodes[1].AstNode, (Qualifier)node.ChildNodes[2].AstNode);
            }
            else
            {
                postQualifier = new Tuple<Expression, Qualifier>(null, (Qualifier)node.ChildNodes[0].AstNode);
            }

            node.AstNode = postQualifier;
        }

        /// <summary>
        /// The create parameter qualifier.
        /// </summary>
        /// <param name="context">
        /// </param>
        /// <param name="node">
        /// </param>
        protected virtual void CreateParameterQualifier(ParsingContext context, ParseTreeNode node)
        {
            ////          [0]
            //// storage_class_specifier | _("in") | "out" | "inout" | "point" | "line" | "triangle" | "triangleadj";
            if (node.ChildNodes[0].AstNode is Qualifier)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
            }
            else
            {
                var qualifier = Shaders.Ast.Hlsl.ParameterQualifier.Parse(node.ChildNodes[0].Token.Text);
                qualifier.Span = SpanConverter.Convert(node.Span);
                node.AstNode = qualifier;
            }
        }

        protected override void CreateStorageQualifier(ParsingContext context, ParseTreeNode node)
        {
            var qualifier = AstCompositeEnum<Qualifier>(node);

            if (node.ChildNodes.Count == 1)
            {
                qualifier = Shaders.Ast.Hlsl.StorageQualifier.Parse(node.ChildNodes[0].Token.Text);
                qualifier.Span = SpanConverter.Convert(node.Span);
            }

            // Use Hlsl Storage Qualifiers to parse the qualifier
            node.AstNode = qualifier;
        }

        /// <summary>
        /// The create variable declarator ast.
        /// </summary>
        /// <param name="parsingcontext">
        /// </param>
        /// <param name="node">
        /// </param>
        protected override void CreateVariableDeclaratorAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            // Create default declarator using the follozing inherited rule
            ////
            //// variable_declarator.Rule = 
            ////          [0]               [1]       [2]
            ////  variable_declarator_raw
            ////| variable_declarator_raw + "=" + initializer;
            ////
            //// Rules added by this override:
            ////| variable_declarator_raw + state_initializer
            ////| variable_declarator_raw + state_array_initializer;
            base.CreateVariableDeclaratorAst(parsingcontext, node);
            var value = (Variable)node.AstNode;

            // Get initial value
            if (node.ChildNodes.Count == 2)
            {
                value.InitialValue = (Expression)node.ChildNodes[1].AstNode;
            }
        }

        protected override void CreateVariableDeclaratorRawAst(ParsingContext parsingcontext, ParseTreeNode node)
        {
            //// Get base declaration:
            //// variable_declarator_raw.Rule = 
            ////          [0]                                       [1]               
            ////  indexable_identifier_declarator + variable_declarator_qualifier_post
            base.CreateVariableDeclaratorRawAst(parsingcontext, node);
            var value = (Variable)node.AstNode;

            //// Add annotations:
            ////  indexable_identifier_declarator + variable_declarator_qualifier_post + 
            value.Attributes.Add((Ast.Hlsl.Annotations)node.ChildNodes[2].AstNode);
        }

        protected virtual void CreateConstantBufferTypeAst(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = ConstantBufferType.Parse(node.ChildNodes[0].Token.Text);
        }

        private static void CreateFloatQualifier(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = FloatQualifier.Parse(node.ChildNodes[0].Token.Text);
        }

        private static void CreateIdentifierDotAst(ParsingContext context, ParseTreeNode node)
        {
            // identifier_dot.Rule = 
            //       [0]                [1]          [2] 
            //  identifier_or_generic + "." + identifier_dot_list;
            var value = Ast<IdentifierDot>(node);
            value.Identifiers.Add((Identifier)node.ChildNodes[0].AstNode);
            value.Identifiers.AddRange((List<Identifier>)node.ChildNodes[2].AstNode);
        }

        private void CreateStringRawLiteral(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = node.Token.Text.Trim('"');
        }

        private static void CreateIdentifierGenericAst(ParsingContext context, ParseTreeNode node)
        {
            // identifier_generic.Rule = 
            //       [0]      [1]                  [2]                  [3]
            //   identifier 
            // | identifier + "<" + class_identifier_generic_parameter_list + ">";
            var identifier = (Identifier)node.ChildNodes[0].AstNode;

            if (node.ChildNodes.Count == 4)
            {
                var value = Ast<IdentifierGeneric>(node);
                value.Text = identifier.Text;
                value.Identifiers.AddRange((List<Identifier>)node.ChildNodes[2].AstNode);
            } 
            else
            {
                node.AstNode = identifier;
            }
        }

        private static void CreateTypeGenericAst(ParsingContext context, ParseTreeNode node)
        {
            var value = Ast<TypeName>(node);
            value.Name = (Identifier)node.ChildNodes[0].AstNode;
        }

        private static void CreateMethodOperatorIdentifierAst(ParsingContext context, ParseTreeNode node)
        {
            // method_operator_identifier.Rule = 
            // _("operator") + "[" + "]"
            // _("operator") + "+"
            // ...etc.
            // Get the deepest valid node (either an AstNode or a Token)
            var value = Ast<Identifier>(node);
            var text = new StringBuilder();
            foreach (var subNode in node.ChildNodes)
                text.Append(subNode.Token.Text);
            value.Text = text.ToString();
        }

        private static void CreateMethodSpecialIdentifierAst(ParsingContext context, ParseTreeNode node)
        {
            //// method_special_identifier.Rule =
            ////     [0]        [1]             [2]
            ////   identifier + "." + method_operator_identifier 
            //// | method_operator_identifier;
            if (node.ChildNodes.Count == 1)
            {
                node.AstNode = node.ChildNodes[0].AstNode;
            }
            else
            {
                var newnode = Ast<IdentifierDot>(node);
                newnode.Identifiers.Add((Identifier)node.ChildNodes[0].AstNode);
                newnode.Identifiers.Add((Identifier)node.ChildNodes[2].AstNode);
            }
        }

        protected virtual void CreateIdentifierSubGenericAst(ParsingContext context, ParseTreeNode node)
        {
            node.AstNode = node.ChildNodes[0].AstNode;
        }
    }
}
