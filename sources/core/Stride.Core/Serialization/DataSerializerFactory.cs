// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System.Collections.ObjectModel;
using System.Reflection;
using Stride.Core.Reflection;
using Stride.Core.Storage;

namespace Stride.Core.Serialization;

/// <summary>
/// An entry to a serialized object.
/// </summary>
public struct AssemblySerializerEntry
{
    /// <summary>
    /// The id of the object.
    /// </summary>
    public readonly ObjectId Id;

    /// <summary>
    /// The type of the object.
    /// </summary>
    public readonly Type ObjectType;

    /// <summary>
    /// The type of the serialized object.
    /// </summary>
    public readonly Type SerializerType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblySerializerEntry"/> struct.
    /// </summary>
    public AssemblySerializerEntry(ObjectId id, Type objectType, Type serializerType)
    {
        Id = id;
        ObjectType = objectType;
        SerializerType = serializerType;
    }
}

public class AssemblySerializersPerProfile : Collection<AssemblySerializerEntry>
{
}

public class AssemblySerializers
{
    public AssemblySerializers(Assembly assembly)
    {
        Assembly = assembly;
        Modules = [];
        Profiles = [];
        DataContractAliases = [];
    }

    public Assembly Assembly { get; }

    public List<Module> Modules { get; }

    public List<DataContractAlias> DataContractAliases { get; }

    public Dictionary<string, AssemblySerializersPerProfile> Profiles { get; }

    public override string ToString()
    {
        return Assembly.ToString();
    }

    public struct DataContractAlias
    {
        public string Name;
        public Type Type;

        /// <summary>
        /// True if generated from a <see cref="DataAliasAttribute"/>, false if generated from a <see cref="DataContractAttribute"/>.
        /// </summary>
        public bool IsAlias;

        public DataContractAlias(string name, Type type, bool isAlias)
        {
            Name = name;
            Type = type;
            IsAlias = isAlias;
        }
    }
}

public static class DataSerializerFactory
{
    internal static object Lock = new();
    internal static int Version;

    // List of all the factories
    private static readonly List<WeakReference<SerializerSelector>> SerializerSelectors = [];

    // List of registered assemblies
    private static readonly List<AssemblySerializers> AssemblySerializers = [];

    private static readonly Dictionary<Assembly, AssemblySerializers> AvailableAssemblySerializers = [];

    // List of serializers per profile
    internal static readonly Dictionary<string, Dictionary<Type, AssemblySerializerEntry>> DataSerializersPerProfile = [];

    private static readonly Dictionary<string, Type> DataContractAliasMapping = [];

    public static void RegisterSerializerSelector(SerializerSelector serializerSelector)
    {
        SerializerSelectors.Add(new WeakReference<SerializerSelector>(serializerSelector));
    }

    public static AssemblySerializerEntry GetSerializer(string profile, Type type)
    {
        lock (Lock)
        {
            if (!DataSerializersPerProfile.TryGetValue(profile, out var serializers) || !serializers.TryGetValue(type, out var assemblySerializerEntry))
                return default;

            return assemblySerializerEntry;
        }
    }

    internal static Type? GetTypeFromAlias(string alias)
    {
        lock (Lock)
        {
            DataContractAliasMapping.TryGetValue(alias, out var type);
            return type;
        }
    }

    public static void RegisterSerializationAssembly(AssemblySerializers assemblySerializers)
    {
        lock (Lock)
        {
            // Register it (so that we can get it back if unregistered)
            AvailableAssemblySerializers.TryAdd(assemblySerializers.Assembly, assemblySerializers);

            // Check if already loaded
            if (AssemblySerializers.Contains(assemblySerializers))
                return;

            // Update existing SerializerSelector
            AssemblySerializers.Add(assemblySerializers);
        }

        // Run module ctor
        foreach (var module in assemblySerializers.Modules)
        {
            ModuleRuntimeHelpers.RunModuleConstructor(module);
        }

        lock (Lock)
        {
            RegisterSerializers(assemblySerializers);

            ++Version;

            // Invalidate each serializer selector (to force them to rebuild combined list of serializers)
            foreach (var weakSerializerSelector in SerializerSelectors)
            {
                if (weakSerializerSelector.TryGetTarget(out var serializerSelector))
                {
                    serializerSelector.Invalidate();
                }
            }
        }
    }

    public static void RegisterSerializationAssembly(Assembly assembly)
    {
        lock (Lock)
        {
            if (AvailableAssemblySerializers.TryGetValue(assembly, out var assemblySerializers))
                RegisterSerializationAssembly(assemblySerializers);
        }
    }

    public static void UnregisterSerializationAssembly(Assembly assembly)
    {
        lock (Lock)
        {
            var removedAssemblySerializer = AssemblySerializers.FirstOrDefault(x => x.Assembly == assembly);
            if (removedAssemblySerializer == null)
                return;

            AssemblySerializers.Remove(removedAssemblySerializer);

            // Unregister data contract aliases
            foreach (var dataContractAliasEntry in removedAssemblySerializer.DataContractAliases)
            {
                // TODO: Warning, exception or override if collision? (currently exception, easiest since we can remove them without worry when unloading assembly)
                DataContractAliasMapping.Remove(dataContractAliasEntry.Name);
            }

            // Rebuild serializer list
            // TODO: For now, we simply reregister all assemblies one-by-one, but it can easily be improved if it proves to be unefficient (for now it shouldn't happen often so probably not a big deal)
            DataSerializersPerProfile.Clear();
            DataContractAliasMapping.Clear();

            foreach (var assemblySerializer in AssemblySerializers)
            {
                RegisterSerializers(assemblySerializer);
            }

            ++Version;

            foreach (var weakSerializerSelector in SerializerSelectors)
            {
                if (weakSerializerSelector.TryGetTarget(out var serializerSelector))
                {
                    serializerSelector.Invalidate();
                }
            }
        }
    }

    public static AssemblySerializers? GetAssemblySerializers(Assembly assembly)
    {
        lock (Lock)
        {
            AvailableAssemblySerializers.TryGetValue(assembly, out var assemblySerializers);
            return assemblySerializers;
        }
    }

    private static void RegisterSerializers(AssemblySerializers assemblySerializers)
    {
        // Register data contract aliases
        foreach (var dataContractAliasEntry in assemblySerializers.DataContractAliases)
        {
            try
            {
                // TODO: Warning, exception or override if collision? (currently exception)
                DataContractAliasMapping.Add(dataContractAliasEntry.Name, dataContractAliasEntry.Type);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Two different classes have the same DataContract Alias [{dataContractAliasEntry.Name}]: {dataContractAliasEntry.Type} and {DataContractAliasMapping[dataContractAliasEntry.Name]}");
            }
        }

        // Register serializers
        foreach (var assemblySerializerPerProfile in assemblySerializers.Profiles)
        {
            var profile = assemblySerializerPerProfile.Key;

            if (!DataSerializersPerProfile.TryGetValue(profile, out var dataSerializers))
            {
                dataSerializers = new Dictionary<Type, AssemblySerializerEntry>();
                DataSerializersPerProfile.Add(profile, dataSerializers);
            }

            foreach (var assemblySerializer in assemblySerializerPerProfile.Value)
            {
                dataSerializers.TryAdd(assemblySerializer.ObjectType, assemblySerializer);
            }
        }
    }
}
