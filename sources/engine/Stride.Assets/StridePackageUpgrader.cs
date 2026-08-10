// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
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
using Microsoft.Build.Evaluation;
using Stride.Core.Annotations;
using static Stride.Assets.CodeUpgrades;

namespace Stride.Assets
{
    [PackageUpgrader(new[] { "Stride.Foundation", "Stride.Core", "Stride.Engine" }, "4.0.0.0", CurrentVersion)]
    public partial class StridePackageUpgrader : CodePackageUpgrader
    {
        // Should match Stride.nupkg
        public const string CurrentVersion = StrideVersion.NuGetVersion;

        public static readonly string DefaultGraphicsCompositorLevel9Url = "/Stride.Engine/DefaultGraphicsCompositorLevel9";
        public static readonly string DefaultGraphicsCompositorLevel10Url = "/Stride.Engine/DefaultGraphicsCompositorLevel10";

        public static readonly Guid DefaultGraphicsCompositorLevel9CameraSlot = new Guid("bbfef2cb-8c63-4cab-9caf-6ae48f44a8ba");
        public static readonly Guid DefaultGraphicsCompositorLevel10CameraSlot = new Guid("d0a6bf72-b3cd-4bd4-94ca-69952999d537");

        public static readonly Guid UpdatePlatformsTemplateId = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

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
        /// Declares the Roslyn-driven source-code migrations. Names are literal strings frozen at
        /// authoring time (never <c>nameof</c>) so a later engine refactor can't retarget an old gate.
        /// The runner resolves these symbols against the from-version closure and rewrites every
        /// reference; the per-version gate decides which run.
        /// </summary>
        public override void DeclareUpgrades(UpgradeRegistry registry)
        {
            // 4.4: PixelFormat members IsSRgb/IsHDR/Is32bppWithAlpha/SizeInBytes changed from extension
            // methods to (extension) properties (fmt.IsSRgb() -> fmt.IsSRgb).
            registry.Code("4.4.0.0",
            [
                Rewrite(
                    MethodToProperty("Stride.Graphics.PixelFormatExtensions", "IsSRgb"),
                    MethodToProperty("Stride.Graphics.PixelFormatExtensions", "IsHDR"),
                    MethodToProperty("Stride.Graphics.PixelFormatExtensions", "Is32bppWithAlpha"),
                    MethodToProperty("Stride.Graphics.PixelFormatExtensions", "SizeInBytes")),

                // 4.4: IndexBufferBinding constructor parameter `count` was renamed `indexCount`,
                // breaking named arguments at call sites (#3249).
                Rewrite(
                    ParameterRename("Stride.Graphics.IndexBufferBinding", ".ctor", "count", "indexCount")),

                // 4.4: SharpDX and SharpFont were dropped, Vortice no longer flows to game projects,
                // and Stride.Core.Shaders was retired by the sdsl rewrite; their leftover (unused)
                // using directives would no longer compile (#3249).
                RemoveUnusedUsings("SharpDX", "SharpFont", "Vortice", "Stride.Core.Shaders"),
            ]);

            // Structural csproj migrations, gated by the version each change landed at. Run against the
            // NEW-version project (after the reference bump) by UpgradeBeforeAssembliesLoaded.
            registry.Project("4.1.0.0", UpgradeProjectTo41);
            registry.Project("4.2.0.0", UpgradeProjectTo42);
            registry.Project("4.3.0.0", UpgradeProjectTo43);
            registry.Project("4.4.0.0", UpgradeProjectTo44);
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

        // Bump every Stride.* PackageReference to the current version (also used standalone by the
        // fresh-template path). The base then runs the gated r.Project structural migrations.
        protected override void UpgradeProjectReferences(UFile projectFullPath, ILogger log)
        {
            UpgradeProjectVersions(projectFullPath.ToOSPath(), log);
        }

        [CanBeNull]
        private static ProjectProperty GetTargetFramework(Project project)
        {
            var tfm = project.GetProperty("TargetFramework");
            if (tfm is null || tfm.IsGlobalProperty)
                tfm = project.GetProperty("TargetFrameworks");

            return tfm.IsGlobalProperty ? null : tfm;
        }

        // 4.1: drop the old Stride ref from Linux/macOS executables (no longer needed for runtime.json),
        // and bump net4.x/net5/netstandard2.0 to net6.0.
        private static void UpgradeProjectTo41(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var solutionProject = context.SolutionProject;

            // Remove Stride reference for older executable projects (it was necessary in the past due to runtime.json)
            if (solutionProject.Type == ProjectType.Executable
                && (solutionProject.Platform == PlatformType.macOS || solutionProject.Platform == PlatformType.Linux))
            {
                var strideReference = project.GetItems("PackageReference").FirstOrDefault(x => x.EvaluatedInclude == "Stride");
                if (strideReference != null)
                {
                    project.RemoveItem(strideReference);
                    context.IsDirty = true;
                }
            }

            var tfm = project.GetProperty("TargetFramework");
            if (tfm != null)
            {
                // Library
                if (tfm.EvaluatedValue == "netstandard2.0"
                    || (tfm.EvaluatedValue.StartsWith("net4", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library))
                {
                    tfm.UnevaluatedValue = "net6.0";
                    context.IsDirty = true;
                    project.ReevaluateIfNecessary();
                }
                // Executable
                else if ((tfm.EvaluatedValue.StartsWith("net4", StringComparison.Ordinal) || tfm.EvaluatedValue.StartsWith("net5", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                {
                    tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net6.0-windows" : "net6.0";
                    context.IsDirty = true;
                    project.ReevaluateIfNecessary();
                }
            }
        }

        // 4.2: bump net6 to net8.
        private static void UpgradeProjectTo42(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var solutionProject = context.SolutionProject;
            if (GetTargetFramework(project) is { } tfm)
            {
                // Library
                if (tfm.EvaluatedValue.StartsWith("net6", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library)
                {
                    tfm.UnevaluatedValue = "net8.0";
                    context.IsDirty = true;
                    project.ReevaluateIfNecessary();
                }
                // Executable
                else if ((tfm.EvaluatedValue.StartsWith("net6", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                {
                    tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net8.0-windows" : "net8.0";
                    context.IsDirty = true;
                    project.ReevaluateIfNecessary();
                }
            }
        }

        // 4.3: bump net8 to net10.
        private static void UpgradeProjectTo43(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var solutionProject = context.SolutionProject;
            if (GetTargetFramework(project) is { } tfm)
            {
                // Library
                if (tfm.EvaluatedValue.StartsWith("net8", StringComparison.Ordinal) && solutionProject.Type == ProjectType.Library)
                {
                    tfm.UnevaluatedValue = "net10.0";
                    context.IsDirty = true;
                }
                // Executable
                else if ((tfm.EvaluatedValue.StartsWith("net8", StringComparison.Ordinal)) && solutionProject.Type == ProjectType.Executable)
                {
                    tfm.UnevaluatedValue = solutionProject.Platform == PlatformType.Windows ? "net10.0-windows" : "net10.0";
                    context.IsDirty = true;
                }
            }
        }

        // 4.4: rename the asset-compiler PackageReference (Stride.Core.Assets.CompilerApp -> Stride.AssetCompiler)
        // and retire the on-disk .sdsl.cs/.sdfx.cs siblings now produced by the Roslyn source generator into obj/.
        private static void UpgradeProjectTo44(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var log = context.Log;

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

                context.IsDirty = true;
            }

            // .sdsl/.sdfx generated C# now comes from a Roslyn source generator into obj/, so rename any
            // on-disk siblings to .bak (recoverable, inert in build) and strip leftover csproj items /
            // Generator metadata.
            var projectDir = context.ProjectFullPath.GetFullDirectory().ToOSPath();
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
                context.IsDirty = true;
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
                        context.IsDirty = true;
                    }
                }
            }

            if (renamedCount > 0)
                log.Info($"Renamed {renamedCount} legacy generated shader file(s) to .bak. The Roslyn source generator now produces these into obj/. Delete the .bak files when you've verified the upgrade.");
        }
    }
}
