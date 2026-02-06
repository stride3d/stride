using System.Text.RegularExpressions;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Analysis;

internal static class SemanticAnalyzer
{
    private static readonly Regex MatchSemanticName = new Regex(@"([A-Za-z_]+)(\d*)");

    public static (string Name, int Index) ParseSemantic(string semantic)
    {
        var match = MatchSemanticName.Match(semantic);
        if (!match.Success)
            return (semantic, 0);

        string baseName = match.Groups[1].Value;
        int value = 0;
        if (!string.IsNullOrEmpty(match.Groups[2].Value))
        {
            value = int.Parse(match.Groups[2].Value);
        }

        return (baseName, value);
    }
}
