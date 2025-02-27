using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Stride.Shaders.Parsing.LSP;

public static class ASTExtensions
{
    public static bool Intersects<N>(this N node, Position position)
        where N : Node
    {
        if (
            position.Line + 1 >= node.Info.Line
            && position.Line + 1 <= node.Info.EndLine
            && position.Character + 1 >= node.Info.Column
            && position.Character + 1 < node.Info.Column + node.Info.Length
        )
        {
            return true;
        }
        return false;
    }
}