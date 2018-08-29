// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Evaluation;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Diagnostics;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Yaml;
using NuGet.ProjectModel;

namespace Xenko.Core.Assets
{
    public enum PackageState
    {
        /// <summary>
        /// Package has been deserialized. References and assets are not ready.
        /// </summary>
        Raw,

        /// <summary>
        /// Dependencies have all been resolved and are also in <see cref="DependenciesReady"/> state.
        /// </summary>
        DependenciesReady,

        /// <summary>
        /// Package upgrade has been failed (either error or denied by user).
        /// Dependencies are ready, but not assets.
        /// Should be manually switched back to DependenciesReady to try upgrade again.
        /// </summary>
        UpgradeFailed,

        /// <summary>
        /// Assembly references and assets have all been loaded.
        /// </summary>
        AssetsReady,
    }

    public enum PackageType
    {
        /// <summary>
        /// Typically from a NuGet folder.
        /// </summary>
        Standalone,

        ProjectOnly,
        ProjectAndPackage,
    }

    /// <summary>
    /// A package managing assets.
    /// </summary>
    [DataContract("Package")]
    [NonIdentifiableCollectionItems]
    [AssetDescription(PackageFileExtension)]
    [DebuggerDisplay("Id: {Id}, Name: {Meta.Name}, Version: {Meta.Version}, Assets [{Assets.Count}]")]
    [AssetFormatVersion("Assets", PackageFileVersion)]
    public sealed partial class Package : IIdentifiable, IFileSynchronizable, IAssetFinder
    {
        private const int PackageFileVersion = 4;

        private Guid id;

        // Note: Please keep this code in sync with Asset class
        /// <summary>
        /// Locks the unique identifier for further changes.
        /// </summary>
        internal bool IsIdLocked;

        [DataMemberIgnore]
        public VisualStudio.Project VSProject;

        private readonly List<UFile> filesToDelete = new List<UFile>();

        private PackageSession session;

        private UFile packagePath;
        private UFile previousPackagePath;
        private bool isDirty;
        private readonly Lazy<PackageUserSettings> settings;

        /// <summary>
        /// Occurs when package dirty changed occurred.
        /// </summary>
        public event DirtyFlagChangedDelegate<Package> PackageDirtyChanged;

        /// <summary>
        /// Occurs when an asset dirty changed occurred.
        /// </summary>
        public event DirtyFlagChangedDelegate<AssetItem> AssetDirtyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Package"/> class.
        /// </summary>
        public Package()
        {
            Id = Guid.NewGuid();
            // Initializse package with default versions (same code as in Asset..ctor())
            var defaultPackageVersion = AssetRegistry.GetCurrentFormatVersions(GetType());
            if (defaultPackageVersion != null)
            {
                SerializedVersion = new Dictionary<string, PackageVersion>(defaultPackageVersion);
            }

            Assets = new PackageAssetCollection(this);
            Bundles = new BundleCollection(this);
            IsDirty = true;
            settings = new Lazy<PackageUserSettings>(() => new PackageUserSettings(this));
        }

        // Note: Please keep this code in sync with Asset class
        /// <summary>
        /// Gets or sets the unique identifier of this package.
        /// </summary>
        /// <value>The identifier.</value>
        [DataMember(-10000)]
        [NonOverridable]
        [Display(Browsable = false)]
        public Guid Id
        {
            get
            {
                return id;
            }
            set
            {
                if (value != id && IsIdLocked)
                    throw new InvalidOperationException("Cannot change an Asset Object Id once it is locked by a package");

                id = value;
            }
        }

        // Note: Please keep this code in sync with Asset class
        /// <summary>
        /// Gets or sets the version number for this asset, used internally when migrating assets.
        /// </summary>
        /// <value>The version.</value>
        [DataMember(-8000, DataMemberMode.Assign)]
        [DataStyle(DataStyle.Compact)]
        [Display(Browsable = false)]
        [DefaultValue(null)]
        [NonOverridable]
        [NonIdentifiableCollectionItems]
        public Dictionary<string, PackageVersion> SerializedVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this package is a system package.
        /// </summary>
        /// <value><c>true</c> if this package is a system package; otherwise, <c>false</c>.</value>
        [DataMemberIgnore]
        public bool IsSystem { get; internal set; }

        /// <summary>
        /// Gets or sets the metadata associated with this package.
        /// </summary>
        /// <value>The meta.</value>
        [DataMember(10)]
        public PackageMeta Meta { get; set; } = new PackageMeta();

        /// <summary>
        /// Gets the local package dependencies used by this package (only valid for local references). Global dependencies
        /// are defined through the <see cref="Meta"/> property in <see cref="PackageMeta.Dependencies"/> 
        /// </summary>
        /// <value>The package local dependencies.</value>
        [DataMember(30)]
        public List<PackageReference> LocalDependencies { get; } = new List<PackageReference>();

        /// <summary>
        /// Gets the profiles.
        /// </summary>
        /// <value>The profiles.</value>
        [DataMember(50)]
        public PackageProfileCollection Profiles { get; } = new PackageProfileCollection();

        /// <summary>
        /// Gets or sets the list of folders that are explicitly created but contains no assets.
        /// </summary>
        [DataMember(70)]
        public List<UDirectory> ExplicitFolders { get; } = new List<UDirectory>();

        /// <summary>
        /// Gets the bundles defined for this package.
        /// </summary>
        /// <value>The bundles.</value>
        [DataMember(80)]
        public BundleCollection Bundles { get; private set; }

