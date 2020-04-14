// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Grammar.Hlsl
{
    /// <summary>
    /// Grammar for Hlsl.
    /// </summary>
    [Language("hlsl", "5.0", "Sample hlsl grammar")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK here.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Reviewed. Suppression is OK here.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]
    public partial class HlslGrammar : ShaderGrammar
    {
        // ReSharper disable InconsistentNaming
        // ------------------------------------------------------------------------------------
        // Literals
        // ------------------------------------------------------------------------------------
        protected readonly Terminal string_literal_raw = new Terminal("string") { AstNodeConfig = new TokenInfo(TokenCategory.String) };
        protected readonly NonTerminal string_literal = T("string_literal", CreateStringLiteralAst);

        // ------------------------------------------------------------------------------------
        // NonTerminals
        // ------------------------------------------------------------------------------------
        protected readonly NonTerminal annotations = T("annotations", CreateAnnotationsAst);
        protected readonly NonTerminal annotations_opt = T("annotations_opt", CreateAnnotationsOptAst);
        protected readonly NonTerminal asm_expression = T("asm_expression", CreateAsmAst);
        private readonly NamedBlockKeyTerm asm_block = new NamedBlockKeyTerm("asm_block", "asm") { AstNodeConfig = new TokenInfo() { TokenCategory = TokenCategory.Keyword }};
        protected readonly NonTerminal attribute_list_opt = T("attribute_list_opt", CreateListFromNode<AttributeBase>);
        protected readonly NonTerminal attribute_modifier = T("attribute_modifier", CreateAttributeAst);
        protected readonly NonTerminal buffer_type = T("buffer_type", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal byte_address_buffer = T("byte_address_buffer", CreateTypeNameFromTokenAst);
        protected readonly NonTerminal cast_expression_raw = T("cast_expression_raw", CreateCastExpressionAst);
        protected readonly NonTerminal class_base_type = T("class_base_type", CreateClassBaseTypeAst);
        protected readonly NonTerminal class_base_type_list = T("class_base_type_list", CreateListFromNode<TypeName>);
        protected readonly NonTerminal class_specifier = T("class_specifier", CreateClassDeclarationAst);
        protected readonly NonTerminal compile_expression = T("compile_expression", CreateCompileExpressionAst);
        protected readonly NonTerminal constant_buffer_resource = T("constant_buffer_resource", CreateConstantBufferAst);
        protected readonly NonTerminal constant_buffer_resource_type = T("constant_buffer_resource_type");
        protected readonly NonTerminal float_qualifier = T("float_qualifier", CreateFloatQualifier);
        protected readonly NonTerminal geometry_stream = TT("geomery_stream");
        protected readonly NonTerminal identifier_dot = T("identifier_dot", CreateIdentifierDotAst);
        protected readonly NonTerminal identifier_or_dot = TT("identifier_or_dot");
        protected readonly NonTerminal identifier_ns = T("identifier_ns", CreateIdentifierNsAst);
        protected readonly NonTerminal identifier_ns_list = T("identifier_ns_list", CreateListFromNode<Identifier>);
        protected readonly NonTerminal identifier_dot_list = T("identifier_dot_list", CreateListFromNode<Identifier>);
        protected readonly NonTerminal identifier_generic_parameter_list = T("identifier_generic_parameter_list", CreateListFromNode<Identifier>);
        protected readonly NonTerminal identifier_generic = T("identifier_generic", CreateIdentifierGenericAst);
        protected readonly NonTerminal identifier_or_generic = TT("identifier_or_generic");
        protected readonly NonTerminal type_generic = T("type_generic", CreateTypeGenericAst);
        protected readonly NonTerminal identifier_special_reference_expression = T("identifier_special_reference_expression", CreateIdentifierSpecialReferenceAst);
        protected readonly NonTerminal identifier_keyword = T("identifier_keyword", CreateIdentifierAst);
        protected readonly NonTerminal indexable_identifier_declarator_list = T("indexable_identifier_declarator_list", CreateIdentifierListAst);
        protected readonly NonTerminal identifier_sub_generic = T("identifier_sub_generic");
        protected readonly NonTerminal interface_specifier = T("interface_specifier", CreateInterfaceAst);
        protected readonly NonTerminal line_stream = T("line_strean", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal method_operator_identifier = T("method_operator_identifier", CreateMethodOperatorIdentifierAst);
        protected readonly NonTerminal method_special_identifier = T("method_special_identifier", CreateMethodSpecialIdentifierAst);
        protected readonly NonTerminal packoffset = T("packoffset", CreatePackOffsetAst);
        protected readonly NonTerminal parameter_qualifier = T("parameter_qualifier");
        protected readonly NonTerminal parameter_qualifier_pre_list_opt = T("parameter_qualifier_pre_list_opt", CreateQualifiersAst);
        protected readonly NonTerminal pass_definition = T("pass_definition", CreatePassAst);
        protected readonly NonTerminal pass_keyword = TT("pass_keyword");
        protected readonly NonTerminal pass_statement = T("pass_statement", CreatePassStatementAst);
        protected readonly NonTerminal patch_generic_type = T("patch_generic_type", CreateGenericTypeAst<ObjectType, Literal>);
        protected readonly NonTerminal patch_type = T("patch_type", CreateTypeNameFromTokenAst);
        protected readonly NonTerminal point_stream = T("point_stream", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal register = T("register", CreateRegisterLocationAst);
        protected readonly NonTerminal state_type = T("state_type", CreateTypeFromTokenAst<ObjectType>);
        protected readonly NonTerminal semantic = T("semantic", CreateSemanticAst);
        protected readonly NonTerminal semantic_list_opt = T("semantic_list_opt", CreateQualifiersAst);
        protected readonly NonTerminal shader_objects = T("shader_objects", CreateTypeNameFromTokenAst);
        protected readonly NonTerminal state_expression = T("state_expression", CreateStateExpressionAst);
        protected readonly NonTerminal state_initializer = T("state_initializer", CreateStateValuesAst);
        protected readonly NonTerminal state_initializer_list = T("state_initializer", CreateListFromNode<StateInitializer>);
        protected readonly NonTerminal state_array_initializer = T("state_array_initializer", CreateStateValuesAst);               
        protected readonly NonTerminal stream_output_object = T("stream_output_object", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal string_type = T("string_type", (context, node) => Ast<TypeName>(node).Name = new Identifier("string") { Span = SpanConverter.Convert(node.Span) });
        protected readonly NonTerminal structured_buffer = T("structured_buffer", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal structured_buffer_type = T("structured_buffer_type", CreateTypeNameFromTokenAst);
        protected readonly NonTerminal technique_definition = T("technique_definition", CreateTechniqueAst);
        protected readonly NonTerminal technique_keyword = T("technique_keyword", CreateIdentifierAst);
        protected readonly NonTerminal texture_dms_type_profile_5 = T("texture_dms_type_profile_5", CreateTypeFromTokenAst<TextureType>);
        protected readonly NonTerminal texture_generic_dms_type = T("texture_generic_dms_type", CreateTextureDMSAst);
        protected readonly NonTerminal texture_generic_simple_type = T("texture_generic_simple_type", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal texture_generic_type = TT("texture_generic_type");
        protected readonly NonTerminal texture_type = T("texture_type", CreateTypeFromTokenAst<TextureType>);
        protected readonly NonTerminal texture_type_list = TT("texture_type_list");
        protected readonly NonTerminal texture_type_profile_4 = T("texture_type_profile_4", CreateTypeFromTokenAst<TextureType>);
        protected readonly NonTerminal texture_type_profile_5 = T("texture_type_profile_5", CreateTypeFromTokenAst<TextureType>);
        protected readonly NonTerminal triangle_stream = T("triangle_stream", CreateGenericTypeAst<ObjectType>);
        protected readonly NonTerminal typedef_declaration = T("typedef_modifier", CreateTypedefAst);

        protected readonly NonTerminal variable_declarator_qualifier_post_hlsl = T("variable_declarator_qualifier_post_hlsl", CreateVariableDeclaratorQualifierPostAst);
        protected readonly TerminalSet _skipTokensInPreview = new TerminalSet();
        // ReSharper restore InconsistentNaming        


        public override LanguageData CreateLanguageData()
        {
            return new ShaderLanguageData(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslGrammar"/> class.
        /// </summary>
        public HlslGrammar()
        {
            GrammarComments = "Hlsl version 5.0";

            Term(string_literal_raw, TokenCategory.String, TokenType.StringLiteral);
            Punc("::", TokenType.IdentifierSeparator);

            // ------------------------------------------------------------------------------------
            // Comments
            // ------------------------------------------------------------------------------------

            identifier_ns_list.Rule = MakePlusRule(identifier_ns_list, ToTerm("::"), identifier_raw);
            identifier_dot_list.Rule = MakePlusRule(identifier_dot_list, ToTerm("."), identifier_raw);
            identifier_ns.Rule = identifier_raw + "::" + identifier_ns_list;
            identifier_dot.Rule = identifier_or_generic + ToTerm(".") + identifier_dot_list;
            identifier_or_dot.Rule = identifier | identifier_dot;

            identifier.Rule |= identifier_ns;

            semi_opt.Rule = Empty | PreferShiftHere() + ";";

            //Prepare term set for conflict resolution
            _skipTokensInPreview.UnionWith(new[] { ToTerm("."), identifier_raw, ToTerm(","), ToTerm("::"), ToTerm("["), ToTerm("]"), float_literal, integer_literal });


            var genericResolverHint = new GenericResolverHint(_skipTokensInPreview);
            less_than.Rule = genericResolverHint + "<";

            // ------------------------------------------------------------------------------------
            // Types
            // ------------------------------------------------------------------------------------

            // String
            string_literal.Rule = string_literal_raw.List();
            string_literal_raw.AstNodeCreator = CreateStringRawLiteral;

            // Add string to literals
            literal.Rule |= string_literal;

            float_qualifier.Rule = Keyword("unorm") | Keyword("snorm");

            // scalars
            var scalarTypes = new[] { ScalarType.Bool, ScalarType.Int, ScalarType.UInt, ScalarType.Float, ScalarType.Half, ScalarType.Double };
            foreach (var scalarType in scalarTypes)
            {
                NonTerminal scalarTerm;
                var localScalarType = scalarType;

                if (scalarType == ScalarType.Float)
                {
                    scalarTerm = new NonTerminal(
                    "float",
                    (context, node) =>
                    {
                        var dynamicFloatType = Ast<ScalarType>(node);
                        dynamicFloatType.Name = new Identifier(localScalarType.Name) { Span = SpanConverter.Convert(node.Span) };
                        dynamicFloatType.Type = localScalarType.Type;
                        dynamicFloatType.Qualifiers = Qualifier.None;
                        if (node.ChildNodes.Count == 2)
                        {
                            dynamicFloatType.Qualifiers = (Qualifier)node.ChildNodes[0].AstNode;
                        }
                    })
                    {
                        Rule = Keyword("float", true) | float_qualifier + Keyword("float", true)
                    };

                }
                else
                {
                    scalarTerm = CreateScalarTerminal(scalarType);
                }

                if (scalars.Rule == null) scalars.Rule = scalarTerm;
                else scalars.Rule |= scalarTerm;
            }

            // Buffer Rules
            buffer_type.Rule = TypeName("Buffer") + less_than + simple_type_or_type_name + ">";

            // Vectors Rules
            vector_type.AstNodeCreator = CreateVectorAst;
            vector_type.Rule = Keyword("vector") + less_than + scalars_or_typename + "," + number + ">";
            vector_type_list.Rule = vector_type;

            // Add all vector int1 int2 int3 int4... float1 float2 float3 float4... etc.
            foreach (var scalarTypeIt in scalarTypes)
            {
                var scalarType = scalarTypeIt;
                for (var dim = 1; dim <= 4; dim++)
                {
                    var vectorTypeInstance = new VectorType(scalarTypeIt, dim);
                    var nonGenericType = vectorTypeInstance.ToNonGenericType();
                    var name = nonGenericType.Name.Text;
                    vector_type_list.Rule |= new NonTerminal(name,
                        (ctx, node) =>
                        {
                                var typeName = vectorTypeInstance.ToNonGenericType(SpanConverter.Convert(node.Span));
                                node.AstNode = typeName;
                            }) { Rule = Keyword(name) };
                }
            }

            // Matrices
            matrix_type_simple.Rule = Keyword("matrix");
            matrix_type_simple.AstNodeCreator = (ctx, node) =>
                {
                    var typeName = Ast<TypeName>(node);
                    typeName.Name = new Identifier("matrix") { Span = SpanConverter.Convert(node.Span) };
                    typeName.TypeInference.TargetType = new MatrixType(ScalarType.Float, 4, 4);
                };

            matrix_type.Rule = Keyword("matrix") + less_than + scalars_or_typename + "," + number + "," + number + ">";
            matrix_type.AstNodeCreator = CreateMatrixAst;
            matrix_type_list.Rule = matrix_type | matrix_type_simple;

            // Add all matrix typedefs: int1x1 int1x2... float1x1 float1x2 float1x3 float1x4... etc.
            foreach (var scalarTypeIt in scalarTypes)
            {
                var scalarType = scalarTypeIt;
                for (var dimX = 1; dimX <= 4; dimX++)
                    for (var dimY = 1; dimY <= 4; dimY++)
                    {
                        var matrixTypeInstance = new MatrixType(scalarTypeIt, dimX, dimY);
                        var nonGenericType = matrixTypeInstance.ToNonGenericType();
                        var name = nonGenericType.Name.Text;

                        // var typeName = new TypeName(name) { Alias = matrixTypeInstance };
                        matrix_type_list.Rule |= new NonTerminal(
                            name,
                            (ctx, node) =>
                                {
                                    var typeName = matrixTypeInstance.ToNonGenericType(SpanConverter.Convert(node.Span));
                                    node.AstNode = typeName;
                                }) { Rule = Keyword(name) };
                    }
            }

            // Sampler types
            state_type.Rule = CreateRuleFromObjectTypes(
                StateType.BlendState,
                StateType.DepthStencilState,
                StateType.RasterizerState,
                StateType.SamplerState,
                StateType.SamplerStateOld,
                StateType.SamplerComparisonState);

            sampler_type.Rule = CreateRuleFromObjectTypes(
                SamplerType.Sampler, 
                SamplerType.Sampler1D, 
                SamplerType.Sampler2D, 
                SamplerType.Sampler3D, 
                SamplerType.SamplerCube);
            
            sampler_type.AstNodeCreator = CreateTypeFromTokenAst<ObjectType>;

            // Texture types
            texture_type_profile_4.Rule = CreateRuleFromObjectTypes(
                TextureType.Texture1D,
                TextureType.Texture1DArray,
                TextureType.Texture2D,
                TextureType.Texture2DArray,
                TextureType.Texture3D,
                TextureType.TextureCube);

            texture_type.Rule = Keyword("texture") | texture_type_profile_4;

            // ByteAddressBuffer
            byte_address_buffer.Rule = TypeName("ByteAddressBuffer") | TypeName("RWByteAddressBuffer");

            // StructuredBuffer
            structured_buffer_type.Rule = TypeName("AppendStructuredBuffer") | TypeName("ConsumeStructuredBuffer") | TypeName("RWStructuredBuffer") | TypeName("StructuredBuffer");
            structured_buffer.Rule = structured_buffer_type + less_than + scalars_and_vectors + ">";

            // RWTexture.*
            texture_type_profile_5.Rule = TypeName("RWBuffer") | TypeName("RWTexture1D") | TypeName("RWTexture1DArray") | TypeName("RWTexture2D") | TypeName("RWTexture2DArray") | TypeName("RWTexture3D");

            texture_generic_simple_type.Rule = texture_type_profile_4 + less_than + scalars_and_vectors + ">"
                                               | texture_type_profile_5 + less_than + scalars_and_vectors + ">";

            texture_dms_type_profile_5.Rule = TypeName("Texture2DMS") | TypeName("Texture2DMSArray");

            texture_generic_dms_type.Rule = texture_dms_type_profile_5 + less_than + scalars_and_vectors + ">"
                                            | texture_dms_type_profile_5 + less_than + scalars_and_vectors + "," + number + ">";

            texture_generic_type.Rule = texture_generic_simple_type | texture_generic_dms_type;

            // HullShader/DomainShader InputPatch/OutputPatch
            patch_type.Rule = TypeName("InputPatch") | TypeName("OutputPatch");

            patch_generic_type.Rule = patch_type + less_than + type + "," + number + ">";

            texture_type_list.Rule = texture_type | texture_generic_type;

            // Types used by the geometry shader
            geometry_stream.Rule = line_stream | point_stream | triangle_stream | stream_output_object;

            triangle_stream.Rule = TypeName("TriangleStream") + less_than + type + ">";

            point_stream.Rule = TypeName("PointStream") + less_than + type + ">";

            line_stream.Rule = TypeName("LineStream") + less_than + type + ">";

            stream_output_object.Rule = TypeName("StreamOutputObject") + less_than + type + ">";

            //// Shader object
            //// shader_objects.Rule = ToTerm("VertexShader") | "PixelShader" | "GeometryShader";

            string_type.Rule = Keyword("string");

            // Add string to simple types
            simple_type.Rule |= string_type;

            // Add Object types
            object_type.Rule |= buffer_type
                                | state_type
                                | texture_type_list
                                | byte_address_buffer
                                | structured_buffer
                                | patch_generic_type
                                | interface_specifier
                                | class_specifier
                                | geometry_stream;
                                ////| shader_objects;

            // Type name 
            typename_for_cast.Rule = identifier + new IdentifierResolverHint(true);

            identifier_generic_parameter_list.Rule = MakePlusRule(identifier_generic_parameter_list, ToTerm(","), identifier_sub_generic);

            identifier_sub_generic.Rule = identifier_or_generic;
            identifier_sub_generic.AstNodeCreator = CreateIdentifierSubGenericAst;

            //identifier_generic.Rule = identifier + new IdentifierResolverHint(true) + "<" + identifier_generic_parameter_list + ">";
            identifier_generic.Rule = identifier + new GenericResolverHint(_skipTokensInPreview) + "<" + identifier_generic_parameter_list + ">";

            identifier_or_generic.Rule = identifier + new IdentifierResolverHint(true)
                                         | identifier_generic + this.ReduceHere();

            type_generic.Rule = identifier_or_generic;

            // Type used for cast (use valuetype)
            type_for_cast.Rule = typename_for_cast
                                 | value_type;

            // ------------------------------------------------------------------------------------
            // Expressions
            // ------------------------------------------------------------------------------------            

            // Add special variable allowed as variable name and keyword
            identifier_extended.Rule |= Keyword("sample") | Keyword("point");

            // postfix_expression
            postfix_expression.Rule |= compile_expression
                                      | asm_expression
                                      | state_expression;

            compile_expression.Rule = Keyword("compile") + identifier + method_invoke_expression_simple;

            // Match an asm block: asm { ... }
            asm_expression.Rule = asm_block;
            KeyTerms.Add(asm_block.Name, asm_block);

            state_expression.Rule = state_type + state_initializer;

            // Add cast_expression
            cast_expression_raw.Rule = "(" + type_for_cast + rank_specifier.ListOpt() + ")" + cast_expression;

            cast_expression.Rule |= cast_expression_raw;

            // Syntax is for example: texture = <g_textureref>;
            identifier_special_reference_expression.Rule = less_than + indexable_identifier + ">";

            identifier_keyword.Rule = Keyword("texture");

            simple_assignment_expression_statement.Rule |= indexable_identifier + assignment_operator + identifier_special_reference_expression + ";"
                                                           | identifier_keyword + assignment_operator + identifier_special_reference_expression + ";"
                                                           | identifier_keyword + assignment_operator + expression + ";";

            state_initializer.Rule = "{" + simple_assignment_expression_statement.ListOpt() + "}";

            // ------------------------------------------------------------------------------------
            // Attribute modifiers
            // ------------------------------------------------------------------------------------
            attribute_qualifier_pre.Rule = attribute_list_opt;

            attribute_list_opt.Rule = MakeStarRule(attribute_list_opt, null, attribute_modifier);

            attribute_modifier.Rule = "[" + identifier + "]"
                                      | "[" + identifier + "(" + literal_list.Opt() + ")" + "]";

            // ------------------------------------------------------------------------------------
            // Variable modifiers
            // ------------------------------------------------------------------------------------
            // storageClass = Storage_Class + Type_Modifier
            storage_qualifier.Rule |= Keyword("extern") | Keyword("nointerpolation") | Keyword("precise") | Keyword("shared") | Keyword("groupshared") | Keyword("static") | Keyword("volatile")
                                      | Keyword("row_major") | Keyword("column_major") | Keyword("linear") | Keyword("centroid") | Keyword("noperspective") | Keyword("sample") | Keyword("unsigned")
                                      | Keyword("inline");

            semantic.Rule = ToTerm(":") + identifier;

            packoffset.Rule = ToTerm(":") + Keyword("packoffset") + "(" + identifier_or_dot + ")";

            register.Rule = ToTerm(":") + Keyword("register") + "(" + indexable_identifier + ")"
                            | ToTerm(":") + Keyword("register") + "(" + identifier + "," + indexable_identifier + ")";


            variable_declarator_qualifier_post_hlsl.Rule = Empty
                                                           | semantic
                                                           | semantic + packoffset + register.ListOpt()
                                                           | semantic + register.List()
                                                           | packoffset + register.ListOpt()
                                                           | register.List();

            variable_declarator_qualifier_post.Rule = variable_declarator_qualifier_post_hlsl;

            // ------------------------------------------------------------------------------------
            // Declarations
            // ------------------------------------------------------------------------------------
            
            // Add typedef and constant buffer resource
            declaration.Rule |= typedef_declaration
                               | constant_buffer_resource;

            indexable_identifier_declarator_list.Rule = MakePlusRule(indexable_identifier_declarator_list, ToTerm(","), indexable_identifier_declarator);

            // typedef [const] Type Name[Index];
            typedef_declaration.Rule = Keyword("typedef") + type + indexable_identifier_declarator_list + ";"
                                       | Keyword("typedef") + storage_qualifier + type + indexable_identifier_declarator_list + ";";

            annotations.Rule = less_than + variable_declaration_raw.ListOpt() + ">";

            annotations_opt.Rule = Empty | annotations;

            // todo: add annotations_opt to variable_declarator qualifier post

            // Add annotations to variable declarator
            variable_declarator_raw.Rule += annotations_opt;

            // Add special 
            variable_declarator.Rule |= variable_declarator_raw + state_initializer
                                        | variable_declarator_raw + state_array_initializer;

            state_initializer_list.Rule = MakePlusRule(state_initializer_list, ToTerm(","), state_initializer);

            state_array_initializer.Rule = "{" + state_initializer_list + "}"
                                           | "{" + state_initializer_list + "," + "}";

            // interface definition
            interface_specifier.Rule = Keyword("interface") + identifier_or_generic + "{" + method_declaration.ListOpt() + "}";

            // class definition
            class_specifier.Rule = Keyword("class") + identifier_or_generic + class_base_type + "{" + scope_declaration.ListOpt() + "}";
            class_base_type_list.Rule = MakePlusRule(class_base_type_list, ToTerm(","), type_generic);
            class_base_type.Rule = (ToTerm(":") + class_base_type_list).Opt();

            // buffer definition
            constant_buffer_resource_type.Rule = Keyword("cbuffer") | Keyword("tbuffer") | Keyword("rgroup");
            constant_buffer_resource_type.AstNodeCreator = CreateConstantBufferTypeAst;

            constant_buffer_resource.Rule = attribute_qualifier_pre + constant_buffer_resource_type + identifier.Opt() + register.Opt() + "{" + declaration.ListOpt() + "}" + semi_opt;

            semantic_list_opt.Rule = semantic.ListOpt();

            // Method
            method_qualifier_post.Rule = semantic_list_opt;

            method_operator_identifier.Rule = Keyword("operator") + "[" + "]"
                                              | Keyword("operator") + "[" + "]" + "[" + "]";
            method_special_identifier.Rule = identifier_extended + "." + method_operator_identifier | method_operator_identifier;

            method_declarator.Rule |= method_special_identifier + "(" + parameter_list + ")";

            parameter_qualifier.Rule = storage_qualifier | Keyword("in") | Keyword("out") | Keyword("inout") | Keyword("point") | Keyword("line") | Keyword("lineadj") | Keyword("triangle") | Keyword("triangleadj");
            parameter_qualifier.AstNodeCreator = CreateParameterQualifier;

            parameter_qualifier_pre_list_opt.Rule = parameter_qualifier.ListOpt();
            parameter_qualifier_pre.Rule = parameter_qualifier_pre_list_opt;
            // Make parameter_qualifier_pre transient as there is nothing else to parse then parameter_qualifier_pre_list_opt
            parameter_qualifier_pre.Flags = TermFlags.IsTransient | TermFlags.NoAstNode;

            parameter_qualifier_post.Rule = semantic_list_opt 
                                           | "=" + initializer + semantic_list_opt;
            parameter_qualifier_post.AstNodeCreator = CreateParameterQualifierPost;

            // ------------------------------------------------------------------------------------
            // Technique/pass
            // ------------------------------------------------------------------------------------

            // technique
            technique_keyword.Rule = Keyword("technique") | Keyword("Technique") | Keyword("technique10") | Keyword("Technique10") | Keyword("technique11") | Keyword("Technique11");

            technique_definition.Rule = attribute_qualifier_pre + technique_keyword + identifier.Opt() + annotations_opt + "{" + pass_definition.List() + "}" + semi_opt;

            // pass
            pass_keyword.Rule = Keyword("pass") | Keyword("Pass");

            pass_statement.Rule = method_invoke_expression_simple + ";"
                                  | simple_assignment_expression_statement;

            pass_definition.Rule = attribute_qualifier_pre + pass_keyword + identifier.Opt() + annotations_opt + "{" + pass_statement.ListOpt() + "}" + semi_opt;
            
            // ------------------------------------------------------------------------------------
            // Top Level
            // ------------------------------------------------------------------------------------

            // Add the technique to the top level
            toplevel_declaration.Rule |= technique_definition;

            /*
            //// ------------------------------------------------------------------------------------
            //// Stride Grammar
            //// ------------------------------------------------------------------------------------
            //var identifier_csharp = new NonTerminal("identifier_csharp");
            //var group = new NonTerminal("group");
            //var using_statement = new NonTerminal("using_statement");
            //group.Rule = "group" + identifier + "{" + scope_declaration.ListOpt() + "}";
            //identifier_csharp.Rule = MakePlusRule(identifier_csharp, ToTerm("."), identifier);
            //using_statement.Rule = "using" + identifier + "=" + identifier_csharp + ";"
            //                       | "using" + identifier_csharp + ";";
            //scope_declaration.Rule |= using_statement;
            //toplevel_declaration.Rule |= group;
            */

            // ------------------------------------------------------------------------------------
            // Globals
            // ------------------------------------------------------------------------------------
            // LanguageFlags = LanguageFlags.NewLineBeforeEOF;
            LanguageFlags |= LanguageFlags.CreateAst;
        }

        //public override void OnResolvingConflict(ConflictResolutionArgs args)
        //{
        //    switch (args.Context.CurrentParserInput.Term.Name)
        //    {
        //        case "<":
        //            args.Scanner.BeginPreview();
        //            int ltCount = 0;
        //            string previewSym;
        //            while (true)
        //            {
        //                //Find first token ahead (using preview mode) that is either end of generic parameter (">") or something else
        //                Token preview;
        //                do
        //                {
        //                    preview = args.Scanner.GetToken();
        //                } while (_skipTokensInPreview.Contains(preview.Terminal) && preview.Terminal != base.Eof);
        //                //See what did we find
        //                previewSym = preview.Terminal.Name;
        //                if (previewSym == "<")
        //                    ltCount++;
        //                else if (previewSym == ">" && ltCount > 0)
        //                {
        //                    ltCount--;
        //                    continue;
        //                }
        //                else
        //                    break;
        //            }
        //            //if we see ">", then it is type argument, not operator
        //            if (previewSym == ">")
        //                args.Result = ParserActionType.Shift;
        //            else
        //                args.Result = ParserActionType.Reduce;
        //            args.Scanner.EndPreview(true); 
        //            return;
        //    }
        //}
    }
}
