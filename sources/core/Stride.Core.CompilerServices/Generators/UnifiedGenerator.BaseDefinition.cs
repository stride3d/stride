using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Core.CompilerServices.Generators;

public partial class UnifiedGenerator
{
    private abstract record BaseDefinition
    {
        public abstract string FullName { get; }

        protected static string EscapeIdentifier(string name)
        {
            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
                || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
                    ? "@" + name
                    : name;
        }
    }
}