        /// <summary>
        /// Gets the template folders.
        /// </summary>
        /// <value>The template folders.</value>
        [DataMember(90)]
        public List<TemplateFolder> TemplateFolders { get; } = new List<TemplateFolder>();

        /// <summary>
        /// Asset references that needs to be compiled even if not directly or indirectly referenced (useful for explicit code references).
        /// </summary>
        [DataMember(100)]
        public RootAssetCollection RootAssets { get; private set; } = new RootAssetCollection();

        /// <summary>
        /// Type of package.
        /// </summary>
        [DataMemberIgnore]
        public PackageType Type { get; set; } = PackageType.Standalone;

        [DataMemberIgnore]
        public List<LockFileLibrary> Dependencies { get; } = new List<LockFileLibrary>();

        [DataMemberIgnore]
        public List<Package> LoadedDependencies { get; } = new List<Package>();

        /// <summary>
        /// Gets the loaded templates from the <see cref="TemplateFolders"/>
        /// </summary>
        /// <value>The templates.</value>
        [DataMemberIgnore]
        public List<TemplateDescription> Templates { get; } = new List<TemplateDescription>();

        /// <summary>
        /// Gets the assets stored in this package.
        /// </summary>
        /// <value>The assets.</value>
        [DataMemberIgnore]
        public PackageAssetCollection Assets { get; }

        /// <summary>
        /// Gets the temporary assets list loaded from disk before they are going into <see cref="Assets"/>.
        /// </summary>
        /// <value>The temporary assets.</value>
        [DataMemberIgnore]
        // TODO: turn that internal!
        public List<AssetItem> TemporaryAssets { get; } = new List<AssetItem>();

        [DataMemberIgnore]
        public List<LockFileLibrary> TopLevelDependencies { get; } = new List<LockFileLibrary>();

        /// <summary>
        /// Gets the path to the package file. May be null if the package was not loaded or saved.
        /// </summary>
        /// <value>The package path.</value>
        [DataMemberIgnore]
        public UFile FullPath
        {
            get
            {
                return packagePath;
            }
            set
            {
                SetPackagePath(value, true);
            }
        }

        [DataMemberIgnore]
        public UFile ProjectFullPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been modified since last saving.
        /// </summary>
        /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
        [DataMemberIgnore]
        public bool IsDirty
        {
            get
            {
                return isDirty;
            }
            set
            {
                var oldValue = isDirty;
                isDirty = value;
                OnPackageDirtyChanged(this, oldValue, value);
            }
        }

        [DataMemberIgnore]
        public PackageState State { get; set; }

        /// <summary>
        /// Gets the top directory of this package on the local disk.
        /// </summary>
        /// <value>The top directory.</value>
        [DataMemberIgnore]
        public UDirectory RootDirectory => FullPath?.GetParent();

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <value>The session.</value>
        /// <exception cref="System.InvalidOperationException">Cannot attach a package to more than one session</exception>
        [DataMemberIgnore]
        public PackageSession Session
        {
            get
            {
                return session;
            }
            internal set
            {
                if (value != null && session != null && !ReferenceEquals(session, value))
                {
                    throw new InvalidOperationException("Cannot attach a package to more than one session");
                }
                session = value;
                IsIdLocked = (session != null);
            }
        }

        /// <summary>
        /// Gets the package user settings. Usually stored in a .user file alongside the package. Lazily loaded on first time.
        /// </summary>
        /// <value>
        /// The package user settings.
        /// </value>
        [DataMemberIgnore]
        public PackageUserSettings UserSettings => settings.Value;

        /// <summary>
        /// Gets the list of assemblies loaded by this package.
        /// </summary>
        /// <value>
        /// The loaded assemblies.
        /// </value>
        [DataMemberIgnore]
        public List<PackageLoadedAssembly> LoadedAssemblies { get; } = new List<PackageLoadedAssembly>();

        /// <summary>
        /// Adds an existing project to this package.
        /// </summary>
        /// <param name="pathToMsproj">The path to msproj.</param>
        /// <returns>LoggerResult.</returns>
        public LoggerResult AddExistingProject(UFile pathToMsproj)
        {
            var logger = new LoggerResult();
            AddExistingProject(pathToMsproj, logger);
            return logger;
        }

