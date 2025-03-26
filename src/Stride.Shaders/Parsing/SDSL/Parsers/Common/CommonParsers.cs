using System.Security.Cryptography;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;




public static class Parsers
{
    public static bool Exit<TScanner, TNode>(ref TScanner scanner, ParseResult result, out TNode parsed, int beginningPosition, in ParseError? orError = null)
        where TScanner : struct, IScanner
        where TNode : class
    {
        if (orError is not null)
        {
            result.Errors.Add(orError.Value);
            scanner.Position = scanner.End;
            parsed = null!;
            return false;
        }
        if (result.Errors.Count == 0)
            scanner.Position = beginningPosition;
        parsed = null!;
        return false;
    }

    public static bool Spaces0<TScanner>(ref TScanner scanner, ParseResult result, out NoNode node, in ParseError? orError = null, bool onlyWhiteSpace = false)
        where TScanner : struct, IScanner
        => new Space0(onlyWhiteSpace).Match(ref scanner, result, out node, in orError);
    public static bool Spaces1<TScanner>(ref TScanner scanner, ParseResult result, out NoNode node, in ParseError? orError = null, bool onlyWhiteSpace = false)
       where TScanner : struct, IScanner
        => new Space1(onlyWhiteSpace).Match(ref scanner, result, out node, in orError);




