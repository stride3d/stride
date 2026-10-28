// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Templates;

/// <summary>
/// Bootstraps the editor-internal template registry (asset / script / project-modification .sdtpl
/// files in Stride.Assets.Templates, plus the dotnet new project templates from Stride.Templates.*;
/// a package's own templates come with its Assets companion). Non-WPF entry point — callable from
/// headless test runners without pulling the editor assemblies (and the WPF dependency tree)
/// into the consumer.
/// </summary>
public static class StrideDefaultTemplates
{
    /// <summary>
    /// The package whose <c>.sdpkg</c> lists the asset template folders, shared by the WPF and cross-platform editors.
    /// </summary>
    public const string AssetTemplatesPackageName = "Stride.Assets.Templates";

    /// <summary>
    /// Loads default templates. Pass <paramref name="loadAssemblyReferences"/>=false in headless
    /// test runners so the .sdpkg's referenced assemblies (Stride.Particles etc.) don't get loaded
    /// from .nuget cache — a later sample regen loads them from the sample's bin/ via a different
    /// path, producing duplicate Assembly objects and a DataContract alias conflict at runtime.
    /// </summary>
    public static void Load(bool loadAssemblyReferences = true)
    {
        LoadAssetTemplates(loadAssemblyReferences);
        DotNetNewTemplateBridge.RegisterProjectTemplates();
    }

    /// <summary>
    /// Registers the asset, script and project-modification templates (the <c>.sdtpl</c> files listed by the
    /// <see cref="AssetTemplatesPackageName"/> package) with the <see cref="TemplateManager"/>, without the dotnet new project
    /// templates: what an editor that registers the project templates itself needs.
    /// </summary>
    /// <param name="loadAssemblyReferences">Whether the assemblies referenced by the package are loaded; see <see cref="Load"/>.</param>
    /// <exception cref="InvalidOperationException">The package is not in the package store, or it could not be loaded.</exception>
    public static void LoadAssetTemplates(bool loadAssemblyReferences = true)
    {
        // Only TemplateFolders are needed here (loaded regardless of this flag); skip the package's
        // assets — they're unused for template registration and slow to load.
        var loadParams = new PackageLoadParameters { LoadAssemblyReferences = loadAssemblyReferences, AutoLoadTemporaryAssets = false };
        foreach (var packageInfo in new[] { new { Name = AssetTemplatesPackageName, Version = StrideVersion.NuGetVersion } })
        {
            var logger = new LoggerResult();
            var packageFile = PackageStore.Instance.GetPackageFileName(packageInfo.Name, new PackageVersionRange(new PackageVersion(packageInfo.Version)));
            if (packageFile is null)
                throw new InvalidOperationException($"Could not find package {packageInfo.Name} {packageInfo.Version}. Ensure packages have been resolved.");
            var package = Package.Load(logger, packageFile.ToOSPath(), loadParams);
            if (logger.HasErrors)
                throw new InvalidOperationException($"Could not load package {packageInfo.Name}:{Environment.NewLine}{logger.ToText()}");

            TemplateManager.RegisterPackage(package);
        }
    }
}