        /// <summary>
        /// Adds an existing project to this package.
        /// </summary>
        /// <param name="pathToMsproj">The path to msproj.</param>
        /// <param name="logger">The logger.</param>
        public void AddExistingProject(UFile pathToMsproj, LoggerResult logger)
        {
            if (pathToMsproj == null) throw new ArgumentNullException(nameof(pathToMsproj));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (!pathToMsproj.IsAbsolute) throw new ArgumentException(@"Expecting relative path", nameof(pathToMsproj));

            try
            {
                // Load a project without specifying a platform to make sure we get the correct platform type
                var msProject = VSProjectHelper.LoadProject(pathToMsproj, platform: "NoPlatform");
                try
                {

                    var projectType = VSProjectHelper.GetProjectTypeFromProject(msProject);
                    if (!projectType.HasValue)
                    {
                        logger.Error("This project is not a project created with the editor");
                    }
                    else
                    {
                        var platformType = VSProjectHelper.GetPlatformTypeFromProject(msProject) ?? PlatformType.Shared;
                        var projectReference = new ProjectReference(VSProjectHelper.GetProjectGuid(msProject), pathToMsproj.MakeRelative(RootDirectory), projectType.Value);

                        // Add the ProjectReference only for the compatible profiles (same platform or no platform)
                        foreach (var profile in Profiles.Where(profile => platformType == profile.Platform))
                        {
                            profile.ProjectReferences.Add(projectReference);
                        }
                    }
                }
                finally
                {
                    msProject.ProjectCollection.UnloadAllProjects();
                    msProject.ProjectCollection.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Unexpected exception while loading project [{pathToMsproj}]", ex);
            }
        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst the current package and its dependencies.</remarks>
        public AssetItem FindAsset(AssetId assetId)
        {
            return this.GetPackagesWithDependencies().Select(p => p.Assets.Find(assetId)).NotNull().FirstOrDefault();
        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst the current package and its dependencies.</remarks>
        public AssetItem FindAsset(UFile location)
        {
            return this.GetPackagesWithDependencies().Select(p => p.Assets.Find(location)).NotNull().FirstOrDefault();
        }

        /// <inheritdoc />
        /// <remarks>Looks for the asset amongst the current package and its dependencies.</remarks>
        public AssetItem FindAssetFromProxyObject(object proxyObject)
        {
            var attachedReference = AttachedReferenceManager.GetAttachedReference(proxyObject);
            return attachedReference != null ? this.FindAsset(attachedReference) : null;
        }

        public UDirectory GetDefaultAssetFolder()
        {
            var sharedProfile = Profiles.FindSharedProfile();
            var folder = sharedProfile?.AssetFolders.FirstOrDefault();
            return folder?.Path ?? ("Assets/" + PackageProfile.SharedName);
        }

        /// <summary>
        /// Deep clone this package.
        /// </summary>
        /// <returns>The package cloned.</returns>
        public Package Clone()
        {
            // Use a new ShadowRegistry to copy override parameters
            // Clone this asset
            var package = AssetCloner.Clone(this); 
            package.FullPath = FullPath;
            foreach (var asset in Assets)
            {
                var newAsset = asset.Asset;
                var assetItem = new AssetItem(asset.Location, newAsset)
                {
                    SourceFolder = asset.SourceFolder,
                    SourceProject = asset.SourceProject
                };
                package.Assets.Add(assetItem);
            }
            return package;
        }

        /// <summary>
        /// Sets the package path.
        /// </summary>
        /// <param name="newPath">The new path.</param>
        /// <param name="copyAssets">if set to <c>true</c> assets will be copied relatively to the new location.</param>
        public void SetPackagePath(UFile newPath, bool copyAssets = true)
        {
            var previousPath = packagePath;
            var previousRootDirectory = RootDirectory;
            packagePath = newPath;
            if (packagePath != null && !packagePath.IsAbsolute)
            {
                packagePath = UPath.Combine(Environment.CurrentDirectory, packagePath);
            }

            if (copyAssets && packagePath != previousPath)
            {
                // Update source folders
                var currentRootDirectory = RootDirectory;
                if (previousRootDirectory != null && currentRootDirectory != null)
                {
                    foreach (var profile in Profiles)
                    {
                        foreach (var sourceFolder in profile.AssetFolders)
                        {
                            if (sourceFolder.Path.IsAbsolute)
                            {
                                var relativePath = sourceFolder.Path.MakeRelative(previousRootDirectory);
                                sourceFolder.Path = UPath.Combine(currentRootDirectory, relativePath);
                            }
                        }
                    }
                }

                foreach (var asset in Assets)
                {
                    asset.IsDirty = true;
                }
                IsDirty = true;
            }
        }

        internal void OnPackageDirtyChanged(Package package, bool oldValue, bool newValue)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            PackageDirtyChanged?.Invoke(package, oldValue, newValue);
        }

        internal void OnAssetDirtyChanged(AssetItem asset, bool oldValue, bool newValue)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            AssetDirtyChanged?.Invoke(asset, oldValue, newValue);
        }
        
        /// <summary>
        /// Saves this package and all dirty assets. See remarks.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <exception cref="System.ArgumentNullException">log</exception>
        /// <remarks>When calling this method directly, it does not handle moving assets between packages.
        /// Call <see cref="PackageSession.Save" /> instead.</remarks>
        public void Save(ILogger log, PackageSaveParameters saveParameters = null)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            if (FullPath == null)
            {
                log.Error(this, null, AssetMessageCode.PackageCannotSave, "null");
                return;
            }

            if (Type == PackageType.ProjectOnly)
                return;

            saveParameters = saveParameters ?? PackageSaveParameters.Default();

            // Use relative paths when saving
            var analysis = new PackageAnalysis(this, new PackageAnalysisParameters()
            {
                SetDirtyFlagOnAssetWhenFixingUFile = false,
                ConvertUPathTo = UPathType.Relative,
                IsProcessingUPaths = true,
            });
            analysis.Run(log);

            var assetsFiltered = false;
            try
            {
                // Update source folders
                UpdateSourceFolders(Assets);

                if (IsDirty)
                {
                    List<UFile> filesToDeleteLocal;
                    lock (filesToDelete)
                    {
                        filesToDeleteLocal = filesToDelete.ToList();
                        filesToDelete.Clear();
                    }

                    try
                    {
                        AssetFileSerializer.Save(FullPath, this, null);

                        // Move the package if the path has changed
                        if (previousPackagePath != null && previousPackagePath != packagePath)
                        {
                            filesToDeleteLocal.Add(previousPackagePath);
                        }
                        previousPackagePath = packagePath;

                        IsDirty = false;
                    }
                    catch (Exception ex)
                    {
                        log.Error(this, null, AssetMessageCode.PackageCannotSave, ex, FullPath);
                        return;
                    }
                    
                    // Delete obsolete files
                    foreach (var file in filesToDeleteLocal)
                    {
                        if (File.Exists(file.FullPath))
                        {
                            try
                            {
                                File.Delete(file.FullPath);
                            }
                            catch (Exception ex)
                            {
                                log.Error(this, null, AssetMessageCode.AssetCannotDelete, ex, file.FullPath);
                            }
                        }
                    }
                }

                //batch projects
                var vsProjs = new Dictionary<string, Project>();

                foreach (var asset in Assets)
                {
                    if (asset.IsDirty)
                    {
                        if (saveParameters.AssetFilter?.Invoke(asset) ?? true)
                        {
                            SaveSingleAsset_NoUpdateSourceFolder(asset, log);
                        }
                        else
                        {
                            assetsFiltered = true;
                        }
                    }

                    // Add new files to .csproj
                    var projectAsset = asset.Asset as IProjectAsset;
                    if (projectAsset != null)
                    {
                        var projectFullPath = asset.SourceProject;
                        var projectInclude = asset.GetProjectInclude();

                        Project project;
                        if (!vsProjs.TryGetValue(projectFullPath, out project))
                        {
                            project = VSProjectHelper.LoadProject(projectFullPath);
                            vsProjs.Add(projectFullPath, project);
                        }

                        //check if the item is already there, this is possible when saving the first time when creating from a template
                        if (project.Items.All(x => x.EvaluatedInclude != projectInclude))
                        {
                            var generatorAsset = projectAsset as IProjectFileGeneratorAsset;
                            if (generatorAsset != null)
                            {
                                var generatedInclude = asset.GetGeneratedInclude();

                                project.AddItem("None", projectInclude,
                                    new List<KeyValuePair<string, string>>
                                    {
                                    new KeyValuePair<string, string>("Generator", generatorAsset.Generator),
                                    new KeyValuePair<string, string>("LastGenOutput", new UFile(generatedInclude).GetFileName())
                                    });

                                project.AddItem("Compile", generatedInclude,
                                    new List<KeyValuePair<string, string>>
                                    {
                                    new KeyValuePair<string, string>("AutoGen", "True"),
                                    new KeyValuePair<string, string>("DesignTime", "True"),
                                    new KeyValuePair<string, string>("DesignTimeSharedInput", "True"),
                                    new KeyValuePair<string, string>("DependentUpon", new UFile(projectInclude).GetFileName())
                                    });
                            }
                            else
                            {
                                // Note: if project has auto items, no need to add it
                                if (string.Compare(project.GetPropertyValue("EnableDefaultCompileItems"), "true", true, CultureInfo.InvariantCulture) != 0)
                                    project.AddItem("Compile", projectInclude);
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

                // If some assets were filtered out, Assets is still dirty
                Assets.IsDirty = assetsFiltered;
            }
            finally
            {
                // Rollback all relative UFile to absolute paths
                analysis.Parameters.ConvertUPathTo = UPathType.Absolute;
                analysis.Run();
            }
        }

        public bool SaveSingleAsset(AssetItem asset, ILogger log)
        {
            // Make sure AssetItem.SourceFolder/Project are generated if they were null
            asset.UpdateSourceFolders();
            return SaveSingleAsset_NoUpdateSourceFolder(asset, log);
        }

        private bool SaveSingleAsset_NoUpdateSourceFolder(AssetItem asset, ILogger log)
        {
            var assetPath = asset.FullPath;

            try
            {
                // Handle the ProjectSourceCodeAsset differently then regular assets in regards of Path
                var projectAsset = asset.Asset as IProjectAsset;
                if (projectAsset != null)
                {
                    assetPath = asset.FullPath;
                }

                // Inject a copy of the base into the current asset when saving
                AssetFileSerializer.Save((string)assetPath, (object)asset.Asset, (AttachedYamlAssetMetadata)asset.YamlMetadata, log);

                // Save generated asset (if necessary)
                var codeGeneratorAsset = asset.Asset as IProjectFileGeneratorAsset;
                if (codeGeneratorAsset != null)
                {
                    codeGeneratorAsset.SaveGeneratedAsset(asset);
                }

                asset.IsDirty = false;
            }
            catch (Exception ex)
            {
                log.Error(this, asset.ToReference(), AssetMessageCode.AssetCannotSave, ex, assetPath);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the package identifier from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// log
        /// or
        /// filePath
        /// </exception>
        public static Guid GetPackageIdFromFile(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            bool hasPackage = false;
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("!Package"))
                    {
                        hasPackage = true;
                    }

                    if (hasPackage && line.StartsWith("Id:"))
                    {
                        var id = line.Substring("Id:".Length).Trim();
                        return Guid.Parse(id);
                    }
                }
            }
            throw new IOException($"File {filePath} doesn't appear to be a valid package");
        }

        /// <summary>
        /// Loads only the package description but not assets or plugins.
        /// </summary>
        /// <param name="log">The log to receive error messages.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        /// <returns>A package.</returns>
        /// <exception cref="System.ArgumentNullException">log
        /// or
        /// filePath</exception>
        public static Package Load(ILogger log, string filePath, PackageLoadParameters loadParametersArg = null)
        {
            var package = LoadRaw(log, filePath);
            if (package != null)
            {
                if (!package.LoadAssembliesAndAssets(log, loadParametersArg))
                    package = null;
            }

            return package;
        }

        /// <summary>
        /// Performs first part of the loading sequence, by deserializing the package but without processing anything yet.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// log
        /// or
        /// filePath
        /// </exception>
        internal static Package LoadRaw(ILogger log, string filePath)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            filePath = FileUtility.GetAbsolutePath(filePath);

            if (!File.Exists(filePath))
            {
                log.Error($"Package file [{filePath}] was not found");
                return null;
            }

            try
            {
                var packageFile = new PackageLoadingAssetFile(filePath, Path.GetDirectoryName(filePath)) { CachedFileSize = filePath.Length };
                var context = new AssetMigrationContext(null, null, filePath, log);
                AssetMigration.MigrateAssetIfNeeded(context, packageFile, "Assets");

                var loadResult = packageFile.AssetContent != null
                    ? AssetFileSerializer.Load<Package>(new MemoryStream(packageFile.AssetContent), filePath, log)
                    : AssetFileSerializer.Load<Package>(filePath, log);
                var package = loadResult.Asset;
                package.FullPath = filePath;
                package.previousPackagePath = package.FullPath;
                package.IsDirty = packageFile.AssetContent != null || loadResult.AliasOccurred;

                return package;
            }
            catch (Exception ex)
            {
                log.Error($"Error while pre-loading package [{filePath}]", ex);
            }

            return null;
        }

        /// <summary>
        /// Second part of the package loading process, when references, assets and package analysis is done.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="log">The log.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        /// <returns></returns>
        internal bool LoadAssembliesAndAssets(ILogger log, PackageLoadParameters loadParametersArg)
        {
            return LoadAssemblies(log, loadParametersArg) && LoadAssets(log, loadParametersArg);
        }

        /// <summary>
        /// Load only assembly references
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="log">The log.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        /// <returns></returns>
        internal bool LoadAssemblies(ILogger log, PackageLoadParameters loadParametersArg)
        {
            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();

            try
            {
                // Load assembly references
                if (loadParameters.LoadAssemblyReferences)
                {
                    LoadAssemblyReferencesForPackage(log, loadParameters);
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error while pre-loading package [{FullPath}]", ex);

                return false;
            }
        }

        /// <summary>
        /// Load assets and perform package analysis.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="log">The log.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        /// <returns></returns>
        internal bool LoadAssets(ILogger log, PackageLoadParameters loadParametersArg)
        {
            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();

            try
            {
                // Load assets
                if (loadParameters.AutoLoadTemporaryAssets)
                {
                    LoadTemporaryAssets(log, loadParameters.AssetFiles, loadParameters.CancelToken, loadParameters.TemporaryAssetsInMsbuild, loadParameters.TemporaryAssetFilter);
                }

                // Convert UPath to absolute
                if (loadParameters.ConvertUPathToAbsolute)
                {
                    var analysis = new PackageAnalysis(this, new PackageAnalysisParameters()
                    {
                        ConvertUPathTo = UPathType.Absolute,
                        IsProcessingUPaths = true, // This is done already by Package.Load
                        SetDirtyFlagOnAssetWhenFixingAbsoluteUFile = true // When loading tag attributes that have an absolute file
                    });
                    analysis.Run(log);
                }

                // Load templates
                LoadTemplates(log);

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error while pre-loading package [{FullPath}]", ex);

                return false;
            }
        }

        public void ValidateAssets(bool alwaysGenerateNewAssetId, bool removeUnloadableObjects, ILogger log)
        {
            if (TemporaryAssets.Count == 0)
            {
                return;
            }

            try
            {
                // Make sure we are suspending notifications before updating all assets
                Assets.SuspendCollectionChanged();

                Assets.Clear();

                // Get generated output items
                var outputItems = new List<AssetItem>();

                // Create a resolver from the package
                var resolver = AssetResolver.FromPackage(this);
                resolver.AlwaysCreateNewId = alwaysGenerateNewAssetId;

                // Clean assets
                AssetCollision.Clean(this, TemporaryAssets, outputItems, resolver, true, removeUnloadableObjects);

                // Add them back to the package
                foreach (var item in outputItems)
                {
                    Assets.Add(item);

                    // Fix collection item ids
                    AssetCollectionItemIdHelper.GenerateMissingItemIds(item.Asset);
                    CollectionItemIdsAnalysis.FixupItemIds(item, log);

                    // Fix duplicate identifiable objects
                    var hasBeenModified = IdentifiableObjectAnalysis.Visit(item.Asset, true, log);
                    if (hasBeenModified)
                        item.IsDirty = true;
                }

                // Don't delete SourceCodeAssets as their files are handled by the package upgrader
                var dirtyAssets = outputItems.Where(o => o.IsDirty && !(o.Asset is SourceCodeAsset))
                    .Join(TemporaryAssets, o => o.Id, t => t.Id, (o, t) => t)
                    .ToList();
                // Dirty assets (except in system package) should be mark as deleted so that are properly saved again later.
                if (!IsSystem && dirtyAssets.Count > 0)
                {
                    IsDirty = true;

                    lock (filesToDelete)
                    {
                        filesToDelete.AddRange(dirtyAssets.Select(a => a.FullPath));
                    }
                }

                TemporaryAssets.Clear();
            }
            finally
            {
                // Restore notification on assets
                Assets.ResumeCollectionChanged();
            }
        }

        /// <summary>
        /// Refreshes this package from the disk by loading or reloading all assets.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="assetFiles">The asset files (loaded from <see cref="ListAssetFiles"/> if null).</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="listAssetsInMsbuild">Specifies if we need to evaluate MSBuild files for assets.</param>
        /// <param name="filterFunc">A function that will filter assets loading</param>
        /// <returns>A logger that contains error messages while refreshing.</returns>
        /// <exception cref="System.InvalidOperationException">Package RootDirectory is null
        /// or
        /// Package RootDirectory [{0}] does not exist.ToFormat(RootDirectory)</exception>
        public void LoadTemporaryAssets(ILogger log, List<PackageLoadingAssetFile> assetFiles = null, CancellationToken? cancelToken = null, bool listAssetsInMsbuild = true, Func<PackageLoadingAssetFile, bool> filterFunc = null)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            // If FullPath is null, then we can't load assets from disk, just return
            if (FullPath == null)
            {
                log.Warning("Fullpath not set on this package");
                return;
            }

            // Clears the assets already loaded and reload them
            TemporaryAssets.Clear();

            // List all package files on disk
            if (assetFiles == null)
            {
                assetFiles = ListAssetFiles(log, this, listAssetsInMsbuild, cancelToken);
                // Sort them by size (to improve concurrency during load)
                assetFiles.Sort(PackageLoadingAssetFile.FileSizeComparer.Default);
            }

            var progressMessage = $"Loading Assets from Package [{FullPath.GetFileName()}]";

            // Display this message at least once if the logger does not log progress (And it shouldn't in this case)
            var loggerResult = log as LoggerResult;
            if (loggerResult == null || !loggerResult.IsLoggingProgressAsInfo)
            {
                log.Verbose(progressMessage);
            }


            // Update step counter for log progress
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < assetFiles.Count; i++)
            {
                var assetFile = assetFiles[i];

                if (filterFunc != null && !filterFunc(assetFile))
                {
                    continue;
                }

                // Update the loading progress
                loggerResult?.Progress(progressMessage, i, assetFiles.Count);

                var task = cancelToken.HasValue ?
                    System.Threading.Tasks.Task.Factory.StartNew(() => LoadAsset(new AssetMigrationContext(this, assetFile.ToReference(), assetFile.FilePath.ToWindowsPath(), log), assetFile), cancelToken.Value) : 
                    System.Threading.Tasks.Task.Factory.StartNew(() => LoadAsset(new AssetMigrationContext(this, assetFile.ToReference(), assetFile.FilePath.ToWindowsPath(), log), assetFile));

                tasks.Add(task);
            }

            if (cancelToken.HasValue)
            {
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), cancelToken.Value);
            }
            else
            {
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            }

