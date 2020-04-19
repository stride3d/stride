// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.TextureConverter;
using Stride.Assets.Effect;
using Stride.Assets.Templates;
using Stride.Graphics;
using System.Text;

namespace Stride.Assets
{
    [PackageUpgrader(new[] { StrideConfig.PackageName, "Stride.Core", "Stride.Engine", "Xenko", "Xenko.Core", "Xenko.Engine" }, "3.1.0.0", CurrentVersion)]
    public partial class StridePackageUpgrader : PackageUpgrader
    {
        // Should match Stride.nupkg
        public const string CurrentVersion = StrideVersion.NuGetVersion;

        public static readonly string DefaultGraphicsCompositorLevel9Url = "DefaultGraphicsCompositorLevel9";
        public static readonly string DefaultGraphicsCompositorLevel10Url = "DefaultGraphicsCompositorLevel10";

        public static readonly Guid DefaultGraphicsCompositorLevel9CameraSlot = new Guid("bbfef2cb-8c63-4cab-9caf-6ae48f44a8ba");
        public static readonly Guid DefaultGraphicsCompositorLevel10CameraSlot = new Guid("d0a6bf72-b3cd-4bd4-94ca-69952999d537");

        public static readonly Guid UpdatePlatformsTemplateId = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
        {
            if (dependency.Version.MinVersion < new PackageVersion("3.1.0.2-beta01"))
            {
                foreach (var assetFile in assetFiles)
                {
                    try
                    {
                        // Add new generic parameter to ShadowMapReceiverDirectional in effect log
                        if (assetFile.OriginalFilePath.GetFileExtension() == ".xkeffectlog")
                        {
                            var assetContent = assetFile.AssetContent ?? File.ReadAllBytes(assetFile.OriginalFilePath.FullPath);
                            var assetContentString = System.Text.Encoding.UTF8.GetString(assetContent);
                            var newAssetContentString = System.Text.RegularExpressions.Regex.Replace(assetContentString, @"([ ]*)-   ClassName:", "$1- !ShaderClassSource\r\n$1    ClassName:");
                            if (assetContentString != newAssetContentString)
                            {
                                // Need replacement, update with replaced text
                                // Save file (usually we shouldn't do that, but since we have the renaming in 4.0 we force everything to be on disk before moving on
                                File.WriteAllBytes(assetFile.OriginalFilePath.FullPath, System.Text.Encoding.UTF8.GetBytes(newAssetContentString));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Warning($"Could not upgrade asset [{assetFile.AssetLocation}] to ShaderClassSource", e);
                    }
                }
            }

            if (dependency.Version.MinVersion < new PackageVersion("4.0.0.0"))
            {
                foreach (var assetFile in assetFiles)
                {
                    try
                    {
                        // Note: usually we shouldn't replace files directly (file might be already moved/modified in-memory)
                        // but since we're currently the first/only upgrader to run, that's fine and later (4.1) we can get rid of this upgrade path
                        assetFile.OriginalFilePath = assetFile.FilePath = XenkoToStrideRenameHelper.RenameStrideFile(assetFile.OriginalFilePath.FullPath, XenkoToStrideRenameHelper.StrideContentType.Asset);
                    }
                    catch (Exception e)
                    {
                        log.Warning($"Could not upgrade asset [{assetFile.AssetLocation}] to Stride", e);
                    }
                }
            }

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
            // Xenko to Stride renaming
            if (dependency.Version.MinVersion < new PackageVersion("4.0.0.0"))
            {
                UpgradeStrideCode(dependentPackage, log);
            }

            // Update NuGet references
            var projectFullPath = (dependentPackage.Container as SolutionProject)?.FullPath;
            if (projectFullPath != null)
            {
                try
                {
                    var project = VSProjectHelper.LoadProject(projectFullPath.ToWindowsPath());
                    var isProjectDirty = false;

                    var packageReferences = project.GetItems("PackageReference").ToList();

                    foreach (var packageReference in packageReferences)
                    {
                        if (packageReference.EvaluatedInclude.StartsWith("Stride.") && packageReference.GetMetadataValue("Version") != CurrentVersion)
                        {
                            packageReference.SetMetadataValue("Version", CurrentVersion).Xml.ExpressedAsAttribute = true;
                            foreach (var metadata in packageReference.Metadata)
                                metadata.Xml.ExpressedAsAttribute = true;
                            isProjectDirty = true;
                        }
                    }

                    // Change shader generated file from .cs to .xksl.cs or .xkfx.cs
                    // Note: we support both 3.1.0.1 (.cs) and 3.2.0.1 (.xksl.cs)
                    if (dependency.Version.MinVersion < new PackageVersion("4.0.0.0"))
                    {
                        // Find xksl files
                        var shaderFiles = project.Items.Where(x => x.ItemType == "None" && (x.EvaluatedInclude.EndsWith(".xksl", StringComparison.InvariantCultureIgnoreCase) || x.EvaluatedInclude.EndsWith(".xkfx", StringComparison.InvariantCultureIgnoreCase)) && x.HasMetadata("Generator")).ToArray();

                        foreach (var shaderFile in shaderFiles)
                        {
                            isProjectDirty = true;

                            var shaderFilePath = Path.Combine(projectFullPath.GetFullDirectory(), new UFile(shaderFile.EvaluatedInclude));
                            var oldGeneratedFilePath = Path.ChangeExtension(shaderFilePath, ".cs");

                            if (!File.Exists(oldGeneratedFilePath))
                            {
                                oldGeneratedFilePath = shaderFilePath + ".cs";
                            }

                            if (File.Exists(oldGeneratedFilePath))
                            {
                                var newShaderFilePath = shaderFilePath.Replace(".xk", ".sd");
                                File.Move(shaderFilePath, newShaderFilePath);
                                File.Move(oldGeneratedFilePath, newShaderFilePath + ".cs");

                                // Remove Items (.xksl .cs and .xksl.cs)
                                foreach (var csElement in project.Xml.ItemGroups.SelectMany(x => x.Items).Where(x =>
                                    new UFile(x.Include) == new UFile(Path.ChangeExtension(shaderFile.EvaluatedInclude, ".cs"))
                                    || new UFile(x.Include) == new UFile(shaderFile.EvaluatedInclude + ".cs")
                                    || new UFile(x.Include) == new UFile(shaderFile.EvaluatedInclude)
                                    || new UFile(x.Update) == new UFile(Path.ChangeExtension(shaderFile.EvaluatedInclude, ".cs"))
                                    || new UFile(x.Update) == new UFile(shaderFile.EvaluatedInclude + ".cs")
                                    || new UFile(x.Update) == new UFile(shaderFile.EvaluatedInclude)
                                    ).ToArray())
                                {
                                    csElement.Parent.RemoveChild(csElement);
                                }

                                if (!shaderFile.IsImported)
                                    project.RemoveItem(shaderFile);
                            }
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
