namespace Stride.Core.CompilerServices.Generators;

public partial class UnifiedGenerator
{
    private sealed record NamespaceDefinition(string Name, BaseDefinition? Parent) : BaseDefinition
    {
        public override string FullName
        {
            get
            {
                return Parent is NamespaceDefinition ? $"{Parent.FullName}.{Name}" : Name;
            }
        }

        public static NamespaceDefinition FromSymbol(INamespaceSymbol symbol)
        {
            BaseDefinition? parent;
            if (symbol.ContainingNamespace is not null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parent = FromSymbol(symbol.ContainingNamespace);
            }
            else
            {
                parent = AssemblyDefinition.FromSymbol(symbol.ContainingAssembly);
            }
            return new NamespaceDefinition(EscapeIdentifier(symbol.Name), parent);
        }
    }
}