            // DEBUG
            // StaticLog.Info("[{0}] Assets files loaded in {1}", assetFiles.Count, clock.ElapsedMilliseconds);

            if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
            {
                log.Warning("Skipping loading assets. PackageSession.Load cancelled");
            }
        }

        private void LoadAsset(AssetMigrationContext context, PackageLoadingAssetFile assetFile)
        {
            var fileUPath = assetFile.FilePath;
            var sourceFolder = assetFile.SourceFolder;

            // Check if asset has been deleted by an upgrader
            if (assetFile.Deleted)
            {
                IsDirty = true;

                lock (filesToDelete)
                {
                    filesToDelete.Add(assetFile.FilePath);
                }

                // Don't create temporary assets for files deleted during package upgrading
                return;
            }

            // An exception can occur here, so we make sure that loading a single asset is not going to break 
            // the loop
            try
            {
                AssetMigration.MigrateAssetIfNeeded(context, assetFile, PackageStore.Instance.DefaultPackageName);

                // Try to load only if asset is not already in the package or assetRef.Asset is null
                var assetPath = assetFile.AssetLocation;

                var assetFullPath = fileUPath.ToWindowsPath();
                var assetContent = assetFile.AssetContent;

                bool aliasOccurred;
                AttachedYamlAssetMetadata yamlMetadata;
                var asset = LoadAsset(context.Log, assetFullPath, assetPath.ToWindowsPath(), assetContent, out aliasOccurred, out yamlMetadata);

                // Create asset item
                var assetItem = new AssetItem(assetPath, asset, this)
                {
                    IsDirty = assetContent != null || aliasOccurred,
                    SourceFolder = sourceFolder.MakeRelative(RootDirectory),
                    SourceProject = asset is IProjectAsset && assetFile.ProjectFile != null ? assetFile.ProjectFile : null,
                };
                yamlMetadata.CopyInto(assetItem.YamlMetadata);

                // Set the modified time to the time loaded from disk
                if (!assetItem.IsDirty)
                    assetItem.ModifiedTime = File.GetLastWriteTime(assetFullPath);

                // TODO: Let's review that when we rework import process
                // Not fixing asset import anymore, as it was only meant for upgrade
                // However, it started to make asset dirty, for ex. when we create a new texture, choose a file and reload the scene later
                // since there was no importer id and base.
                //FixAssetImport(assetItem);

                // Add to temporary assets
                lock (TemporaryAssets)
                {
                    TemporaryAssets.Add(assetItem);
                }
            }
            catch (Exception ex)
            {
                int row = 1;
                int column = 1;
                var yamlException = ex as YamlException;
                if (yamlException != null)
                {
                    row = yamlException.Start.Line + 1;
                    column = yamlException.Start.Column;
                }

                var assetReference = new AssetReference(AssetId.Empty, fileUPath.FullPath);
                context.Log.Error(this, assetReference, AssetMessageCode.AssetLoadingFailed, ex, fileUPath, ex.Message);
            }
        }

