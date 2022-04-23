namespace Stride.Shader.Parser;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

public class SDSLPParser
{
    private static readonly Parser<char, char> LBrace = Char('{');
    private static readonly Parser<char, char> RBrace = Char('}');
    private static readonly Parser<char, char> LBracket = Char('[');
    private static readonly Parser<char, char> RBracket = Char(']');
    private static readonly Parser<char, char> Quote = Char('"');
    private static readonly Parser<char, char> Colon = Char(':');
    private static readonly Parser<char, char> ColonWhitespace =
        Colon.Between(SkipWhitespaces);
    private static readonly Parser<char, char> Comma = Char(',');
    private static readonly Parser<char, string> Identifier =
            Token(c => char.IsLetterOrDigit(c) || c.Equals('_'))
                .ManyString();
    
    public static Result<char, string> Parse(string code)
    {
        return Identifier.Parse(code);
    }
}