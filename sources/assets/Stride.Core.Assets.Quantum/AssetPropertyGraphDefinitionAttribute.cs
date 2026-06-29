
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(AssetPropertyGraphDefinition))]
[AssemblyScan]
public class AssetPropertyGraphDefinitionAttribute : Attribute
{
    public AssetPropertyGraphDefinitionAttribute(Type assetType)
    {
        ArgumentNullException.ThrowIfNull(assetType);
        if (!typeof(Asset).IsAssignableFrom(assetType)) throw new ArgumentException($"The given type must be assignable to the {nameof(Asset)} type.");
        AssetType = assetType;
    }

    public Type AssetType { get; }
}