        /// <summary>
        /// Loads the assembly references that were not loaded before.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loadParametersArg">The load parameters argument.</param>
        public void UpdateAssemblyReferences(ILogger log, PackageLoadParameters loadParametersArg = null)
        {
            if (State < PackageState.DependenciesReady)
                return;

            var loadParameters = loadParametersArg ?? PackageLoadParameters.Default();
            LoadAssemblyReferencesForPackage(log, loadParameters);
        }

        /// <summary>
        /// Restore NuGet packages of all projects in this package.
        /// </summary>
        /// <param name="log">The log.</param>
        public void RestoreNugetPackages(ILogger log)
        {
            foreach (var profile in Profiles)
            {
                VSProjectHelper.RestoreNugetPackagesNonRecursive(log, profile.ProjectReferences.Select(projectReference => UPath.Combine(RootDirectory, projectReference.Location).ToWindowsPath())).Wait();
            }
        }

        private static Asset LoadAsset(ILogger log, string assetFullPath, string assetPath, byte[] assetContent, out bool assetDirty, out AttachedYamlAssetMetadata yamlMetadata)
        {
            var loadResult = assetContent != null
                ? AssetFileSerializer.Load<Asset>(new MemoryStream(assetContent), assetFullPath, log)
                : AssetFileSerializer.Load<Asset>(assetFullPath, log);

            assetDirty = loadResult.AliasOccurred;
            yamlMetadata = loadResult.YamlMetadata;

            // Set location on source code asset
            var sourceCodeAsset = loadResult.Asset as SourceCodeAsset;
            if (sourceCodeAsset != null)
            {
                // Use an id generated from the location instead of the default id
                sourceCodeAsset.Id = SourceCodeAsset.GenerateIdFromLocation(assetPath);
            }

            return loadResult.Asset;
        }

