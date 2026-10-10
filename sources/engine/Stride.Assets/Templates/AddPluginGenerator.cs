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
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets.Templates;

/// <summary>
/// Adds the stride-plugin template's runtime, Assets and Editor projects to the session.
/// The game library that owns the game settings references the runtime project.
/// </summary>
public sealed class AddPluginGenerator : SessionTemplateGenerator
{
    /// <summary>Short name of the dotnet new template this generator dispatches.</summary>
    public const string PluginTemplateShortName = "stride-plugin";

    private readonly IAddPluginParameterPrompt? prompt;
    private readonly PackageLoadParameters? loadParameters;
    private string? referencingProject;

    public AddPluginGenerator() : this(null) { }

    /// <param name="loadParameters">How the new projects' references load; the defaults when null.</param>
    public AddPluginGenerator(IAddPluginParameterPrompt? prompt, PackageLoadParameters? loadParameters = null)
    {
        this.prompt = prompt;
        this.loadParameters = loadParameters;
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription is TemplateDotNetNewDescription dnn
            && string.Equals(dnn.TemplateShortName, PluginTemplateShortName, StringComparison.Ordinal);
    }

    public override async Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        var candidates = FindReferencingCandidates(parameters.Session);
        referencingProject = candidates.Count > 0 ? candidates[0].FullPath.ToOSPath() : null;
        if (parameters.Unattended || prompt == null)
            return true;

        // A new session (the startup dialog): the plugin stands alone and takes the solution's name
        if (!parameters.Session.Projects.OfType<SolutionProject>().Any())
        {
            parameters.Name = Utilities.BuildValidProjectName(parameters.Name);
            parameters.Namespace = Utilities.BuildValidNamespaceName(parameters.Name);
            return true;
        }

        var existingNames = parameters.Session.Projects
            .OfType<SolutionProject>()
            .Where(p => p.FullPath != null)
            .Select(p => p.FullPath.GetFileNameWithoutExtension())
            .ToList();
        // The companions take the plugin's name with a suffix, so those names must be free too
        bool IsNameTaken(string name) => existingNames.Any(n =>
            string.Equals(n, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(n, name + ".Assets", StringComparison.OrdinalIgnoreCase)
            || string.Equals(n, name + ".Editor", StringComparison.OrdinalIgnoreCase));
        var defaultName = NamingHelper.ComputeNewName(parameters.Name, (UFile uf) => IsNameTaken(uf), "{0}{1}");

        var choices = candidates.Count > 1 ? candidates.Select(p => p.Name).ToList() : [];
        var result = await prompt.PromptAsync(defaultName, IsNameTaken, choices).ConfigureAwait(true);
        if (result == null)
            return false;

        parameters.Name = Utilities.BuildValidProjectName(result.PluginName);
        parameters.Namespace = Utilities.BuildValidNamespaceName(parameters.Name);
        if (result.ReferencingProject is not null)
            referencingProject = candidates.FirstOrDefault(p => p.Name == result.ReferencingProject)?.FullPath.ToOSPath() ?? referencingProject;
        return !IsNameTaken(parameters.Name);
    }

    public override bool Generate(SessionTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();
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

        // The session root; the template creates {Name}, {Name}.Assets and {Name}.Editor folders in it
        var outputDir = parameters.OutputDirectory;
        Directory.CreateDirectory(outputDir);

        // Skip the template's solution file: the session has one
        ITemplateCreationResult creation;
        try
        {
            creation = DotNetNewTemplateBridge.Registry
                .InstantiateAsync(template, parameters.Name, outputDir, new Dictionary<string, string> { ["skipSolution"] = "true" })
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

        // The runtime first: the companions reference it
        var projects = new List<SolutionProject>();
        foreach (var suffix in new[] { "", ".Assets", ".Editor" })
        {
            var projectName = parameters.Name + suffix;
            var csprojPath = Path.Combine(outputDir, projectName, projectName + ".csproj");
            if (!File.Exists(csprojPath))
            {
                log.Error($"Instantiated plugin template contains no {projectName}.csproj at {Path.GetDirectoryName(csprojPath)}; cannot add to session.");
                return false;
            }
            var project = (SolutionProject)Package.LoadProject(log, csprojPath);
            project.Type = ProjectType.Library;
            project.Platform = PlatformType.Shared;
            projects.Add(project);
        }
        foreach (var project in projects)
            parameters.Session.Projects.Add(project);

        // The game references the runtime project only; the ProjectReference is written by the dependency change
        var runtime = projects[0];
        var referencing = referencingProject is not null
            ? parameters.Session.Projects.OfType<SolutionProject>().FirstOrDefault(p => string.Equals(p.FullPath?.ToOSPath(), referencingProject, StringComparison.OrdinalIgnoreCase))
            : null;
        if (referencing is not null)
        {
            referencing.DirectDependencies.Add(new DependencyRange(runtime.Name, null, DependencyType.Project) { MSBuildProject = runtime.FullPath.ToOSPath() });
            log.Info($"[{referencing.Name}] now references [{runtime.Name}].");
        }
        else
        {
            log.Info($"No game project owns a GameSettings asset: reference [{runtime.Name}] from your game project by hand.");
            foreach (var project in parameters.Session.Projects.OfType<SolutionProject>())
                log.Verbose($"  [{project.Name}]: {project.Type}, {project.Package.Assets.Count} assets, namespace [{project.AssetNamespace}]");
        }

        parameters.Session.LoadMissingReferences(log, loadParameters);
        return true;
    }

    /// <summary>
    /// The library projects whose own package holds the GameSettings asset, those of the current project's game first.
    /// </summary>
    private static List<SolutionProject> FindReferencingCandidates(PackageSession session)
    {
        var candidates = session.Projects
            .OfType<SolutionProject>()
            .Where(p => p.FullPath != null && p.Type == ProjectType.Library && OwnsGameSettings(p))
            .ToList();
        if (session.CurrentProject is { } current)
        {
            var currentGame = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current.Name };
            foreach (var dependency in current.FlattenedDependencies)
                currentGame.Add(dependency.Name);
            foreach (var dependency in current.DirectDependencies)
                currentGame.Add(dependency.Name);
            // A stable sort: the session's order within each group
            candidates = candidates.OrderByDescending(p => currentGame.Contains(p.Name)).ToList();
        }
        return candidates;
    }

    private static bool OwnsGameSettings(SolutionProject project)
    {
        return project.Package.Assets.Find(project.Qualify(new UFile(GameSettingsAsset.GameSettingsLocation)))?.Asset is GameSettingsAsset;
    }
}
