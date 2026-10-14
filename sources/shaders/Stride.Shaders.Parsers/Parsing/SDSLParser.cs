using System.Reflection;
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
    /// <param name="upgrade">
    /// Whether the shader upgrade reads the code: the errors marked <see cref="RecoverableInUpgradeAttribute"/> go to
    /// <see cref="ParseResult{T}.Recovered"/>, and the code reads as its new form. Builds never set it.
    /// </param>
    public static ParseResult Parse(string code, bool isEffectFile = false, bool upgrade = false)
    {
        var c = new CommentProcessedCode(code);
        var result = Grammar.Match<CommentProcessedCode, ShaderFileParser, ShaderFile>(c, new ShaderFileParser { IsEffectFile = isEffectFile });
        if (upgrade)
        {
            result.Recovered.AddRange(result.Errors.Where(IsRecoverableInUpgrade));
            result.Errors.RemoveAll(IsRecoverableInUpgrade);
        }
        return result;
    }

    // The codes of the errors marked RecoverableInUpgrade, the names of their constants
    private static readonly HashSet<string> RecoverableInUpgrade = typeof(SDSLErrorMessages).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(x => x.IsDefined(typeof(RecoverableInUpgradeAttribute), false))
        .Select(x => x.Name)
        .ToHashSet();

    private static bool IsRecoverableInUpgrade(ParseError error) => RecoverableInUpgrade.Contains(error.Message.Split(':')[0]);
}
