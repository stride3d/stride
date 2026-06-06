// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Templates;

/// <summary>
/// <see cref="SessionTemplateGenerator"/> that instantiates a
/// <see cref="TemplateDotNetNewDescription"/> through <see cref="DotNetNewTemplateRegistry"/> —
/// the same dotnet new flow CLI users get.
/// </summary>
/// <remarks>
/// Generation runs as three phases: <see cref="InstantiateFiles"/> →
/// <see cref="UpgradeGeneratedProjects"/> → <see cref="IntegrateIntoSession"/>. The editor-facing
/// <see cref="Generate"/> runs all three; the headless <see cref="GenerateFilesOnly"/> stops after
/// phase 2, so test runners and CLI tools get the file output without an
/// <see cref="Stride.Core.Reflection.AssemblyContainer"/> in the loop.
/// </remarks>
public class DotNetNewTemplateGenerator : SessionTemplateGenerator
{
    /// <summary>User-supplied parameter values from the parameter dialog (or unattended setup).</summary>
    private static readonly PropertyKey<IReadOnlyDictionary<string, string>> ParameterValuesKey
        = new("ParameterValues", typeof(DotNetNewTemplateGenerator));

    private readonly IDotNetNewParameterPrompt? prompt;

    /// <summary>Headless ctor (no UI). PrepareForRun returns true without collecting parameter values; callers must <see cref="SetParameters"/> on parameters.Unattended runs.</summary>
    public DotNetNewTemplateGenerator() : this(null) { }

    /// <summary>Editor ctor — <paramref name="prompt"/> collects parameter values via UI in attended PrepareForRun.</summary>
    public DotNetNewTemplateGenerator(IDotNetNewParameterPrompt? prompt)
    {
        this.prompt = prompt;
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription is TemplateDotNetNewDescription;
    }

    /// <summary>
    /// Sets the parameter values for an unattended template run (CI / programmatic instantiation).
    /// When invoked, <see cref="PrepareForRun"/> bypasses the parameter dialog.
    /// </summary>
    public static void SetParameters(SessionTemplateGeneratorParameters parameters, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.SetTag(ParameterValuesKey, values);
    }

    public override async Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        // Skip the dialog when running unattended (e.g. CI / programmatic instantiation); the
        // caller is expected to have stuffed ParameterValues directly via SetParameters.
        if (parameters.Unattended || prompt == null)
            return true;

        var description = (TemplateDotNetNewDescription)parameters.Description;
        var template = await ResolveTemplate(description).ConfigureAwait(true);
        if (template == null)
        {
            parameters.Logger.Error($"Could not find dotnet new template '{description.TemplateIdentity}'. Is the matching Stride templates package (Stride.Templates.Games / Stride.Templates.Games.Starters / Stride.Templates.Samples) installed?");
            return false;
        }

        var values = await prompt.PromptAsync(template).ConfigureAwait(true);
        if (values == null)
            return false;

