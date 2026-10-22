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
                if (field is null)
                {
                    var genericParameters = GenericParameters.Count > 0 ? $"<{string.Join(", ", GenericParameters)}>" : "";
                    field = Parent is NamespaceDefinition or TypeDefinition ? $"{Parent.FullName}.{Name}{genericParameters}" : $"{Name}{genericParameters}";
                }
                return field;
            }
        }

        public string UnboundFullName
        {
            get
            {
                if (field is null)
                {
                    var genericParameters = GenericParameters.Count > 0 ? $"<{new string(',', GenericParameters.Count - 1)}>" : "";
                    if (Parent is NamespaceDefinition)
                    {
                        field = $"{Parent.FullName}.{Name}{genericParameters}";
                    }
                    else if (Parent is TypeDefinition declaringType)
                    {
                        field = $"{declaringType.UnboundFullName}.{Name}{genericParameters}";
                    }
                    else
                    {
                        field = $"{Name}{genericParameters}";
                    }
                }
                return field;
            }
        }

        public static TypeDefinition FromSymbol(INamedTypeSymbol symbol)
        {
            var genericParameters = symbol.TypeParameters.Select(tp => tp.Name).ToArray();
            BaseDefinition? parent = null;
            if (symbol.ContainingType is not null)
            {
                parent = FromSymbol(symbol.ContainingType);
            }
            else if (symbol.ContainingNamespace is not null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parent = NamespaceDefinition.FromSymbol(symbol.ContainingNamespace);
            }
            return new TypeDefinition(symbol.Name, parent, genericParameters);
        }
    }
}