        private void LoadAssemblyReferencesForPackage(ILogger log, PackageLoadParameters loadParameters)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (loadParameters == null) throw new ArgumentNullException(nameof(loadParameters));
            var assemblyContainer = loadParameters.AssemblyContainer ?? AssemblyContainer.Default;

            if (ProjectFullPath != null)
            {
                // Check if already loaded
                // TODO: More advanced cases: unload removed references, etc...
                var projectReference = new ProjectReference(Id, ProjectFullPath, ProjectType.Library);
                if (LoadedAssemblies.Any(x => x.ProjectReference == projectReference))
                    return;

                string assemblyPath = null;
                var fullProjectLocation = ProjectFullPath.ToWindowsPath();

                try
                {
                    var forwardingLogger = new ForwardingLoggerResult(log);
                    assemblyPath = VSProjectHelper.GetOrCompileProjectAssembly(Session?.SolutionPath, fullProjectLocation, forwardingLogger, "Build", loadParameters.AutoCompileProjects, loadParameters.BuildConfiguration, extraProperties: loadParameters.ExtraCompileProperties, onlyErrors: true);
                    if (String.IsNullOrWhiteSpace(assemblyPath))
                    {
                        log.Error($"Unable to locate assembly reference for project [{fullProjectLocation}]");
                        return;
                    }

                    var loadedAssembly = new PackageLoadedAssembly(projectReference, assemblyPath);
                    LoadedAssemblies.Add(loadedAssembly);

                    if (!File.Exists(assemblyPath) || forwardingLogger.HasErrors)
                    {
                        log.Error($"Unable to build assembly reference [{assemblyPath}]");
                        return;
                    }

                    var assembly = assemblyContainer.LoadAssemblyFromPath(assemblyPath, log);
                    if (assembly == null)
                    {
                        log.Error($"Unable to load assembly reference [{assemblyPath}]");
                    }

                    loadedAssembly.Assembly = assembly;

                    if (assembly != null)
                    {
                        // Register assembly in the registry
                        AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Unexpected error while loading project [{fullProjectLocation}] or assembly reference [{assemblyPath}]", ex);
                }
            }
        }

