// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core.Assets.Analysis;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization.Contents;
using Stride.Core.VisualStudio;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Serialization;

namespace Stride.Core.Assets;

/// <summary>
/// A registry for various content associated with assets.
/// </summary>
public static class AssetRegistry
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Assets.Registry");

    private static readonly Dictionary<Type, string?> RegisteredDefaultAssetExtension = [];
    private static readonly HashSet<Type> AssetTypes = [];
    private static readonly HashSet<Type> RegisteredPackageSessionAnalysisTypes = [];
    private static readonly List<IAssetImporter> RegisteredImportersInternal = [];
    private static readonly Dictionary<Type, SortedList<string, PackageVersion>> RegisteredFormatVersions = [];
    private static readonly HashSet<Type> AlwaysMarkAsRootAssetTypes = [];
    private static readonly Dictionary<KeyValuePair<Type, string>, AssetUpgraderCollection> RegisteredAssetUpgraders = [];
    private static readonly Dictionary<string, Type> RegisteredAssetFileExtensions = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly Dictionary<string, PackageUpgrader> RegisteredPackageUpgraders = [];
    private static readonly HashSet<Assembly> RegisteredEngineAssemblies = [];
    private static readonly HashSet<Assembly> RegisteredAssetAssemblies = [];
    private static readonly HashSet<IYamlSerializableFactory> RegisteredSerializerFactories = [];
    private static readonly List<IDataCustomVisitor> RegisteredDataVisitNodes = [];
    private static readonly Dictionary<string, IAssetFactory<Asset>> RegisteredAssetFactories = [];
    private static readonly Dictionary<Type, List<Type>?> RegisteredContentTypes = []; // FIXME values are always null
    private static readonly Dictionary<Type, List<Type>> AssignableToContent = [];
    private static readonly Dictionary<Type, List<Type>> ContentToAssetTypes = [];
    private static readonly Dictionary<Type, Type> AssetToContentTypes = [];

    // Global lock used to secure the registry with threads
    private static readonly object RegistryLock = new();

    /// <summary>
    /// Gets the list of engine assemblies currently registered.
    /// </summary>
    public static IEnumerable<Assembly> EngineAssemblies { get { lock (RegistryLock) { return [.. RegisteredEngineAssemblies]; } } }

    /// <summary>
    /// Gets the list of asset assemblies currently registered.
    /// </summary>
    public static IEnumerable<Assembly> AssetAssemblies { get { lock (RegistryLock) { return [.. RegisteredAssetAssemblies]; } } }

    /// <summary>
    /// Gets the supported platforms.
    /// </summary>
    /// <value>The supported platforms.</value>
    public static SolutionPlatformCollection SupportedPlatforms { get; }

    /// <summary>
    /// Gets an enumeration of registered importers.
    /// </summary>
    /// <value>The registered importers.</value>
    public static IEnumerable<IAssetImporter> RegisteredImporters { get { lock (RegistryLock) { return RegisteredImportersInternal; } } }

    /// <summary>
    /// Registers the supported platforms.
    /// </summary>
    /// <param name="platforms">The platforms.</param>
    /// <exception cref="System.ArgumentNullException">platforms</exception>
    public static void RegisterSupportedPlatforms(List<SolutionPlatform> platforms)
    {
        ArgumentNullException.ThrowIfNull(platforms);
        if (SupportedPlatforms.Count > 0) throw new InvalidOperationException("Cannot register new platforms. RegisterSupportedPlatforms can only be called once");
        SupportedPlatforms.AddRange(platforms);
    }

    /// <summary>
    /// Determines whether the file is an asset file type.
    /// </summary>
    /// <param name="extension">The file.</param>
    /// <returns><c>true</c> if [is asset file file] [the specified file]; otherwise, <c>false</c>.</returns>
    public static bool IsAssetFileExtension(string? extension)
    {
        if (extension == null) return false;
        lock (RegistryLock)
        {
            return RegisteredAssetFileExtensions.ContainsKey(extension);
        }
    }

    /// <summary>
    /// Gets the asset type from the extension. If no asset type is found, return null.
    /// </summary>
    /// <param name="extension">The extension of the asset file.</param>
    /// <returns>Type of the associated asset or null if not found.</returns>
    public static Type? GetAssetTypeFromFileExtension(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        lock (RegistryLock)
        {
            RegisteredAssetFileExtensions.TryGetValue(extension, out var result);
            return result;
        }
    }

    public static bool IsProjectCodeGeneratorAssetFileExtension(string? extension)
    {
        if (extension == null) return false;
        lock (RegisteredAssetFileExtensions)
        {
            var valid = RegisteredAssetFileExtensions.ContainsKey(extension);
            if (valid)
            {
                var type = RegisteredDefaultAssetExtension.Where(x => x.Value == extension).Select(x => x.Key).FirstOrDefault();
                if (type != null)
                {
                    return typeof(IProjectFileGeneratorAsset).IsAssignableFrom(type);
                }
            }
            return false;
        }
    }

    public static bool IsProjectAssetFileExtension(string? extension)
    {
        if (extension == null) return false;
        lock (RegisteredAssetFileExtensions)
        {
            var valid = RegisteredAssetFileExtensions.ContainsKey(extension);
            if (valid)
            {
                var type = RegisteredDefaultAssetExtension.Where(x => x.Value == extension).Select(x => x.Key).FirstOrDefault();
                if (type != null)
                {
                    return typeof(IProjectAsset).IsAssignableFrom(type);
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the default file associated with an asset.
    /// </summary>
    /// <param name="assetType">The type.</param>
    /// <returns>System.String.</returns>
    public static string? GetDefaultExtension(Type assetType)
    {
        IsAssetOrPackageType(assetType, true);
        lock (RegistryLock)
        {
            RegisteredDefaultAssetExtension.TryGetValue(assetType, out var extension);
            return extension;
        }
    }

    /// <summary>
    /// Gets the current format version of an asset.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The current format version of this asset.</returns>
    public static SortedList<string, PackageVersion>? GetCurrentFormatVersions(Type assetType)
    {
        IsAssetOrPackageType(assetType, true);
        lock (RegistryLock)
        {
            RegisteredFormatVersions.TryGetValue(assetType, out var versions);
            return versions;
        }
    }

    /// <summary>
    /// Gets the <see cref="AssetUpgraderCollection"/> of an asset type, if available.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <param name="dependencyName">The dependency name.</param>
    /// <returns>The <see cref="AssetUpgraderCollection"/> of an asset type if available, or <c>null</c> otherwise.</returns>
    public static AssetUpgraderCollection? GetAssetUpgraders(Type assetType, string dependencyName)
    {
        IsAssetOrPackageType(assetType, true);
        lock (RegistryLock)
        {
            RegisteredAssetUpgraders.TryGetValue(new KeyValuePair<Type, string>(assetType, dependencyName), out var upgraders);
            return upgraders;
        }
    }

    public static PackageUpgrader? GetPackageUpgrader(string packageName)
    {
        lock (RegistryLock)
        {
            RegisteredPackageUpgraders.TryGetValue(packageName, out var upgrader);
            return upgrader;
        }
    }

    /// <summary>
    /// Gets the default file associated with an asset.
    /// </summary>
    /// <typeparam name="T">Type of the asset.</typeparam>
    /// <returns>System.String.</returns>
    public static string? GetDefaultExtension<T>() where T : Asset
    {
        return GetDefaultExtension(typeof(T));
    }

    public static IEnumerable<Type> GetPackageSessionAnalysisTypes()
    {
        lock (RegistryLock)
        {
            return RegisteredPackageSessionAnalysisTypes;
        }
    }

    public static IAssetFactory<Asset>? GetAssetFactory(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        lock (RegistryLock)
        {
            RegisteredAssetFactories.TryGetValue(typeName, out var factory);
            return factory;
        }
    }

    public static IEnumerable<IAssetFactory<Asset>> GetAllAssetFactories()
    {
        lock (RegistryLock)
        {
            return [.. RegisteredAssetFactories.Values];
        }
    }

    public static bool IsAssetTypeAlwaysMarkAsRoot(Type type)
    {
        lock (AlwaysMarkAsRootAssetTypes)
        {
            return AlwaysMarkAsRootAssetTypes.Contains(type);
        }
    }

    /// <summary>
    /// Returns an array of asset types that are non-abstract and public.
    /// </summary>
    /// <returns>An array of <see cref="Type"/> elements.</returns>
    public static Type[] GetPublicTypes()
    {
        lock (RegistryLock)
        {
            return [.. AssetTypes];
        }
    }

    public static IList<Type> GetContentTypes()
    {
        lock (RegistryLock)
        {
            return [.. RegisteredContentTypes.Keys];
        }
    }

    public static Type? GetContentType(Type assetType)
    {
        IsAssetType(assetType, true);
        lock (RegistryLock)
        {
            var currentType = assetType;
            while (currentType != null)
            {
                if (AssetToContentTypes.TryGetValue(assetType, out var contentType))
                    return contentType;

                currentType = currentType.BaseType;
            }
            return null;
        }
    }

    /// <summary>
    /// Whether a property of this type can be set to asset content, outputs the assets it can hold if true
    /// </summary>
    /// <remarks>
    /// The difference between this one and <see cref="CanPropertyHandleContent"/> is that this one returns the asset types,
    /// meaning the types that would be compiled into 'content' to be used at runtime.
    /// This means that the types contained in <see cref="AssetTypes"/> are not directly assignable to <paramref name="propertyType"/>
    /// </remarks>
    public static bool CanPropertyHandleAssets(Type propertyType, [MaybeNullWhen(false)] out HashSet<Type> assetTypes)
    {
        lock (RegistryLock)
        {
            if (typeof(AssetReference).IsAssignableFrom(propertyType))
            {
                assetTypes = [.. GetAssetTypes(propertyType)];
                return true;
            }

            if (UrlReferenceBase.TryGetAssetType(propertyType, out var assetType))
            {
                propertyType = assetType;
            }

            if (AssignableToContent.TryGetValue(propertyType, out var concreteContentTypes))
            {
                assetTypes = [];
                foreach (var item in concreteContentTypes)
                {
                    if (ContentToAssetTypes.TryGetValue(item, out var values))
                    {
                        foreach (var type in values)
                        {
                            assetTypes.Add(type);
                        }
                    }
                }
                return true;
            }

            assetTypes = null;
            return false;
        }
    }

    /// <summary>
    /// Whether a property of this type can be set to content, outputs the content types it can hold if true
    /// </summary>
    /// <remarks>
    /// The difference between this one and <see cref="CanPropertyHandleAssets"/> is that this one returns the content types,
    /// meaning the concrete types that are types that would be compiled into 'content' to be used at runtime.
    /// The types contained in <see cref="contentTypes"/> are not guaranteed to be assignable to <paramref name="propertyType"/>
    /// </remarks>
    public static bool CanPropertyHandleContent(Type propertyType, [MaybeNullWhen(false)] out IReadOnlyList<Type> contentTypes)
    {
        lock (RegistryLock)
        {
            if (UrlReferenceBase.TryGetAssetType(propertyType, out var assetType))
            {
                propertyType = assetType;
            }

            if (AssignableToContent.TryGetValue(propertyType, out var concreteContentTypes))
            {
                contentTypes = concreteContentTypes;
                return true;
            }

            contentTypes = null;
            return false;
        }
    }

    public static IReadOnlyList<Type> GetAssetTypes(Type contentType)
    {
        lock (RegistryLock)
        {
            var currentType = contentType;
            if (UrlReferenceBase.TryGetAssetType(contentType, out var assetType))
            {
                currentType = assetType;
            }
            return ContentToAssetTypes.TryGetValue(currentType, out var assetTypes) ? new List<Type>(assetTypes) : [];
        }
    }

    /// <summary>
    /// Finds the importer associated with an asset by the file of the file to import.
    /// </summary>
    /// <param name="file">The file to import.</param>
    /// <returns>An instance of the importer of null if not found.</returns>
    public static IEnumerable<IAssetImporter> FindImporterForFile(string file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Impl();

        IEnumerable<IAssetImporter> Impl()
        {
            lock (RegistryLock)
            {
                foreach (var importer in RegisteredImportersInternal)
                {
                    if (importer.IsSupportingFile(file))
                    {
                        yield return importer;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds an importer by its id.
    /// </summary>
    /// <param name="importerId">The importer identifier.</param>
    /// <returns>An instance of the importer of null if not found.</returns>
    public static IAssetImporter? FindImporterById(Guid importerId)
    {
        lock (RegistryLock)
        {
            return RegisteredImportersInternal.FirstOrDefault(t => t.Id == importerId);
        }
    }

    /// <summary>
    /// Registers a <see cref="IAssetImporter" /> for the specified asset type.
    /// </summary>
    /// <param name="importer">The importer.</param>
    /// <exception cref="System.ArgumentNullException">importer</exception>
    public static void RegisterImporter(IAssetImporter importer)
    {
        ArgumentNullException.ThrowIfNull(importer);

        // Register this importer
        lock (RegistryLock)
        {
            var existingImporter = FindImporterById(importer.Id);
            if (existingImporter != null)
            {
                RegisteredImportersInternal.Remove(existingImporter);
            }

            RegisteredImportersInternal.Add(importer);
            RegisteredImportersInternal.Sort( (left, right) => left.Order.CompareTo(right.Order));
        }
    }

    public static IEnumerable<IDataCustomVisitor> GetDataVisitNodes()
    {
        lock (RegistryLock)
        {
            return RegisteredDataVisitNodes;
        }
    }

    /// <summary>
    /// Can a property of this type hold content
    /// </summary>
    /// <remarks>
    /// You may want to use <see cref="IsExactContentType"/> instead if you're checking if this type is a concrete content type
    /// </remarks>
    public static bool CanBeAssignedToContentTypes([Annotations.NotNull] Type type, bool checkIsUrlType)
    {
        lock (RegistryLock)
        {
            if (checkIsUrlType && (type.IsAssignableTo(typeof(AssetReference)) || UrlReferenceBase.IsUrlReferenceType(type)))
                return true;

            return AssignableToContent.ContainsKey(type);
        }
    }

    /// <summary>
    /// Is this type a concrete content type, or derives from a concrete content type
    /// </summary>
    /// <remarks>
    /// You may want to use <see cref="CanBeAssignedToContentTypes"/> instead if you're checking
    /// whether a property of this type could hold at least one type of content
    /// </remarks>
    public static bool IsExactContentType([NotNullWhen(true)] Type? type)
    {
        lock (RegistryLock)
        {
            var currentType = type;
            while (currentType != null)
            {
                if (RegisteredContentTypes.ContainsKey(currentType))
                    return true;

                currentType = currentType.BaseType;
            }
            return false;
        }
    }

    private static void RegisterEngineAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (RegistryLock)
        {
            if (!RegisteredEngineAssemblies.Add(assembly))
                return;

            var assemblyScanTypes = AssemblyRegistry.GetScanTypes(assembly);
            if (assemblyScanTypes != null)
            {
                // Process reference types.
                if (assemblyScanTypes.Types.TryGetValue(typeof(ReferenceSerializerAttribute), out var referenceTypes))
                {
                    foreach (var type in referenceTypes)
                    {
                        RegisteredContentTypes.Add(type, null);

                        AddToAssignable(type, type);

                        foreach (var @interface in type.GetInterfaces())
                            AddToAssignable(@interface, type);

                        for (var baseType = type; baseType != null; baseType = baseType.BaseType)
                        {
                            if (!baseType.IsAbstract)
                                continue;

                            AddToAssignable(baseType, type);
                        }

                        static void AddToAssignable(Type key, Type value)
                        {
                            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(AssignableToContent, key, out _);
                            list ??= [];
                            list.Add(value);
                        }
                    }
                }
            }
        }
    }

    private static void UnregisterEngineAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (RegistryLock)
        {
            if (!RegisteredEngineAssemblies.Contains(assembly))
                return;

            RegisteredEngineAssemblies.Remove(assembly);

            foreach (var type in RegisteredContentTypes.Keys.Where(x => x.Assembly == assembly).ToList())
            {
                RegisteredContentTypes.Remove(type);

                RemoveFromAssignable(type, type);

                foreach (var @interface in type.GetInterfaces())
                    RemoveFromAssignable(@interface, type);

                for (var baseType = type; baseType != null; baseType = baseType.BaseType)
                {
                    if (!baseType.IsAbstract)
                        continue;

                    RemoveFromAssignable(baseType, type);
                }

                static void RemoveFromAssignable(Type key, Type value)
                {
                    var list = AssignableToContent[key];
                    list.Remove(value);
                    if (list.Count == 0)
                        AssignableToContent.Remove(key);
                }
            }
        }
    }

    private static void RegisterAssetAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (RegistryLock)
        {
            if (RegisteredAssetAssemblies.Contains(assembly))
                return;

            RegisteredAssetAssemblies.Add(assembly);

            var assemblyScanTypes = AssemblyRegistry.GetScanTypes(assembly);
            if (assemblyScanTypes != null)
            {
                var instantiatedObjects = new Dictionary<Type, object>();

                // Register serializer factories
                if (assemblyScanTypes.Types.TryGetValue(typeof(IYamlSerializableFactory), out var types))
                {
                    foreach (var type in types)
                    {
                        // Register serializer factories
                        if (!type.IsAbstract &&
                            type.GetCustomAttribute<YamlSerializerFactoryAttribute>() != null && type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            try
                            {
                                var instance = Activator.CreateInstance(type)!;
                                instantiatedObjects.Add(type, instance);
                                RegisteredSerializerFactories.Add((IYamlSerializableFactory)instance);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Unable to instantiate serializer factory [{type}]", ex);
                            }
                        }
                    }
                }

                // Custom visitors
                if (assemblyScanTypes.Types.TryGetValue(typeof(IDataCustomVisitor), out types))
                {
                    foreach (var type in types)
                    {
                        if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            try
                            {
                                if (!instantiatedObjects.TryGetValue(type, out var instance))
                                {
                                    instance = Activator.CreateInstance(type)!;
                                    instantiatedObjects.Add(type, instance);
                                }
                                RegisteredDataVisitNodes.Add((IDataCustomVisitor)instance);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Unable to instantiate custom visitor [{type}]", ex);
                            }
                        }
                    }
                }

                // Asset importer
                if (assemblyScanTypes.Types.TryGetValue(typeof(IAssetImporter), out types))
                {
                    foreach (var type in types)
                    {
                        if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            try
                            {
                                if (!instantiatedObjects.TryGetValue(type, out var instance))
                                {
                                    instance = Activator.CreateInstance(type)!;
                                    instantiatedObjects.Add(type, instance);
                                }
                                // Register the importer instance
                                RegisterImporter((IAssetImporter)instance);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Unable to instantiate importer [{type.Name}]", ex);
                            }
                        }
                    }
                }

                // Register asset factory
                if (assemblyScanTypes.Types.TryGetValue(typeof(IAssetFactory<>), out types))
                {
                    foreach (var type in types)
                    {
                        if (!type.IsAbstract && !type.IsGenericTypeDefinition && type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            if (!instantiatedObjects.TryGetValue(type, out var instance))
                            {
                                instance = Activator.CreateInstance(type)!;
                                instantiatedObjects.Add(type, instance);
                            }
                            RegisteredAssetFactories.Add(type.Name, (IAssetFactory<Asset>)instance);
                        }
                    }
                }

                // Package upgraders
                if (assemblyScanTypes.Types.TryGetValue(typeof(PackageUpgraderAttribute), out types))
                {
                    foreach (var type in types)
                    {
                        var packageUpgraderAttribute = type.GetCustomAttribute<PackageUpgraderAttribute>();
                        if (packageUpgraderAttribute != null)
                        {
                            try
                            {
                                var packageUpgrader = (PackageUpgrader)Activator.CreateInstance(type)!;
                                packageUpgrader.Attribute = packageUpgraderAttribute;
                                foreach (var packageName in packageUpgraderAttribute.PackageNames)
                                    RegisteredPackageUpgraders[packageName] = packageUpgrader;
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Unable to instantiate package upgrader [{type.Name}]", ex);
                            }
                        }
                    }
                }

                // Package analyzers
                if (assemblyScanTypes.Types.TryGetValue(typeof(PackageSessionAnalysisBase), out types))
                {
                    foreach (var type in types)
                    {
                        if (type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            RegisteredPackageSessionAnalysisTypes.Add(type);
                        }
                    }
                }

                // Asset types (and Package type)
                var assemblyContainsPackageType = assembly == typeof(Package).Assembly;
                if (assemblyScanTypes.Types.TryGetValue(typeof(Asset), out types) || assemblyContainsPackageType)
                {
                    if (assemblyContainsPackageType)
                    {
                        var extraTypes = new[] { typeof(Package) };
                        types = types?.Concat(extraTypes).ToList() ?? [.. extraTypes];
                    }
                    foreach (var assetType in types!)
                    {
                        // Store in a list all asset types loaded
                        if (assetType.IsPublic && !assetType.IsAbstract)
                        {
                            AssetTypes.Add(assetType);
                        }

                        // Asset FileExtensions
                        var assetDescriptionAttribute = assetType.GetCustomAttribute<AssetDescriptionAttribute>();
                        if (assetDescriptionAttribute != null)
                        {
                            if (assetDescriptionAttribute.FileExtensions != null)
                            {
                                var extensions = FileUtility.GetFileExtensions(assetDescriptionAttribute.FileExtensions);
                                RegisteredDefaultAssetExtension[assetType] = extensions.FirstOrDefault();
                                foreach (var extension in extensions)
                                {
                                    RegisteredAssetFileExtensions.TryAdd(extension, assetType);
                                }
                            }

                            if (assetDescriptionAttribute.AlwaysMarkAsRoot)
                            {
                                lock (AlwaysMarkAsRootAssetTypes)
                                {
                                    AlwaysMarkAsRootAssetTypes.Add(assetType);
                                }
                            }
                        }

                        // Content type associated to assets
                        var assetContentType = assetType.GetCustomAttribute<AssetContentTypeAttribute>();
                        if (assetContentType != null)
                        {
                            if (!ContentToAssetTypes.TryGetValue(assetContentType.ContentType, out var assetTypes))
                            {
                                assetTypes = [];
                                ContentToAssetTypes[assetContentType.ContentType] = assetTypes;
                            }
                            assetTypes.Add(assetType);
                            AssetToContentTypes.Add(assetType, assetContentType.ContentType);
                        }

                        // Asset format version (process name by name)
                        var assetFormatVersions = assetType.GetCustomAttributes<AssetFormatVersionAttribute>();
                        foreach (var assetFormatVersion in assetFormatVersions)
                        {
                            var formatVersion = assetFormatVersion.Version;
                            var minVersion = assetFormatVersion.MinUpgradableVersion;
                            if (!RegisteredFormatVersions.TryGetValue(assetType, out var formatVersions))
                            {
                                RegisteredFormatVersions.Add(assetType, formatVersions = []);
                            }
                            formatVersions.Add(assetFormatVersion.Name, formatVersion);

                            // Asset upgraders (only those matching current name)
                            var assetUpgraders = assetType.GetCustomAttributes<AssetUpgraderAttribute>().Where(x => x.Name == assetFormatVersion.Name);
                            AssetUpgraderCollection? upgraderCollection = null;
                            foreach (var upgrader in assetUpgraders)
                            {
                                (upgraderCollection ??= new AssetUpgraderCollection(assetType, formatVersion)).RegisterUpgrader(upgrader.AssetUpgraderType, upgrader.StartVersion, upgrader.TargetVersion);
                            }
                            if (upgraderCollection != null)
                            {
                                upgraderCollection.Validate(minVersion);
                                RegisteredAssetUpgraders.Add(new KeyValuePair<Type, string>(assetType, assetFormatVersion.Name), upgraderCollection);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void UnregisterAssetAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (RegistryLock)
        {
            if (!RegisteredAssetAssemblies.Contains(assembly))
                return;

            RegisteredAssetAssemblies.Remove(assembly);

            foreach (var typeToRemove in RegisteredDefaultAssetExtension.Keys.Where(type => type.Assembly == assembly).ToList())
            {
                RegisteredDefaultAssetExtension.Remove(typeToRemove);
            }

            foreach (var typeToRemove in AssetTypes.ToList().Where(type => type.Assembly == assembly))
            {
                AssetTypes.Remove(typeToRemove);
            }

            foreach (var typeToRemove in RegisteredPackageSessionAnalysisTypes.Where(type => type.Assembly == assembly).ToList())
            {
                RegisteredPackageSessionAnalysisTypes.Remove(typeToRemove);
            }

            foreach (var instance in RegisteredImportersInternal.Where(instance => instance.GetType().Assembly == assembly).ToList())
            {
                RegisteredImportersInternal.Remove(instance);
            }

            foreach (var typeToRemove in RegisteredFormatVersions.Keys.Where(type => type.Assembly == assembly).ToList())
            {
                RegisteredFormatVersions.Remove(typeToRemove);
            }

            foreach (var typeToRemove in RegisteredAssetUpgraders.Keys.Where(type => type.Key.Assembly == assembly).ToList())
            {
                RegisteredAssetUpgraders.Remove(typeToRemove);
            }

            foreach (var extensionToRemove in RegisteredAssetFileExtensions.Where(keyValue => keyValue.Value.Assembly == assembly).Select(keyValue => keyValue.Key).ToList())
            {
                RegisteredAssetFileExtensions.Remove(extensionToRemove);
            }

            foreach (var upgraderToRemove in RegisteredPackageUpgraders.Where(keyValue => keyValue.Value.GetType().Assembly == assembly).Select(keyValue => keyValue.Key).ToList())
            {
                RegisteredPackageUpgraders.Remove(upgraderToRemove);
            }

            foreach (var instance in RegisteredSerializerFactories.Where(instance => instance.GetType().Assembly == assembly).ToList())
            {
                RegisteredSerializerFactories.Remove(instance);
            }

            foreach (var instance in RegisteredDataVisitNodes.Where(instance => instance.GetType().Assembly == assembly).ToList())
            {
                RegisteredDataVisitNodes.Remove(instance);
            }
        }
    }

    /// <summary>
    /// Check if the specified type is an asset.
    /// </summary>
    /// <param name="assetType">Type of the asset.</param>
    /// <param name="throwException">A boolean indicating whether this method should throw an exception if the type is not an asset type.</param>
    /// <returns><c>true</c> if the asset is an asset type, false otherwise.</returns>
    public static bool IsAssetType(Type assetType, bool throwException = false)
    {
        ArgumentNullException.ThrowIfNull(assetType);

        if (!typeof(Asset).IsAssignableFrom(assetType))
        {
            if (throwException)
                throw new ArgumentException("Type [{0}] must be assignable to Asset or be a Package".ToFormat(assetType), nameof(assetType));
            return false;
        }
        return true;
    }

    /// <summary>
    /// Check if the specified type is an asset.
    /// </summary>
    /// <param name="assetType">Type of the asset.</param>
    /// <param name="throwException">A boolean indicating whether this method should throw an exception if the type is not an asset type.</param>
    /// <returns><c>true</c> if the asset is an asset type, false otherwise.</returns>
    public static bool IsAssetOrPackageType(Type assetType, bool throwException = false)
    {
        ArgumentNullException.ThrowIfNull(assetType);

        if (!typeof(Asset).IsAssignableFrom(assetType) && assetType != typeof(Package))
        {
            if (throwException)
                throw new ArgumentException("Type [{0}] must be assignable to Asset or be a Package".ToFormat(assetType), nameof(assetType));
            return false;
        }
        return true;
    }

    static AssetRegistry()
    {
        SupportedPlatforms = [];
        // Statically find all assemblies related to assets and register them
        foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Engine))
        {
            RegisterEngineAssembly(assembly);
        }
        foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Assets))
        {
            RegisterAssetAssembly(assembly);
        }
        AssemblyRegistry.AssemblyRegistered += AssemblyRegistryAssemblyRegistered;
        AssemblyRegistry.AssemblyUnregistered += AssemblyRegistryOnAssemblyUnregistered;
    }

    private static void AssemblyRegistryOnAssemblyUnregistered(object? sender, AssemblyRegisteredEventArgs e)
    {
        if (e.Categories.Contains(AssemblyCommonCategories.Assets))
        {
            UnregisterAssetAssembly(e.Assembly);
        }
        if (e.Categories.Contains(AssemblyCommonCategories.Engine))
        {
            UnregisterEngineAssembly(e.Assembly);
        }
    }

    private static void AssemblyRegistryAssemblyRegistered(object? sender, AssemblyRegisteredEventArgs e)
    {
        // Handle delay-loading assemblies
        if (e.Categories.Contains(AssemblyCommonCategories.Engine))
        {
            RegisterEngineAssembly(e.Assembly);
        }
        if (e.Categories.Contains(AssemblyCommonCategories.Assets))
        {
            RegisterAssetAssembly(e.Assembly);
        }
    }
}
