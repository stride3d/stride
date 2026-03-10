using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct SwitchStatementParser : IParser<SwitchStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out SwitchStatement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("switch", ref scanner, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var selector, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(')', ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char('{', ref scanner, advance: true)
        )
        {
            parsed = new(selector, scanner[position..scanner.Position]);
            Parsers.Spaces0(ref scanner, result, out _);

            while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
            {
                if (SwitchSection(ref scanner, result, out var section))
                {
                    parsed.Sections.Add(section);
                    Parsers.Spaces0(ref scanner, result, out _);
                }
                else
                    return Parsers.Exit(ref scanner, result, out parsed, position, new("Expected case or default label", scanner[scanner.Position], scanner.Memory));
            }

            parsed.Info = scanner[position..scanner.Position];
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool SwitchSection<TScanner>(ref TScanner scanner, ParseResult result, out SwitchSection parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        var labels = new List<SwitchLabel>();

        // Parse one or more labels (case or default)
        while (SwitchLabel(ref scanner, result, out var label))
        {
            labels.Add(label);
            Parsers.Spaces0(ref scanner, result, out _);
        }

        if (labels.Count == 0)
            return false;

        // Parse statements until next case/default/closing brace
        var statements = new List<Statement>();
        while (
            !scanner.IsEof
            && !Tokens.Literal("case", ref scanner)
            && !Tokens.Literal("default", ref scanner)
            && !Tokens.Char('}', ref scanner))
        {
            if (StatementParsers.Statement(ref scanner, result, out var statement))
            {
                statements.Add(statement);
                Parsers.Spaces0(ref scanner, result, out _);
            }
            else
                return Parsers.Exit(ref scanner, result, out parsed!, position, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory));
        }

        parsed = new SwitchSection(labels, statements, scanner[position..scanner.Position]);
        return true;
    }

    public static bool SwitchLabel<TScanner>(ref TScanner scanner, ParseResult result, out SwitchLabel parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;

        if (
            Tokens.Literal("case", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var value)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(':', ref scanner, advance: true)
        )
        {
            parsed = new CaseLabel(value, scanner[position..scanner.Position]);
            return true;
        }

        scanner.Position = position;

        if (
            Tokens.Literal("default", ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(':', ref scanner, advance: true)
        )
        {
            parsed = new DefaultLabel(scanner[position..scanner.Position]);
            return true;
        }

        scanner.Position = position;
        return false;
    }
}
