namespace Stride.Core.CompilerServices.Common;

internal static class SyntaxNodeExtensions
{
    public static bool IsFirstSyntaxReference(this SyntaxNode node, ISymbol symbol)
    {
        var syntaxReferences = symbol.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0)
            return false;

        var firstSyntaxReference = syntaxReferences[0];
        return firstSyntaxReference.SyntaxTree == node.SyntaxTree && firstSyntaxReference.Span == node.Span;
    }
}
