using Microsoft.VisualBasic;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct PreprocessorParser : IParser<PreProcessableCode>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out PreProcessableCode parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var p = new PreProcessableCode(new());
        while (!scanner.IsEof && DirectiveStatementParsers.Statement(ref scanner, result, out var statement))
            p.Snippets.Add(statement);
        p.Info = scanner[position..scanner.Position];
        parsed = p;
        return true;
    }

    public static bool PreCode<TScanner>(ref TScanner scanner, ParseResult result, out PreProcessableCode parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PreprocessorParser().Match(ref scanner, result, out parsed, orError);
}

public record struct DirectiveStatementParsers : IParser<DirectiveStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out DirectiveStatement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        if (Conditional(ref scanner, result, out var conditional))
        {
            parsed = conditional;
            return true;
        }
        else if(Define(ref scanner, result, out var obj))
        {
            parsed = obj;
            return true;
        }
        else if (DefineFunc(ref scanner, result, out var func))
        {
            parsed = func;
            return true;
        }
        else if (Code(ref scanner, result, out var code))
        {
            parsed = code;
            return true;
        }
        else return false;
    }

    public static bool AnyIf<TScanner>(ref TScanner scanner, ParseResult result, out IfDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (If(ref scanner, result, out var ifDirective))
        {
            parsed = ifDirective;
            return true;
        }
        else if (IfDef(ref scanner, result, out var ifdefDirective))
        {
            parsed = ifdefDirective;
            return true;
        }
        else if (IfNDef(ref scanner, result, out var ifndefDirective))
        {
            parsed = ifndefDirective;
            return true;
        }
        parsed = null!;
        return false;
    }
    public static bool Define<TScanner>(ref TScanner scanner, ParseResult result, out ObjectDefineDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ObjectDefineDirectiveParser().Match(ref scanner, result, out parsed, orError);
    public static bool DefineFunc<TScanner>(ref TScanner scanner, ParseResult result, out FunctionDefineDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new FunctionDefineDirectiveParser().Match(ref scanner, result, out parsed, orError);
    public static bool If<TScanner>(ref TScanner scanner, ParseResult result, out IfDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalIfDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool IfDef<TScanner>(ref TScanner scanner, ParseResult result, out IfDefDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalIfDefDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool IfNDef<TScanner>(ref TScanner scanner, ParseResult result, out IfNDefDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalIfNDefDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool Elif<TScanner>(ref TScanner scanner, ParseResult result, out IfDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalElifDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool Endif<TScanner>(ref TScanner scanner, ParseResult result, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new EndifDirectiveParser().Match(ref scanner, result, out _, orError);
    public static bool Code<TScanner>(ref TScanner scanner, ParseResult result, out DirectiveCode parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveCodeParser().Match(ref scanner, result, out parsed, orError);
    public static bool Else<TScanner>(ref TScanner scanner, ParseResult result, out ElseDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalElseDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool Conditional<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalDirectives parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ConditionalDirectivesParser().Match(ref scanner, result, out parsed, orError);
    public static bool Statement<TScanner>(ref TScanner scanner, ParseResult result, out DirectiveStatement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveStatementParsers().Match(ref scanner, result, out parsed, orError);
}

public struct ConditionalDirectivesParser : IParser<ConditionalDirectives>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalDirectives parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (DirectiveStatementParsers.AnyIf(ref scanner, result, out var ifDirective, orError))
        {
            if (PreprocessorParser.PreCode(ref scanner, result, out var c))
                ifDirective.Code = c;

            var elifDirectives = new List<ElifDirective>();
            while (DirectiveStatementParsers.Elif(ref scanner, result, out var elifDirective, orError))
            {
                elifDirectives.Add((ElifDirective)elifDirective);
                if (PreprocessorParser.PreCode(ref scanner, result, out c))
                    elifDirective.Code = c;
            }

            if (DirectiveStatementParsers.Else(ref scanner, result, out var elseDirective, orError))
                if (PreprocessorParser.PreCode(ref scanner, result, out c))
                    elseDirective.Code = c;

            if (DirectiveStatementParsers.Endif(ref scanner, result, orError))
            {
                parsed = new ConditionalDirectives(ifDirective, scanner[position..scanner.Position])
                {
                    Elifs = elifDirectives,
                    Else = elseDirective
                };
                return true;
            }
            else
            {
                parsed = null!;
                return false;
            }
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}

public record struct DirectiveCodeParser : IParser<DirectiveCode>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out DirectiveCode parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var beginningOfLine = scanner.Position;
        int lineCount = 0;
        while (
            !(
                Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true)
                && (
                    Tokens.Literal("#if", ref scanner)
                    || Tokens.Literal("#define", ref scanner)
                    || Tokens.Literal("#endif", ref scanner)
                    || Tokens.Literal("#elif", ref scanner)
                )
            )
            && !scanner.IsEof
        )
        {
            Parsers.Until(ref scanner, '\n', advance: true);
            beginningOfLine = scanner.Position;
            lineCount += 1;
        }
        if (lineCount > 0)
        {
            scanner.Position = beginningOfLine;
            parsed = new DirectiveCode(scanner[position..scanner.Position]);
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}

public record struct ConditionalIfDefDirectivesParser : IParser<IfDefDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out IfDefDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
        if (
            Tokens.Literal("#ifdef", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true, orError: new(SDSLErrorMessages.SDSL0016, scanner[scanner.Position], scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var id, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            var cond = new IfDefDirective(id, scanner[position..scanner.Position]);
            parsed = cond;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}
public record struct ConditionalIfNDefDirectivesParser : IParser<IfNDefDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out IfNDefDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
        if (
            Tokens.Literal("#ifndef", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true)
            && LiteralsParser.Identifier(ref scanner, result, out var id)
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            var cond = new IfNDefDirective(id, scanner[position..scanner.Position]);
            parsed = cond;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}

public record struct ConditionalIfDirectivesParser : IParser<IfDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out IfDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
        if (
            Tokens.Literal("#if", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true)
            && DirectiveExpressionParser.Expression(ref scanner, result, out var expression)
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            var cond = new IfDirective(expression, scanner[position..scanner.Position]);
            parsed = cond;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}


public record struct ConditionalElifDirectivesParser : IParser<IfDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out IfDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
        if (
            Tokens.Literal("#elif", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true)
            && DirectiveExpressionParser.Expression(ref scanner, result, out var expression)
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            var cond = new ElifDirective(expression, scanner[position..scanner.Position]);
            parsed = cond;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}
public record struct ConditionalElseDirectivesParser : IParser<ElseDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ElseDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
        if (
            Tokens.Literal("#else", ref scanner, advance: true)
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            var cond = new ElseDirective(scanner[position..scanner.Position]);
            parsed = cond;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}

public record struct EndifDirectiveParser : IParser<NoNode>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out NoNode parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);

        if (
            Tokens.Literal("#endif", ref scanner, advance: true)
            && Tokens.EOL(ref scanner, advance: true)
        )
        {
            parsed = null!;
            return true;
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}


public record struct ObjectDefineDirectiveParser : IParser<ObjectDefineDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ObjectDefineDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);

        if (
            Tokens.Literal("#define", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
        )
        {
            if (
                DirectiveExpressionParser.Expression(ref scanner, result, out var expression)
                && Tokens.EOL(ref scanner, advance: true)
            )
            {
                parsed = new(identifier, expression, scanner[position..scanner.Position]);
                return true;
            }
            else if(Tokens.EOL(ref scanner, advance: true))
            {
                parsed = new(identifier, null, scanner[position..scanner.Position]);
                return true;
            }
            else
            {
                scanner.Position = position;
                parsed = null!;
                return false;
            }
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}


public record struct FunctionDefineDirectiveParser : IParser<FunctionDefineDirective>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out FunctionDefineDirective parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);

        if (
            Tokens.Literal("#define", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, onlyWhiteSpace: true)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true)
            && Tokens.Char('(', ref scanner, advance: true)
        )
        {
            Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true);
            var func = new FunctionDefineDirective(identifier, "", new());
            
            if (
                LiteralsParser.Identifier(ref scanner, result, out var param) 
                && Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true)
            )
                func.Parameters.Add(param);
            while(
                Tokens.Char(',', ref scanner, advance: true)
                && Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true)
                && LiteralsParser.Identifier(ref scanner, result, out param)
                && Parsers.Spaces0(ref scanner, result, out _, onlyWhiteSpace: true)

            )
                func.Parameters.Add(param);
            if(!Tokens.Char(')', ref scanner, advance: true))
            {
                result.Errors.Add(new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
                scanner.Position = position;
                parsed = null!;
                return false;
            }
            else
            {
                var startPattern = scanner.Position;
                while(!(scanner.IsEof || Tokens.Char('\n', ref scanner) || Tokens.Literal("\r\n", ref scanner)))
                    scanner.Advance(1);
                func.Pattern = scanner.Memory[startPattern..scanner.Position].TrimEnd().TrimStart().ToString();
                if(!Tokens.Char('\n', ref scanner, advance: true))
                    Tokens.Literal("\r\n", ref scanner, advance: true);
                parsed = func;
                return true;
            }
        }
        else
        {
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}