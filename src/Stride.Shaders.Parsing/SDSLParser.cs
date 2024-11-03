using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Parsing.SDSL.PreProcessing;

namespace Stride.Shaders.Parsing;


public static class SDSLParser
{
    public static ParseResult Parse(string code)
    {
        var c = new CommentProcessedCode(code);
        return Grammar.Match<CommentProcessedCode, ShaderFileParser, ShaderFile>(c);
    }
}