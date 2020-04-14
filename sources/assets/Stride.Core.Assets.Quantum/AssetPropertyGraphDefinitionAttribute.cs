using System;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Quantum
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(AssetPropertyGraphDefinition))]
    public class AssetPropertyGraphDefinitionAttribute : Attribute
    {
        public AssetPropertyGraphDefinitionAttribute([NotNull] Type assetType)
        {
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(Asset).IsAssignableFrom(assetType)) throw new ArgumentException($"The given type must be assignable to the {nameof(Asset)} type.");
            AssetType = assetType;
        }

        public Type AssetType { get; }
    }
}
