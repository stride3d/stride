using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Generators;

internal partial class UnifiedGenerator
{
    private sealed record TypeDefinition(string Name, BaseDefinition? Parent, EquatableArray<string> GenericParameters) : BaseDefinition
    {
        public TypeDefinition(string Name, BaseDefinition? Parent) : this(Name, Parent, EquatableArray<string>.Empty)
        {
        }

        public override string FullName
        {
            get
            {
                var genericParameters = GenericParameters.Count > 0 ? $"<{string.Join(", ", GenericParameters)}>" : "";
                return Parent is NamespaceDefinition or TypeDefinition ? $"{Parent.FullName}.{Name}{genericParameters}" : $"{Name}{genericParameters}";
            }
        }

        public string UnboundFullName
        {
            get
            {
                var genericParameters = GenericParameters.Count > 0 ? $"<{new string(',', GenericParameters.Count - 1)}>" : "";
                if (Parent is NamespaceDefinition)
                {
                    return $"{Parent.FullName}.{Name}{genericParameters}";
                }
                else if (Parent is TypeDefinition declaringType)
                {
                    return $"{declaringType.UnboundFullName}.{Name}{genericParameters}";
                }
                else
                {
                    return $"{Name}{genericParameters}";
                }
            }
        }

        public static TypeDefinition FromSymbol(INamedTypeSymbol symbol)
        {
            var genericParameters = symbol.TypeParameters.Select(tp => EscapeIdentifier(tp.Name)).ToArray();
            BaseDefinition? parent = null;
            if (symbol.ContainingType is not null)
            {
                parent = FromSymbol(symbol.ContainingType);
            }
            else if (symbol.ContainingNamespace is not null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parent = NamespaceDefinition.FromSymbol(symbol.ContainingNamespace);
            }
            return new TypeDefinition(EscapeIdentifier(symbol.Name), parent, genericParameters);
        }
    }
}
