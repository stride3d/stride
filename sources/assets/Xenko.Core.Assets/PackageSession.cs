// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Diagnostics;
using Xenko.Core.Assets.Tracking;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Packages;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using ILogger = Xenko.Core.Diagnostics.ILogger;

namespace Xenko.Core.Assets
{
    public abstract class PackageContainer
    {
        private PackageSession session;

        public PackageContainer([NotNull] Package package)
        {
            Package = package;
            Package.Container = this;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        [CanBeNull]
        public PackageSession Session { get; private set; }

        [NotNull]
        public Package Package { get; }

        public ObservableCollection<Package> LoadedDependencies { get; } = new ObservableCollection<Package>();

        internal void SetSessionInternal(PackageSession session)
        {
            Session = session;
        }
    }

    public class StandalonePackage : PackageContainer
    {
        private readonly Package package;

        public StandalonePackage([NotNull] Package package)
            : base(package)
        {
        }
    }

    public enum DependencyType
    {
        Package,
        Project,
    }

    public class Dependency
    {
        public Dependency(string name, PackageVersion version, DependencyType type)
        {
            Name = name;
            Version = version;
            Type = type;
        }

        public string Name { get; set; }

        public string MSBuildProject { get; set; }

        public PackageVersion Version { get; set; }

        public DependencyType Type { get; set; }

        public override string ToString()
        {
            return $"{Name} {Version} ({Type})";
        }
    }

    public class DependencyRange
    {
        public DependencyRange(string name, PackageVersionRange versionRange, DependencyType type)
        {
            Name = name;
            VersionRange = versionRange;
            Type = type;
        }

        public string Name { get; set; }

        public string MSBuildProject { get; set; }

        public PackageVersionRange VersionRange { get; set; }

        public DependencyType Type { get; set; }
    }

    public enum ProjectState
    {
        /// <summary>
        /// Project has been deserialized. References and assets are not ready.
        /// </summary>
        Raw,

        /// <summary>
        /// Dependencies have all been resolved and are also in <see cref="DependenciesReady"/> state.
        /// </summary>
        DependenciesReady,
    }

    public class SolutionProject : PackageContainer
    {
        private PackageSession session;
        private readonly Package package;

        public SolutionProject([NotNull] Package package, string fullPath)
            : base(package)
        {
            VSProject = new VisualStudio.Project(package.Id, VisualStudio.KnownProjectTypeGuid.CSharp, Path.GetFileNameWithoutExtension(fullPath), fullPath, Guid.Empty,
                Enumerable.Empty<VisualStudio.Section>(),
                Enumerable.Empty<VisualStudio.PropertyItem>(),
                Enumerable.Empty<VisualStudio.PropertyItem>());
        }

        public SolutionProject([NotNull] Package package, VisualStudio.Project vsProject)
            : base(package)
        {
            VSProject = vsProject;
        }

        public VisualStudio.Project VSProject { get; set; }

        public Guid Id => VSProject.Guid;

        public ProjectType Type { get; set; }

        public PlatformType Platform { get; set; }

        public string Name => VSProject.Name;

        public UFile FullPath => VSProject.FullPath;

        public ProjectState State { get; set; }

        public ObservableCollection<DependencyRange> DirectDependencies { get; } = new ObservableCollection<DependencyRange>();

        public ObservableCollection<Dependency> FlattenedDependencies { get; } = new ObservableCollection<Dependency>();
    }

    public sealed class ProjectCollection : ObservableCollection<PackageContainer>
    {
    }

    /// <summary>
    /// A session for editing a package.
    /// </summary>
    public sealed partial class PackageSession : IDisposable, IAssetFinder
    {
        /// <summary>
        /// The visual studio version property used for newly created project solution files
        /// </summary>
        public static readonly Version DefaultVisualStudioVersion = new Version("14.0.23107.0");

        private readonly ConstraintProvider constraintProvider = new ConstraintProvider();
        private readonly PackageCollection packages;
        private readonly PackageCollection packagesCopy;
        private readonly object dependenciesLock = new object();
        private SolutionProject currentProject;
        private AssetDependencyManager dependencies;
        private AssetSourceTracker sourceTracker;
        private bool? packageUpgradeAllowed;
        public event DirtyFlagChangedDelegate<AssetItem> AssetDirtyChanged;
        private TaskCompletionSource<int> saveCompletion;