        parameters.SetTag(ParameterValuesKey, values);
        return true;
    }

    public override bool Generate(SessionTemplateGeneratorParameters parameters)
    {
        var sdpkg = InstantiateFiles(parameters);
        if (sdpkg == null)
            return false;
        if (!UpgradeGeneratedProjects(parameters))
            return false;
        return IntegrateIntoSession(sdpkg, parameters);
    }

    /// <summary>
    /// Headless entry point: runs <see cref="InstantiateFiles"/> + <see cref="UpgradeGeneratedProjects"/>
    /// without touching the session graph or the <see cref="Stride.Core.Reflection.AssemblyContainer"/>.
    /// For test runners and CLI tools that only need the files on disk.
    /// </summary>
    public bool GenerateFilesOnly(SessionTemplateGeneratorParameters parameters)
    {
        return InstantiateFiles(parameters) != null && UpgradeGeneratedProjects(parameters);
    }

    /// <summary>
    /// Phase 1 — invokes the dotnet new bootstrapper. Writes the project tree under
    /// <see cref="TemplateGeneratorParameters.OutputDirectory"/> and returns the generated .sdpkg path.
    /// </summary>
    protected virtual string? InstantiateFiles(SessionTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        var description = (TemplateDotNetNewDescription)parameters.Description;
        var log = parameters.Logger;

        if (DotNetNewTemplateBridge.Registry == null)
        {
            log.Error("DotNetNewTemplateBridge is not initialized; cannot dispatch template.");
            return null;
        }

        var template = ResolveTemplate(description).GetAwaiter().GetResult();
        if (template == null)
        {
            log.Error($"Template '{description.TemplateIdentity}' could not be resolved from the bootstrapper.");
            return null;
        }

        var values = parameters.TryGetTag(ParameterValuesKey) ?? new Dictionary<string, string>();

        // The dotnet new sourceName substitution is fed by the project name; this is the same
        // value the user typed in GameStudio's New-Project name field.
        var outputDir = parameters.OutputDirectory;
        Directory.CreateDirectory(outputDir);

        ITemplateCreationResult creation;
        try
        {
            creation = DotNetNewTemplateBridge.Registry.InstantiateAsync(template, parameters.Name, outputDir, values)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            log.Error($"Template instantiation failed: {ex.Message}", ex);
            return null;
        }

        if (creation.Status != CreationResultStatus.Success)
        {
            log.Error($"Template instantiation failed: {creation.Status} — {creation.ErrorMessage}");
            return null;
        }

        // Convention: the .sdpkg sits next to its owning .csproj (the game library); per-platform
        // exec csprojs live alongside in sibling directories.
        var sdpkg = Directory
            .EnumerateFiles(outputDir, "*.sdpkg", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (sdpkg == null)
            log.Error("Instantiated template contains no .sdpkg.");
        return sdpkg;
    }

    /// <summary>
    /// Phase 2 — fresh templates ship with Stride.* <c>PackageReference</c>s pinned to the
    /// previous release (samples track stable versions, not dev1/dev2 suffixes); this rewrites
    /// them to <see cref="StridePackageUpgrader.CurrentVersion"/>. Runs without a session or
    /// AssemblyContainer — same standalone path the legacy session-load uses internally.
    /// </summary>
    protected virtual bool UpgradeGeneratedProjects(SessionTemplateGeneratorParameters parameters)
    {
        var log = parameters.Logger;
        foreach (var csproj in Directory.EnumerateFiles(parameters.OutputDirectory, "*.csproj", SearchOption.AllDirectories))
        {
            StridePackageUpgrader.UpgradeProjectVersions(csproj, log);
            // Future: also invoke StridePackageUpgrader.UpgradeProjectCode here once the
            // Roslyn-based code upgrade is implemented (fromVersion derived from a Stride.*
            // PackageReference scan — the version the template's .cs files were authored against).
        }
        return true;
    }

    /// <summary>
    /// Phase 3 — wires the generated package into <see cref="TemplateGeneratorParameters.Session"/>:
    /// <see cref="PackageSession.AddExistingProject"/>, per-platform exec registration,
    /// dependency resolution, and asset-reference fixup. Skipped by <see cref="GenerateFilesOnly"/>.
    /// </summary>
    protected virtual bool IntegrateIntoSession(string sdpkgPath, SessionTemplateGeneratorParameters parameters)
    {
        var log = parameters.Logger;

        var loadParams = PackageLoadParameters.Default();
        loadParams.GenerateNewAssetIds = false;  // GUIDs are already fresh per-instantiation via template.json's generated/guid symbols.
        var loaded = parameters.Session.AddExistingProject(sdpkgPath, log, loadParams);
        if (loaded == null)
        {
            log.Error("Failed to add generated project to session.");
            return false;
        }

        // AddExistingProject only registers the library project that owns the .sdpkg; per-platform
        // exec csprojs are siblings and aren't reachable via project references (the dep flow runs
        // exec -> library, not vice versa). Without explicit registration here, the session's
        // VSSolution drops them on save and Run/LivePlay can't find an exec target.
        RegisterPerPlatformExecutables(parameters, sdpkgPath, log);

        // Resolve dependencies for the per-platform exec projects: NuGet restore (so F5/build
        // finds obj/project.assets.json) and FlattenedDependencies graph (so editor flows that
        // walk the exec's deps find library-owned assets like GameSettings).
        parameters.Session.LoadMissingDependencies(log);

        // Remap stale asset references (incl. Archetype) via location fallback. Sample assets
        // can carry archetype refs whose Id was rotated in engine evolution (e.g. animatedmodel's
        // GraphicsCompositor archetype Id is 2E995C78... in the sample, but engine ships
        // 9af5337...). FixAssetReferences uses the existing FindAsset(id) → FindAsset(location)
        // fallback to rewrite the Id to the current engine asset, emitting a warning.
        AssetAnalysis.FixAssetReferences(loaded.Package.Assets);

        return true;
    }

    /// <summary>
    /// Per-platform exec project dir-suffix → <see cref="PlatformType"/>. Matches the
    /// preprocessor's dir naming convention: <c>&lt;Name&gt;.Windows</c>,
    /// <c>&lt;Name&gt;.Linux</c>, etc.
    /// </summary>
    private static readonly (string Suffix, PlatformType Type)[] PlatformSuffixes =
    {
        (".Windows", PlatformType.Windows),
        (".Linux",   PlatformType.Linux),
        (".macOS",   PlatformType.macOS),
        (".iOS",     PlatformType.iOS),
        (".Android", PlatformType.Android),
    };

    /// <summary>
    /// Discovers sibling per-platform exec csprojs (e.g. <c>MyGame.Windows</c> next to
    /// <c>MyGame.Game</c>) and registers each with the session so the session's VSSolution
    /// preserves them on save.
    /// </summary>
    private static void RegisterPerPlatformExecutables(SessionTemplateGeneratorParameters parameters, string sdpkg, LoggerResult log)
    {
        var sdpkgDir = Path.GetDirectoryName(sdpkg);
        var projectRoot = sdpkgDir != null ? Path.GetDirectoryName(sdpkgDir) : null;
        if (projectRoot == null || !Directory.Exists(projectRoot))
            return;
        SolutionProject? hostPlatformProject = null;
        SolutionProject? windowsProject = null;
        foreach (var platformDir in Directory.EnumerateDirectories(projectRoot))
        {
            var dirName = Path.GetFileName(platformDir);
            var match = PlatformSuffixes.FirstOrDefault(p => dirName.EndsWith(p.Suffix, StringComparison.Ordinal));
            if (match.Suffix == null)
                continue;
            var csproj = Path.Combine(platformDir, dirName + ".csproj");
            if (!File.Exists(csproj))
                continue;
            // Package.LoadProject returns a SolutionProject with an empty placeholder Package
            // (no sibling .sdpkg). Type/Platform stay at default until dep-resolution evaluates
            // the MSBuild project, but the session's RegisterProject already uses them for
            // VS-startup ordering — set them explicitly so .sln save lists per-platform projects
            // in the same order the preprocessor's GenerateSlnIfMissing produces.
            var execProject = (SolutionProject)Package.LoadProject(log, csproj);
            execProject.Type = ProjectType.Executable;
            execProject.Platform = match.Type;
            parameters.Session.Projects.Add(execProject);
            log.Info($"Registered platform exec project: {dirName} ({match.Type})");
            if (match.Type == PlatformType.Windows)
                windowsProject = execProject;
            if (hostPlatformProject == null && match.Type == CurrentHostPlatform())
                hostPlatformProject = execProject;
        }
        // Make Session.CurrentProject point at a runnable exec (host platform if among the
        // generated set, else Windows as the GameStudio-only fallback). Without this the
        // Run/LivePlay button can't dispatch (it asserts CurrentProject.Type == Executable),
        // and the .sln on save loses the startup-project hint that RegisterProject sets via
        // the Windows-first VSSolution insertion.
        var startupProject = hostPlatformProject ?? windowsProject;
        if (startupProject != null)
            parameters.Session.CurrentProject = startupProject;
    }

    /// <summary>
    /// Maps the OS GameStudio is running on to the matching <see cref="PlatformType"/>. Used
    /// to pick which per-platform exec to surface as Session.CurrentProject after instantiation.
    /// Returns <see cref="PlatformType.Windows"/> as a last-resort fallback (GameStudio is
    /// Windows-only today, so callers can treat that as "the host is Windows").
    /// </summary>
    private static PlatformType CurrentHostPlatform()
    {
        if (OperatingSystem.IsWindows()) return PlatformType.Windows;
        if (OperatingSystem.IsLinux())   return PlatformType.Linux;
        if (OperatingSystem.IsMacOS())   return PlatformType.macOS;
        return PlatformType.Windows;
    }

    /// <summary>
    /// Resolves a <see cref="TemplateDotNetNewDescription"/> back to its <see cref="ITemplateInfo"/>
    /// via the registry. The bridge populates one description per installed template; the bootstrapper
    /// is the source of truth for the live <see cref="ITemplateInfo"/>.
    /// </summary>
    private static async Task<ITemplateInfo?> ResolveTemplate(TemplateDotNetNewDescription description)
    {
        if (DotNetNewTemplateBridge.Registry == null)
            return null;
        var templates = await DotNetNewTemplateBridge.Registry.GetTemplatesAsync().ConfigureAwait(true);
        return templates.FirstOrDefault(t => string.Equals(t.Identity, description.TemplateIdentity, StringComparison.Ordinal));
    }
}
