// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Reflection;

namespace Stride.Assets.Editor.AssemblyReloading;

/// <summary>
/// Reloads the game code of a session in the editor's process, with <see cref="GameStudioAssemblyReloader"/>: the steps both
/// editors share, and <see cref="IGameCodeReloader"/> for the editors that decide what to reload from the rebuilt projects.
/// </summary>
/// <remarks>
/// <para>
/// A reload replaces the rebuilt assemblies and every loaded assembly that references one of them, directly or through another:
/// a reference stays bound to the copy it resolved first. A dependency whose restored version changed loads its new version
/// with them. The whole reload is one change of the undo history, named "Reload game assemblies".
/// </para>
/// <para>
/// Some changes can't be reloaded in place, and the session has to open again in a fresh editor: an assembly the editor itself
/// uses (its own copy, or a copy its code is bound to), an assembly with views of a UI toolkit (an editor plugin: its views,
/// styles and templates stay registered), an assembly the session could not load, and a dependency whose version can't change
/// in this session.
/// </para>
/// </remarks>
public sealed class GameCodeReloader : IGameCodeReloader
{
    // The assemblies of the UI toolkits: an assembly that references one has views, which stay registered with the toolkit
    private static readonly string[] UserInterfaceAssemblyPrefixes = ["Avalonia", "PresentationFramework", "PresentationCore"];

    /// <inheritdoc/>
    public async Task<GameCodeReloadResult> ReloadAsync(SessionViewModel session, IReadOnlyDictionary<string, string> builtAssemblies, ILogger log, Action undone)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(builtAssemblies);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(undone);

        var comparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var built = builtAssemblies.ToDictionary(x => Path.GetFullPath(x.Key), x => x.Value, comparer);
        var loadedByProject = new Dictionary<PackageLoadedAssembly, string>();
        foreach (var package in session.AllPackages)
        {
            foreach (var loadedAssembly in package.LoadedAssemblies)
            {
                if (ProjectPath(package, loadedAssembly) is { } projectPath)
                    loadedByProject[loadedAssembly] = projectPath;
            }
        }

        // The rebuilt projects the session loads
        var changed = new Dictionary<PackageLoadedAssembly, string>();
        foreach (var (loadedAssembly, projectPath) in loadedByProject)
        {
            if (!built.TryGetValue(projectPath, out var newPath))
                continue;
            if (loadedAssembly.Assembly is null)
                return GameCodeReloadResult.Restart($"[{Path.GetFileNameWithoutExtension(projectPath)}] is not loaded in this session");
            changed[loadedAssembly] = newPath;
        }
        if (changed.Count == 0)
        {
            log.Info("No assembly of the session changed: nothing to reload.");
            return GameCodeReloadResult.InPlace;
        }

        // A dependency moving to another version loads its new version with them: they build against it
        var versionChanges = await FindDependencyVersionChangesAsync(session, changed.Keys.Select(x => (x, false)), log);
        if (versionChanges.FirstOrDefault(x => x.Blocker is not null) is { } blocked)
            return GameCodeReloadResult.Restart($"[{blocked}] can't replace the loaded version in this session ({blocked.Blocker})");
        foreach (var versionChange in versionChanges)
        {
            foreach (var (packageAssembly, newPath) in versionChange.PackageAssemblies)
                changed[packageAssembly] = newPath;
        }

        // The loaded assemblies that reference a reloaded one reload too: a rebuilt project from its new build, another one
        // from its file again
        var changedLibraries = versionChanges.SelectMany(x => x.Libraries).Select(x => x.LoadedAssembly.Assembly);
        var loaded = session.AllPackages.SelectMany(x => x.LoadedAssemblies).Where(x => x.Assembly is not null)
            .GroupBy(x => x.Assembly).ToDictionary(x => x.Key, x => x.First());
        var closure = session.AssemblyContainer.GetDependentsClosure(changed.Keys.Select(x => x.Assembly).Concat(changedLibraries))
            .Where(loaded.ContainsKey).Select(x => loaded[x]).ToList();
        var reloaded = new Dictionary<PackageLoadedAssembly, string>();
        // The ones the container doesn't hold (none, unless the session loaded them another way) go first, as WPF does
        foreach (var (loadedAssembly, newPath) in changed)
        {
            if (!closure.Contains(loadedAssembly))
                reloaded.Add(loadedAssembly, newPath);
        }
        foreach (var loadedAssembly in closure)
        {
            reloaded[loadedAssembly] = changed.TryGetValue(loadedAssembly, out var newPath) ? newPath
                : loadedByProject.TryGetValue(loadedAssembly, out var projectPath) && built.TryGetValue(projectPath, out var builtPath) ? builtPath
                : loadedAssembly.Path;
        }

        var usedByEditor = FindAssembliesUsedByEditor(reloaded.Keys.Select(x => x.Assembly).NotNull());
        if (usedByEditor.Count > 0)
            return GameCodeReloadResult.Restart($"[{string.Join(", ", usedByEditor)}] in use by Game Studio itself");
        var withViews = reloaded.Keys.Select(x => x.Assembly).NotNull().Where(HasViews).Select(x => x.GetName().Name).ToList();
        if (withViews.Count > 0)
            return GameCodeReloadResult.Restart($"[{string.Join(", ", withViews)}] has views of the editor, which stay registered");

