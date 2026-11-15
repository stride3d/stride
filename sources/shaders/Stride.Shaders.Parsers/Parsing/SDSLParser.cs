using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Parsing.SDSL.PreProcessing;

namespace Stride.Shaders.Parsing;

/// <summary>
/// Wrapper class for both grammar and code preprocessor
/// </summary>
public static class SDSLParser
{
    /// <summary>Parses a shader file.</summary>
    /// <param name="isEffectFile">Whether the code is an .sdfx file, where <c>shader</c> can declare an effect (see <see cref="SDFX.AST.ShaderEffect.ShaderKeyword"/>).</param>
    public static ParseResult Parse(string code, bool isEffectFile = false)
    {
        var c = new CommentProcessedCode(code);
        return Grammar.Match<CommentProcessedCode, ShaderFileParser, ShaderFile>(c, new ShaderFileParser { IsEffectFile = isEffectFile });
    }
}