        /// <summary>
        /// In case <see cref="AssetItem.SourceFolder"/> or <see cref="AssetItem.SourceProject"/> were null, generates them.
        /// </summary>
        internal void UpdateSourceFolders(IReadOnlyCollection<AssetItem> assets)
        {
            // If there are not assets, we don't need to update or create an asset folder
            if (assets.Count == 0)
            {
                return;
            }

            // Make sure there is a shared profile at least
            var sharedProfile = Profiles.FindSharedProfile();
            if (sharedProfile == null)
            {
                sharedProfile = PackageProfile.NewShared();
                Profiles.Add(sharedProfile);
            }

            // Use by default the first asset folders if not defined on the asset item
            var defaultFolder = sharedProfile.AssetFolders.Count > 0 ? sharedProfile.AssetFolders.First().Path : UDirectory.This;
            var assetFolders = new HashSet<UDirectory>(GetDistinctAssetFolderPaths());
            foreach (var asset in assets)
            {
                if (asset.Asset is IProjectAsset)
                {
                    if (asset.SourceFolder == null)
                    {
                        if (ProjectFullPath != null)
                        {
                            asset.SourceProject = ProjectFullPath;
                            asset.SourceFolder = asset.SourceProject.GetFullDirectory().GetParent().MakeRelative(RootDirectory);
                            asset.IsDirty = true;
                        }
                    }
                }
                else
                {
                    if (asset.SourceFolder == null)
                    {
                        asset.SourceFolder = defaultFolder.IsAbsolute ? defaultFolder.MakeRelative(RootDirectory) : defaultFolder;
                        asset.IsDirty = true;
                    }

                    var assetFolderAbsolute = UPath.Combine(RootDirectory, asset.SourceFolder);
                    if (!assetFolders.Contains(assetFolderAbsolute))
                    {
                        assetFolders.Add(assetFolderAbsolute);
                        sharedProfile.AssetFolders.Add(new AssetFolder(assetFolderAbsolute));
                        IsDirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// Loads the templates.
        /// </summary>
        /// <param name="log">The log result.</param>
        private void LoadTemplates(ILogger log)
        {
            foreach (var templateDir in TemplateFolders)
            {
                foreach (var filePath in templateDir.Files)
                {
                    try
                    {
                        var file = new FileInfo(filePath);
                        if (!file.Exists)
                        {
                            log.Warning($"Template [{file}] does not exist ");
                            continue;
                        }

                        var templateDescription = YamlSerializer.Default.Load<TemplateDescription>(file.FullName);
                        templateDescription.FullPath = file.FullName;
                        Templates.Add(templateDescription);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error while loading template from [{filePath}]", ex);
                    }
                }
            }
        }

        private List<UDirectory> GetDistinctAssetFolderPaths()
        {
            var existingAssetFolders = new List<UDirectory>();
            foreach (var profile in Profiles)
            {
                foreach (var folder in profile.AssetFolders)
                {
                    var folderPath = RootDirectory != null ? UPath.Combine(RootDirectory, folder.Path) : folder.Path;
                    if (!existingAssetFolders.Contains(folderPath))
                    {
                        existingAssetFolders.Add(folderPath);
                    }
                }
            }
            return existingAssetFolders;
        }

        public static List<PackageLoadingAssetFile> ListAssetFiles(ILogger log, Package package, bool listAssetsInMsbuild, CancellationToken? cancelToken)
        {
            var listFiles = new List<PackageLoadingAssetFile>();

            // TODO Check how to handle refresh correctly as a public API
            if (package.RootDirectory == null)
            {
                throw new InvalidOperationException("Package RootDirectory is null");
            }

            if (!Directory.Exists(package.RootDirectory))
            {
                return listFiles;
            }

            var sharedProfile = package.Profiles.FindSharedProfile();
            var hasProject = sharedProfile != null && sharedProfile.ProjectReferences.Count > 0;

            // Iterate on each source folders
            foreach (var sourceFolder in package.GetDistinctAssetFolderPaths())
            {
                // Lookup all files
                foreach (var directory in FileUtility.EnumerateDirectories(sourceFolder, SearchDirection.Down))
                {
                    var files = directory.GetFiles();

                    foreach (var filePath in files)
                    {
                        // Don't load package via this method
                        if (filePath.FullName.EndsWith(PackageFileExtension))
                        {
                            continue;
                        }

                        // Make an absolute path from the root of this package
                        var fileUPath = new UFile(filePath.FullName);
                        if (fileUPath.GetFileExtension() == null)
                        {
                            continue;
                        }

                        // If this kind of file an asset file?
                        var ext = fileUPath.GetFileExtension();

                        //make sure to add default shaders in this case, since we don't have a csproj for them
                        if (AssetRegistry.IsProjectCodeGeneratorAssetFileExtension(ext) && !hasProject)
                        {
                            listFiles.Add(new PackageLoadingAssetFile(fileUPath, sourceFolder) { CachedFileSize = filePath.Length });
                            continue;
                        }

                        //project source code assets follow the csproj pipeline
                        if (!AssetRegistry.IsAssetFileExtension(ext) || AssetRegistry.IsProjectAssetFileExtension(ext))
                        {
                            continue;
                        }

                        listFiles.Add(new PackageLoadingAssetFile(fileUPath, sourceFolder) { CachedFileSize = filePath.Length });
                    }
                }
            }

            //find also assets in the csproj
            if (listAssetsInMsbuild)
            {
                FindAssetsInProject(listFiles, package);
            }

            return listFiles;
        }

        public static List<UFile> FindAssetsInProject(string projectFullPath, out string nameSpace)
        {
            var realFullPath = new UFile(projectFullPath);
            var project = VSProjectHelper.LoadProject(realFullPath);
            var dir = new UDirectory(realFullPath.GetFullDirectory());

            nameSpace = project.GetPropertyValue("RootNamespace");
            if (nameSpace == string.Empty)
                nameSpace = null;

            var result = project.Items.Where(x => (x.ItemType == "Compile" || x.ItemType == "None") && string.IsNullOrEmpty(x.GetMetadataValue("AutoGen")))
                .Select(x => new UFile(x.EvaluatedInclude)).Where(x => AssetRegistry.IsProjectAssetFileExtension(x.GetFileExtension()))
                .Select(projectItem => UPath.Combine(dir, projectItem))
                // avoid duplicates otherwise it might save a single file as separte file with renaming
                // had issues with case such as Effect.xksl being registered twice (with glob pattern) and being saved as Effect.xksl and Effect (2).xksl
                .Distinct()
                .ToList();

            project.ProjectCollection.UnloadAllProjects();
            project.ProjectCollection.Dispose();

            return result;
        }

        private static void FindAssetsInProject(ICollection<PackageLoadingAssetFile> list, Package package)
        {
            if (package.IsSystem) return;

            var profile = package.Profiles.FindSharedProfile();
            if (profile == null) return;

            foreach (var libs in profile.ProjectReferences.Where(x => x.Type == ProjectType.Library))
            {
                var realFullPath = UPath.Combine(package.RootDirectory, libs.Location);
                string defaultNamespace;
                var codePaths = FindAssetsInProject(realFullPath, out defaultNamespace);
                libs.RootNamespace = defaultNamespace;
                var dir = new UDirectory(realFullPath.GetFullDirectory());
                var parentDir = dir.GetParent();

                foreach (var codePath in codePaths)
                {
                    list.Add(new PackageLoadingAssetFile(codePath, parentDir, realFullPath));
                }
            }
        }
    }
}
