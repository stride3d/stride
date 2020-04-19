// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum.Visitors
{
    /// <summary>
    /// An implementation of <see cref="AssetNodeMetadataCollectorBase"/> that generates the path to all object references in the given asset.
    /// </summary>
    public class ObjectReferencePathGenerator : AssetNodeMetadataCollectorBase
    {
        private readonly AssetPropertyGraphDefinition propertyGraphDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReferencePathGenerator"/> class.
        /// </summary>
        /// <param name="propertyGraphDefinition">The <see cref="AssetPropertyGraphDefinition"/> used to analyze object references.</param>
        public ObjectReferencePathGenerator(AssetPropertyGraphDefinition propertyGraphDefinition)
        {
            this.propertyGraphDefinition = propertyGraphDefinition;
        }

        /// <summary>
        /// Gets the resulting metadata that can be passed to YAML serialization.
        /// </summary>
        public YamlAssetMetadata<Guid> Result { get; } = new YamlAssetMetadata<Guid>();

        /// <summary>
        /// Gets or sets a method that indicates if a given identifier should be output to the list of object references.
        /// </summary>
        public Func<Guid, bool> ShouldOutputReference { get; set; }

        /// <inheritdoc/>
        protected override void VisitMemberNode([NotNull] IAssetMemberNode memberNode, int inNonIdentifiableType)
        {
            var value = memberNode.Retrieve();
            if (propertyGraphDefinition.IsMemberTargetObjectReference(memberNode, value))
            {
                if (value == null)
                    return;

                var identifiable = value as IIdentifiable;
                if (identifiable == null)
                    throw new InvalidOperationException("IsObjectReference returned true for an object that is not IIdentifiable");

                var id = identifiable.Id;
                if (ShouldOutputReference?.Invoke(id) ?? true)
                    Result.Set(ConvertPath(CurrentPath, inNonIdentifiableType), id);
            }
        }

        /// <inheritdoc/>
        protected override void VisitObjectNode([NotNull] IAssetObjectNode objectNode, int inNonIdentifiableType)
        {
            if (!objectNode.IsReference)
                return;

            foreach (var index in ((IAssetObjectNodeInternal)objectNode).Indices)
            {
                if (!propertyGraphDefinition.IsTargetItemObjectReference(objectNode, index, objectNode.Retrieve(index)))
                    continue;

                var itemPath = ConvertPath(CurrentPath, inNonIdentifiableType);
                if (CollectionItemIdHelper.HasCollectionItemIds(objectNode.Retrieve()))
                {
                    var itemId = objectNode.IndexToId(index);
                    itemPath.PushItemId(itemId);
                }
                else
                {
                    itemPath.PushIndex(index.Value);
                }
                var value = objectNode.Retrieve(index) as IIdentifiable;
                if (value == null)
                    throw new InvalidOperationException("IsObjectReference returned true for an object that is not IIdentifiable");
                var id = value.Id;
                if (ShouldOutputReference?.Invoke(id) ?? true)
                    Result.Set(itemPath, id);
            }
        }
    }
}
