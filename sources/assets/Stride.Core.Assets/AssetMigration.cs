// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Serializers;
using Stride.Core.Diagnostics;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Helper for migrating asset to newer versions.
    /// </summary>
    public static class AssetMigration
    {
        public static bool MigrateAssetIfNeeded(AssetMigrationContext context, PackageLoadingAssetFile loadAsset, string dependencyName, PackageVersion untilVersion = null)
        {
            var assetFullPath = loadAsset.FilePath.FullPath;

            // Determine if asset was Yaml or not
            var assetFileExtension = Path.GetExtension(assetFullPath);
            if (assetFileExtension == null)
                return false;

            assetFileExtension = assetFileExtension.ToLowerInvariant();

            var serializer = AssetFileSerializer.FindSerializer(assetFileExtension);
            if (!(serializer is YamlAssetSerializer))
                return false;

            // We've got a Yaml asset, let's get expected and serialized versions
            var serializedVersion = PackageVersion.Zero;
            PackageVersion expectedVersion;
            Type assetType;

            // Read from Yaml file the asset version and its type (to get expected version)
            // Note: It tries to read as few as possible (SerializedVersion is expected to be right after Id, so it shouldn't try to read further than that)
            using (var assetStream = loadAsset.OpenStream())
            using (var streamReader = new StreamReader(assetStream))
            {
                var yamlEventReader = new EventReader(new Parser(streamReader));

                // Skip header
                yamlEventReader.Expect<StreamStart>();
                yamlEventReader.Expect<DocumentStart>();
                var mappingStart = yamlEventReader.Expect<MappingStart>();

                var tagTypeRegistry = AssetYamlSerializer.Default.GetSerializerSettings().TagTypeRegistry;
                bool typeAliased;
                assetType = tagTypeRegistry.TypeFromTag(mappingStart.Tag, out typeAliased);

                var expectedVersions = AssetRegistry.GetCurrentFormatVersions(assetType);
                expectedVersion = expectedVersions?.FirstOrDefault(x => x.Key == dependencyName).Value ?? PackageVersion.Zero;

                Scalar assetKey;
                while ((assetKey = yamlEventReader.Allow<Scalar>()) != null)
                {
                    // Only allow Id before SerializedVersion
                    if (assetKey.Value == nameof(Asset.Id))
                    {
                        yamlEventReader.Skip();
                    }
                    else if (assetKey.Value == nameof(Asset.SerializedVersion))
                    {
                        // Check for old format: only a scalar
                        var scalarVersion = yamlEventReader.Allow<Scalar>();
                        if (scalarVersion != null)
                        {
                            serializedVersion = PackageVersion.Parse("0.0." + Convert.ToInt32(scalarVersion.Value, CultureInfo.InvariantCulture));

                            // Let's update to new format
                            using (var yamlAsset = loadAsset.AsYamlAsset())
                            {
                                yamlAsset.DynamicRootNode.RemoveChild(nameof(Asset.SerializedVersion));
                                AssetUpgraderBase.SetSerializableVersion(yamlAsset.DynamicRootNode, dependencyName, serializedVersion);

                                var baseBranch = yamlAsset.DynamicRootNode["~Base"];
                                if (baseBranch != null)
                                {
                                    var baseAsset = baseBranch["Asset"];
                                    if (baseAsset != null)
                                    {
                                        baseAsset.RemoveChild(nameof(Asset.SerializedVersion));
                                        AssetUpgraderBase.SetSerializableVersion(baseAsset, dependencyName, serializedVersion);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // New format: package => version mapping
                            yamlEventReader.Expect<MappingStart>();

                            while (!yamlEventReader.Accept<MappingEnd>())
                            {
                                var packageName = yamlEventReader.Expect<Scalar>().Value;
                                var packageVersion = PackageVersion.Parse(yamlEventReader.Expect<Scalar>().Value);

                                // For now, we handle only one dependency at a time
                                if (packageName == dependencyName)
                                {
                                    serializedVersion = packageVersion;
                                }
                            }

                            yamlEventReader.Expect<MappingEnd>();
                        }
                        break;
                    }
                    else
                    {
                        // If anything else than Id or SerializedVersion, let's stop
                        break;
                    }
                }
            }

            if (serializedVersion > expectedVersion)
            {
                // Try to open an asset newer than what we support (probably generated by a newer Stride)
                throw new InvalidOperationException($"Asset of type {assetType} has been serialized with newer version {serializedVersion}, but only version {expectedVersion} is supported. Was this asset created with a newer version of Stride?");
            }

            if (serializedVersion < expectedVersion)
            {
                // Perform asset upgrade
                context.Log.Verbose($"{Path.GetFullPath(assetFullPath)} needs update, from version {serializedVersion} to version {expectedVersion}");

                using (var yamlAsset = loadAsset.AsYamlAsset())
                {
                    var yamlRootNode = yamlAsset.RootNode;

                    // Check if there is any asset updater
                    var assetUpgraders = AssetRegistry.GetAssetUpgraders(assetType, dependencyName);
                    if (assetUpgraders == null)
                    {
                        throw new InvalidOperationException($"Asset of type {assetType} should be updated from version {serializedVersion} to {expectedVersion}, but no asset migration path was found");
                    }

                    // Instantiate asset updaters
                    var currentVersion = serializedVersion;
                    while (currentVersion != expectedVersion)
                    {
                        PackageVersion targetVersion;
                        // This will throw an exception if no upgrader is available for the given version, exiting the loop in case of error.
                        var upgrader = assetUpgraders.GetUpgrader(currentVersion, out targetVersion);

                        // Stop if the next version would be higher than what is expected
                        if (untilVersion != null && targetVersion > untilVersion)
                            break;

                        upgrader.Upgrade(context, dependencyName, currentVersion, targetVersion, yamlRootNode, loadAsset);
                        currentVersion = targetVersion;
                    }

                    // Make sure asset is updated to latest version
                    YamlNode serializedVersionNode;
                    PackageVersion newSerializedVersion = null;
                    if (yamlRootNode.Children.TryGetValue(new YamlScalarNode(nameof(Asset.SerializedVersion)), out serializedVersionNode))
                    {
                        var newSerializedVersionForDefaultPackage = ((YamlMappingNode)serializedVersionNode).Children[new YamlScalarNode(dependencyName)];
                        newSerializedVersion = PackageVersion.Parse(((YamlScalarNode)newSerializedVersionForDefaultPackage).Value);
                    }

                    if (untilVersion == null && newSerializedVersion != expectedVersion)
                    {
                        throw new InvalidOperationException($"Asset of type {assetType} was migrated, but still its new version {newSerializedVersion} doesn't match expected version {expectedVersion}.");
                    }

                    context.Log.Verbose($"{Path.GetFullPath(assetFullPath)} updated from version {serializedVersion} to version {expectedVersion}");
                }

                return true;
            }

            return false;
        }
    }
}
