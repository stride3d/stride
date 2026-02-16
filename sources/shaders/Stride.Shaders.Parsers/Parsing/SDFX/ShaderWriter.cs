using System.Text;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Core;

/// <summary>
/// Writes shader AST to text. Note that implementation is not complete and verified (it was only for SDFX needs), please don't use in production.
/// </summary>
public class ShaderWriter : NodeWalker
{
    /// <summary>
    ///   Gets or sets a value indicating whether [enable new line].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [enable new line]; otherwise, <c>false</c>.
    /// </value>
    protected bool EnableNewLine { get; set; } = true;

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
    private StringBuilder StringBuilder { get; set; } = new();
    
    private void PrefixIndent()
    {
        if (NewLine)
        {
            for (int i = 0; i < IndentLevel; ++i) 
                Append("    ");

            NewLine = false;
        }
    }
    
    /// <summary>
    ///   Gets the text.
    /// </summary>
    public string Text => StringBuilder.ToString();

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
        Write("{");
        WriteLine();
        Indent();
        return this;
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
        }
        else StringBuilder.Append(text);

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
    /// Writes the content of the statement.
    /// </summary>
    /// <param name="statement">
    /// The statement.
    /// </param>
    protected void WriteStatementContent(Statement statement)
    {
        if (statement is BlockStatement)
        {
            VisitNode(statement);
        }
        else
        {
            Indent();
            VisitNode(statement);
            Outdent();
        }
    }
    
    public override void VisitIdentifier(Identifier identifier)
    {
        Write(identifier.Name);
    }

    public override void VisitGenericIdentifier(GenericIdentifier identifier)
    {
        Write(identifier.Name);
        if (identifier.Generics != null && identifier.Generics.Values.Count > 0)
        {
            Write("<");
            for (var i = 0; i < identifier.Generics.Values.Count; i++)
            {
                VisitNode(identifier.Generics.Values[i]);
                if (i < identifier.Generics.Values.Count - 1)
                    Write(",").WriteSpace();
            }
            Write(">");
        }
    }

    public override void VisitTypeName(TypeName typeName)
    {
        Write(typeName.Name);
        if (typeName.Generics.Count > 0)
        {
            Write("<");
            for (var i = 0; i < typeName.Generics.Count; i++)
            {
                VisitNode(typeName.Generics[i]);
                if (i < typeName.Generics.Count - 1) Write(",").WriteSpace();
            }
            Write(">");
        }
        if (typeName.IsArray)
        {
            foreach (var size in typeName.ArraySize!)
            {
                Write("[");
                VisitNode(size);
                Write("]");
            }
        }
    }

    public override void VisitIntegerLiteral(IntegerLiteral integerLiteral)
    {
        Write(integerLiteral.Value.ToString());
    }

    public override void VisitFloatLiteral(FloatLiteral floatLiteral)
    {
        Write(floatLiteral.Value.ToString());
    }

    public override void VisitBoolLiteral(BoolLiteral boolLiteral)
    {
        Write(boolLiteral.Value ? "true" : "false");
    }

    public override void VisitStringLiteral(StringLiteral stringLiteral)
    {
        Write("\"").Write(stringLiteral.Value).Write("\"");
    }

    public override void VisitBinaryExpression(BinaryExpression binaryExpression)
    {
        Write("(");
        VisitNode(binaryExpression.Left);
        WriteSpace().Write(binaryExpression.Op.ToSymbol()).WriteSpace();
        VisitNode(binaryExpression.Right);
        Write(")");
    }

    public override void VisitPrefixExpression(PrefixExpression prefixExpression)
    {
        Write(prefixExpression.Operator.ToSymbol());
        VisitNode(prefixExpression.Expression);
    }

    public override void VisitPostfixIncrement(PostfixIncrement postfixIncrement)
    {
        Write(postfixIncrement.Operator.ToSymbol());
    }

    public override void VisitMethodCall(MethodCall methodCall)
    {
        VisitNode(methodCall.Name);
        Write("(");
        for (var i = 0; i < methodCall.Arguments.Values.Count; i++)
        {
            VisitNode(methodCall.Arguments.Values[i]);
            if (i < methodCall.Arguments.Values.Count - 1) Write(",").WriteSpace();
        }
        Write(")");
    }

    public override void VisitIndexerExpression(IndexerExpression indexerExpression)
    {
        Write("[");
        VisitNode(indexerExpression.Index);
        Write("]");
    }

    public override void VisitCastExpression(CastExpression castExpression)
    {
        Write("(");
        VisitNode(castExpression.TypeName);
        Write(")");
        VisitNode(castExpression.Expression);
    }

    public override void VisitTernaryExpression(TernaryExpression ternaryExpression)
    {
        Write("(");
        VisitNode(ternaryExpression.Condition);
        WriteSpace().Write("?").WriteSpace();
        VisitNode(ternaryExpression.Left);
        WriteSpace().Write(":").WriteSpace();
        VisitNode(ternaryExpression.Right);
        Write(")");
    }

    public override void VisitAccessorChainExpression(AccessorChainExpression accessorChainExpression)
    {
        VisitNode(accessorChainExpression.Source);
        for (var i = 0; i < accessorChainExpression.Accessors.Count; i++)
        {
            var accessor = accessorChainExpression.Accessors[i];
            if (accessor is Identifier or MethodCall) Write(".");
            VisitNode(accessor);
        }
    }

    public override void VisitParenthesisExpression(ParenthesisExpression parenthesisExpression)
    {
        Write("(");
        VisitNode(parenthesisExpression.Expression);
        Write(")");
    }

    public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        VisitNode(expressionStatement.Expression);
        WriteLine(";");
    }

    public override void VisitReturn(Return @return)
    {
        Write("return");
        if (@return.Value != null)
        {
            WriteSpace();
            VisitNode(@return.Value);
        }
        WriteLine(";");
    }

    public override void VisitDeclare(Declare declare)
    {
        for (var i = 0; i < declare.Variables.Count; i++)
        {
            VisitNode(declare.Variables[i]);
            if (i < declare.Variables.Count - 1) Write(",").WriteSpace();
        }
    }

    public override void VisitVariableAssign(VariableAssign variableAssign)
    {
        VisitNode(variableAssign.Variable);
        if (variableAssign.Value != null)
        {
            WriteSpace().Write(variableAssign.Operator?.ToAssignSymbol() ?? "=").WriteSpace();
            VisitNode(variableAssign.Value);
        }
    }

    public override void VisitDeclaredVariableAssign(DeclaredVariableAssign declaredVariableAssign)
    {
        if (declaredVariableAssign.TypeName.Name != "void")
        {
            VisitNode(declaredVariableAssign.TypeName);
            WriteSpace();
        }
        VisitNode(declaredVariableAssign.Variable);
        if (declaredVariableAssign.Value != null)
        {
            WriteSpace().Write(declaredVariableAssign.Operator?.ToAssignSymbol() ?? "=").WriteSpace();
            VisitNode(declaredVariableAssign.Value);
        }
        WriteLine(";");
    }

    public override void VisitAssign(Assign assign)
    {
        for (var i = 0; i < assign.Variables.Count; i++)
        {
            VisitNode(assign.Variables[i]);
            if (i < assign.Variables.Count - 1) Write(",").WriteSpace();
        }
        WriteLine(";");
    }

    public override void VisitShaderClass(ShaderClass shaderClass)
    {
        Write("shader").WriteSpace();
        VisitNode(shaderClass.Name);
        if (shaderClass.Generics != null && shaderClass.Generics.Parameters.Count > 0)
        {
            Write("<");
            for (var i = 0; i < shaderClass.Generics.Parameters.Count; i++)
            {
                var p = shaderClass.Generics.Parameters[i];
                VisitNode(p.TypeName);
                WriteSpace();
                VisitNode(p.Name);
                if (i < shaderClass.Generics.Parameters.Count - 1) Write(",").WriteSpace();
            }
            Write(">");
        }

        if (shaderClass.Mixins.Count > 0)
        {
            WriteSpace().Write(":").WriteSpace();
            for (var i = 0; i < shaderClass.Mixins.Count; i++)
            {
                VisitNode(shaderClass.Mixins[i]);
                // Mixins can have generics too but let's see if Mixin class has them
                if (i < shaderClass.Mixins.Count - 1) Write(",").WriteSpace();
            }
        }
        
        WriteLine();
        OpenBrace();
        foreach (var element in shaderClass.Elements)
        {
            VisitNode(element);
        }
        CloseBrace();
    }

    public override void VisitShaderMember(ShaderMember shaderMember)
    {
        if (shaderMember.Attributes != null && shaderMember.Attributes.Count > 0)
        {
            Write("[");
            for (var i = 0; i < shaderMember.Attributes.Count; i++)
            {
                VisitNode(shaderMember.Attributes[i]);
                if (i < shaderMember.Attributes.Count - 1) Write(",").WriteSpace();
            }
            WriteLine("]");
        }

        if (shaderMember.IsStaged) Write("stage").WriteSpace();
        if (shaderMember.StreamKind != StreamKind.None) Write(shaderMember.StreamKind.ToString().ToLowerInvariant()).WriteSpace();
        if (shaderMember.StorageClass != StorageClass.None) Write(shaderMember.StorageClass.ToString().ToLowerInvariant()).WriteSpace();
        
        VisitNode(shaderMember.TypeName);
        WriteSpace();
        VisitNode(shaderMember.Name);
        if (shaderMember.Value != null)
        {
            WriteSpace().Write("=").WriteSpace();
            VisitNode(shaderMember.Value);
        }
        WriteLine(";");
    }

    public override void VisitShaderMethod(ShaderMethod shaderMethod)
    {
        if (shaderMethod.IsStaged) Write("stage").WriteSpace();
        if (shaderMethod.IsOverride) Write("override").WriteSpace();
        if (shaderMethod.IsStatic) Write("static").WriteSpace();

        VisitNode(shaderMethod.ReturnTypeName);
        WriteSpace();
        VisitNode(shaderMethod.Name);
        Write("(");
        for (var i = 0; i < shaderMethod.Parameters.Count; i++)
        {
            var p = shaderMethod.Parameters[i];
            VisitNode(p.TypeName);
            WriteSpace();
            VisitNode(p.Name);
            if (i < shaderMethod.Parameters.Count - 1) Write(",").WriteSpace();
        }
        Write(")");
        
        if (shaderMethod.Body != null)
        {
            WriteLine();
            WriteStatementContent(shaderMethod.Body);
        }
        else
        {
            WriteLine(";");
        }
    }

    public override void VisitAnyShaderAttribute(AnyShaderAttribute anyShaderAttribute)
    {
        VisitNode(anyShaderAttribute.Name);
        if (anyShaderAttribute.Parameters.Count > 0)
        {
            Write("(");
            for (var i = 0; i < anyShaderAttribute.Parameters.Count; i++)
            {
                VisitNode(anyShaderAttribute.Parameters[i]);
                if (i < anyShaderAttribute.Parameters.Count - 1) Write(",").WriteSpace();
            }
            Write(")");
        }
    }

    public override void VisitBreak(Break breakStatement)
    {
        WriteLine("break;");
    }

    public override void VisitContinue(Continue continueStatement)
    {
        WriteLine("continue;");
    }

    public override void VisitDiscard(Discard discardStatement)
    {
        WriteLine("discard;");
    }

    public override void VisitConditionalFlow(ConditionalFlow conditionalFlow)
    {
        VisitNode(conditionalFlow.If);
        foreach (var elseIf in conditionalFlow.ElseIfs)
        {
            VisitNode(elseIf);
        }

        if (conditionalFlow.Else != null)
        {
            VisitNode(conditionalFlow.Else);
        }
    }

    public override void VisitFor(For forStatement)
    {
        Write("for").WriteSpace().Write("(");
        VisitNode(forStatement.Initializer);
        WriteSpace();
        VisitNode(forStatement.Condition);
        Write(";").WriteSpace();
        for (var i = 0; i < forStatement.Update.Count; i++)
        {
            VisitNode(forStatement.Update[i]);
            if (i < forStatement.Update.Count - 1) Write(",").WriteSpace();
        }
        WriteLine(")");
        WriteStatementContent(forStatement.Body);
    }

    public override void VisitWhile(While whileStatement)
    {
        Write("while").WriteSpace().Write("(");
        VisitNode(whileStatement.Condition);
        WriteLine(")");
        WriteStatementContent(whileStatement.Body);
    }

    public override void VisitForEach(ForEach forEach)
    {
        WriteLinkLine(forEach);
        Write("foreach").WriteSpace().Write("(");
        VisitNode(forEach.TypeName);
        WriteSpace();
        VisitNode(forEach.Variable);
        WriteSpace().Write("in").WriteSpace();
        VisitNode(forEach.Collection);
        WriteLine(")");
        WriteStatementContent(forEach.Body);
    }

    public override void VisitBlockStatement(BlockStatement blockStatement)
    {
        WriteLinkLine(blockStatement);
        OpenBrace();
        foreach (var statement in blockStatement.Statements)
        {
            VisitNode(statement);
        }

        CloseBrace();
    }

    public override void VisitIf(If @if)
    {
        Write("if").WriteSpace().Write("(");
        VisitNode(@if.Condition);
        WriteLine(")");
        WriteStatementContent(@if.Body);
    }
    
    public override void VisitElseIf(ElseIf elseIf)
    {
        Write("else if").WriteSpace().Write("(");
        VisitNode(elseIf.Condition);
        WriteLine(")");
        WriteStatementContent(elseIf.Body);
    }
    
    public override void VisitElse(Else @else)
    {
        WriteLine("else");
        WriteStatementContent(@else.Body);
    }

    public override void DefaultVisit(Node node)
    {
        //throw new NotImplementedException($"No shader text writer for {node.GetType().Name}");
    }
    
    protected ShaderWriter WriteLinkLine(Node node)
    {
        return this;
    }
}