// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Stride.Core.Solutions;

/// <summary>
/// Reads and writes Visual Studio solution files through Microsoft.VisualStudio.SolutionPersistence,
/// mapping between its model and the <see cref="Solution"/> model used by the asset pipeline.
/// </summary>
internal static class SolutionSerialization
{
    // A solution created from scratch gets a single Any CPU platform and Debug/Release build types;
    // SolutionPersistence generates the per-project configuration mappings from these. A solution loaded
    // from disk keeps whatever it already had (see Write).
    private const string DefaultPlatform = "Any CPU";
    private static readonly string[] DefaultBuildTypes = ["Debug", "Release"];

    public static Solution Read(string solutionFullPath)
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFullPath)
            ?? throw new SolutionFileException($"Unsupported solution file format: '{solutionFullPath}'.");
        var model = serializer.OpenAsync(solutionFullPath, CancellationToken.None).GetAwaiter().GetResult();
        return ToSolution(model, solutionFullPath);
    }

    public static Solution Read(string solutionFullPath, Stream stream)
    {
        var model = SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
        return ToSolution(model, solutionFullPath);
    }

    public static void Write(Solution solution, string outputPath)
    {
        // Behave like Visual Studio: a solution loaded from disk keeps everything it had (platforms, build
        // configurations, solution folders, projects Stride doesn't manage) and only its project list is
        // reconciled. A solution that wasn't loaded from disk is created from a fresh model with defaults.
        var model = solution.SourceModel is { } source ? new SolutionModel(source) : NewModel(solution);
        Reconcile(model, solution, outputPath);

        byte[] content;
        using (var memory = new MemoryStream())
        {
            SolutionSerializers.SlnFileV12.SaveAsync(memory, model, CancellationToken.None).GetAwaiter().GetResult();
            content = memory.ToArray();
        }

        // Only write when the content actually changed, so Visual Studio doesn't reload the solution.
        if (File.Exists(outputPath) && File.ReadAllBytes(outputPath).AsSpan().SequenceEqual(content))
            return;

        File.WriteAllBytes(outputPath, content);
    }

    private static SolutionModel NewModel(Solution solution)
    {
        var model = new SolutionModel();
        model.AddPlatform(DefaultPlatform);
        foreach (var buildType in DefaultBuildTypes)
            model.AddBuildType(buildType);

        if (TryGetVersion(solution, "VisualStudioVersion", out var version))
            model.VisualStudioProperties.Version = version;

        return model;
    }

    // Adds projects/folders the session gained and removes those it dropped, leaving every other item
    // untouched. An existing project is never rewritten, so its on-disk project type guid and per-project
    // configuration survive verbatim; only newly added projects get the SDK-style project type guid.
    private static void Reconcile(SolutionModel model, Solution solution, string outputPath)
    {
        var solutionDirectory = Path.GetDirectoryName(outputPath) ?? string.Empty;

        var projectIds = solution.Projects.Where(project => !project.IsSolutionFolder).Select(project => project.Guid).ToHashSet();
        var folderIds = solution.Projects.Where(project => project.IsSolutionFolder).Select(project => project.Guid).ToHashSet();

        foreach (var project in model.SolutionProjects.ToList())
        {
            if (!projectIds.Contains(project.Id))
                model.RemoveProject(project);
        }

        foreach (var folder in model.SolutionFolders.ToList())
        {
            if (!folderIds.Contains(folder.Id))
                model.RemoveFolder(folder);
        }

        // Folders first, so newly added projects can be parented to them.
        var folders = model.SolutionFolders.ToDictionary(folder => folder.Id);
        foreach (var folder in solution.Projects.Where(project => project.IsSolutionFolder && !folders.ContainsKey(project.Guid)))
            folders[folder.Guid] = model.AddFolder(FolderPath(folder, solution));

        var existingProjectIds = model.SolutionProjects.Select(project => project.Id).ToHashSet();
        foreach (var project in solution.Projects.Where(project => !project.IsSolutionFolder && !existingProjectIds.Contains(project.Guid)))
        {
            var parent = project.ParentGuid != Guid.Empty && folders.TryGetValue(project.ParentGuid, out var folder) ? folder : null;
            var relativePath = Path.GetRelativePath(solutionDirectory, project.FullPath).Replace('/', '\\');
            var added = model.AddProject(relativePath, ProjectTypeName(project), parent);
            added.Id = project.Guid;
        }
    }

    private static Solution ToSolution(SolutionModel model, string solutionFullPath)
    {
        var solution = new Solution { FullPath = solutionFullPath, SourceModel = model };
        var solutionDirectory = Path.GetDirectoryName(solutionFullPath) ?? string.Empty;

        if (model.VisualStudioProperties.Version is { } version)
            solution.Properties.Add(new PropertyItem("VisualStudioVersion", version.ToString()));

        // Solution folders become folder projects; their ids let child items point back at them.
        foreach (var folder in model.SolutionFolders)
        {
            solution.Projects.Add(new Project(
                folder.Id, KnownProjectTypeGuid.SolutionFolder, folder.Name, folder.Name,
                folder.Parent?.Id ?? Guid.Empty, [], [], []));
        }

        foreach (var project in model.SolutionProjects)
        {
            var fullPath = Path.GetFullPath(project.FilePath.Replace('\\', Path.DirectorySeparatorChar), solutionDirectory);
            var name = project.ActualDisplayName ?? Path.GetFileNameWithoutExtension(fullPath);
            solution.Projects.Add(new Project(
                project.Id, ProjectTypeGuid(project), name, fullPath,
                project.Parent?.Id ?? Guid.Empty, [], [], []));
        }

        return solution;
    }

    // A .csproj uses the SDK-style project type guid; other project types keep whatever the serializer parsed.
    private static Guid ProjectTypeGuid(SolutionProjectModel project)
        => project.FilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? KnownProjectTypeGuid.CSharp
            : project.TypeId;

    // The project type guid to emit for a newly added project (C# projects are created with the SDK-style guid).
    private static string? ProjectTypeName(Project project)
        => project.TypeGuid == Guid.Empty ? null : project.TypeGuid.ToString();

    // The solution-relative folder path SolutionPersistence expects, e.g. "/Group/Subgroup/".
    private static string FolderPath(Project folder, Solution solution)
    {
        var segments = new List<string>();
        for (var current = folder; current is not null;
             current = solution.Projects.FirstOrDefault(project => project.Guid == current.ParentGuid && project.IsSolutionFolder))
        {
            segments.Insert(0, current.Name);
        }

        return "/" + string.Join("/", segments) + "/";
    }

    private static bool TryGetVersion(Solution solution, string name, out Version version)
    {
        var property = solution.Properties.FirstOrDefault(item => item.Name == name);
        if (property is not null && Version.TryParse(property.Value, out var parsed))
        {
            version = parsed;
            return true;
        }

        version = null!;
        return false;
    }
}