        foreach (var versionChange in versionChanges)
            log.Info($"[{versionChange}] loads the new version.");
        var reloadedCopies = reloaded.Keys.ToList();
        Apply(session, log, reloaded, versionChanges, () => { }, undone);
        log.Info($"Reloaded {string.Join(", ", reloadedCopies.Select(x => x.Assembly?.GetName().Name).NotNull())} in the editor.");
        return GameCodeReloadResult.InPlace;
    }

    /// <summary>
    /// The names of the assemblies the editor itself uses: its own copy of an assembly (the default load context), or a copy its
    /// code is bound to. A new copy next to it would mix the types of both; only a new session uses a new build of them.
    /// </summary>
    public static List<string> FindAssembliesUsedByEditor(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .Where(x => AssemblyLoadContext.GetLoadContext(x) == AssemblyLoadContext.Default || AssemblyContainer.IsBoundByHost(x))
            .Select(x => x.GetName().Name!)
            .ToList();
    }

    /// <summary>
    /// The dependencies of the given loaded assemblies' projects whose restored version differs from what the session loaded.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="assemblies">The loaded assemblies, each with whether its project file changed: such a project restores first,
    /// so that its build uses the dependencies it now asks for.</param>
    /// <param name="log">Where a project that could not be read is reported.</param>
    public static async Task<List<DependencyVersionChange>> FindDependencyVersionChangesAsync(SessionViewModel session, IEnumerable<(PackageLoadedAssembly Assembly, bool ProjectChanged)> assemblies, ILogger log)
    {
        var changes = new List<DependencyVersionChange>();
        var projects = new HashSet<SolutionProject>();
        foreach (var (loadedAssembly, projectChanged) in assemblies)
        {
            var package = session.AllPackages.FirstOrDefault(x => x.LoadedAssemblies.Contains(loadedAssembly))?.Package;
            if (package?.Container is not SolutionProject { FullPath: { } projectPath } project || package.Session is not { } packageSession || !projects.Add(project))
                continue;

            try
            {
                if (projectChanged)
                    await VSProjectHelper.RestoreNugetPackages(log, projectPath.ToOSPath());
                changes.AddRange(packageSession.FindDependencyVersionChanges(project));
            }
            catch (Exception e)
            {
                log.Error($"[{projectPath.GetFileNameWithoutExtension()}] could not read its restored dependencies", e);
            }
        }

        // A dependency of several projects changes once
        return changes.GroupBy(x => x.Dependency.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
    }

    /// <summary>
    /// Reloads assemblies of the session as one change of the undo history, "Reload game assemblies": the dependency version
    /// changes first, so that the reloaded assemblies resolve the new versions, then the assemblies, then the assets follow their
    /// bases again and the property grid shows the new types.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="log">Where the reload reports.</param>
    /// <param name="assemblies">The assemblies to reload, each with the file of its new copy, dependencies first.</param>
    /// <param name="versionChanges">The dependencies that load another version.</param>
    /// <param name="postReloadAction">Runs after the swap, and again after its undo and its redo.</param>
    /// <param name="undoAction">Runs when the reload is undone.</param>
    public static void Apply(SessionViewModel session, ILogger log, IReadOnlyDictionary<PackageLoadedAssembly, string> assemblies, IReadOnlyList<DependencyVersionChange> versionChanges, Action postReloadAction, Action undoAction)
    {
        var undoRedo = session.UndoRedoService ?? throw new InvalidOperationException("The session has no undo history.");
        using (var transaction = undoRedo.CreateTransaction())
        {
            // Before the reload, so that the reloaded assemblies resolve the new versions; undone after it
            if (versionChanges.Count > 0)
            {
                void ApplyVersions() => versionChanges.ForEach(x => x.Apply());
                void RevertVersions() => Enumerable.Reverse(versionChanges).ForEach(x => x.Revert());
                ApplyVersions();
                undoRedo.PushOperation(new AnonymousDirtyingOperation([], RevertVersions, ApplyVersions));
            }

            GameStudioAssemblyReloader.Reload(session, log, postReloadAction, undoAction, assemblies.ToDictionary(x => x.Key, x => x.Value));
            session.AllAssets.ForEach(x => x.PropertyGraph?.RefreshBase());
            session.AllAssets.ForEach(x => x.PropertyGraph?.ReconcileWithBase());

            undoRedo.SetName(transaction, "Reload game assemblies");
        }
        // Make sure we refresh the property grid so we don't reference any old type
        session.AssetViewProperties.RefreshSelectedPropertiesAsync().Forget();
    }

    private static bool HasViews(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies().Any(x => x.Name is { } name && UserInterfaceAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
    }

    private static string? ProjectPath(Core.Assets.Presentation.ViewModels.PackageViewModel package, PackageLoadedAssembly loadedAssembly)
    {
        if (loadedAssembly.ProjectReference?.Location is not { } location)
            return null;
        if (location.IsRelative)
            location = UPath.Combine(package.PackagePath.GetFullDirectory(), location);
        return Path.GetFullPath(location.ToOSPath());
    }
}
