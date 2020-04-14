// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Storage;

namespace Stride.Core.Serialization
{
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
            Modules = new List<Module>();
            Profiles = new Dictionary<string, AssemblySerializersPerProfile>();
            DataContractAliases = new List<DataContractAlias>();
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
        internal static object Lock = new object();
        internal static int Version;

        // List of all the factories
        private static readonly List<WeakReference<SerializerSelector>> SerializerSelectors = new List<WeakReference<SerializerSelector>>();

        // List of registered assemblies
        private static readonly List<AssemblySerializers> AssemblySerializers = new List<AssemblySerializers>();

        private static readonly Dictionary<Assembly, AssemblySerializers> AvailableAssemblySerializers = new Dictionary<Assembly, AssemblySerializers>();

        // List of serializers per profile
        internal static readonly Dictionary<string, Dictionary<Type, AssemblySerializerEntry>> DataSerializersPerProfile = new Dictionary<string, Dictionary<Type, AssemblySerializerEntry>>();

        private static readonly Dictionary<string, Type> DataContractAliasMapping = new Dictionary<string, Type>();

        public static void RegisterSerializerSelector(SerializerSelector serializerSelector)
        {
            SerializerSelectors.Add(new WeakReference<SerializerSelector>(serializerSelector));
        }

        public static AssemblySerializerEntry GetSerializer([NotNull] string profile, Type type)
        {
            lock (Lock)
            {
                Dictionary<Type, AssemblySerializerEntry> serializers;
                AssemblySerializerEntry assemblySerializerEntry;
                if (!DataSerializersPerProfile.TryGetValue(profile, out serializers) || !serializers.TryGetValue(type, out assemblySerializerEntry))
                    return default(AssemblySerializerEntry);

                return assemblySerializerEntry;
            }
        }

        [CanBeNull]
        internal static Type GetTypeFromAlias([NotNull] string alias)
        {
            lock (Lock)
            {
                Type type;
                DataContractAliasMapping.TryGetValue(alias, out type);
                return type;
            }
        }

        public static void RegisterSerializationAssembly([NotNull] AssemblySerializers assemblySerializers)
        {
            lock (Lock)
            {
                // Register it (so that we can get it back if unregistered)
                if (!AvailableAssemblySerializers.ContainsKey(assemblySerializers.Assembly))
                    AvailableAssemblySerializers.Add(assemblySerializers.Assembly, assemblySerializers);

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
                    SerializerSelector serializerSelector;
                    if (weakSerializerSelector.TryGetTarget(out serializerSelector))
                    {
                        serializerSelector.Invalidate();
                    }
                }
            }
        }

        public static void RegisterSerializationAssembly([NotNull] Assembly assembly)
        {
            lock (Lock)
            {
                AssemblySerializers assemblySerializers;
                if (AvailableAssemblySerializers.TryGetValue(assembly, out assemblySerializers))
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
                    SerializerSelector serializerSelector;
                    if (weakSerializerSelector.TryGetTarget(out serializerSelector))
                    {
                        serializerSelector.Invalidate();
                    }
                }
            }
        }

        public static AssemblySerializers GetAssemblySerializers([NotNull] Assembly assembly)
        {
            lock (Lock)
            {
                AssemblySerializers assemblySerializers;
                AvailableAssemblySerializers.TryGetValue(assembly, out assemblySerializers);
                return assemblySerializers;
            }
        }

        private static void RegisterSerializers([NotNull] AssemblySerializers assemblySerializers)
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

                Dictionary<Type, AssemblySerializerEntry> dataSerializers;
                if (!DataSerializersPerProfile.TryGetValue(profile, out dataSerializers))
                {
                    dataSerializers = new Dictionary<Type, AssemblySerializerEntry>();
                    DataSerializersPerProfile.Add(profile, dataSerializers);
                }

                foreach (var assemblySerializer in assemblySerializerPerProfile.Value)
                {
                    if (!dataSerializers.ContainsKey(assemblySerializer.ObjectType))
                    {
                        dataSerializers.Add(assemblySerializer.ObjectType, assemblySerializer);
                    }
                }
            }
        }
    }
}
