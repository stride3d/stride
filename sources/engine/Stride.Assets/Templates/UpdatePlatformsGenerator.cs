// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Edge.Template;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Assets.Templates;

/// <summary>
/// Adds, regenerates, or removes per-platform exec projects via the <c>stride-platforms</c>
/// dotnet new template. Mirrors the AddLibrary pattern: bootstrapper does the file copy +
/// substitution, this class drives the prompt, computes the platform diff, and registers the
/// produced csprojs as Executable projects on the session.
/// </summary>
public sealed class UpdatePlatformsGenerator : TemplateGeneratorBase<PackageTemplateGeneratorParameters>
{
    public const string PlatformsTemplateShortName = "stride-platforms";

    private static readonly PropertyKey<IReadOnlyList<PlatformType>> SelectedPlatformsKey = new("SelectedPlatforms", typeof(UpdatePlatformsGenerator));
    private static readonly PropertyKey<bool> ForceRegenerationKey = new("ForceRegeneration", typeof(UpdatePlatformsGenerator));

    private readonly IUpdatePlatformsParameterPrompt? prompt;

    public UpdatePlatformsGenerator() : this(null) { }

    public UpdatePlatformsGenerator(IUpdatePlatformsParameterPrompt? prompt)
    {
        this.prompt = prompt;
    }

