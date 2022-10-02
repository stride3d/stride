using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.Extensions
{
    public static class SyntaxReferenceExtensions
    {
        public static Location ToLocation(this SyntaxReference reference)
            => Location.Create(reference.SyntaxTree, reference.Span);
    }
}
