// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Microsoft.Build.Evaluation;
using Stride.Core.Annotations;

namespace Stride.Assets
{
    [PackageUpgrader(new[] { StrideConfig.LogicalPackageName, "Stride.Core", "Stride.Engine" }, "4.0.0.0", CurrentVersion)]
    public partial class StridePackageUpgrader : PackageUpgrader
    {
        // Should match Stride.nupkg
        public const string CurrentVersion = StrideVersion.NuGetVersion;

        public static readonly string DefaultGraphicsCompositorLevel9Url = "DefaultGraphicsCompositorLevel9";
        public static readonly string DefaultGraphicsCompositorLevel10Url = "DefaultGraphicsCompositorLevel10";

        public static readonly Guid DefaultGraphicsCompositorLevel9CameraSlot = new Guid("bbfef2cb-8c63-4cab-9caf-6ae48f44a8ba");
        public static readonly Guid DefaultGraphicsCompositorLevel10CameraSlot = new Guid("d0a6bf72-b3cd-4bd4-94ca-69952999d537");

        public static readonly Guid UpdatePlatformsTemplateId = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

        // True if the version upgraded *from* is numerically below <paramref name="version"/>, ignoring the whole
        // prerelease suffix — so 4.4.0-beta1, 4.4.0-dev2, 4.4.0-mystudio all count as already at 4.4.0 and the gate
        // won't re-run. This is the standard gate: the suffix (beta/dev/custom) is cosmetic, so a format change must
        // be paired with a numeric Patch bump (e.g. 4.4.0 -> 4.4.1), and gates key off that numeric version.
        // The historical direct PackageVersion compares below (e.g. < 3.1.0.2-beta01) predate this rule; new gates
        // should use this helper with a clean numeric threshold.
        private static bool UpgradingFromBefore(PackageDependency dependency, string version)
            => dependency.Version.MinVersion.Version < new PackageVersion(version).Version;

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

            if (UpgradingFromBefore(dependency, "4.0.0.0"))
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

                var context = new AssetMigrationContext(dependentPackage, assetFile.ToReference(), assetFile.FilePath.ToOSPath(), log);
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

        /// <summary>
        /// Rewrites every <c>Stride.*</c> <c>PackageReference</c> in the csproj at
        /// <paramref name="csprojPath"/> to <see cref="CurrentVersion"/>. Standalone — no
        /// <see cref="PackageSession"/> or <see cref="Stride.Core.Reflection.AssemblyContainer"/>
        /// involved. Common building block for fresh-template instantiation and legacy
        /// session-load paths.
        /// </summary>
        public static void UpgradeProjectVersions(string csprojPath, ILogger log)
        {
            ArgumentNullException.ThrowIfNull(csprojPath);
            try
            {
                var project = VSProjectHelper.LoadProject(csprojPath);
                var isDirty = false;

                foreach (var package in project.GetItems("PackageReference"))
                {
                    if (IsSkippedPackage(package.EvaluatedInclude))
                        continue;

                    if (package.EvaluatedInclude.StartsWith("Stride.", StringComparison.Ordinal)
                        && package.GetMetadataValue("Version") != CurrentVersion)
                    {
                        package.SetMetadataValue("Version", CurrentVersion).Xml.ExpressedAsAttribute = true;
                        foreach (var metadata in package.Metadata)
                            metadata.Xml.ExpressedAsAttribute = true;
                        isDirty = true;
                    }
                }

                if (isDirty)
                    project.Save();

                project.ProjectCollection.UnloadAllProjects();
                project.ProjectCollection.Dispose();
            }
            catch (Exception e)
            {
                log.Warning($"Unable to upgrade Stride.* PackageReference versions in [{Path.GetFileName(csprojPath)}]", e);
            }
        }

        /// <summary>
        /// Future: Roslyn-driven symbol-level code migration across Stride versions (e.g.
        /// 4.x→5.0 API renames, signature changes). Planned replacement for the legacy regex
        /// <see cref="UpgradeStrideCode"/>. Will use <c>MSBuildWorkspace</c> so old-version
        /// Stride.* nupkgs resolve as <c>MetadataReference</c>s and the AST has full symbol
        /// info — distinct from the AppDomain assembly-load path.
        /// </summary>
        /// <remarks>
        /// Intentionally empty for now. Same standalone shape as <see cref="UpgradeProjectVersions"/>
        /// so it can serve fresh-template instantiation and legacy session-load equally once
        /// implemented.
        /// </remarks>
        public static void UpgradeProjectCode(string csprojPath, PackageVersion fromVersion, ILogger log)
        {
            ArgumentNullException.ThrowIfNull(csprojPath);
        }