        internal VisualStudio.Solution VSSolution;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageSession"/> class.
        /// </summary>
        public PackageSession()
        {
            VSSolution = new VisualStudio.Solution();
            VSSolution.Headers.Add(PackageSessionHelper.SolutionHeader);

            constraintProvider.AddConstraint(PackageStore.Instance.DefaultPackageName, new PackageVersionRange(PackageStore.Instance.DefaultPackageVersion));

            Projects = new ProjectCollection();
            Projects.CollectionChanged += ProjectsCollectionChanged;

            packages = new PackageCollection();
            packagesCopy = new PackageCollection();
            AssemblyContainer = new AssemblyContainer();
            packages.CollectionChanged += PackagesCollectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageSession"/> class.
        /// </summary>
        public PackageSession(Package package) : this()
        {
            Projects.Add(new StandalonePackage(package));
        }

        public bool IsDirty { get; set; }

        /// <summary>
        /// Gets the packages referenced by the solution.
        /// </summary>
        /// <value>The packages.</value>
        public IReadOnlyPackageCollection Packages => packages;

        /// <summary>
        /// The projects referenced by the solution.
        /// </summary>
        public ProjectCollection Projects { get; }

        /// <summary>
        /// Gets the user packages (excluding system packages).
        /// </summary>
        /// <value>The user packages.</value>
        public IEnumerable<Package> LocalPackages => Packages.Where(package => !package.IsSystem);

        /// <summary>
        /// Gets a task that completes when the session is finished saving.
        /// </summary>
        [NotNull]
        public Task SaveCompletion => saveCompletion?.Task ?? Task.CompletedTask;

        /// <summary>
        /// Gets or sets the solution path (sln) in case the session was loaded from a solution.
        /// </summary>
        /// <value>The solution path.</value>
        public UFile SolutionPath
        {
            get => VSSolution.FullPath;
            set => VSSolution.FullPath = value;
        }

        public AssemblyContainer AssemblyContainer { get; }

        /// <summary>
        /// The targeted visual studio version (if specified by the loaded package)
        /// </summary>
        public Version VisualStudioVersion { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            dependencies?.Dispose();
            sourceTracker?.Dispose();

            var loadedAssemblies = Packages.SelectMany(x => x.LoadedAssemblies).ToList();
            for (int index = loadedAssemblies.Count - 1; index >= 0; index--)
            {
                var loadedAssembly = loadedAssemblies[index];
                if (loadedAssembly == null)
                    continue;

                // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
                AssemblyRegistry.Unregister(loadedAssembly.Assembly);

                // Unload binary serialization
                DataSerializerFactory.UnregisterSerializationAssembly(loadedAssembly.Assembly);

                // Unload assembly
                AssemblyContainer.UnloadAssembly(loadedAssembly.Assembly);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has dependency manager.
        /// </summary>
        /// <value><c>true</c> if this instance has dependency manager; otherwise, <c>false</c>.</value>
        public bool HasDependencyManager
        {
            get
            {
                lock (dependenciesLock)
                {
                    return dependencies != null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected current package.
        /// </summary>
        /// <value>The selected current package.</value>
        /// <exception cref="System.InvalidOperationException">Expecting a package that is already registered in this session</exception>
        public SolutionProject CurrentProject
        {
            get
            {
                return currentProject;
            }
            set
            {
                if (value != null)
                {
                    if (!Projects.Contains(value))
                    {
                        throw new InvalidOperationException("Expecting a package that is already registered in this session");
                    }
                }
                currentProject = value;
            }
        }

        /// <summary>
        /// Gets the packages referenced by the current package.
        /// </summary>
        /// <returns>IEnumerable&lt;Package&gt;.</returns>
        public IEnumerable<Package> GetPackagesFromCurrent()
        {
            if (CurrentProject.Package == null)
            {
                yield return CurrentProject.Package;
            }

            foreach (var dependency in CurrentProject.FlattenedDependencies)
            {
                var loadedPackage = packages.Find(dependency);
                // In case the package is not found (when working with session not fully loaded/resolved with all deps)
                if (loadedPackage == null)
                {
                    yield return loadedPackage;
                }
            }
        }

        /// <summary>
        /// Gets the dependency manager.
        /// </summary>
        /// <value>AssetDependencyManager.</value>
        public AssetDependencyManager DependencyManager
        {
            get
            {
                lock (dependenciesLock)
                {
                    return dependencies ?? (dependencies = new AssetDependencyManager(this));
                }
            }
        }

        public AssetSourceTracker SourceTracker
        {
            get
            {
                lock (dependenciesLock)
                {
                    return sourceTracker ?? (sourceTracker = new AssetSourceTracker(this));
                }
            }
        }

        /// <summary>
        /// Adds an existing package to the current session.
        /// </summary>
        /// <param name="projectPath">The project or package path.</param>
        /// <param name="logger">The session result.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        /// <exception cref="System.ArgumentNullException">packagePath</exception>
        /// <exception cref="System.ArgumentException">Invalid relative path. Expecting an absolute package path;packagePath</exception>
        /// <exception cref="System.IO.FileNotFoundException">Unable to find package</exception>
        public PackageContainer AddExistingProject(UFile projectPath, ILogger logger, PackageLoadParameters loadParametersArg = null)
        {
            if (projectPath == null) throw new ArgumentNullException(nameof(projectPath));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (!projectPath.IsAbsolute) throw new ArgumentException(@"Invalid relative path. Expecting an absolute project path", nameof(projectPath));
            if (!File.Exists(projectPath)) throw new FileNotFoundException("Unable to find project", projectPath);

            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();

            Package package;
            PackageContainer project;
            try
            {
                // Enable reference analysis caching during loading
                AssetReferenceAnalysis.EnableCaching = true;

                project = LoadProject(logger, projectPath.ToWindowsPath(), false, loadParametersArg);
                Projects.Add(project);

                package = project.Package;

                if (loadParameters.AutoCompileProjects && loadParameters.ForceNugetRestore)
                {
                    // Note: solution needs to be saved right away so that we can restore nuget packages
                    Save(logger);
                    VSProjectHelper.RestoreNugetPackages(logger, SolutionPath).Wait();
                }

                // Load all missing references/dependencies
                LoadMissingDependencies(logger, loadParameters);

                // Process everything except current one (it needs different load parameters)
                var dependencyLoadParameters = loadParameters.Clone();
                dependencyLoadParameters.GenerateNewAssetIds = false;
                LoadMissingAssets(logger, Packages.Where(x => x != package).ToList(), dependencyLoadParameters);

                LoadMissingAssets(logger, new[] { package }, loadParameters);

                // Run analysis after
                // TODO CSPROJ=XKPKG
                //foreach (var packageToAdd in packagesLoaded)
                //{
                //    var analysis = new PackageAnalysis(packageToAdd, GetPackageAnalysisParametersForLoad());
                //    analysis.Run(logger);
                //}
            }
            finally
            {
                // Disable reference analysis caching after loading
                AssetReferenceAnalysis.EnableCaching = false;
            }
            return project;
        }

        /// <summary>
        /// Adds an existing package to the current session and runs the package analysis before adding it.
        /// </summary>
        /// <param name="package">The package to add</param>
        /// <param name="logger">The logger</param>
        public void AddExistingPackage(Package package, ILogger logger)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (Packages.Contains(package))
            {
                return;
            }

            // Preset the session on the package to allow the session to look for existing asset
            packages.Add(package);

            // Run analysis after
            var analysis = new PackageAnalysis(package, GetPackageAnalysisParametersForLoad());
            analysis.Run(logger);

        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst all the packages of this session.</remarks>
        public AssetItem FindAsset(AssetId assetId)
        {
            return Packages.Select(p => p.Assets.Find(assetId)).NotNull().FirstOrDefault();
        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst all the packages of this session.</remarks>
        public AssetItem FindAsset(UFile location)
        {
            return Packages.Select(p => p.Assets.Find(location)).NotNull().FirstOrDefault();
        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst all the packages of this session.</remarks>
        public AssetItem FindAssetFromProxyObject(object proxyObject)
        {
            var reference = AttachedReferenceManager.GetAttachedReference(proxyObject);
            return reference != null ? (FindAsset(reference.Id) ?? FindAsset(reference.Url)) : null;
        }

        private PackageContainer LoadProject(ILogger log, string filePath, bool isSystem, PackageLoadParameters loadParameters = null)
        {
            var project = Package.LoadProject(log, filePath);

            var package = project.Package;
            package.IsSystem = isSystem;

            // If the package doesn't have a meta name, fix it here (This is supposed to be done in the above disabled analysis - but we still need to do it!)
            if (string.IsNullOrWhiteSpace(package.Meta.Name) && package.FullPath != null)
            {
                package.Meta.Name = package.FullPath.GetFileNameWithoutExtension();
                package.IsDirty = true;
            }

            // Package has been loaded, register it in constraints so that we force each subsequent loads to use this one (or fails if version doesn't match)
            if (package.Meta.Version != null)
            {
                constraintProvider.AddConstraint(package.Meta.Name, new PackageVersionRange(package.Meta.Version));
            }

            return project;
        }

        /// <summary>
        /// Loads a package from specified file path.
        /// </summary>
        /// <param name="filePath">The file path to a package file.</param>
        /// <param name="sessionResult">The session result.</param>
        /// <param name="loadParameters">The load parameters.</param>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        /// <exception cref="System.ArgumentException">File [{0}] must exist.ToFormat(filePath);filePath</exception>
        public static void Load(string filePath, PackageSessionResult sessionResult, PackageLoadParameters loadParameters = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (sessionResult == null) throw new ArgumentNullException(nameof(sessionResult));

            // Make sure with have valid parameters
            loadParameters = loadParameters ?? PackageLoadParameters.Default();

            // Make sure to use a full path.
            filePath = FileUtility.GetAbsolutePath(filePath);

            if (!File.Exists(filePath)) throw new ArgumentException($@"File [{filePath}] must exist", nameof(filePath));

            try
            {
                // Enable reference analysis caching during loading
                AssetReferenceAnalysis.EnableCaching = true;

                using (var profile = Profiler.Begin(PackageSessionProfilingKeys.Loading))
                {
                    sessionResult.Clear();
                    sessionResult.Progress("Loading..", 0, 1);

                    var session = new PackageSession();

                    var cancelToken = loadParameters.CancelToken;
                    SolutionProject firstProject = null;

                    // If we have a solution, load all packages
                    if (PackageSessionHelper.IsSolutionFile(filePath))
                    {
                        // The session should save back its changes to the solution
                        var solution = session.VSSolution = VisualStudio.Solution.FromFile(filePath);

                        // Keep header
                        var versionHeader = solution.Properties.FirstOrDefault(x => x.Name == "VisualStudioVersion");
                        Version version;
                        if (versionHeader != null && Version.TryParse(versionHeader.Value, out version))
                            session.VisualStudioVersion = version;
                        else
                            session.VisualStudioVersion = null;

                        foreach (var vsProject in solution.Projects)
                        {
                            if (vsProject.TypeGuid == VisualStudio.KnownProjectTypeGuid.CSharp)
                            {
                                var project = (SolutionProject)session.LoadProject(sessionResult, vsProject.FullPath, false, loadParameters);
                                project.VSProject = vsProject;
                                project.Package.Id = vsProject.Guid;
                                session.Projects.Add(project);

                                if (firstProject == null)
                                    firstProject = project;

                                // Output the session only if there is no cancellation
                                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                                {
                                    return;
                                }
                            }
                        }

                        session.LoadMissingDependencies(sessionResult, loadParameters);
                    }
                    else if (Path.GetExtension(filePath).ToLowerInvariant() == ".csproj")
                    {
                        var project = (SolutionProject)session.LoadProject(sessionResult, filePath, false, loadParameters);
                        session.Projects.Add(project);
                        firstProject = project;
                    }
                    else
                    {
                        sessionResult.Error($"Unsupported file extension (only .sln are supported)");
                        return;
                    }

                    if (loadParameters.AutoCompileProjects && loadParameters.ForceNugetRestore && PackageSessionHelper.IsPackageFile(filePath))
                    {
                        // Restore nuget packages
                        if (PackageSessionHelper.IsSolutionFile(filePath))
                        {
                            VSProjectHelper.RestoreNugetPackages(sessionResult, filePath).Wait();
                        }
                        else
                        {
                            // No .sln, run NuGet restore for each project
                            foreach (var package in session.Packages)
                                package.RestoreNugetPackages(sessionResult);
                        }
                    }

                    // Load all missing references/dependencies
                    session.LoadMissingReferences(sessionResult, loadParameters);

                    // Fix relative references
                    var analysis = new PackageSessionAnalysis(session, GetPackageAnalysisParametersForLoad());
                    var analysisResults = analysis.Run();
                    analysisResults.CopyTo(sessionResult);

                    // Run custom package session analysis
                    foreach (var type in AssetRegistry.GetPackageSessionAnalysisTypes())
                    {
                        var pkgAnalysis = (PackageSessionAnalysisBase)Activator.CreateInstance(type);
                        pkgAnalysis.Session = session;
                        var results = pkgAnalysis.Run();
                        results.CopyTo(sessionResult);
                    }

                    // Output the session only if there is no cancellation
                    if (!cancelToken.HasValue || !cancelToken.Value.IsCancellationRequested)
                    {
                        sessionResult.Session = session;

                        // Defer the initialization of the dependency manager
                        //session.DependencyManager.InitializeDeferred();
                    }

                    // Setup the current package when loading it
                    if (firstProject != null)
                        session.CurrentProject = firstProject;

                    // The session is not dirty when loading it
                    session.IsDirty = false;
                }
            }
            finally
            {
                // Disable reference analysis caching after loading
                AssetReferenceAnalysis.EnableCaching = false;
            }
        }

        /// <summary>
        /// Loads a package from specified file path.
        /// </summary>
        /// <param name="filePath">The file path to a package file.</param>
        /// <param name="loadParameters">The load parameters.</param>
        /// <returns>A package.</returns>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        public static PackageSessionResult Load(string filePath, PackageLoadParameters loadParameters = null)
        {
            var result = new PackageSessionResult();
            Load(filePath, result, loadParameters);
            return result;
        }

        /// <summary>
        /// Make sure packages have their dependencies and assets loaded.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loadParameters">The load parameters.</param>
        public void LoadMissingReferences(ILogger log, PackageLoadParameters loadParameters = null)
        {
            LoadMissingDependencies(log, loadParameters);
            LoadMissingAssets(log, Packages.ToList(), loadParameters);
        }

        /// <summary>
        /// Make sure packages have their dependencies loaded.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        public void LoadMissingDependencies(ILogger log, PackageLoadParameters loadParametersArg = null)
        {
            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();

            var cancelToken = loadParameters.CancelToken;

            var previousProjects = Projects.ToList();
            foreach (var project in previousProjects)
            {
                // Output the session only if there is no cancellation
                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                {
                    return;
                }

                if (project is SolutionProject solutionProject)
                    PreLoadPackageDependencies(log, solutionProject, loadParameters).Wait();
            }
        }

        /// <summary>
        /// Make sure packages have their assets loaded.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="packages">The packages to try to load missing assets from.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        public void LoadMissingAssets(ILogger log, IEnumerable<Package> packages, PackageLoadParameters loadParametersArg = null)
        {
            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();

            var cancelToken = loadParameters.CancelToken;

            // Make a copy of Packages as it can be modified by PreLoadPackageDependencies
            foreach (var package in packages)
            {
                // Output the session only if there is no cancellation
                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                {
                    return;
                }

                TryLoadAssets(this, log, package, loadParameters);
            }
        }

        /// <summary>
        /// Saves all packages and assets.
        /// </summary>
        /// <param name="log">The <see cref="LoggerResult"/> in which to report result.</param>
        /// <param name="saveParameters">The parameters for the save operation.</param>
        public void Save(ILogger log, PackageSaveParameters saveParameters = null)
        {
            //var clock = Stopwatch.StartNew();
            var loggerResult = new ForwardingLoggerResult(log);
            using (var profile = Profiler.Begin(PackageSessionProfilingKeys.Saving))
            {
                var packagesSaved = false;
                var packagesDirty = false;
                try
                {
                    saveCompletion = new TaskCompletionSource<int>();

                    saveParameters = saveParameters ?? PackageSaveParameters.Default();
                    var assetsOrPackagesToRemove = BuildAssetsOrPackagesToRemove();

                    // Compute packages that have been renamed
                    // TODO: Disable for now, as not sure if we want to delete a previous package
                    //foreach (var package in packagesCopy)
                    //{
                    //    var newPackage = packages.Find(package.Id);
                    //    if (newPackage != null && package.PackagePath != null && newPackage.PackagePath != package.PackagePath)
                    //    {
                    //        assetsOrPackagesToRemove[package.PackagePath] = package;
                    //    }
                    //}

                    // If package are not modified, return immediately
                    if (!CheckModifiedPackages() && assetsOrPackagesToRemove.Count == 0)
                    {
                        return;
                    }

                    // Suspend tracking when saving as we don't want to receive
                    // all notification events
                    dependencies?.BeginSavingSession();
                    sourceTracker?.BeginSavingSession();

                    // Return immediately if there is any error
                    if (loggerResult.HasErrors)
                        return;
       
                    //batch projects
                    var vsProjs = new Dictionary<string, Microsoft.Build.Evaluation.Project>();

                    // Delete previous files
                    foreach (var fileIt in assetsOrPackagesToRemove)
                    {
                        var assetPath = fileIt.Key;
                        var assetItemOrPackage = fileIt.Value;

                        var assetItem = assetItemOrPackage as AssetItem;
                        try
                        {
                            //If we are within a csproj we need to remove the file from there as well
                            var projectFullPath = (assetItem.Package.Container as SolutionProject)?.FullPath;
                            if (projectFullPath != null)
                            {
                                var projectAsset = assetItem.Asset as IProjectAsset;
                                if (projectAsset != null)
                                {
                                    var projectInclude = assetItem.GetProjectInclude();

                                    Microsoft.Build.Evaluation.Project project;
                                    if (!vsProjs.TryGetValue(projectFullPath, out project))
                                    {
                                        project = VSProjectHelper.LoadProject(projectFullPath.ToWindowsPath());
                                        vsProjs.Add(projectFullPath, project);
                                    }
                                    var projectItem = project.Items.FirstOrDefault(x => (x.ItemType == "Compile" || x.ItemType == "None") && x.EvaluatedInclude == projectInclude);
                                    if (projectItem != null && !projectItem.IsImported)
                                    {
                                        project.RemoveItem(projectItem);
                                    }

                                    //delete any generated file as well
                                    var generatorAsset = assetItem.Asset as IProjectFileGeneratorAsset;
                                    if (generatorAsset != null)
                                    {
                                        var generatedAbsolutePath = assetItem.GetGeneratedAbsolutePath().ToWindowsPath();

                                        File.Delete(generatedAbsolutePath);

                                        var generatedInclude = assetItem.GetGeneratedInclude();
                                        var generatedItem = project.Items.FirstOrDefault(x => (x.ItemType == "Compile" || x.ItemType == "None") && x.EvaluatedInclude == generatedInclude);
                                        if (generatedItem != null)
                                        {
                                            project.RemoveItem(generatedItem);
                                        }
                                    }
                                }
                            }

                            File.Delete(assetPath);
                        }
                        catch (Exception ex)
                        {
                            if (assetItem != null)
                            {
                                loggerResult.Error(assetItem.Package, assetItem.ToReference(), AssetMessageCode.AssetCannotDelete, ex, assetPath);
                            }
                            else
                            {
                                var package = assetItemOrPackage as Package;
                                if (package != null)
                                {
                                    loggerResult.Error(package, null, AssetMessageCode.AssetCannotDelete, ex, assetPath);
                                }
                            }
                        }
                    }

                    foreach (var project in vsProjs.Values)
                    {
                        project.Save();
                        project.ProjectCollection.UnloadAllProjects();
                        project.ProjectCollection.Dispose();
                    }

                    // Save all dirty assets
                    packagesCopy.Clear();
                    foreach (var package in LocalPackages)
                    {
                        // Save the package to disk and all its assets
                        package.Save(loggerResult, saveParameters);

                        // Check if everything was saved (might not be the case if things are filtered out)
                        if (package.IsDirty || package.Assets.IsDirty)
                            packagesDirty = true;

                        // Clone the package (but not all assets inside, just the structure)
                        var packageClone = package.Clone();
                        packagesCopy.Add(packageClone);
                    }

                    packagesSaved = true;
                }
                finally
                {
                    sourceTracker?.EndSavingSession();
                    dependencies?.EndSavingSession();

                    // Once all packages and assets have been saved, we can save the solution (as we need to have fullpath to
                    // be setup for the packages)
                    if (packagesSaved)
                    {
                        VSSolution.Save();
                    }
                    saveCompletion?.SetResult(0);
                    saveCompletion = null;
                }

                //System.Diagnostics.Trace.WriteLine("Elapsed saved: " + clock.ElapsedMilliseconds);
                IsDirty = packagesDirty;
            }
        }

        private Dictionary<UFile, object> BuildAssetsOrPackagesToRemove()
        {
            // Grab all previous assets
            var previousAssets = new Dictionary<AssetId, AssetItem>();
            foreach (var assetItem in packagesCopy.SelectMany(package => package.Assets))
            {
                previousAssets[assetItem.Id] = assetItem;
            }

            // Grab all new assets
            var newAssets = new Dictionary<AssetId, AssetItem>();
            foreach (var assetItem in LocalPackages.SelectMany(package => package.Assets))
            {
                newAssets[assetItem.Id] = assetItem;
            }

            // Compute all assets that were removed
            var assetsOrPackagesToRemove = new Dictionary<UFile, object>();
            foreach (var assetIt in previousAssets)
            {
                var asset = assetIt.Value;

                AssetItem newAsset;
                if (!newAssets.TryGetValue(assetIt.Key, out newAsset) || newAsset.Location != asset.Location)
                {
                    assetsOrPackagesToRemove[asset.FullPath] = asset;
                }
            }
            return assetsOrPackagesToRemove;
        }

        /// <summary>
        /// Loads the assembly references that were not loaded before.
        /// </summary>
        /// <param name="log">The log.</param>
        public void UpdateAssemblyReferences(LoggerResult log)
        {
            foreach (var package in LocalPackages)
            {
                package.UpdateAssemblyReferences(log);
            }
        }

        private bool CheckModifiedPackages()
        {
            if (IsDirty)
            {
                return true;
            }

            foreach (var package in LocalPackages)
            {
                if (package.IsDirty || package.Assets.IsDirty)
                {
                    return true;
                }
                if (package.Assets.Any(item => item.IsDirty))
                {
                    return true;
                }
            }
            return false;
        }

        private void ProjectsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    RegisterProject((PackageContainer)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UnRegisterProject((PackageContainer)e.OldItems[0]);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    packagesCopy.Clear();

                    foreach (var oldProject in e.OldItems.OfType<PackageContainer>())
                    {
                        UnRegisterProject(oldProject);
                    }

                    foreach (var projectToCopy in Projects)
                    {
                        RegisterProject(projectToCopy);
                    }
                    break;
            }
        }

        private void RegisterProject(PackageContainer project)
        {
            if (project.Session != null)
            {
                throw new InvalidOperationException("Cannot attach a project to more than one session");
            }

            project.SetSessionInternal(this);

            if (project is SolutionProject solutionProject)
            {
                // Note: when loading, package might already be there
                // TODO CSPROJ=XKPKG: skip it in a proper way? (context info)
                if (!VSSolution.Projects.Contains(solutionProject.VSProject))
                    VSSolution.Projects.Add(solutionProject.VSProject);
            }

            packages.Add(project.Package);
        }

        private void UnRegisterProject(PackageContainer project)
        {
            if (project.Session != this)
            {
                throw new InvalidOperationException("Cannot detach a project that was not attached to this session");
            }

            if (project.Package != null)
                packages.Remove(project.Package);
            if (project is SolutionProject solutionProject)
            {
                VSSolution.Projects.Remove(solutionProject.VSProject);
            }

            project.SetSessionInternal(null);
        }

        private void PackagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    RegisterPackage((Package)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UnRegisterPackage((Package)e.OldItems[0]);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    packagesCopy.Clear();

                    foreach (var oldPackage in e.OldItems.OfType<Package>())
                    {
                        UnRegisterPackage(oldPackage);
                    }

                    foreach (var packageToCopy in Packages)
                    {
                        RegisterPackage(packageToCopy);
                    }
                    break;
            }
        }

        private void RegisterPackage(Package package)
        {
            package.IsIdLocked = true;
            if (package.IsSystem)
                return;
            package.AssetDirtyChanged += OnAssetDirtyChanged;

            // If the package doesn't have any temporary assets, we can freeze it
            if (package.TemporaryAssets.Count == 0)
            {
                FreezePackage(package);
            }

            IsDirty = true;
        }

        /// <summary>
        /// Freeze a package once it is loaded with all its assets
        /// </summary>
        /// <param name="package">The package to freeze.</param>
        private void FreezePackage(Package package)
        {
            if (package.IsSystem)
                return;

            // Freeze only when assets are loaded
            if (package.State < PackageState.AssetsReady)
                return;

            packagesCopy.Add(package.Clone());
        }

        private void UnRegisterPackage(Package package)
        {
            package.IsIdLocked = false;
            if (package.IsSystem)
                return;
            package.AssetDirtyChanged -= OnAssetDirtyChanged;

            packagesCopy.RemoveById(package.Id);

            IsDirty = true;
        }

        private void OnAssetDirtyChanged(AssetItem asset, bool oldValue, bool newValue)
        {
            AssetDirtyChanged?.Invoke(asset, oldValue, newValue);
        }

        private Package PreLoadPackage(ILogger log, string filePath, bool isSystemPackage, PackageLoadParameters loadParameters)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (loadParameters == null) throw new ArgumentNullException(nameof(loadParameters));

            try
            {
                // Load the package without loading any assets
                var package = Package.LoadRaw(log, filePath);
                package.IsSystem = isSystemPackage;

                // Convert UPath to absolute (Package only)
                // Removed for now because it is called again in PackageSession.LoadAssembliesAndAssets (and running it twice result in dirty package)
                // If we remove it from here (and call it only in the other method), templates are not loaded (Because they are loaded via the package store that do not use PreLoadPackage)
                //if (loadParameters.ConvertUPathToAbsolute)
                //{
                //    var analysis = new PackageAnalysis(package, new PackageAnalysisParameters()
                //    {
                //        ConvertUPathTo = UPathType.Absolute,
                //        SetDirtyFlagOnAssetWhenFixingAbsoluteUFile = true,
                //        IsProcessingUPaths = true,
                //    });
                //    analysis.Run(log);
                //}
                // If the package doesn't have a meta name, fix it here (This is supposed to be done in the above disabled analysis - but we still need to do it!)
                if (string.IsNullOrWhiteSpace(package.Meta.Name) && package.FullPath != null)
                {
                    package.Meta.Name = package.FullPath.GetFileNameWithoutExtension();
                    package.IsDirty = true;
                }

                // Package has been loaded, register it in constraints so that we force each subsequent loads to use this one (or fails if version doesn't match)
                if (package.Meta.Version != null)
                {
                    constraintProvider.AddConstraint(package.Meta.Name, new PackageVersionRange(package.Meta.Version));
                }

                return package;
            }
            catch (Exception ex)
            {
                log.Error($"Error while pre-loading package [{filePath}]", ex);
            }

            return null;
        }

        private bool TryLoadAssets(PackageSession session, ILogger log, Package package, PackageLoadParameters loadParameters)
        {
            // Already loaded
            if (package.State >= PackageState.AssetsReady)
                return true;

            // A package upgrade has previously been tried and denied, so let's keep the package in this state
            if (package.State == PackageState.UpgradeFailed)
                return false;

            try
            {
                // First, check that dependencies have their assets loaded
                bool dependencyError = false;
                foreach (var dependency in package.FindDependencies(false))
                {
                    if (!TryLoadAssets(session, log, dependency, loadParameters))
                        dependencyError = true;
                }

                if (dependencyError)
                    return false;

                // TODO CSPROJ=XKPKG: get package upgraders from PreLoadPackageDependencies
                var pendingPackageUpgrades = new List<PendingPackageUpgrade>();

                // Note: Default state is upgrade failed (for early exit on error/exceptions)
                // We will update to success as soon as loading is finished.
                package.State = PackageState.UpgradeFailed;

                // Prepare asset loading
                var newLoadParameters = loadParameters.Clone();
                newLoadParameters.AssemblyContainer = session.AssemblyContainer;

                // Default package version override
                newLoadParameters.ExtraCompileProperties = new Dictionary<string, string>();
                var defaultPackageOverride = NugetStore.GetPackageVersionVariable(PackageStore.Instance.DefaultPackageName) + "Override";
                var defaultPackageVersion = PackageStore.Instance.DefaultPackageVersion.Version;
                newLoadParameters.ExtraCompileProperties.Add(defaultPackageOverride, new Version(defaultPackageVersion.Major, defaultPackageVersion.Minor).ToString());
                if (loadParameters.ExtraCompileProperties != null)
                {
                    foreach (var property in loadParameters.ExtraCompileProperties)
                    {
                        newLoadParameters.ExtraCompileProperties[property.Key] = property.Value;
                    }
                }

                if (pendingPackageUpgrades.Count > 0)
                {
                    var upgradeAllowed = packageUpgradeAllowed != false ? PackageUpgradeRequestedAnswer.Upgrade : PackageUpgradeRequestedAnswer.DoNotUpgrade;

                    // Need upgrades, let's ask user confirmation
                    if (loadParameters.PackageUpgradeRequested != null && !packageUpgradeAllowed.HasValue)
                    {
                        upgradeAllowed = loadParameters.PackageUpgradeRequested(package, pendingPackageUpgrades);
                        if (upgradeAllowed == PackageUpgradeRequestedAnswer.UpgradeAll)
                            packageUpgradeAllowed = true;
                        if (upgradeAllowed == PackageUpgradeRequestedAnswer.DoNotUpgradeAny)
                            packageUpgradeAllowed = false;
                    }

                    if (!PackageLoadParameters.ShouldUpgrade(upgradeAllowed))
                    {
                        log.Error($"Necessary package migration for [{package.Meta.Name}] has not been allowed");
                        return false;
                    }

                    // Perform pre assembly load upgrade
                    foreach (var pendingPackageUpgrade in pendingPackageUpgrades)
                    {
                        var packageUpgrader = pendingPackageUpgrade.PackageUpgrader;
                        var dependencyPackage = pendingPackageUpgrade.DependencyPackage;
                        if (!packageUpgrader.UpgradeBeforeAssembliesLoaded(loadParameters, session, log, package, pendingPackageUpgrade.Dependency, dependencyPackage))
                        {
                            log.Error($"Error while upgrading package [{package.Meta.Name}] for [{dependencyPackage.Meta.Name}] from version [{pendingPackageUpgrade.Dependency.Version}] to [{dependencyPackage.Meta.Version}]");
                            return false;
                        }
                    }
                }

                // Load assemblies. Set the package filename to the path on disk, in case of renaming.
                // TODO: Could referenced projects be associated to other packages than this one?
                newLoadParameters.ExtraCompileProperties.Add("XenkoCurrentPackagePath", package.FullPath);
                package.LoadAssemblies(log, newLoadParameters);

                // Load list of assets
                newLoadParameters.AssetFiles = Package.ListAssetFiles(log, package, true, loadParameters.CancelToken);
                // Sort them by size (to improve concurrency during load)
                newLoadParameters.AssetFiles.Sort(PackageLoadingAssetFile.FileSizeComparer.Default);

                if (pendingPackageUpgrades.Count > 0)
                {
                    // Perform upgrades
                    foreach (var pendingPackageUpgrade in pendingPackageUpgrades)
                    {
                        var packageUpgrader = pendingPackageUpgrade.PackageUpgrader;
                        var dependencyPackage = pendingPackageUpgrade.DependencyPackage;
                        if (!packageUpgrader.Upgrade(loadParameters, session, log, package, pendingPackageUpgrade.Dependency, dependencyPackage, newLoadParameters.AssetFiles))
                        {
                            log.Error($"Error while upgrading package [{package.Meta.Name}] for [{dependencyPackage.Meta.Name}] from version [{pendingPackageUpgrade.Dependency.Version}] to [{dependencyPackage.Meta.Version}]");
                            return false;
                        }

                        // Update dependency to reflect new requirement
                        pendingPackageUpgrade.Dependency.Version = pendingPackageUpgrade.PackageUpgrader.Attribute.UpdatedVersionRange;
                    }

                    // Mark package as dirty
                    package.IsDirty = true;
                }

                // Load assets
                package.LoadAssets(log, newLoadParameters);

                // Validate assets from package
                package.ValidateAssets(newLoadParameters.GenerateNewAssetIds, newLoadParameters.RemoveUnloadableObjects, log);

                if (pendingPackageUpgrades.Count > 0)
                {
                    // Perform post asset load upgrade
                    foreach (var pendingPackageUpgrade in pendingPackageUpgrades)
                    {
                        var packageUpgrader = pendingPackageUpgrade.PackageUpgrader;
                        var dependencyPackage = pendingPackageUpgrade.DependencyPackage;
                        if (!packageUpgrader.UpgradeAfterAssetsLoaded(loadParameters, session, log, package, pendingPackageUpgrade.Dependency, dependencyPackage, pendingPackageUpgrade.DependencyVersionBeforeUpgrade))
                        {
                            log.Error($"Error while upgrading package [{package.Meta.Name}] for [{dependencyPackage.Meta.Name}] from version [{pendingPackageUpgrade.Dependency.Version}] to [{dependencyPackage.Meta.Version}]");
                            return false;
                        }
                    }

                    // Mark package as dirty
                    package.IsDirty = true;
                }

                // Mark package as ready
                package.State = PackageState.AssetsReady;

                // Freeze the package after loading the assets
                session.FreezePackage(package);

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error while pre-loading package [{package}]", ex);
                return false;
            }
        }

        private static PackageUpgrader CheckPackageUpgrade(ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
        {
            // Don't do anything if source is a system (read-only) package for now
            // We only want to process local packages
            if (dependentPackage.IsSystem)
                return null;

            // Check if package might need upgrading
            var dependentPackagePreviousMinimumVersion = dependency.Version.MinVersion;
            if (dependentPackagePreviousMinimumVersion < dependencyPackage.Meta.Version)
            {
                // Find upgrader for given package
                // Note: If no upgrader is found, we assume it is still compatible with previous versions, so do nothing
                var packageUpgrader = AssetRegistry.GetPackageUpgrader(dependencyPackage.Meta.Name);
                if (packageUpgrader != null)
                {
                    // Check if upgrade is necessary
                    if (dependency.Version.MinVersion >= packageUpgrader.Attribute.UpdatedVersionRange.MinVersion)
                    {
                        return null;
                    }

                    // Check if upgrade is allowed
                    if (dependency.Version.MinVersion < packageUpgrader.Attribute.PackageMinimumVersion)
                    {
                        // Throw an exception, because the package update is not allowed and can't be done
                        throw new InvalidOperationException($"Upgrading package [{dependentPackage.Meta.Name}] to use [{dependencyPackage.Meta.Name}] from version [{dependentPackagePreviousMinimumVersion}] to [{dependencyPackage.Meta.Version}] is not supported");
                    }

                    log.Info($"Upgrading package [{dependentPackage.Meta.Name}] to use [{dependencyPackage.Meta.Name}] from version [{dependentPackagePreviousMinimumVersion}] to [{dependencyPackage.Meta.Version}] will be required");
                    return packageUpgrader;
                }
            }

            return null;
        }

        public class PendingPackageUpgrade
        {
            public readonly PackageUpgrader PackageUpgrader;
            public readonly PackageDependency Dependency;
            public readonly Package DependencyPackage;
            public readonly PackageVersionRange DependencyVersionBeforeUpgrade;

            public PendingPackageUpgrade(PackageUpgrader packageUpgrader, PackageDependency dependency, Package dependencyPackage)
            {
                PackageUpgrader = packageUpgrader;
                Dependency = dependency;
                DependencyPackage = dependencyPackage;
                DependencyVersionBeforeUpgrade = Dependency.Version;
            }
        }

        private static PackageAnalysisParameters GetPackageAnalysisParametersForLoad()
        {
            return new PackageAnalysisParameters()
            {
                IsPackageCheckDependencies = true,
                IsProcessingAssetReferences = true,
                IsLoggingAssetNotFoundAsError = true,
            };
        }
    }
}