    public static bool Alternatives<TScanner,TResult>(ref TScanner scanner, ParseResult result, out TResult parsed, in ParseError? orError = null, params ReadOnlySpan<ParserDelegate<TScanner,TResult>> parsers)
        where TScanner : struct, IScanner
        where TResult : Node
    {
        var position = scanner.Position;
        foreach(var p in parsers)
            if(p.Invoke(ref scanner, result, out parsed))
                return true;
        return Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool Sequences<TScanner,TResult>(ref TScanner scanner, ParseResult result, out List<TResult> parsed, in ParseError? orError = null, bool withSPaces = false, string? separator = null, params ReadOnlySpan<ParserDelegate<TScanner,TResult>> parsers)
        where TScanner : struct, IScanner
        where TResult : Node
    {
        parsed = [];
        var position = scanner.Position;
        foreach(var p in parsers)
            if(p.Invoke(ref scanner, result, out var r))
                parsed.Add(r);
            else
                return Exit(ref scanner, result, out parsed, position, orError);
        return true;
    }

    public static bool SequenceOf<TScanner>(ref TScanner scanner, ReadOnlySpan<string> literals, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        foreach (var l in literals)
        {
            if (!(Tokens.Literal(l, ref scanner, advance: true) && Spaces1(ref scanner, null!, out _)))
            {
                scanner.Position = position;
                return false;
            }
        }
        scanner.Position = advance ? scanner.Position : position;
        return true;
    }


    public static bool MethodModifiers<TScanner>(ref TScanner scanner, ParseResult result, out bool isStaged, out bool isStatic, out bool isClone, out bool isOverride, out bool isAbstract, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        isStaged = false;
        isStatic = false;
        isOverride = false;
        isAbstract = false;
        isClone = false;
        bool matched = false;
        // legacy
        while (
            Tokens.AnyOf(
                [
                    "stage", 
                    "override",
                    "clone",
                    "abstract",
                    "static"
                ], 
                ref scanner, 
                out string match,
                advance: true) 
            && Spaces1(ref scanner, result, out _))
        {
            matched = true;
            if(match == "stage")
                isStaged = true; 
            else if(match == "override")
                isOverride = true;
            else if(match == "clone")
                isClone = true;
            else if(match == "abstract")
                isAbstract = true;
            else if(match == "static")
                isStatic = true;
            else break;
        }
        if(!advance)
            scanner.Position = position;
        return matched;
    }

    public static bool VariableModifiers<TScanner>(ref TScanner scanner, ParseResult result, out bool isStaged, out StreamKind streamKind, out InterpolationModifier interpolation, out TypeModifier typeModifier, out StorageClass storageClass, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        isStaged = false;
        streamKind = StreamKind.None;
        interpolation = InterpolationModifier.None;
        typeModifier = TypeModifier.None;
        storageClass = StorageClass.None;
        bool matched = false;
        // legacy
        while (
            Tokens.AnyOf(
                [
                    "stage", 
                    "stream", 
                    "patchstream", 
                    "linear", 
                    "centroid", 
                    "nointerpolation", 
                    "noperspective", 
                    "sample",
                    "extern", 
                    "nointerpolation", 
                    "precise", 
                    "shared", 
                    "groupshared", 
                    "static", 
                    "uniform", 
                    "volatile",
                    "const",
                    "rowmajor",
                    "columnmajor"
                ], 
                ref scanner, 
                out string match,
                advance: true) 
            && Spaces1(ref scanner, result, out _))
        {
            matched = true;
            if (match == "stage")
                isStaged = true;
            else if(match == "stream")
                streamKind = StreamKind.Stream;
            else if(match == "patchstream")
                streamKind = StreamKind.PatchStream;
            else if(match == "linear")
                interpolation = InterpolationModifier.Linear;
            else if(match == "centroid")
                interpolation = InterpolationModifier.Centroid;
            else if(match == "nointerpolation")
                interpolation = InterpolationModifier.NoInterpolation;
            else if(match == "noperspective")
                interpolation = InterpolationModifier.NoPerspective;
            else if(match == "sample")
                interpolation = InterpolationModifier.Sample;
            else if(match == "extern")
                storageClass = StorageClass.Extern;
            else if(match == "nointerpolation")
                storageClass = StorageClass.NoInterpolation;
            else if(match == "precise")
                storageClass = StorageClass.Precise;
            else if(match == "shared")
                storageClass = StorageClass.Shared;
            else if(match == "groupshared")
                storageClass = StorageClass.GroupShared;
            else if(match == "static")
                storageClass = StorageClass.Static;
            else if(match == "uniform")
                storageClass = StorageClass.Uniform;
            else if(match == "volatile")
                storageClass = StorageClass.Volatile;
            else if(match == "const")
                typeModifier = TypeModifier.Const;
            else if(match == "rowmajor")
                typeModifier = TypeModifier.RowMajor;
            else if(match == "columnmajor")
                typeModifier = TypeModifier.ColumnMajor;
            else break;
        }
        if(!advance)
            scanner.Position = position;
        return matched;
    }


    public static bool IdentifierArraySizeOptionalValue<TScanner>(ref TScanner scanner, ParseResult result, out Identifier identifier, out List<Expression> arraySizes, out Expression? value, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        arraySizes = null!;
        value = null!;

        if (
            LiteralsParser.Identifier(ref scanner, result, out identifier)
            && !FollowedBy(ref scanner, Tokens.Char('.'), withSpaces: true, advance: true)
        )
        {
            var tmp = scanner.Position;
            Spaces0(ref scanner, result, out _);
            if (!FollowedByDelList(ref scanner, result, ArraySizes, out arraySizes, withSpaces: true, advance: true))
            {
                scanner.Position = tmp;
            }
            tmp = scanner.Position;
            if (
                !(
                    FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true)
                    && FollowedBy(ref scanner, result, ExpressionParser.Expression, out value, withSpaces: true, advance: true)
                )
            )
            {
                scanner.Position = tmp;
            }
            if (!advance)
                scanner.Position = position;
            return true;
        }
        else
        {
            scanner.Position = position;
            identifier = null!;
            arraySizes = null!;
            return false;
        }
    }
    public static bool TypeNameIdentifierArraySizeValue<TScanner>(ref TScanner scanner, ParseResult result, out TypeName typeName, out Identifier identifier, out Expression? value, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        value = null!;

        if (
            LiteralsParser.TypeName(ref scanner, result, out typeName)
            && Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out identifier))
        {
            var tmp = scanner.Position;
            Spaces0(ref scanner, result, out _);
            if (FollowedByDelList(ref scanner, result, ArraySizes, out List<Expression> arraySize, withSpaces: true, advance: true))
                typeName.ArraySize = arraySize;
            else
                scanner.Position = tmp;
            tmp = scanner.Position;
            if (
                !(
                    FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true)
                    && FollowedBy(ref scanner, result, ExpressionParser.Expression, out value, withSpaces: true, advance: true)
                )
            )
            {
                scanner.Position = tmp;
            }
            if (!advance)
                scanner.Position = position;
            return true;
        }
        else
        {
            scanner.Position = position;
            if (
                LiteralsParser.TypeName(ref scanner, result, out typeName)
                && FollowedByDelList(ref scanner, result, ArraySizes, out List<Expression> sizes, withSpaces: true, advance: true)
                && Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out identifier))
            {
                var tmp = scanner.Position;
                Spaces0(ref scanner, result, out _);
                if (
                    !(
                        Tokens.Char('=', ref scanner, advance: true)
                        && Spaces0(ref scanner, result, out _)
                        && ExpressionParser.Expression(ref scanner, result, out value)
                    )
                )
                {
                    scanner.Position = tmp;
                }
                if (!advance)
                    scanner.Position = position;
                return true;
            }
        }
        scanner.Position = position;
        typeName = null!;
        identifier = null!;
        return false;
    }

    public static bool MixinIdentifierArraySizeValue<TScanner>(ref TScanner scanner, ParseResult result, out Mixin mixin, out Identifier identifier, out List<Expression> arraySize, out Expression? value, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        arraySize = null!;
        value = null!;

        if (
            ShaderClassParsers.Mixin(ref scanner, result, out mixin)
            && Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out identifier))
        {
            var tmp = scanner.Position;
            Spaces0(ref scanner, result, out _);
            if (!FollowedByDelList(ref scanner, result, ArraySizes, out arraySize, withSpaces: true, advance: true))
            {
                scanner.Position = tmp;
            }
            tmp = scanner.Position;
            if (
                !(
                    FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true)
                    && FollowedBy(ref scanner, result, ExpressionParser.Expression, out value, withSpaces: true, advance: true)
                )
            )
            {
                scanner.Position = tmp;
            }
            if (!advance)
                scanner.Position = position;
            return true;
        }
        else
        {
            scanner.Position = position;
            if (
                ShaderClassParsers.Mixin(ref scanner, result, out mixin)
                && FollowedByDelList(ref scanner, result, ArraySizes, out List<Expression> sizes, withSpaces: true, advance: true)
                && Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out identifier))
            {
                var tmp = scanner.Position;
                Spaces0(ref scanner, result, out _);
                if (
                    !(
                        Tokens.Char('=', ref scanner, advance: true)
                        && Spaces0(ref scanner, result, out _)
                        && ExpressionParser.Expression(ref scanner, result, out value)
                    )
                )
                {
                    scanner.Position = tmp;
                }
                if (!advance)
                    scanner.Position = position;
                return true;
            }
        }
        scanner.Position = position;
        mixin = null!;
        identifier = null!;
        arraySize = null!;
        return false;
    }

    public static bool ArraySizes<TScanner>(ref TScanner scanner, ParseResult result, out List<Expression> arraySizes, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        arraySizes = [];
        while (!scanner.IsEof)
        {
            if (FollowedBy(ref scanner, Tokens.Char('['), withSpaces: true, advance: true))
            {
                if(FollowedBy(ref scanner, Tokens.Char(']'), withSpaces: true, advance: true))
                    break;
                else if (FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression arraySize, withSpaces: true, advance: true))
                {
                    arraySizes.Add(arraySize);
                    if (!FollowedBy(ref scanner, Tokens.Char(']'), withSpaces: true, advance: true))
                        return Exit(ref scanner, result, out arraySizes, scanner.Position);
                }
                else return Exit(ref scanner, result, out arraySizes, scanner.Position);
            }
            else break;
        }
        return true;
    }

    public static bool TypeNameMixinArraySizeValue<TScanner>(ref TScanner scanner, ParseResult result, out TypeName typeName, out Mixin mixin, out Expression? arraySize, out Expression? value, bool advance = true)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        arraySize = null!;
        value = null!;
        if (
            LiteralsParser.TypeName(ref scanner, result, out typeName)
            && Spaces1(ref scanner, result, out _)
            && ShaderClassParsers.Mixin(ref scanner, result, out mixin))
        {
            var tmp = scanner.Position;
            Spaces0(ref scanner, result, out _);
            if (
                !(
                    Tokens.Char('[', ref scanner, advance: true)
                    && Spaces0(ref scanner, result, out _)
                    && ExpressionParser.Expression(ref scanner, result, out arraySize)
                    && Spaces0(ref scanner, result, out _)
                    && Tokens.Char(']', ref scanner, advance: true)
                )
            )
            {
                scanner.Position = tmp;
            }
            tmp = scanner.Position;
            if (
                !(
                    Tokens.Char('=', ref scanner, advance: true)
                    && Spaces0(ref scanner, result, out _)
                    && ExpressionParser.Expression(ref scanner, result, out value)
                )
            )
            {
                scanner.Position = tmp;
            }
            if (!advance)
                scanner.Position = position;
            return true;
        }
        else
        {
            scanner.Position = position;
            if (
                LiteralsParser.TypeName(ref scanner, result, out typeName)
                && FollowedBy(ref scanner, Tokens.Char('['), withSpaces: true, advance: true)
                && ExpressionParser.Expression(ref scanner, result, out arraySize)
                && FollowedBy(ref scanner, Tokens.Char(']'), withSpaces: true, advance: true)
                && Spaces1(ref scanner, result, out _)
                && ShaderClassParsers.Mixin(ref scanner, result, out mixin))
            {
                var tmp = scanner.Position;
                Spaces0(ref scanner, result, out _);
                if (
                    !(
                        Tokens.Char('=', ref scanner, advance: true)
                        && Spaces0(ref scanner, result, out _)
                        && ExpressionParser.Expression(ref scanner, result, out value)
                    )
                )
                {
                    scanner.Position = tmp;
                }
                if (!advance)
                    scanner.Position = position;
                return true;
            }
        }
        scanner.Position = position;
        typeName = null!;
        mixin = null!;
        arraySize = null!;
        return false;
    }

    public static bool Optional<TScanner, TTerminal>(ref TScanner scanner, TTerminal terminal, bool advance = false)
        where TScanner : struct, IScanner
        where TTerminal : struct, IToken
    {
        terminal.Match(ref scanner, advance: advance);
        return true;
    }
    public static bool Optional<TScanner, TNode>(ref TScanner scanner, IParser<TNode> parser, ParseResult result, out TNode? node)
        where TScanner : struct, IScanner
        where TNode : Node
    {
        parser.Match(ref scanner, result, out node);
        return true;
    }


    public static bool FollowedBy<TScanner, TTerminal>(ref TScanner scanner, TTerminal terminal, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
        where TTerminal : struct, IToken
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (terminal.Match(ref scanner, advance: advance))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }
    public static bool FollowedByAny<TScanner>(ref TScanner scanner, ReadOnlySpan<string> literals, out string matched, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        foreach (var l in literals)
        {
            if (Tokens.Literal(l, ref scanner, advance: advance))
            {
                if (!advance)
                    scanner.Position = position;
                matched = l;
                return true;
            }
        }
        matched = null!;
        scanner.Position = position;
        return false;
    }
    public static bool FollowedByAny<TScanner>(ref TScanner scanner, string literals, out char matched, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        foreach (var l in literals)
        {
            if (Tokens.Char(l, ref scanner, advance: advance))
            {
                if (!advance)
                    scanner.Position = position;
                matched = l;
                return true;
            }
        }
        matched = '0';
        scanner.Position = position;
        return false;
    }
    public static bool FollowedByDel<TScanner>(ref TScanner scanner, ParseResult result, ParserDelegate<TScanner> func, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (func.Invoke(ref scanner, result))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }
    public static bool FollowedByDel<TScanner, TResult>(ref TScanner scanner, ParseResult result, ParserDelegate<TScanner, TResult> func, out TResult parsed, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (func.Invoke(ref scanner, result, out parsed))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }
    public static bool FollowedByDelList<TScanner, TResult>(ref TScanner scanner, ParseResult result, ParserListDelegate<TScanner, TResult> func, out List<TResult> parsed, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (func.Invoke(ref scanner, result, out parsed))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }
    public static bool FollowedBy<TScanner, TResult>(ref TScanner scanner, ParseResult result, ParserDelegate<TScanner, TResult> func, out TResult parsed, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (func.Invoke(ref scanner, result, out parsed))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }

    public static bool FollowedBy<TScanner, TParser, TResult>(ref TScanner scanner, TParser parser, ParseResult result, out TResult parsed, bool withSpaces = false, bool advance = false)
        where TScanner : struct, IScanner
        where TParser : struct, IParser<TResult>
        where TResult : Node
    {
        var position = scanner.Position;
        if (withSpaces)
            Spaces0(ref scanner, null!, out _);
        if (parser.Match(ref scanner, result, out parsed))
        {
            if (!advance)
                scanner.Position = position;
            return true;
        }
        scanner.Position = position;
        return false;
    }

    public static bool Until<TScanner>(ref TScanner scanner, char value, bool advance = false)
        where TScanner : struct, IScanner
    {
        while (!scanner.IsEof && !Tokens.Char(value, ref scanner, advance))
            scanner.Advance(1);
        return scanner.IsEof;
    }
    public static bool Until<TScanner>(ref TScanner scanner, string value, bool advance = false)
        where TScanner : struct, IScanner
    {
        while (!scanner.IsEof && !Tokens.Literal(value, ref scanner, advance))
            scanner.Advance(1);
        return scanner.IsEof;
    }
    public static bool Until<TScanner>(ref TScanner scanner, ReadOnlySpan<string> values, bool advance = false)
        where TScanner : struct, IScanner
    {
        while (!scanner.IsEof)
        {
            foreach (var value in values)
                if (Tokens.Literal(value, ref scanner, advance))
                    return scanner.IsEof;
            scanner.Advance(1);
        }
        return scanner.IsEof;
    }
    public static bool Until<TScanner, TTerminal>(ref Scanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        where TTerminal : struct, IToken
    {
        var t = new TTerminal();
        while (!scanner.IsEof && !t.Match(ref scanner, advance))
            scanner.Advance(1);
        return !scanner.IsEof;
    }
    public static bool Until<TScanner, TTerminal1, TTerminal2>(ref Scanner scanner, TTerminal1? terminal1 = null, TTerminal2? terminal2 = null, bool advance = false)
        where TScanner : struct, IScanner
        where TTerminal1 : struct, IToken
        where TTerminal2 : struct, IToken
    {
        var t1 = terminal1 ?? new TTerminal1();
        var t2 = terminal2 ?? new TTerminal2();
        while (!scanner.IsEof && !(t1.Match(ref scanner, advance) || t2.Match(ref scanner, advance)))
            scanner.Advance(1);
        return !scanner.IsEof;
    }
    public static bool Until<TScanner, TTerminal1, TTerminal2, TTerminal3>(ref Scanner scanner, TTerminal1? terminal1 = null, TTerminal2? terminal2 = null, TTerminal3? terminal3 = null, bool advance = false)
        where TScanner : struct, IScanner
        where TTerminal1 : struct, IToken
        where TTerminal2 : struct, IToken
        where TTerminal3 : struct, IToken
    {
        var t1 = terminal1 ?? new TTerminal1();
        var t2 = terminal2 ?? new TTerminal2();
        var t3 = terminal3 ?? new TTerminal3();
        while (!scanner.IsEof && !(t1.Match(ref scanner, advance) || t2.Match(ref scanner, advance) || t3.Match(ref scanner, advance)))
            scanner.Advance(1);
        return !scanner.IsEof;
    }


    public static bool Repeat<TScanner, TParser, TNode>(ref TScanner scanner, TParser parser, ParseResult result, out List<TNode> nodes, int minimum, bool withSpaces = false, string? separator = null, in ParseError? orError = null)
        where TScanner : struct, IScanner
        where TParser : struct, IParser<TNode>
        where TNode : Node
    {
        return Repeat(ref scanner, result, (ref TScanner s, ParseResult r, out TNode node, in ParseError? orError) => new TParser().Match(ref s, r, out node, orError), out nodes, minimum, withSpaces, separator, orError);
    }
    public static bool Repeat<TScanner, TNode>(ref TScanner scanner, ParseResult result, ParserDelegate<TScanner, TNode> parser, out List<TNode> nodes, int minimum, bool withSpaces = false, string? separator = null, in ParseError? orError = null)
        where TScanner : struct, IScanner
        where TNode : Node
    {
        var position = scanner.Position;
        nodes = [];
        while (!scanner.IsEof)
        {
            if (parser.Invoke(ref scanner, result, out var node))
            {
                nodes.Add(node);
                if (withSpaces)
                    Spaces0(ref scanner, result, out _);
            }
            else break;

            if (separator is not null)
            {
                if (Tokens.Literal(separator, ref scanner, advance: true))
                {
                    if (withSpaces)
                        Spaces0(ref scanner, result, out _);
                }
                else if (nodes.Count >= minimum)
                    return true;
                else return Exit(ref scanner, result, out nodes, position, orError);
            }
        }
        if (nodes.Count >= minimum)
            return true;
        else return Exit(ref scanner, result, out nodes, position, orError);
    }
}