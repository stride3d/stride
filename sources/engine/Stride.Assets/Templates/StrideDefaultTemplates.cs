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
/// files in Stride.Assets.Presentation + Stride.SpriteStudio.Offline, plus the dotnet new project
/// templates from Stride.Templates.*). Non-WPF entry point — callable from headless test runners
/// without pulling Stride.Assets.Presentation (and its WPF dependency tree) into the consumer.
/// </summary>
public static class StrideDefaultTemplates
{
    /// <summary>
    /// Loads default templates. Pass <paramref name="loadAssemblyReferences"/>=false in headless
    /// test runners so the .sdpkg's referenced assemblies (Stride.Particles etc.) don't get loaded
    /// from .nuget cache — a later sample regen loads them from the sample's bin/ via a different
    /// path, producing duplicate Assembly objects and a DataContract alias conflict at runtime.
    /// </summary>
    public static void Load(bool loadAssemblyReferences = true)
    {
        var loadParams = new PackageLoadParameters { LoadAssemblyReferences = loadAssemblyReferences };
        foreach (var packageInfo in new[] { new { Name = "Stride.Assets.Presentation", Version = StrideVersion.NuGetVersion }, new { Name = "Stride.SpriteStudio.Offline", Version = StrideVersion.NuGetVersion } })
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

        DotNetNewTemplateBridge.RegisterProjectTemplates();
    }
}
