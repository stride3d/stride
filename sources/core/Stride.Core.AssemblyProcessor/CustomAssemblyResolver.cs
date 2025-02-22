// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor;

/// <summary>
/// Allow to register assemblies manually, with their in-memory representation if necessary.
/// </summary>
public class CustomAssemblyResolver : BaseAssemblyResolver
{
    /// <summary>
    /// Assemblies stored as byte arrays.
    /// </summary>
    private readonly Dictionary<AssemblyDefinition, byte[]> assemblyData = [];

    private readonly List<string> references = [];

    private readonly HashSet<string> existingWindowsKitsReferenceAssemblies;

    protected override void Dispose(bool disposing)
    {
        foreach (var ass in cache)
        {
            ass.Value.Dispose();
        }
        cache.Clear();
        assemblyData.Clear();
        references.Clear();
        existingWindowsKitsReferenceAssemblies?.Clear();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets or sets the windows kits directory for Windows 10 apps.
    /// </summary>
    public string WindowsKitsReferenceDirectory { get; set; }

    readonly IDictionary<string, AssemblyDefinition> cache;

    public List<string> References
    {
        get { return references; }
    }

    public CustomAssemblyResolver()
    {
        cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (cache.TryGetValue(name.FullName, out var assembly))
            return assembly;

        assembly = base.Resolve(name);
        cache[name.FullName] = assembly;

        return assembly;
    }

    public void RegisterAssembly(AssemblyDefinition assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var name = assembly.Name.FullName;
        if (cache.ContainsKey(name))
            return;

        cache[name] = assembly;
    }

    public void RegisterAssemblies(List<AssemblyDefinition> mergedAssemblies)
    {
        foreach (var assemblyDefinition in mergedAssemblies)
        {
            RegisterAssembly(assemblyDefinition);
        }
    }

    /// <summary>
    /// Registers the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to register.</param>
    public void Register(AssemblyDefinition assembly)
    {
        RegisterAssembly(assembly);
    }

    public void RegisterReference(string path)
    {
        references.Add(path);
    }

    /// <summary>
    /// Gets the assembly data (if it exists).
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns></returns>
    public byte[] GetAssemblyData(AssemblyDefinition assembly)
    {
        assemblyData.TryGetValue(assembly, out var data);
        return data;
    }

    /// <summary>
    /// Registers the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to register.</param>
    public void Register(AssemblyDefinition assembly, byte[] peData)
    {
        assemblyData[assembly] = peData;
        RegisterAssembly(assembly);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        // Try list of references
        foreach (var reference in references)
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(reference), name.Name, StringComparison.OrdinalIgnoreCase) && File.Exists(reference))
            {
                return GetAssembly(reference, parameters);
            }
        }

        parameters ??= new ReaderParameters();

        try
        {
            return base.Resolve(name, parameters);
        }
        catch (AssemblyResolutionException)
        {
            // Check cache again, ignoring version numbers this time
            foreach (var assembly in cache)
            {
                if (assembly.Value.Name.Name == name.Name)
                {
                    return assembly.Value;
                }
            }
            throw;
        }
    }

    // Copied from BaseAssemblyResolver
    AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        parameters.AssemblyResolver ??= this;

        return ModuleDefinition.ReadModule(file, parameters).Assembly;
    }
}