    /// <summary>Pre-set selected platforms for an unattended run.</summary>
    public static void SetPlatforms(PackageTemplateGeneratorParameters parameters, IReadOnlyList<PlatformType> platforms, bool forceRegeneration = false)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.SetTag(SelectedPlatformsKey, platforms);
        parameters.SetTag(ForceRegenerationKey, forceRegeneration);
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription is TemplateDotNetNewDescription dnn
            && string.Equals(dnn.TemplateShortName, PlatformsTemplateShortName, StringComparison.Ordinal);
    }

    public override async Task<bool> PrepareForRun(PackageTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        // When invoked on a platform-specific exec project, redirect to the shared-profile game
        // library it belongs to (matched by stripping the platform suffix from the package name).
        if (!ResolveGameLibrary(parameters))
            return false;

        if (parameters.Unattended)
            return parameters.HasTag(SelectedPlatformsKey);

        if (prompt == null)
            return false;

        var installed = new HashSet<PlatformType>(GetInstalledPlatforms(parameters));
        var result = await prompt.PromptAsync(installed).ConfigureAwait(true);
        if (result == null)
            return false;

        parameters.SetTag(SelectedPlatformsKey, result.SelectedPlatforms);
        parameters.SetTag(ForceRegenerationKey, result.ForceRegeneration);
        return true;
    }

    public override bool Run(PackageTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        var log = parameters.Logger;
        var package = parameters.Package;
        var sessionDir = package.RootDirectory.GetParent();
        var baseName = StripGameSuffix(parameters.Name);

        var selected = new HashSet<PlatformType>(parameters.GetTag(SelectedPlatformsKey));
        var forceRegen = parameters.GetTag(ForceRegenerationKey);
        var installed = new HashSet<PlatformType>(GetInstalledPlatforms(parameters));

        var toRemove = installed.Where(p => !selected.Contains(p)).ToList();
        var toAdd = selected.Where(p => !installed.Contains(p)).ToList();
        var toRegenerate = forceRegen
            ? selected.Where(p => installed.Contains(p)).ToList()
            : new List<PlatformType>();

        foreach (var type in toRemove)
            RemovePlatformProject(parameters, type, baseName, sessionDir);

        foreach (var type in toRegenerate)
        {
            RemovePlatformProject(parameters, type, baseName, sessionDir);
        }

        var toInstantiate = toAdd.Concat(toRegenerate).ToList();
        if (toInstantiate.Count > 0)
        {
            if (!InstantiatePlatforms(parameters, toInstantiate, baseName, sessionDir))
                return false;
        }

        package.Session.LoadMissingReferences(log);
        package.IsDirty = true;
        return true;
    }

    /// <summary>
    /// If the right-clicked package is a platform exec, swap <paramref name="parameters"/> to
    /// point at the corresponding shared-profile game library. Returns false on unrecoverable
    /// mismatch (no game library found).
    /// </summary>
    private static bool ResolveGameLibrary(PackageTemplateGeneratorParameters parameters)
    {
        if (parameters.Package.Container is SolutionProject lib
            && lib.Platform == PlatformType.Shared
            && lib.Type == ProjectType.Library)
        {
            return true;
        }

        var name = parameters.Name;
        foreach (var platform in AssetRegistry.SupportedPlatforms)
        {
            var suffix = "." + platform.Name;
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
                break;
            }
        }

        var gameLib = parameters.Package.Session.LocalPackages.FirstOrDefault(p =>
            p.Container is SolutionProject sp
            && sp.Type == ProjectType.Library
            && sp.Platform == PlatformType.Shared
            && string.Equals(p.Meta.Name, name, StringComparison.OrdinalIgnoreCase));

        if (gameLib == null)
        {
            parameters.Logger.Error($"Could not find a game library matching '{name}' in the current session.");
            return false;
        }

        parameters.Package = gameLib;
        parameters.Name = gameLib.Meta.Name;
        parameters.OutputDirectory = gameLib.FullPath.GetFullDirectory();
        return true;
    }

    /// <summary>Currently-installed platforms = exec projects on the session whose Type/Platform match.</summary>
    private static IEnumerable<PlatformType> GetInstalledPlatforms(PackageTemplateGeneratorParameters parameters)
    {
        return parameters.Package.Session.Projects
            .OfType<SolutionProject>()
            .Where(p => p.Type == ProjectType.Executable && p.Platform != PlatformType.Shared)
            .Select(p => p.Platform);
    }

    private static string StripGameSuffix(string name)
    {
        return name.EndsWith(".Game", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - ".Game".Length)
            : name;
    }

    private static void RemovePlatformProject(PackageTemplateGeneratorParameters parameters, PlatformType type, string baseName, UDirectory sessionDir)
    {
        var existing = parameters.Package.Session.Projects
            .OfType<SolutionProject>()
            .FirstOrDefault(p => p.Type == ProjectType.Executable && p.Platform == type);
        if (existing != null)
            parameters.Package.Session.Projects.Remove(existing);

        var dirName = $"{baseName}.{type}";
        var projectDir = Path.Combine(sessionDir.ToOSPath(), dirName);
        if (Directory.Exists(projectDir))
        {
            try { Directory.Delete(projectDir, recursive: true); }
            catch (Exception ex) { parameters.Logger.Warning($"Could not delete {projectDir}: {ex.Message}"); }
        }
    }

    private static bool InstantiatePlatforms(PackageTemplateGeneratorParameters parameters, List<PlatformType> platforms, string baseName, UDirectory sessionDir)
    {
        var log = parameters.Logger;
        if (DotNetNewTemplateBridge.Registry == null)
        {
            log.Error("DotNetNewTemplateBridge is not initialized; cannot dispatch template.");
            return false;
        }

        var description = (TemplateDotNetNewDescription)parameters.Description;
        var template = DotNetNewTemplateBridge.Registry.GetTemplatesAsync()
            .GetAwaiter().GetResult()
            .FirstOrDefault(t => string.Equals(t.Identity, description.TemplateIdentity, StringComparison.Ordinal));
        if (template == null)
        {
            log.Error($"Template '{description.TemplateIdentity}' could not be resolved from the bootstrapper.");
            return false;
        }

        var platformsValue = string.Join("|", platforms.Select(PlatformChoiceName));
        ITemplateCreationResult creation;
        try
        {
            creation = DotNetNewTemplateBridge.Registry
                .InstantiateAsync(template, baseName, sessionDir.ToOSPath(),
                    new Dictionary<string, string> { ["platforms"] = platformsValue })
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            log.Error($"Template instantiation failed: {ex.Message}", ex);
            return false;
        }

        if (creation.Status != CreationResultStatus.Success)
        {
            log.Error($"Template instantiation failed: {creation.Status} — {creation.ErrorMessage}");
            return false;
        }

        foreach (var type in platforms)
        {
            var dirName = $"{baseName}.{type}";
            var csprojPath = Path.Combine(sessionDir.ToOSPath(), dirName, $"{dirName}.csproj");
            if (!File.Exists(csprojPath))
            {
                log.Warning($"Expected csproj not found at {csprojPath}");
                continue;
            }
            var project = (SolutionProject)Package.LoadProject(log, csprojPath);
            project.Type = ProjectType.Executable;
            project.Platform = type;
            parameters.Package.Session.Projects.Add(project);
            log.Info($"Registered {dirName}");
        }
        return true;
    }

    /// <summary>Maps <see cref="PlatformType"/> to the dotnet new <c>platforms</c> choice string (lowercased <see cref="SolutionPlatformPart.Name"/>).</summary>
    private static string PlatformChoiceName(PlatformType type)
    {
        var platform = AssetRegistry.SupportedPlatforms.FirstOrDefault(p => p.Type == type)
            ?? throw new NotSupportedException($"Platform {type} is not in AssetRegistry.SupportedPlatforms.");
        return platform.Name.ToLowerInvariant();
    }
}