        private static bool IsSkippedPackage(string packageName)
        {
            for (int i = 0; i < StridePackagesToSkipUpgrade.PackageNames.Length; i++)
            {
                if (packageName.StartsWith(StridePackagesToSkipUpgrade.PackageNames[i])
                    || StridePackagesToSkipUpgrade.PackageNames[i].Equals(packageName))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool UpgradeBeforeAssembliesLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
        {
            // Xenko to Stride renaming
            if (UpgradingFromBefore(dependency, "4.0.0.0"))
            {
                UpgradeStrideCode(dependentPackage, log);
            }

            var solutionProject = dependentPackage.Container as SolutionProject;
            var projectFullPath = solutionProject?.FullPath;
            if (projectFullPath == null)
                return true;

            // Stride.* PackageReference version bump (shared with the fresh-template path).
            UpgradeProjectVersions(projectFullPath.ToOSPath(), log);

            // Legacy version-gated structural changes (remove old Stride ref for Linux/macOS
            // execs, .xksl→.sdsl renames, TFM bumps, .sdsl.cs→.bak). These need their own
            // ProjectCollection load — the standalone above already saved + disposed its copy.
            try
            {
                var project = VSProjectHelper.LoadProject(projectFullPath.ToOSPath());
                var isProjectDirty = false;

                // Remove Stride reference for older executable projects (it was necessary in the past due to runtime.json)
                if (UpgradingFromBefore(dependency, "4.1.0.0")
                    && solutionProject.Type == ProjectType.Executable
                    && (solutionProject.Platform == PlatformType.macOS || solutionProject.Platform == PlatformType.Linux))
                {
                    var strideReference = project.GetItems("PackageReference").FirstOrDefault(x => x.EvaluatedInclude == "Stride");
                    if (strideReference != null)
                    {
                        project.RemoveItem(strideReference);
                        isProjectDirty = true;
                    }
                }

                // Change shader generated file from .cs to .xksl.cs or .xkfx.cs
                // Note: we support both 3.1.0.1 (.cs) and 3.2.0.1 (.xksl.cs)
                if (UpgradingFromBefore(dependency, "4.0.0.0"))
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

                if (UpgradingFromBefore(dependency, "4.1.0.0") && solutionProject != null)
                {
                    var tfm = project.GetProperty("TargetFramework");
                    if (tfm != null)
                    {
                        // Library
                        if (tfm.EvaluatedValue == "netstandard2.0"
                            || (tfm.EvaluatedValue.StartsWith("net4", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library))
                        {
                            tfm.UnevaluatedValue = "net6.0";
                            isProjectDirty = true;
                            project.ReevaluateIfNecessary();
                        }
                        // Executable
                        else if ((tfm.EvaluatedValue.StartsWith("net4", StringComparison.Ordinal) || tfm.EvaluatedValue.StartsWith("net5", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                        {
                            tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net6.0-windows" : "net6.0";
                            isProjectDirty = true;
                            project.ReevaluateIfNecessary();
                        }
                    }
                }

                if (UpgradingFromBefore(dependency, "4.2.0.0") && solutionProject != null)
                {
                    if (GetTargetFramework(project) is { } tfm)
                    {
                        // Library
                        if (tfm.EvaluatedValue.StartsWith("net6", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library)
                        {
                            tfm.UnevaluatedValue = "net8.0";
                            isProjectDirty = true;
                            project.ReevaluateIfNecessary();
                        }
                        // Executable
                        else if ((tfm.EvaluatedValue.StartsWith("net6", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                        {
                            tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net8.0-windows" : "net8.0";
                            isProjectDirty = true;
                            project.ReevaluateIfNecessary();
                        }
                    }
                }

                if (UpgradingFromBefore(dependency, "4.3.0.0") && solutionProject != null)
                {
                    if (GetTargetFramework(project) is { } tfm)
                    {
                        // Library
                        if (tfm.EvaluatedValue.StartsWith("net8", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library)
                        {
                            tfm.UnevaluatedValue = "net10.0";
                            isProjectDirty = true;
                        }
                        // Executable
                        else if ((tfm.EvaluatedValue.StartsWith("net8", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                        {
                            tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net10.0-windows" : "net10.0";
                            isProjectDirty = true;
                        }
                    }
                }

                if (UpgradingFromBefore(dependency, "4.4.0.0") && solutionProject != null)
                {
                    // Asset compiler package was renamed Stride.Core.Assets.CompilerApp -> Stride.AssetCompiler.
                    // Also normalize its asset flow to build;buildTransitive: the reference lives on the Game
                    // library, and the build targets only reach the executable (which actually compiles assets)
                    // when they propagate transitively. Drop the inert PrivateAssets (the package ships no
                    // contentfiles/analyzers).
                    foreach (var compilerRef in project.Xml.ItemGroups
                        .SelectMany(g => g.Items)
                        .Where(x => x.ItemType == "PackageReference"
                            && (x.Include == "Stride.Core.Assets.CompilerApp" || x.Include == "Stride.AssetCompiler"))
                        .ToArray())
                    {
                        if (compilerRef.Include == "Stride.Core.Assets.CompilerApp")
                            compilerRef.Include = "Stride.AssetCompiler";

                        var includeAssets = compilerRef.Metadata.FirstOrDefault(m => m.Name == "IncludeAssets");
                        if (includeAssets != null)
                            includeAssets.Value = "build;buildTransitive";
                        else
                            compilerRef.AddMetadata("IncludeAssets", "build;buildTransitive", true);

                        var privateAssets = compilerRef.Metadata.FirstOrDefault(m => m.Name == "PrivateAssets");
                        if (privateAssets != null)
                            compilerRef.RemoveChild(privateAssets);

                        isProjectDirty = true;
                    }

                    // .sdsl/.sdfx generated C# now comes from a Roslyn source generator into obj/, so rename any
                    // on-disk siblings to .bak (recoverable, inert in build) and strip leftover csproj items /
                    // Generator metadata.
                    var projectDir = projectFullPath.GetFullDirectory().ToOSPath();
                    int renamedCount = 0;
                    foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
                    {
                        if (!(file.EndsWith(".sdsl.cs", StringComparison.OrdinalIgnoreCase)
                            || file.EndsWith(".sdfx.cs", StringComparison.OrdinalIgnoreCase)))
                            continue;

                        // Skip build output (Roslyn-generated copies live in obj/, copies may be staged in bin/)
                        var rel = Path.GetRelativePath(projectDir, file).Replace('\\', '/');
                        if (rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
                            || rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
                            || rel.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                            || rel.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            File.Move(file, file + ".bak", overwrite: true);
                            log.Info($"Renamed legacy generated shader file: {rel} -> {rel}.bak");
                            renamedCount++;
                        }
                        catch (Exception e)
                        {
                            log.Warning($"Could not rename legacy generated shader file [{rel}]", e);
                        }
                    }

                    // Strip leftover csproj item nodes referencing .sdsl.cs / .sdfx.cs paths
                    foreach (var item in project.Xml.ItemGroups
                        .SelectMany(g => g.Items)
                        .Where(x =>
                        {
                            var path = x.Include ?? x.Update ?? string.Empty;
                            return path.EndsWith(".sdsl.cs", StringComparison.OrdinalIgnoreCase)
                                || path.EndsWith(".sdfx.cs", StringComparison.OrdinalIgnoreCase);
                        })
                        .ToArray())
                    {
                        item.Parent.RemoveChild(item);
                        isProjectDirty = true;
                    }

                    // Strip obsolete Generator/LastGenOutput metadata from .sdsl/.sdfx items.
                    // Walk the project XML directly — project.Items returns evaluated items
                    // (including ones from imported SDK props), which can't be mutated.
                    foreach (var item in project.Xml.ItemGroups
                        .SelectMany(g => g.Items)
                        .Where(x =>
                        {
                            var path = x.Include ?? x.Update ?? string.Empty;
                            return path.EndsWith(".sdsl", StringComparison.OrdinalIgnoreCase)
                                || path.EndsWith(".sdfx", StringComparison.OrdinalIgnoreCase);
                        })
                        .ToArray())
                    {
                        foreach (var metadata in item.Metadata.ToArray())
                        {
                            if (metadata.Name == "Generator" || metadata.Name == "LastGenOutput")
                            {
                                item.RemoveChild(metadata);
                                isProjectDirty = true;
                            }
                        }
                    }

                    if (renamedCount > 0)
                        log.Info($"Renamed {renamedCount} legacy generated shader file(s) to .bak. The Roslyn source generator now produces these into obj/. Delete the .bak files when you've verified the upgrade.");
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

            return true;
        }

        [CanBeNull]
        private static ProjectProperty GetTargetFramework(Project project)
        {
            var tfm = project.GetProperty("TargetFramework");
            if (tfm is null || tfm.IsGlobalProperty)
                tfm = project.GetProperty("TargetFrameworks");

            return tfm.IsGlobalProperty ? null : tfm;
        }
    }
}
