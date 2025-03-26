namespace Stride.Shaders.Parsing.SDSL.AST;


public class PreProcessableCode(TextLocation info) : Node(info)
{
    public List<DirectiveStatement> Snippets { get; set; } = [];
    public override string ToString()
    {
        return string.Join("\n", Snippets);
    }
}

public abstract class DirectiveStatement(TextLocation info) : Node(info);
/// <summary>
/// Represents a directive code snippet
/// </summary>
/// <param name="info"></param>
public class DirectiveCode(TextLocation info) : DirectiveStatement(info)
{
    public override string ToString()
    {
        return Info.Text.ToString();
    }
}
/// <summary>
/// Represents a directive macro
/// </summary>
/// <param name="info"></param>
public abstract class Directive(TextLocation info) : DirectiveStatement(info);

/// <summary>
/// Represents a directive flow control using conditionals (#if #ifdef #ifndef #elif #else #endif)
/// </summary>
/// <param name="expression"></param>
/// <param name="info"></param>
public abstract class DirectiveFlow(Expression? expression, TextLocation info) : Node(info)
{
    public Expression? Expression { get; set; } = expression;
    public PreProcessableCode? Code { get; set; }
}
/// <summary>
/// Represents a directive define macro
/// </summary>
/// <param name="identifier"></param>
/// <param name="expression"></param>
/// <param name="info"></param>
public class ObjectDefineDirective(Identifier identifier, Expression? expression, TextLocation info) : Directive(info)
{
    public Identifier Identifier { get; set; } = identifier;
    public Expression? Expression { get; set; } = expression;
}

/// <summary>
/// Represents a directive define function
/// </summary>
/// <param name="functionName"></param>
/// <param name="pattern"></param>
/// <param name="info"></param>
public class FunctionDefineDirective(Identifier functionName, string pattern, TextLocation info) : Directive(info)
{
    public Identifier FunctionName { get; set; } = functionName;
    public List<Identifier> Parameters { get; set; } = [];
    public string Pattern { get; set; } = pattern;
}
/// <summary>
/// Represents a directive conditional flow control #if
/// </summary>
/// <param name="expression"></param>
/// <param name="info"></param>
public class IfDirective(Expression expression, TextLocation info) : DirectiveFlow(expression, info)
{
    public override string ToString()
    {
        return $"#if {Expression}\n{Code}";
    }
}

/// <summary>
/// Represents a directive conditional flow control #ifdef
/// </summary>
/// <param name="value"></param>
/// <param name="info"></param>
public class IfDefDirective(Identifier value, TextLocation info) : IfDirective(value, info)
{
    public override string ToString()
    {
        return $"#ifdef {Expression}\n{Code}";
    }
}
/// <summary>
/// Represents a directive conditional flow control #ifndef
/// </summary>
/// <param name="value"></param>
/// <param name="info"></param>
public class IfNDefDirective(Identifier value, TextLocation info) : IfDirective(value, info)
{
    public override string ToString()
    {
        return $"#ifndef {Expression}\n{Code}";
    }
}
/// <summary>
/// Represents a directive conditional flow control #elif
/// </summary>
/// <param name="expression"></param>
/// <param name="info"></param>
public class ElifDirective(Expression expression, TextLocation info) : IfDirective(expression, info)
{
    public override string ToString()
    {
        return $"#elif {Expression}{Code}";
    }
}
/// <summary>
/// Represents a directive conditional flow control #else
/// </summary>
/// <param name="info"></param>
public class ElseDirective(TextLocation info) : DirectiveFlow(null, info)
{
    public override string ToString()
    {
        return $"#else\n{Code}";
    }
}
public class EndIfDirective(TextLocation info) : DirectiveFlow(null, info)
{
    public override string ToString()
    {
        return "#endif";
    }
}

/// <summary>
/// Represents a directive conditional flow control
/// </summary>
/// <param name="ifExp"></param>
/// <param name="info"></param>
public class ConditionalDirectives(IfDirective ifExp, TextLocation info) : Directive(info)
{
    public IfDirective If { get; set; } = ifExp;
    public List<ElifDirective> Elifs { get; set; } = [];
    public ElseDirective? Else { get; set; }

    public override string ToString()
    {
        return (If, Elifs, Else) switch
        {
            (var i, [], null) => $"{i}",
            (var i, var e, null) when e is not [] && e is not null => $"{i}\n{string.Join("\n", e)}",
            (var i, var e, var el) when e is not [] && e is not null => $"{i}\n{string.Join("\n", e)}\n{el}",
            (var i, [], var el) => $"{i}\n{el}",
            _ => throw new NotImplementedException()
        } + "#endif";
    }
}


