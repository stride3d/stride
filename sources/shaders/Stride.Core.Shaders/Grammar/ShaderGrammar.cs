// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Grammar
{
    /// <summary>
    /// Generic grammar for a shading language. 
    /// </summary>
    /// <remarks>
    /// This grammar provides the core grammar for a shading language including expressions (binary, unary, methods...), statements (if, for, while...).
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK here.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Reviewed. Suppression is OK here.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]
    public abstract partial class ShaderGrammar : Irony.Parsing.Grammar
    {
        // ReSharper disable InconsistentNaming
        // ------------------------------------------------------------------------------------
        // Comments
        // ------------------------------------------------------------------------------------
        public readonly Terminal single_line_comment = new Terminal("single_line_comment", Irony.Parsing.TokenCategory.Comment, TermFlags.IsNonGrammar) { AstNodeConfig = new TokenInfo(TokenCategory.Comment) };
        public readonly Terminal multi_line_comment = new Terminal("multi_line_comment", Irony.Parsing.TokenCategory.Comment, TermFlags.IsNonGrammar) { AstNodeConfig = new TokenInfo(TokenCategory.Comment) };

        // ------------------------------------------------------------------------------------
        // Literals
        // ------------------------------------------------------------------------------------
        public readonly Terminal float_literal = new Terminal("float_literal") { AstNodeConfig = new TokenInfo(TokenCategory.Number) };
        public readonly Terminal integer_literal = new Terminal("integer_literal") { AstNodeConfig = new TokenInfo(TokenCategory.Number) };
        public readonly NonTerminal number = TT("number");
        public readonly Terminal identifier_raw = new Terminal("identifier") { AstNodeConfig = new TokenInfo(TokenCategory.Identifier) };
        public readonly NonTerminal identifier = TT("identifier");
        public readonly NonTerminal boolean = T("boolean", (context, node) => node.AstNode = bool.Parse(node.ChildNodes[0].Token.Text));

        // ------------------------------------------------------------------------------------
        // Terminals
        // ------------------------------------------------------------------------------------
        public readonly Terminal unknown = new Terminal("unknown", Irony.Parsing.TokenCategory.Content);

        public readonly Terminal whitespace = new Terminal("whitespace", Irony.Parsing.TokenCategory.Content, TermFlags.IsNonGrammar) { AstNodeConfig = new TokenInfo(TokenCategory.WhiteSpace) };
        public readonly Terminal newline = new Terminal("newline", Irony.Parsing.TokenCategory.Content, TermFlags.IsNonGrammar)  { AstNodeConfig = new TokenInfo(TokenCategory.WhiteSpace)};
        // Pseudo terminal
        protected readonly NonTerminal semi_opt = TT("semi_opt");
        protected readonly NonTerminal less_than = TT("less_than");

        // ------------------------------------------------------------------------------------
        // NonTerminals
        // ------------------------------------------------------------------------------------
        protected readonly NonTerminal additive_expression = TT("additive_expression");
        protected readonly NonTerminal additive_expression_raw = T("additive_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal and_expression = TT("and_expression");
        protected readonly NonTerminal and_expression_raw = T("and_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal argument_expression_list = T("argument_expression_list", CreateListFromNode<Expression>);
        protected readonly NonTerminal array_initializer_expression = T("array_initializer_expression", CreateArrayInitializerExpressionAst);
        protected readonly NonTerminal assignment_expression = TT("assignment_expression");
        protected readonly NonTerminal assignment_expression_raw = T("assignment_expression_raw", CreateAssignementExpressionAst);
        protected readonly NonTerminal assignment_operator = T("assignment_operator", CreateAssignmentOperator);
        protected readonly NonTerminal attribute_qualifier_pre = TT("attribute_qualifier_pre");
        protected readonly NonTerminal block_item = TT("block_item");
        protected readonly NonTerminal block_statement = T("block_statement", CreateBlockStatementAst);
        protected readonly NonTerminal break_statement = T("break_statement", CreateExpressionStatementAst);
        protected readonly NonTerminal cast_expression = TT("cast_expression");
        protected readonly NonTerminal conditional_expression = TT("conditional_expression");
        protected readonly NonTerminal conditional_expression_raw = T("conditional_expression_raw", CreateConditionalExpressionAst);
        protected readonly NonTerminal constant_expression = TT("constant_expression");
        protected readonly NonTerminal continue_statement = T("continue_statement", CreateExpressionStatementAst);
        protected readonly NonTerminal declaration = TT("declaration");
        protected readonly NonTerminal declaration_specifiers = T("declaration_specifiers", CreateDeclarationSpecifier);
        protected readonly NonTerminal declaration_statement = T("declaration_statement", CreateDeclarationStatementAst);
        protected readonly NonTerminal discard_statement = T("discard_statement", CreateExpressionStatementAst);
        protected readonly NonTerminal do_while_statement = T("do_while_statement", CreateDoWhileStatementAst);
        protected readonly NonTerminal empty_statement = T("empty_statement", CreateEmptyStatementAst);
        protected readonly NonTerminal equality_expression = TT("equality_expression");
        protected readonly NonTerminal equality_expression_raw = T("equality_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal exclusive_or_expression = TT("exclusive_or_expression");
        protected readonly NonTerminal exclusive_or_expression_raw = T("exclusive_or_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal expression = TT("expression");
        protected readonly NonTerminal expression_list = T("expression_list", CreateExpressionListAst);
        protected readonly NonTerminal expression_or_empty = T("expression_or_empty", CreateExpressionOrEmptyAst);
        protected readonly NonTerminal expression_statement = T("expression_statement", CreateExpressionStatementAst);
        protected readonly NonTerminal for_statement = T("for_statement", CreateForStatementAst);
        protected readonly NonTerminal field_declaration = T("field_declaration", CheckFieldDeclarationAst);
        protected readonly NonTerminal identifier_list = T("identifier_list", CreateIdentifierListAst);
        protected readonly NonTerminal if_statement = T("if_terminal", CreateIfStatementAst);
        protected readonly NonTerminal inclusive_or_expression = TT("inclusive_or_expression");
        protected readonly NonTerminal inclusive_or_expression_raw = T("inclusive_or_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal incr_or_decr = TT("incr_or_decr");
        protected readonly NonTerminal identifier_extended = T("identifier_extended", CreateIdentifierAst);
        protected readonly NonTerminal indexable_identifier = T("identifier_indexable", CreateIdentifierIndexableAst);
        protected readonly NonTerminal indexable_identifier_declarator = T("indexable_identifier_declarator", CreateIdentifierIndexableAst);
        protected readonly NonTerminal indexer_expression = T("indexer-expression", CreateIndexerExpressionAst);
        protected readonly NonTerminal initializer = TT("initializer");
        protected readonly NonTerminal initializer_list = T("initializer_list", CreateListFromNode<Expression>);
        protected readonly NonTerminal iteration_statement = TT("iteration_statement");
        protected readonly NonTerminal jump_statement = TT("jump_statement");
        protected readonly NonTerminal literal = T("literal", CreateLiteralAst);
        protected readonly NonTerminal literal_expression = T("literal-expression", CreateLiteralExpressionAst);
        protected readonly NonTerminal literal_list = T("literal_list", CreateListFromNode<Literal>);
        protected readonly NonTerminal logical_and_expression = TT("logical_and_expression");
        protected readonly NonTerminal logical_and_expression_raw = T("logical_and_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal logical_or_expression = TT("logical_or_expression");
        protected readonly NonTerminal logical_or_expression_raw = T("logical_or_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal matrix_type = T("matrix_type");
        protected readonly NonTerminal matrix_type_simple = T("matrix_type_simple");
        protected readonly NonTerminal matrix_type_list = TT("matricx_type_list");
        protected readonly NonTerminal member_reference_expression = T("member_reference_expression", CreateMemberReferenceExpressionAst);
        protected readonly NonTerminal method_declaration = T("method_declaration", CreateMethodDeclarationAst);
        protected readonly NonTerminal method_declaration_raw = T("method_declaration_raw", CreateMethodDeclarationRawAst);
        protected readonly NonTerminal method_declarator = T("method_declarator", CreateMethodDeclaratorAst);
        protected readonly NonTerminal method_definition = T("method_definition", CreateMethodDefinitionAst);
        protected readonly NonTerminal method_definition_or_declaration = TT("method_definition_or_declaration");
        protected readonly NonTerminal method_invoke_expression = T("method_invoke_expression", CreateMethodInvokeExpressionAst);
        protected readonly NonTerminal method_invoke_expression_simple = T("method_invoke_expression_simple", CreateMethodInvokeExpressionAst);
        protected readonly NonTerminal method_qualifier_post = TT("method_qualifier_post");
        protected readonly NonTerminal multiplicative_expression = TT("multiplicative_expression");
        protected readonly NonTerminal multiplicative_expression_raw = T("multiplicative_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal object_type = TT("object_type");
        protected readonly NonTerminal parameter_declaration = T("parameter_declaration");
        protected readonly NonTerminal parameter_list = T("parameter_list", CreateListFromNode<Parameter>);
        protected readonly NonTerminal parameter_qualifier_pre = T("parameter_qualifier_pre");
        protected readonly NonTerminal parameter_qualifier_post = T("parameter_qualifier_post");
        protected readonly NonTerminal parameter_type = TT("parameter_type");
        protected readonly NonTerminal parenthesized_expression = T("parenthesized_expression", CreateParenthesizedExpressionAst);
        protected readonly NonTerminal post_incr_decr_expression = T("post_incr_decr_expression", CreatePostfixUnaryExpressionAst);
        protected readonly NonTerminal postfix_expression = TT("postfix_expression");
        protected readonly NonTerminal primary_expression = TT("primary-expression");
        protected readonly NonTerminal rank_specifier = T("rank_specifier", CreateRankSpecifierAst);
        protected readonly NonTerminal rank_specifier_empty = T("rank_specifier_empty", CreateRankSpecifierAst);
        protected readonly NonTerminal relational_expression = TT("relational_expression");
        protected readonly NonTerminal relational_expression_raw = T("relational_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal return_statement = T("return_statement", CreateReturnStatementAst);
        protected readonly NonTerminal sampler_type = T("sampler_type");
        protected readonly NonTerminal scalars = TT("scalars");
        protected readonly NonTerminal scalars_and_vectors = TT("scalars_and_vectors");
        protected readonly NonTerminal scalars_or_typename = TT("scalars_or_typename");
        protected readonly NonTerminal scope_declaration = TT("scope_declaration");
        protected readonly NonTerminal selection_statement = TT("selection_statement");
        protected readonly NonTerminal shader = T("shader", CreateShaderAst);
        protected readonly NonTerminal shift_expression = TT("shift_expression");
        protected readonly NonTerminal shift_expression_raw = T("shift_expression_raw", CreateBinaryExpressionAst);
        protected readonly NonTerminal simple_assignment_expression_statement = T("simple_assignment_expression_statement", CreateAssignementExpressionAst);
        protected readonly NonTerminal simple_type = TT("simple_type");
        protected readonly NonTerminal simple_type_or_type_name = TT("simple_type_or_type_name");
        protected readonly NonTerminal statement = T("Statement", CreateStatementAst);
        protected readonly NonTerminal statement_raw = TT("statement_raw");
        protected readonly NonTerminal storage_qualifier = T("storage_qualifier");
        protected readonly NonTerminal storage_qualifier_list_opt = T("storage_qualifier_list_opt", CreateQualifiers);
        protected readonly NonTerminal struct_specifier = T("struct_specifier", CreateStructureAst);
        protected readonly NonTerminal switch_case_group = T("switch_case_group", CreateSwitchCastGroupAst);
        protected readonly NonTerminal switch_case_statement = T("switch_case_statement", CreateCaseStatementAst);
        protected readonly NonTerminal switch_statement = T("switch_statement", CreateSwitchStatementAst);
        protected readonly NonTerminal toplevel_declaration = TT("toplevel_declaration");
        protected readonly NonTerminal toplevel_declaration_list = T("toplevel_declaration_list", CreateListFromNode<Node>);
        protected readonly NonTerminal type = TT("type");
        protected readonly NonTerminal type_for_cast = TT("type_for_cast");
        protected readonly NonTerminal type_name = T("type_name", CreateTypeNameAst);
        protected readonly NonTerminal typename_for_cast = T("typename_for_cast", CreateTypeNameAst);
        protected readonly NonTerminal unary_expression = TT("unary_expression");
        protected readonly NonTerminal unary_expression_raw = T("unary_expression_raw", CreateUnaryExpressionAst);
        protected readonly NonTerminal unary_operator = TT("unary_operator");
        protected readonly NonTerminal value_type = TT("value-type");
        protected readonly NonTerminal type_reference_expression = T("type_reference_expression", CreateTypeReferenceExpression);
        protected readonly NonTerminal variable_declaration = T("variable_declaration", CreateVariableGroupAst);
        protected readonly NonTerminal variable_declaration_raw = T("variable_declaration_raw", CreateVariableGroupRawAst);
        protected readonly NonTerminal variable_declarator = T("variable_declarator");
        protected readonly NonTerminal variable_declarator_raw = T("variable_declarator_raw");
        protected readonly NonTerminal variable_declarator_qualifier_post = TT("variable_declarator_qualifier_post");
        protected readonly NonTerminal variable_declarator_list = T("variable_declarator_list", CreateListFromNode<Variable>);
        protected readonly NonTerminal variable_identifier = T("variable_identifier", CreateVariableReferenceExpressionAst);

        ////protected readonly NonTerminal layout_qualifier_pre = T("layout_qualifier_pre");
        ////protected readonly NonTerminal layout_qualifier_post = T("layout_qualifier_post");

        protected readonly NonTerminal vector_type = T("vector_type");
        protected readonly NonTerminal vector_type_list = TT("vector_type_list");
        protected readonly NonTerminal void_type = T("void_type", (context, node) => Ast<TypeName>(node).Name = new Identifier("void") { Span = SpanConverter.Convert(node.Span) });
        protected readonly NonTerminal while_statement = T("while_statement", CreateWhileStatementAst);

        // ReSharper restore InconsistentNaming   
     
        internal Dictionary<TokenType, Terminal> TokenTypeToTerminals = new Dictionary<TokenType, Terminal>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderGrammar"/> class.
        /// </summary>
        protected ShaderGrammar()
        {
            GrammarComments = "Shader abstract";

            // ------------------------------------------------------------------------------------
            // Prepare mapping for GoldScanner
            // ------------------------------------------------------------------------------------

            // Global mappings
            Term(whitespace, TokenCategory.WhiteSpace, TokenType.Whitespace);
            Term(newline, TokenCategory.WhiteSpace, TokenType.NewLine);
            Term(single_line_comment, TokenCategory.Comment, TokenType.SingleLineComment);
            Term(multi_line_comment, TokenCategory.MultilineComment, TokenType.MultiLineComment);

            // Special unknown terminal
            Term(unknown, TokenCategory.Identifier, TokenType.Unknown);

            // Mapping for identifier, numbers
            Term(identifier_raw, TokenCategory.Identifier, TokenType.Identifier);
            Term(float_literal, TokenCategory.Number, TokenType.FloatingPointLiteral);
            Term(float_literal, TokenCategory.Number, TokenType.FloatingPointLiteralExponent);
            Term(integer_literal, TokenCategory.Number, TokenType.HexIntegerLiteral);
            Term(integer_literal, TokenCategory.Number, TokenType.OctalIntegerLiteral);
            Term(integer_literal, TokenCategory.Number, TokenType.StartWithNoZeroDecimalIntegerLiteral);
            Term(integer_literal, TokenCategory.Number, TokenType.StartWithZeroDecimalIntegerLiteral);

            // Preprocessor terminals
            Punc("\\", TokenType.LineContinuation);
            Punc("#", TokenType.Preprocessor);
            Punc("##", TokenType.TokenPasting);
            
            // Mapping for symbols, punctuation, delimiters...
            Op("@", TokenType.Arrobas);
            Op("!", TokenType.Not);
            Op("!=", TokenType.NotEqual);
            Op("&&", TokenType.And);
            Punc("(", TokenType.LeftParen);
            Punc(")", TokenType.RightParen);
            Op("*", TokenType.Mul);
            Op("*=", TokenType.MulAssign);
            Op("+", TokenType.Plus);
            Op("++", TokenType.PlusPlus);
            Op("+=", TokenType.AddAssign);
            Punc(",", TokenType.Comma);
            Op("-", TokenType.Minus);
            Op("--", TokenType.MinusMinus);
            Op("-=", TokenType.SubAssign);
            Op("/", TokenType.Div);
            Op("/=", TokenType.DivAssign);
            Op("%", TokenType.Mod);
            Op("%=", TokenType.ModAssign);
            Punc(":", TokenType.Colon);
            Punc(";", TokenType.Semi);
            Op("<", TokenType.LessThan);
            Op("<=", TokenType.LessThanOrEqual);
            Op("=", TokenType.Assign);
            Op("==", TokenType.Equal);
            Op(">", TokenType.GreaterThan);
            Op(">=", TokenType.GreaterThanOrEqual);
            Punc("?", TokenType.Question);
            Punc("[", TokenType.LeftBracket);
            Punc("]", TokenType.RightBracket);
            Punc("{", TokenType.LeftCurly);
            Op("||", TokenType.Or);
            Punc("}", TokenType.RightCurly);
            Punc(".", TokenType.Dot);
            Op("~", TokenType.BitwiseNot);
            Op("<<", TokenType.BitwiseShiftLeft);
            Op(">>", TokenType.BitwiseShiftRight);
            Op("&", TokenType.BitwiseAnd);
            Op("|", TokenType.BitwiseOr);
            Op("^", TokenType.BitwiseXor);
            Op("<<=", TokenType.BitwiseShiftLeftAssign);
            Op(">>=", TokenType.BitwiseShiftRightAssign);
            Op("&=", TokenType.BitwiseAndAssign);
            Op("|=", TokenType.BitwiseOrAssign);
            Op("^=", TokenType.BitwiseXorAssign);

            // ------------------------------------------------------------------------------------
            // Comments
            // ------------------------------------------------------------------------------------
            NonGrammarTerminals.Add(single_line_comment);
            NonGrammarTerminals.Add(multi_line_comment);

            identifier.Rule = identifier_raw;
            identifier_raw.AstNodeCreator = CreateIdentifierAst;

            semi_opt.Rule = Empty | PreferShiftHere() + ";";

            less_than.Rule = "<";

            // ------------------------------------------------------------------------------------
            // Types
            // ------------------------------------------------------------------------------------

            // Numnber rule
            number.Rule = integer_literal | float_literal;
            integer_literal.AstNodeCreator = CreateIntegerLiteral;
            float_literal.AstNodeCreator = CreateFloatLiteral;

            // Boolean rule
            boolean.Rule = Keyword("true") | Keyword("false");

            // Literal rule (no strings!)
            literal.Rule = number | boolean;

            // Predefined scalars and vectors
            scalars_and_vectors.Rule = vector_type_list | scalars | type_name;

            scalars_or_typename.Rule = scalars | type_name;

            // Typename
            type_name.Rule = identifier + new IdentifierResolverHint(true);

            // Simple types
            simple_type.Rule = scalars
                               | vector_type_list
                               | matrix_type_list;

            // Value types
            value_type.Rule = simple_type
                              | struct_specifier;

            // Object types
            object_type.Rule = sampler_type;

            // Void type
            void_type.Rule = Keyword("void");

            // Main type
            type.Rule = type_name
                        | value_type
                        | object_type
                        | void_type;

            // Parameter type doesn't have void because void can be used by a method without arguments
            parameter_type.Rule = type_name
                                  | value_type
                                  | object_type;

            simple_type_or_type_name.Rule = type_name | simple_type;


            // Special valuetype used for cast
            type_reference_expression.Rule = simple_type;

            // ------------------------------------------------------------------------------------
            // Complex type
            // ------------------------------------------------------------------------------------            
            field_declaration.Rule = variable_declaration;

            struct_specifier.Rule = Keyword("struct") + identifier.Opt() + "{" + field_declaration.ListOpt() + "}";

            // ------------------------------------------------------------------------------------
            // Expressions
            // ------------------------------------------------------------------------------------            

            // primary expressions
            primary_expression.Rule = variable_identifier | literal_expression | parenthesized_expression;

            identifier_extended.Rule = identifier;

            variable_identifier.Rule = identifier_extended;

            literal_expression.Rule = literal;

            parenthesized_expression.Rule = "(" + expression + ")";

            // postfix_expression
            postfix_expression.Rule = primary_expression
                                      | indexer_expression
                                      | method_invoke_expression
                                      | member_reference_expression
                                      | post_incr_decr_expression;

            indexer_expression.Rule = postfix_expression + rank_specifier;

            method_invoke_expression_simple.Rule = identifier + "(" + argument_expression_list.Opt() + ")";

            // Force to add value type for cast in order to support construct like x = (int(5));  <= need to disambiguate with cast (int[5])
            method_invoke_expression.Rule = type_reference_expression + "(" + argument_expression_list.Opt() + ")"
                                            | postfix_expression + "(" + argument_expression_list.Opt() + ")";

            member_reference_expression.Rule = postfix_expression + "." + identifier;

            post_incr_decr_expression.Rule = postfix_expression + incr_or_decr;

            argument_expression_list.Rule = MakePlusRule(argument_expression_list, ToTerm(","), assignment_expression);

            // unary_expression
            unary_expression_raw.Rule = incr_or_decr + unary_expression
                                        | unary_operator + cast_expression;

            unary_expression.Rule = postfix_expression
                                    | unary_expression_raw;

            incr_or_decr.Rule = ToTerm("++") | "--";

            unary_operator.Rule = ToTerm("+") | "-" | "!" | "~" | "*";

            cast_expression.Rule = unary_expression;

            // multiplicative_expression
            multiplicative_expression_raw.Rule = multiplicative_expression + "*" + cast_expression
                                                 | multiplicative_expression + "/" + cast_expression
                                                 | multiplicative_expression + "%" + cast_expression;

            multiplicative_expression.Rule = cast_expression | multiplicative_expression_raw;

            // additive_expression
            additive_expression_raw.Rule = additive_expression + "+" + multiplicative_expression | additive_expression + "-" + multiplicative_expression;

            additive_expression.Rule = multiplicative_expression | additive_expression_raw;

            // shift_expression
            shift_expression_raw.Rule = shift_expression + "<<" + additive_expression | shift_expression + ">>" + additive_expression;

            shift_expression.Rule = additive_expression | shift_expression_raw;

            // relational_expression
            relational_expression_raw.Rule = relational_expression + less_than + shift_expression | relational_expression + ">" + shift_expression
                                             | relational_expression + "<=" + shift_expression | relational_expression + ">=" + shift_expression;

            relational_expression.Rule = shift_expression | relational_expression_raw;

            // equality_expression
            equality_expression_raw.Rule = equality_expression + "==" + relational_expression | equality_expression + "!=" + relational_expression;

            equality_expression.Rule = relational_expression | equality_expression_raw;

            // and_expression
            and_expression_raw.Rule = and_expression + "&" + equality_expression;

            and_expression.Rule = equality_expression
                                  | and_expression_raw;

            // exclusive
            exclusive_or_expression_raw.Rule = exclusive_or_expression + "^" + and_expression;
            exclusive_or_expression.Rule = and_expression
                                           | exclusive_or_expression_raw;

            // inclusive
            inclusive_or_expression_raw.Rule = inclusive_or_expression + "|" + exclusive_or_expression;

            inclusive_or_expression.Rule = exclusive_or_expression | inclusive_or_expression_raw;

            // logical and          
            logical_and_expression_raw.Rule = logical_and_expression + "&&" + inclusive_or_expression;

            logical_and_expression.Rule = inclusive_or_expression | logical_and_expression_raw;

            // logical or
            logical_or_expression_raw.Rule = logical_or_expression + "||" + logical_and_expression;

            logical_or_expression.Rule = logical_and_expression
                                         | logical_or_expression_raw;

            // conditional
            conditional_expression.Rule = logical_or_expression
                                          | conditional_expression_raw;

            conditional_expression_raw.Rule = logical_or_expression + "?" + expression + ":" + conditional_expression;

            // assignment
            assignment_expression.Rule = conditional_expression
                                         | assignment_expression_raw;

            assignment_expression_raw.Rule = unary_expression + assignment_operator + assignment_expression;

            assignment_operator.Rule = ToTerm("=") | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | ">>=";

            // expression
            expression.Rule = expression_list;

            expression_list.Rule = MakePlusRule(expression_list, ToTerm(","), assignment_expression);
            expression_list.ErrorRule = SyntaxError + ";";

            expression_or_empty.Rule = Empty | expression;

            // rank_specifier
            rank_specifier.Rule = "[" + expression + "]";
            rank_specifier_empty.Rule = "[" + expression_or_empty + "]";

            simple_assignment_expression_statement.Rule = indexable_identifier + assignment_operator + expression + ";";

            indexable_identifier.Rule = identifier_extended + rank_specifier.ListOpt();

            indexable_identifier_declarator.Rule = identifier_extended + rank_specifier_empty.ListOpt();

            // constant expression - used to plug builtin verification during Ast creation
            constant_expression.Rule = conditional_expression;

            // ------------------------------------------------------------------------------------
            // Variable modifiers
            // ------------------------------------------------------------------------------------
            // storageClass = Storage_Class + Type_Modifier
            storage_qualifier.Rule = Keyword("const") | Keyword("uniform");
            storage_qualifier.AstNodeCreator = CreateStorageQualifier;

            storage_qualifier_list_opt.Rule = Empty | storage_qualifier.List();

            // layout_qualifier_pre.Rule = null;

            // layout_qualifier_post.Rule = null;

            variable_declarator_qualifier_post.Rule = null;

            // ------------------------------------------------------------------------------------
            // Declarations
            // ------------------------------------------------------------------------------------
            declaration.Rule = variable_declaration;
            
            variable_declaration.Rule = attribute_qualifier_pre + variable_declaration_raw;

            variable_declaration_raw.Rule = declaration_specifiers + variable_declarator_list.Opt() + ";";

            declaration_specifiers.Rule = type
                                          | storage_qualifier.List() + type;

            variable_declarator_list.Rule = MakePlusRule(variable_declarator_list, ToTerm(","), variable_declarator);
            
            variable_declarator_raw.Rule = indexable_identifier_declarator + variable_declarator_qualifier_post;
            variable_declarator_raw.AstNodeCreator = CreateVariableDeclaratorRawAst;

            variable_declarator.Rule = variable_declarator_raw
                                       | variable_declarator_raw + "=" + initializer;
            variable_declarator.AstNodeCreator = CreateVariableDeclaratorAst;

            initializer.Rule = assignment_expression
                               | array_initializer_expression;

            array_initializer_expression.Rule = "{" + initializer_list + "}"
                                                | "{" + initializer_list + "," + "}";
      
            initializer_list.Rule = MakePlusRule(initializer_list, ToTerm(","), initializer);

            // Attribute qualifier pre
            attribute_qualifier_pre.Rule = null;

            // Method
            method_qualifier_post.Rule = null;

            method_declaration_raw.Rule = attribute_qualifier_pre + declaration_specifiers + method_declarator + method_qualifier_post;

            method_declaration.Rule = method_declaration_raw + ";";

            var optional_block_statement_list = block_item.ListOpt();
            method_definition.Rule = method_declaration_raw + "{" + optional_block_statement_list + "}" + semi_opt;


            method_definition_or_declaration.Rule = method_declaration | method_definition;

            method_declarator.Rule = identifier + "(" + parameter_list + ")"
                                     | identifier + "(" + "void" + ")"
                                     | identifier + "(" + ")";

            parameter_list.Rule = MakePlusRule(parameter_list, ToTerm(","), parameter_declaration);

            // Need to be fill out woth pre qualifier
            parameter_qualifier_pre.Rule = null;
            parameter_qualifier_post.Rule = null;

            parameter_declaration.Rule = attribute_qualifier_pre + parameter_qualifier_pre + parameter_type + indexable_identifier.Opt() + parameter_qualifier_post; // +parameter_declaration_raw;
            parameter_declaration.AstNodeCreator = CreateParameterAst;

            identifier_list.Rule = MakePlusRule(identifier_list, ToTerm(","), identifier);

            // ------------------------------------------------------------------------------------
            // Statements
            // ------------------------------------------------------------------------------------
            statement.Rule = attribute_qualifier_pre + statement_raw;

            statement_raw.Rule = discard_statement
                             | block_statement
                             | expression_statement
                             | selection_statement
                             | iteration_statement
                             | jump_statement;
            
            discard_statement.Rule = Keyword("discard") + ";";

            // skip all until semicolon
            // statement.ErrorRule = SyntaxError + ";" | SyntaxError + "}";

            block_statement.Rule = "{" + optional_block_statement_list + "}";

            // Add an error rule at optional_block_statement_list
            optional_block_statement_list.ErrorRule = SyntaxError + ";";

            declaration_statement.Rule = declaration;

            block_item.Rule = declaration_statement
                              | statement;

            empty_statement.Rule = ";";

            expression_statement.Rule = empty_statement
                                        | expression + ";";

            selection_statement.Rule = if_statement
                                       | switch_statement;

            iteration_statement.Rule = while_statement
                                       | do_while_statement
                                       | for_statement;

            if_statement.Rule = Keyword("if") + "(" + expression + ")" + statement
                                | Keyword("if") + "(" + expression + ")" + statement + PreferShiftHere() + Keyword("else") + statement;


            switch_statement.Rule = Keyword("switch") + "(" + expression + ")" + "{" + switch_case_group.ListOpt() + "}";

            switch_case_group.Rule = switch_case_statement.List() + statement.List();

            switch_case_statement.Rule = Keyword("case") + constant_expression + ":"
                                         | Keyword("default") + ":";

            while_statement.Rule = Keyword("while") + "(" + expression + ")" + statement;

            do_while_statement.Rule = Keyword("do") + statement + "while" + "(" + expression + ")" + ";";

            for_statement.Rule = Keyword("for") + "(" + expression_statement + expression.Opt() + ";" + expression.Opt() + ")" + statement
                                 | Keyword("for") + "(" + variable_declaration_raw + expression.Opt() + ";" + expression.Opt() + ")" + statement;

            break_statement.Rule = Keyword("break") + ";";
            continue_statement.Rule = Keyword("continue") + ";";
            return_statement.Rule = Keyword("return") + ";" | Keyword("return") + expression + ";";

            jump_statement.Rule = continue_statement
                                  | break_statement
                                  | return_statement;

            literal_list.Rule = MakePlusRule(literal_list, ToTerm(","), literal);

            // ------------------------------------------------------------------------------------
            // Top Level
            // ------------------------------------------------------------------------------------
            toplevel_declaration.Rule = scope_declaration;

            scope_declaration.Rule = method_definition_or_declaration | declaration | empty_statement;

            toplevel_declaration_list.Rule = MakeStarRule(toplevel_declaration_list, null, toplevel_declaration);

            shader.Rule = toplevel_declaration_list;

            shader.ErrorRule = SyntaxError + ";";

            Root = shader;

            // ------------------------------------------------------------------------------------
            // Preprocessor declaration
            // ------------------------------------------------------------------------------------

            // Using a special terminal to match lines
            //NonGrammarTerminals.Add(new PreprocessorLines());            

            // ------------------------------------------------------------------------------------
            // Globals
            // ------------------------------------------------------------------------------------
            // MarkReservedWords("true", "false", "do", "while", "for", "if", "then", "else", "case", "switch", "continue", "break", "return");

            Delimiters = "{}[](),:;+-*/%&|^!~<>=";
            MarkPunctuation(";", ",", ":");

            // CR, linefeed, nextLine, LineSeparator, paragraphSeparator
            LineTerminators = "\r\n\u2085\u2028\u2029";

            // Add extra line terminators
            WhitespaceChars = " \t\r\n\v\u2085\u2028\u2029";

            LanguageFlags = LanguageFlags.NewLineBeforeEOF;
        }
    }
}
