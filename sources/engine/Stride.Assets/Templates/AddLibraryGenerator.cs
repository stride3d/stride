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
/// Adds a code library to the current session by instantiating the <c>stride-library</c>
/// dotnet new template through the shared <see cref="DotNetNewTemplateBridge.Registry"/>.
/// The produced .csproj is registered as a <see cref="ProjectType.Library"/> project on the session.
/// </summary>
public sealed class AddLibraryGenerator : SessionTemplateGenerator
{
    /// <summary>Short name of the dotnet new template this generator dispatches.</summary>
    public const string LibraryTemplateShortName = "stride-library";

    private readonly IAddLibraryParameterPrompt? prompt;

    public AddLibraryGenerator() : this(null) { }

    public AddLibraryGenerator(IAddLibraryParameterPrompt? prompt)
    {
        this.prompt = prompt;
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription is TemplateDotNetNewDescription dnn
            && string.Equals(dnn.TemplateShortName, LibraryTemplateShortName, StringComparison.Ordinal);
    }

    public override async Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if (parameters.Unattended || prompt == null)
            return true;

        var existingNames = parameters.Session.Projects
            .OfType<SolutionProject>()
            .Where(p => p.FullPath != null)
            .Select(p => p.FullPath.GetFileNameWithoutExtension())
            .ToList();
        bool IsNameTaken(string name) => existingNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));

        var defaultName = NamingHelper.ComputeNewName(parameters.Name, (UFile uf) => IsNameTaken(uf), "{0}{1}");
        var result = await prompt.PromptAsync(defaultName, IsNameTaken).ConfigureAwait(true);
        if (result == null)
            return false;

        parameters.Name = Utilities.BuildValidProjectName(result.LibraryName);
        parameters.Namespace = Utilities.BuildValidNamespaceName(result.Namespace);
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

        // OutputDirectory is the session root; dotnet new sourceName substitution renames the
        // template's MyTemplate/ dir to {Name}/ inside it.
        var outputDir = parameters.OutputDirectory;
        Directory.CreateDirectory(outputDir);

        ITemplateCreationResult creation;
        try
        {
            creation = DotNetNewTemplateBridge.Registry
                .InstantiateAsync(template, parameters.Name, outputDir, new Dictionary<string, string>())
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

        // The bootstrapper renames the template's MyTemplate/ dir to {parameters.Name}/ via
        // sourceName; the new csproj lives inside it. Scope the lookup to that subdir so we don't
        // pick up pre-existing csprojs elsewhere under the session's solution directory.
        var libraryDir = Path.Combine(outputDir, parameters.Name);
        var csprojPath = Directory.Exists(libraryDir)
            ? Directory.EnumerateFiles(libraryDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault()
            : null;
        if (csprojPath == null)
        {
            log.Error($"Instantiated library template contains no .csproj at {libraryDir}; cannot add to session.");
            return false;
        }

        var project = (SolutionProject)Package.LoadProject(log, csprojPath);
        project.Type = ProjectType.Library;
        project.Platform = PlatformType.Shared;
        parameters.Session.Projects.Add(project);
        parameters.Session.LoadMissingReferences(log);
        return true;
    }
}
