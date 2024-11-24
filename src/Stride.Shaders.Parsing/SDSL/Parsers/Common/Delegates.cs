namespace Stride.Shaders.Parsing.SDSL;


public delegate bool ParserDelegate<TScanner>(ref TScanner scanner, ParseResult result)
    where TScanner : struct, IScanner;
public delegate bool ParserDelegate<TScanner, TResult>(ref TScanner scanner, ParseResult result, out TResult parsed, in ParseError? orError = null)
    where TScanner : struct, IScanner;
public delegate bool ParserListDelegate<TScanner, TResult>(ref TScanner scanner, ParseResult result, out List<TResult> parsed, in ParseError? orError = null)
    where TScanner : struct, IScanner;
public delegate bool ParserOptionalDelegate<TScanner, TResult>(ref TScanner scanner, ParseResult result, out TResult? parsed, in ParseError? orError = null)
    where TScanner : struct, IScanner;
