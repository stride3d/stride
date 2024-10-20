using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

public static class Grammar
{
    public static ParseResult Match<TParser, TValue>(string code, TParser? parser = null)
        where TValue : Node
        where TParser : struct, IParser<TValue>
    {
        var p = parser ?? new TParser();
        var scanner = new Scanner(code);
        var result = new ParseResult();
        if (p.Match(ref scanner, result, out var fnum))
            result.AST = fnum;
        if(!Terminals.EOF(ref scanner))
            result.Errors.Add(new(SDSLErrors.SDSL0009, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        return result;
    }

    public static ParseResult Match<TScannable, TParser, TValue>(TScannable code, TParser? parser = null)
        where TScannable : IScannableCode
        where TValue : Node
        where TParser : struct, IParser<TValue>
    {
        var p = parser ?? new TParser();
        var scanner = new Scanner<TScannable>(code);

        var result = new ParseResult();
        if (p.Match(ref scanner, result, out var fnum))
            result.AST = fnum;
        if(!Terminals.EOF(ref scanner))
            result.Errors.Add(new(SDSLErrors.SDSL0009, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        return result;
    }


    public static ParseResult<TValue> MatchTyped<TParser, TValue>(string code, TParser? parser = null)
        where TValue : Node
        where TParser : struct, IParser<TValue>
    {
        var result = Match<TParser, TValue>(code, parser);
        if (result.AST is TValue r)
        {
            return new ParseResult<TValue>()
            {
                AST = r,
                Errors = result.Errors
            };
        }
        else return null!;
    }
}