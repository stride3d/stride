namespace Stride.Core.CompilerServices.Common;
internal static class WellKnownReferences
{
    public const string ModuleInitializerAttributeName = "Stride.Core.ModuleInitializerAttribute";

    public const string AssemblyScanAttributeName = "Stride.Core.Reflection.AssemblyScanAttribute";

    public const string EffectKeysAttributeName = "Stride.Rendering.EffectKeysAttribute";

    public static INamedTypeSymbol? DataMemberAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
    }

    public static INamedTypeSymbol? IDictionary_generic(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
    }

    public static INamedTypeSymbol? DataMemberIgnoreAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
    }

    public static INamedTypeSymbol? DataMemberMode(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
    }

    public static INamedTypeSymbol? DataMemberUpdatableAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Updater.DataMemberUpdatableAttribute");
    }

    public static INamedTypeSymbol? DataContractAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
    }

    public static INamedTypeSymbol? ModuleInitializerAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(ModuleInitializerAttributeName);
    }

    public static INamedTypeSymbol? IProjectAsset(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.Assets.IProjectAsset");
    }

    public static INamedTypeSymbol? IProjectFileGeneratorAsset(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.Assets.IProjectFileGeneratorAsset");
    }

    public static INamedTypeSymbol? AssetDescriptionAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.Assets.AssetDescriptionAttribute");
    }

    public static INamedTypeSymbol? AssetContentTypeAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.Assets.AssetContentTypeAttribute");
    }

    public static INamedTypeSymbol? AssetFileExtensionAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.Serialization.AssetFileExtensionAttribute");
    }

    public static INamedTypeSymbol? ParameterKey(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Rendering.ParameterKey");
    }

    public static INamedTypeSymbol? EffectKeysAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(EffectKeysAttributeName);
    }

    public static INamedTypeSymbol? GeneratedCodeAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");
    }

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
}
