// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Assets.Entities
{
    partial class EntityHierarchyAssetBase
    {
        /// <summary>
        /// Updates the Gravity field on all CharacterComponents in a SceneAsset from float to Vector3 to support three-dimensional gravity.
        /// </summary>
        protected class CharacterComponentGravityVector3Upgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                // Set up asset and entity hierarchy for reading.
                var hierarchy = asset.Hierarchy;
                var entities = (DynamicYamlArray)hierarchy.Parts;

                // Loop through the YAML file.
                foreach (dynamic entityDesign in entities)
                {
                    // Get the entity.
                    var entity = entityDesign.Entity;

                    // Further loop to find CharacterComponents to upgrade.
                    foreach (var component in entity.Components)
                    {
                        try
                        {
                            var componentTag = component.Value.Node.Tag;

                            // Is this a character component?
                            if (componentTag == "!CharacterComponent")
                            {
                                // Retrieve old gravity value.
                                var oldGravity = component.Value.Gravity as DynamicYamlScalar;

                                //Actually upgrade Gravity to a Vector3.
                                if (component.Value.ContainsChild("Gravity"))
                                {
                                    component.Value.Gravity = new YamlMappingNode
                                    {
                                        { new YamlScalarNode("X"), new YamlScalarNode("0.0") },
                                        { new YamlScalarNode("Y"), new YamlScalarNode(oldGravity.Node.Value) },
                                        { new YamlScalarNode("Z"), new YamlScalarNode("0.0") }
                                    };
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Ignore();
                        }
                    }
                }
            }
        }
    }
}
