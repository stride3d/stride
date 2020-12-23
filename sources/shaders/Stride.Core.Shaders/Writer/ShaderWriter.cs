// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Writer
{
    /// <summary>
    /// A writer for a shader.
    /// </summary>
    public class ShaderWriter : ShaderWalker
    {
        private bool isInVariableGroup;
        private bool isVisitingVariableInlines;

        private int lineCount;
        
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderWriter"/> class.
        /// </summary>
        /// <param name="buildScopeDeclaration">if set to <c>true</c> [build scope declaration].</param>
        /// <param name="useNodeStack">if set to <c>true</c> [use node stack].</param>
        public ShaderWriter(bool buildScopeDeclaration = false, bool useNodeStack = false)
            : base(buildScopeDeclaration, useNodeStack)
        {
            StringBuilder = new StringBuilder();
            EnableNewLine = true;
            lineCount = 1;
            SourceLocations = new List<SourceLocation>();
        }

        #endregion

        #region Public Properties

        public List<SourceLocation> SourceLocations { get; set; }

        /// <summary>
        ///   Gets the text.
        /// </summary>
        public string Text
        {
            get
            {
                return StringBuilder.ToString();
            }
        }

        #endregion

        #region Properties

        public bool EnablePreprocessorLine { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether [enable new line].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable new line]; otherwise, <c>false</c>.
        /// </value>
        protected bool EnableNewLine { get; set; }

        /// <summary>
        ///   Gets or sets the indent level.
        /// </summary>
        /// <value>
        ///   The indent level.
        /// </value>
        private int IndentLevel { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether [new line].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [new line]; otherwise, <c>false</c>.
        /// </value>
        private bool NewLine { get; set; }

        /// <summary>
        ///   Gets or sets the string builder.
        /// </summary>
        /// <value>
        ///   The string builder.
        /// </value>
        private StringBuilder StringBuilder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is visiting variable inlines.
        /// </summary>
        /// <value><c>true</c> if this instance is visiting variable inlines; otherwise, <c>false</c>.</value>
        public bool IsVisitingVariableInlines
        {
            get
            {
                return isVisitingVariableInlines;
            }
            set
            {
                isVisitingVariableInlines = value;
            }
        }

        protected Stack<bool> IsDeclaratingVariable = new Stack<bool>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Indents this instance.
        /// </summary>
        /// <returns>
        /// this instance
        /// </returns>
        public ShaderWriter Indent()
        {
            IndentLevel++;
            return this;
        }

        /// <summary>
        /// Outdents this instance.
        /// </summary>
        /// <returns>
        /// this instance
        /// </returns>
        public ShaderWriter Outdent()
        {
            IndentLevel--;
            return this;
        }

        /// <summary>
        /// Visits the specified shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        /// <returns></returns>
        public override void Visit(Shader shader)
        {
            base.Visit(shader);
        }

        /// <inheritdoc />
        public override void Visit(StructType structType)
        {
            WriteLinkLine(structType);

            // Pre Attributes
            Write(structType.Attributes, true);

            WriteLinkLine(structType);
            Write("struct");
            if (structType.Name != null)
            {
                Write(" ");
                Write(structType.Name);
                WriteSpace();
            }

            // Post Attributes
            Write(structType.Attributes, false);

            OpenBrace();

            foreach (var variableDeclaration in structType.Fields) 
                VisitDynamic(variableDeclaration);

            CloseBrace(false).Write(";").WriteLine();
        }

        /// <inheritdoc />
        public override void Visit(WhileStatement whileStatement)
        {
            WriteLinkLine(whileStatement);
            VisitStatement(whileStatement);

            if (whileStatement.IsDoWhile)
            {
                Write("do").WriteSpace();
                WriteStatementContent(whileStatement.Statement);
                Write("while").WriteSpace().Write("(");
                VisitDynamic(whileStatement.Condition);
                Write(")");
                Write(";");
                WriteLine();
            }
            else
            {
                Write("while").WriteSpace().Write("(");
                VisitDynamic(whileStatement.Condition);
                Write(")");
                WriteStatementContent(whileStatement.Statement);
            }

            WriteLine();
        }

        /// <inheritdoc />
        public override void Visit(ArrayInitializerExpression arrayInitializerExpression)
        {
            Write("{").WriteSpace();
            for (int i = 0; i < arrayInitializerExpression.Items.Count; i++)
            {
                var expression = arrayInitializerExpression.Items[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(expression);
            }

            Write("}");
        }

        /// <inheritdoc />
        public override void Visit(BlockStatement blockStatement)
        {
            OpenBrace();
            foreach (var statement in blockStatement.Statements)
            {
                VisitDynamic(statement);
            }

            CloseBrace();
        }

        /// <inheritdoc />
        public override void Visit(AssignmentExpression assignmentExpression)
        {
            VisitDynamic(assignmentExpression.Target);
            WriteSpace().Write(assignmentExpression.Operator.ConvertToString()).WriteSpace();
            VisitDynamic(assignmentExpression.Value);
        }

        /// <inheritdoc />
        public override void Visit(BinaryExpression binaryExpression)
        {
            VisitDynamic(binaryExpression.Left);
            WriteSpace().Write(binaryExpression.Operator.ConvertToString()).WriteSpace();
            VisitDynamic(binaryExpression.Right);
        }

        /// <inheritdoc />
        public override void Visit(CaseStatement statement)
        {
            WriteLinkLine(statement);
            if (statement.Case == null) WriteLine("default:");
            else
            {
                Write("case").Write(" ");
                VisitDynamic(statement.Case);
                WriteLine(":");
            }
        }

        /// <inheritdoc/>
        public override void Visit(ArrayType arrayType)
        {
            VisitDynamic(arrayType.Type);
            WriteRankSpecifiers(arrayType.Dimensions);
        }

        /// <inheritdoc />
        public override void Visit(ExpressionStatement expressionStatement)
        {
            WriteLinkLine(expressionStatement);
            VisitDynamic(expressionStatement.Expression);
            WriteLine(";");
        }

        /// <inheritdoc />
        public override void Visit(ForStatement forStatement)
        {
            WriteLine();
            WriteLinkLine(forStatement);
            VisitStatement(forStatement);
            Write("for").WriteSpace().Write("(");
            EnableNewLine = false;
            VisitDynamic(forStatement.Start);
            WriteSpace();
            VisitDynamic(forStatement.Condition);
            Write(";");
            WriteSpace();
            VisitDynamic(forStatement.Next);
            EnableNewLine = true;
            Write(")");
            WriteStatementContent(forStatement.Body);
        }

        /// <inheritdoc />
        public override void Visit(Identifier identifier)
        {
            Write(identifier);
        }

        /// <inheritdoc/>
        public void VisitStatement(Statement statement)
        {
            Write(statement.Attributes, true);
        }

        /// <inheritdoc/>
        public override void Visit(StatementList statementList)
        {
            foreach (var statement in statementList)
                VisitDynamic(statement);
        }

        /// <inheritdoc />
        public override void Visit(IfStatement ifStatement)
        {
            WriteLinkLine(ifStatement);
            VisitStatement(ifStatement);

            Write("if").WriteSpace().Write("(");
            VisitDynamic(ifStatement.Condition);
            Write(")");
            WriteStatementContent(ifStatement.Then);
            if (ifStatement.Else != null)
            {
                WriteLinkLine(ifStatement.Else);
                Write("else");
                var nestedIfStatement = ifStatement.Else as IfStatement;
                if (nestedIfStatement != null && nestedIfStatement.Attributes.Count == 0)
                {
                    Write(" ");
                    Visit(nestedIfStatement);
                }
                else WriteStatementContent(ifStatement.Else);
            }
        }

        /// <inheritdoc />
        public override void Visit(IndexerExpression indexerExpression)
        {
            VisitDynamic(indexerExpression.Target);
            Write("[");
            VisitDynamic(indexerExpression.Index);
            Write("]");
        }

        /// <inheritdoc />
        public override void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            VisitDynamic(memberReferenceExpression.Target);
            Write(".");
            VisitDynamic(memberReferenceExpression.Member);
        }

        /// <inheritdoc />
        public override void Visit(MethodInvocationExpression methodInvocationExpression)
        {
            VisitDynamic(methodInvocationExpression.Target);
            Write("(");
            for (int i = 0; i < methodInvocationExpression.Arguments.Count; i++)
            {
                var expression = methodInvocationExpression.Arguments[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(expression);
            }

            Write(")");
        }

        /// <inheritdoc />
        public override void Visit(Parameter parameter)
        {
            WriteVariable(parameter);
        }

        /// <inheritdoc />
        public override void Visit(ParenthesizedExpression parenthesizedExpression)
        {
            Write("(");
            VisitDynamic(parenthesizedExpression.Content);
            Write(")");
        }

        /// <inheritdoc />
        public override void Visit(ExpressionList expressionList)
        {
            for (int i = 0; i < expressionList.Count; i++)
            {
                var expression = expressionList[i];
                if (i > 0) Write(",").WriteSpace();
                VisitDynamic(expression);
            }
        }
        
        /// <inheritdoc />
        public override void Visit(ReturnStatement returnStatement)
        {
            WriteLinkLine(returnStatement);
            Write("return");
            if (returnStatement.Value != null)
            {
                Write(" ");
                VisitDynamic(returnStatement.Value);
            }

            WriteLine(";");
        }

        /// <inheritdoc />
        public override void Visit(ConditionalExpression conditionalExpression)
        {
            VisitDynamic(conditionalExpression.Condition);
            WriteSpace().Write("?").WriteSpace();
            VisitDynamic(conditionalExpression.Left);
            WriteSpace().Write(":").WriteSpace();
            VisitDynamic(conditionalExpression.Right);
        }

        /// <inheritdoc />
        public override void Visit(UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operator.IsPostFix())
            {
                VisitDynamic(unaryExpression.Expression);
                Write(unaryExpression.Operator.ConvertToString());
            }
            else
            {
                Write(unaryExpression.Operator.ConvertToString());
                VisitDynamic(unaryExpression.Expression);
            }
        }

        /// <inheritdoc />
        public override void Visit(SwitchStatement switchStatement)
        {
            WriteLinkLine(switchStatement);
            Write("switch").WriteSpace().Write("(");
            VisitDynamic(switchStatement.Condition);
            Write(")");
            WriteLine();
            OpenBrace();

            VisitList(switchStatement.Groups);

            CloseBrace();
        }

        /// <inheritdoc />
        public override void Visit(SwitchCaseGroup switchCaseGroup)
        {
            VisitList(switchCaseGroup.Cases);
            Indent();
            VisitDynamic(switchCaseGroup.Statements);
            Outdent();
        }

        /// <inheritdoc />
        public override void Visit(DeclarationStatement declarationStatement)
        {
            WriteLinkLine(declarationStatement);
            VisitDynamic(declarationStatement.Content);
        }

        /// <inheritdoc />
        public override void Visit(MethodDeclaration methodDeclaration)
        {
            WriteLinkLine(methodDeclaration);
            WriteMethodDeclaration(methodDeclaration).WriteLine(";");
        }

        /// <inheritdoc />
        public override void Visit(MethodDefinition methodDefinition)
        {
            WriteLinkLine(methodDefinition);
            WriteMethodDeclaration(methodDefinition);
            
            OpenBrace();
            foreach (var statement in methodDefinition.Body) 
                VisitDynamic(statement);
            CloseBrace();
        }

        /// <inheritdoc />
        public override void Visit(Variable variable)
        {
            WriteLinkLine(variable);
            WriteVariable(variable);
        }

        /// <inheritdoc />
        public override void Visit(ObjectType typeBase)
        {
            Write(typeBase.Name);
        }

        /// <inheritdoc />
        public override void Visit(TypeName typeBase)
        {
            Write(typeBase.Name);
        }

        /// <inheritdoc />
        public override void Visit(ScalarType scalarType)
        {
            Write(scalarType.Qualifiers, true);
            Write(scalarType.Name);
        }

        /// <inheritdoc />
        public override void Visit(GenericType genericType)
        {
            Write(genericType.Name).Write("<");
            for (int i = 0; i < genericType.Parameters.Count; i++)
            {
                var parameter = genericType.Parameters[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(parameter);
            }

            Write(">");
        }

        /// <inheritdoc />
        public override void Visit(VectorType vectorType)
        {
            Write(vectorType.Name).Write("<");
            for (int i = 0; i < vectorType.Parameters.Count; i++)
            {
                var parameter = vectorType.Parameters[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(parameter);
            }

            Write(">");
        }

        /// <inheritdoc />
        public override void Visit(MatrixType vectorType)
        {
            Write(vectorType.Name).Write("<");
            for (int i = 0; i < vectorType.Parameters.Count; i++)
            {
                var parameter = vectorType.Parameters[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(parameter);
            }

            Write(">");
        }

        /// <inheritdoc />
        public override void Visit(Literal literal)
        {
            if (literal == null)
            {
                return;
            }

            var isStringLiteral = literal.Value is string && !literal.Text.StartsWith("\"");
            if (isStringLiteral)
            {
                Write("\"");
            }
            if (literal.SubLiterals != null && literal.SubLiterals.Count > 0)
            {
                foreach (var subLiteral in literal.SubLiterals) Write(subLiteral.Text);
            }
            else Write(literal.Text);
            if (isStringLiteral)
            {
                Write("\"");
            }
        }

        /// <inheritdoc/>
        public override void Visit(Qualifier qualifier)
        {
            Write(qualifier.Key.ToString());
        }

        /// <inheritdoc/>
        public override void Visit(Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            Write(layoutQualifier.Key.ToString());
        }

        /// <summary>
        /// Writes the specified qualifier.
        /// </summary>
        /// <param name="qualifiers">
        /// The qualifier.
        /// </param>
        /// <param name="writePreQualifiers">
        /// if set to <c>true</c> [write pre qualifiers].
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        public ShaderWriter Write(Qualifier qualifiers, bool writePreQualifiers)
        {
            if (qualifiers == Qualifier.None) return this;

            foreach (var genericQualifier in qualifiers.Values)
            {
                var qualifier = (Qualifier)genericQualifier;

                if (qualifier == Qualifier.None || qualifier.IsPost == writePreQualifiers) 
                    continue;

                if (qualifier.IsPost) 
                    Write(" ");

                VisitDynamic(qualifier);

                if (!qualifier.IsPost) 
                    Write(" ");
            }

            return this;
        }

        /// <summary>
        /// Writes the specified attributes.
        /// </summary>
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        /// <param name="writePreQualifiers">
        /// if set to <c>true</c> [write pre qualifiers].
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        public ShaderWriter Write(List<AttributeBase> attributes, bool writePreQualifiers)
        {
            if (attributes == null || attributes.Count == 0) return this;

            foreach (var attribute in attributes)
            {
                if (attribute is PostAttributeBase == writePreQualifiers) 
                    continue;

                VisitDynamic(attribute);
            }

            return this;
        }

        /// <summary>
        /// Writes the specified text.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// this instance
        /// </returns>
        public ShaderWriter Write(string text)
        {
            PrefixIndent();
            Append(text);
            return this;
        }

        /// <summary>
        /// Writes the initializer.
        /// </summary>
        /// <param name="expression">
        /// The expression.
        /// </param>
        public virtual void WriteInitializer(Expression expression)
        {
            if (expression == null) return;

            WriteSpace().Write("=");
            WriteSpace();
            VisitDynamic(expression);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <returns>
        /// This instance
        /// </returns>
        public ShaderWriter WriteLine()
        {
            if (EnableNewLine)
            {
                StringBuilder.AppendLine();
                NewLine = true;
                lineCount++;
            }

            return this;
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// this instance
        /// </returns>
        public ShaderWriter WriteLine(string text)
        {
            if (EnableNewLine)
            {
                PrefixIndent();
                StringBuilder.AppendLine(text);
                NewLine = true;
                lineCount++;
            }
            else StringBuilder.Append(text);

            return this;
        }

        private string previousSourceFileName = null;

        /// <summary>
        /// Writes a link line using #line preprocessing directive with the specified node
        /// </summary>
        /// <param name="node">The node to use the Span.</param>
        /// <returns>This instance</returns>
        protected ShaderWriter WriteLinkLine(Node node)
        {
            if (!EnablePreprocessorLine || node.Span.Location.Line == 0)
                return this;

            var newSourceFile = node.Span.Location.FileSource;
            var sourceLocation = string.Empty;
            if (previousSourceFileName != newSourceFile)
            {
                sourceLocation = string.Format(" \"{0}\"", newSourceFile);
                previousSourceFileName = newSourceFile;
            }

            Append(Environment.NewLine).Append("#line {0}{1}", node.Span.Location.Line, sourceLocation).Append(Environment.NewLine);
            NewLine = true;
            lineCount++;
            return this;
        }

        /// <summary>
        /// Writes the space.
        /// </summary>
        /// <returns>
        /// this instance
        /// </returns>
        public ShaderWriter WriteSpace()
        {
            Append(" ");
            return this;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Appends the specified text.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// this instance
        /// </returns>
        protected ShaderWriter Append(string text)
        {
            StringBuilder.Append(text);
            return this;
        }

        /// <summary>
        /// Appends the specified formatted text.
        /// </summary>
        /// <param name="format">The formatted text.</param>
        /// <param name="args">The args to apply to the formatted text.</param>
        /// <returns>This instance</returns>
        protected ShaderWriter Append(string format, params object[] args)
        {
            PrefixIndent();
            StringBuilder.AppendFormat(format, args);
            return this;
        }

        /// <summary>
        /// Closes the brace.
        /// </summary>
        /// <param name="newLine">
        /// if set to <c>true</c> [new line].
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        protected ShaderWriter CloseBrace(bool newLine = true)
        {
            Outdent();
            Write("}");
            if (newLine) WriteLine();

            return this;
        }

        /// <summary>
        /// Opens the brace.
        /// </summary>
        /// <returns>
        /// This instance
        /// </returns>
        protected ShaderWriter OpenBrace()
        {
            WriteLine();
            Write("{");
            WriteLine();
            Indent();
            return this;
        }

        /// <summary>
        /// Writes the specified identifier.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        protected virtual ShaderWriter Write(Identifier identifier)
        {
            if (identifier.IsSpecialReference) 
                Write("<");

            Write(identifier.Text);

            if (identifier.HasIndices) 
                WriteRankSpecifiers(identifier.Indices);

            if (identifier.IsSpecialReference) 
                Write(">");

            return this;
        }

        /// <summary>
        /// Writes the specified method declaration.
        /// </summary>
        /// <param name="methodDeclaration">
        /// The method declaration.
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        protected virtual ShaderWriter WriteMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            isVisitingVariableInlines = true;

            // Pre Attributes
            Write(methodDeclaration.Attributes, true);

            // Pre Qualifiers
            Write(methodDeclaration.Qualifiers, true);

            VisitDynamic(methodDeclaration.ReturnType);

            Write(" ");
            Write(methodDeclaration.Name);

            Write("(");

            for (int i = 0; i < methodDeclaration.Parameters.Count; i++)
            {
                var parameter = methodDeclaration.Parameters[i];
                if (i > 0) Write(",").WriteSpace();

                VisitDynamic(parameter);
            }

            Write(")");

            // Post Qualifiers
            Write(methodDeclaration.Qualifiers, false);

            // Post Attributes
            Write(methodDeclaration.Attributes, false);

            isVisitingVariableInlines = false;
            return this;
        }

        /// <summary>
        /// Writes the rank specifiers.
        /// </summary>
        /// <param name="expressionList">
        /// The expression list.
        /// </param>
        /// <returns>
        /// This instance
        /// </returns>
        protected ShaderWriter WriteRankSpecifiers(IEnumerable<Expression> expressionList)
        {
            foreach (var expression in expressionList)
            {
                Write("[");
                VisitDynamic(expression);
                Write("]");
            }

            return this;
        }

        /// <summary>
        /// Writes the content of the statement.
        /// </summary>
        /// <param name="statement">
        /// The statement.
        /// </param>
        protected void WriteStatementContent(Statement statement)
        {
            if (statement is BlockStatement)
            {
                VisitDynamic(statement);
            }
            else
            {
                bool needBraces = (statement is StatementList && ((StatementList)statement).Count > 1);

                if (needBraces)
                {
                    OpenBrace();
                    VisitDynamic(statement);
                    CloseBrace();
                }
                else
                {
                    WriteLine();
                    Indent();
                    VisitDynamic(statement);
                    Outdent();
                }
            }
        }

        /// <summary>
        /// Writes the variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        protected void WriteVariable(Variable variable)
        {
            // Pre Attributes
            Write(variable.Attributes, true);

            // Pre qualifiers
            Write(variable.Qualifiers, true);

            var arrayType = variable.Type as ArrayType;
            var baseType = arrayType == null ? variable.Type : arrayType.Type;

            // If this is a variable declarator not attached to a group, output the type
            if (!isInVariableGroup)
            {
                IsDeclaratingVariable.Push(true);
                VisitDynamic(baseType);
                IsDeclaratingVariable.Pop();
                WriteSpace();
            }

            if (variable.IsGroup)
            {
                // Enter a variable group
                isInVariableGroup = true;

                for (int i = 0; i < variable.SubVariables.Count; i++)
                {
                    var subVariable = variable.SubVariables[i];
                    if (i > 0)
                        Write(",").WriteSpace();
                    VisitDynamic(subVariable);
                }

                isInVariableGroup = false;
            }
            else
            {
                Write(variable.Name);
            }

            if (arrayType != null)
            {
                WriteRankSpecifiers(arrayType.Dimensions);
            }

            // Post qualifiers
            Write(variable.Qualifiers, false);

            // Post Attributes
            Write(variable.Attributes, false);

            WriteInitializer(variable.InitialValue);

            // A variable can be a parameter or a grouped variable.
            // If this is a parameter and we are visiting a method declaration, don't output the ";"
            // If we are inside a group variable, don't output ";" as the upper level will add "," to separate variables.
            if (!isInVariableGroup && !isVisitingVariableInlines)
                WriteLine(";");

        }

        private void PrefixIndent()
        {
            if (NewLine)
            {
                for (int i = 0; i < IndentLevel; ++i) 
                    Append("    ");

                NewLine = false;
            }
        }

        protected override bool PreVisitNode(Node node)
        {
            var fileSource = Path.GetFileName(node.Span.Location.FileSource);

            if (string.Compare(fileSource, "internal_hlsl_declarations.hlsl", StringComparison.OrdinalIgnoreCase) != 0 && node.Span.Length > 0)
            {
                while (SourceLocations.Count < lineCount)
                {
                    SourceLocations.Add(new SourceLocation(node.Span.Location.FileSource, node.Span.Location.Position, node.Span.Location.Line, 1));
                }
            }

            return base.PreVisitNode(node);
        }

        #endregion
    }
}
