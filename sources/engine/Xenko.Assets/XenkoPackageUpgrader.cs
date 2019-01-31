// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Storage;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.TextureConverter;
using Xenko.Assets.Effect;
using Xenko.Assets.Templates;
using Xenko.Graphics;

namespace Xenko.Assets
{
    [PackageUpgrader(new[] { XenkoConfig.PackageName, "Xenko.Core", "Xenko.Engine" }, "3.0.0.0", CurrentVersion)]
    public partial class XenkoPackageUpgrader : PackageUpgrader
    {
        // Should match Xenko.nupkg
        public const string CurrentVersion = XenkoVersion.NuGetVersion;

        public static readonly string DefaultGraphicsCompositorLevel9Url = "DefaultGraphicsCompositorLevel9";
        public static readonly string DefaultGraphicsCompositorLevel10Url = "DefaultGraphicsCompositorLevel10";

        public static readonly Guid DefaultGraphicsCompositorLevel9CameraSlot = new Guid("bbfef2cb-8c63-4cab-9caf-6ae48f44a8ba");
        public static readonly Guid DefaultGraphicsCompositorLevel10CameraSlot = new Guid("d0a6bf72-b3cd-4bd4-94ca-69952999d537");

        public static readonly Guid UpdatePlatformsTemplateId = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
        {
            return true;
        }

        private void RunAssetUpgradersUntilVersion(ILogger log, Package dependentPackage, string dependencyName, IList<PackageLoadingAssetFile> assetFiles, PackageVersion maxVersion)
        {
            foreach (var assetFile in assetFiles)
            {
                if (assetFile.Deleted)
                    continue;

                var context = new AssetMigrationContext(dependentPackage, assetFile.ToReference(), assetFile.FilePath.ToWindowsPath(), log);
                AssetMigration.MigrateAssetIfNeeded(context, assetFile, dependencyName, maxVersion);
            }
        }

        /// <inheritdoc/>
        public override bool UpgradeAfterAssetsLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, PackageVersionRange dependencyVersionBeforeUpdate)
        {
            if (dependencyVersionBeforeUpdate.MinVersion < new PackageVersion("1.3.0-alpha02"))
            {
                // Add everything as root assets (since we don't know what the project was doing in the code before)
                foreach (var assetItem in dependentPackage.Assets)
                {
                    if (!AssetRegistry.IsAssetTypeAlwaysMarkAsRoot(assetItem.Asset.GetType()))
                        dependentPackage.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));
                }
            }

            if (dependencyVersionBeforeUpdate.MinVersion < new PackageVersion("1.6.0-beta"))
            {
                // Mark all assets dirty to force a resave
                foreach (var assetItem in dependentPackage.Assets)
                {
                    if (!(assetItem.Asset is SourceCodeAsset))
                    {
                        assetItem.IsDirty = true;
                    }
                }
            }

            return true;
        }

        public override bool UpgradeBeforeAssembliesLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
        {
            // Update NuGet references
            var projectFullPath = (dependentPackage.Container as SolutionProject)?.FullPath;
            if (projectFullPath != null)
            {
                try
                {
                    var project = VSProjectHelper.LoadProject(projectFullPath.ToWindowsPath());
                    var isProjectDirty = false;

                    var packageReferences = project.GetItems("PackageReference").ToList();

                    // Upgrade from 3.0 to 3.1 (Xenko split in several nuget packages)
                    if (dependency.Version.MinVersion < new PackageVersion("3.1.0.0"))
                    {
                        var xenkoReference = packageReferences.FirstOrDefault(packageReference => packageReference.EvaluatedInclude == "Xenko");
                        if (xenkoReference != null)
                        {
                            var items = new List<Microsoft.Build.Evaluation.ProjectItem> { xenkoReference };

                            // Turn Xenko reference into Xenko.Engine
                            xenkoReference.UnevaluatedInclude = "Xenko.Engine";
                            xenkoReference.SetMetadataValue("Version", CurrentVersion);

                            // Add plugins (old Xenko is equivalent to a meta package with all plugins)
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Video", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Physics", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Navigation", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Particles", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));
                            items.AddRange(project.AddItem("PackageReference", "Xenko.UI", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));
                            // Necessary until "build" flows transitively
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Core", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") }));

                            // Asset compiler
                            items.AddRange(project.AddItem("PackageReference", "Xenko.Core.Assets.CompilerApp", new[] { new KeyValuePair<string, string>("Version", CurrentVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers"), new KeyValuePair<string, string>("IncludeAssets", "build") }));

                            foreach (var item in items)
                            {
                                foreach (var metadata in item.Metadata)
                                    metadata.Xml.ExpressedAsAttribute = true;
                            }

                            isProjectDirty = true;
                        }
                    }

                    foreach (var packageReference in packageReferences)
                    {
                        if (packageReference.EvaluatedInclude.StartsWith("Xenko.") && packageReference.GetMetadataValue("Version") != CurrentVersion)
                        {
                            packageReference.SetMetadataValue("Version", CurrentVersion).Xml.ExpressedAsAttribute = true;
                            foreach (var metadata in packageReference.Metadata)
                                metadata.Xml.ExpressedAsAttribute = true;
                            isProjectDirty = true;
                        }
                    }

                    if (isProjectDirty)
                        project.Save();

                    project.ProjectCollection.UnloadAllProjects();
                    project.ProjectCollection.Dispose();
                }
                catch (Exception e)
                {
                    log.Warning($"Unable to load project [{projectFullPath.GetFileName()}]", e);
                }
            }

            return true;
        }
    }
}
