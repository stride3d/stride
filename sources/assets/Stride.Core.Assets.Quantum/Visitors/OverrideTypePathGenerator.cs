// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Yaml;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum.Visitors
{
    /// <summary>
    /// An implementation of <see cref="AssetNodeMetadataCollectorBase"/> that generates the path to all object references in the given asset.
    /// </summary>
    public class OverrideTypePathGenerator : AssetNodeMetadataCollectorBase
    {
        /// <summary>
        /// Gets the resulting metadata that can be passed to YAML serialization.
        /// </summary>
        [NotNull]
        public YamlAssetMetadata<OverrideType> Result { get; } = new YamlAssetMetadata<OverrideType>();

        /// <inheritdoc/>
        protected override void VisitMemberNode(IAssetMemberNode memberNode, int inNonIdentifiableType)
        {
            if (memberNode?.IsContentOverridden() == true)
            {
                Result.Set(ConvertPath(CurrentPath, inNonIdentifiableType), memberNode.GetContentOverride());
            }
        }

        /// <inheritdoc/>
        protected override void VisitObjectNode([NotNull] IAssetObjectNode objectNode, int inNonIdentifiableType)
        {
            foreach (var index in objectNode.GetOverriddenItemIndices())
            {
                var id = objectNode.IndexToId(index);
                var itemPath = ConvertPath(CurrentPath, inNonIdentifiableType);
                itemPath.PushItemId(id);
                Result.Set(itemPath, ((IAssetObjectNodeInternal)objectNode).GetItemOverride(index));
            }
            foreach (var index in objectNode.GetOverriddenKeyIndices())
            {
                var id = objectNode.IndexToId(index);
                var itemPath = ConvertPath(CurrentPath, inNonIdentifiableType);
                itemPath.PushIndex(id);
                Result.Set(itemPath, ((IAssetObjectNodeInternal)objectNode).GetKeyOverride(index));
            }
        }
    }
}
