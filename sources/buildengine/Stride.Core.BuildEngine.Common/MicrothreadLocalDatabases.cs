// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.IO;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// A static class that allows to have a different <see cref="ObjectDatabase"/> on each <see cref="MicroThread"/>. Objects can still be shared
    /// between micro-threads by using the <see cref="AddToSharedGroup"/> method.
    /// </summary>
    public static class MicrothreadLocalDatabases
    {
        private static readonly Dictionary<ObjectUrl, OutputObject> SharedOutputObjects = new Dictionary<ObjectUrl, OutputObject>();
        private static readonly MicroThreadLocal<DatabaseFileProvider> MicroThreadLocalDatabaseFileProvider;

        static MicrothreadLocalDatabases()
        {
            MicroThreadLocalDatabaseFileProvider = new MicroThreadLocal<DatabaseFileProvider>();

            ProviderService = new MicroThreadLocalProviderService();
        }

        public static IDatabaseFileProviderService ProviderService { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has a valid database file provider.
        /// </summary>
        /// <value><c>true</c> if this instance has database file provider; otherwise, <c>false</c>.</value>
        public static bool HasValidDatabaseFileProvider => MicroThreadLocalDatabaseFileProvider.Value != null;

        /// <summary>
        /// Gets the currently mounted microthread-local database provider.
        /// </summary>
        public static DatabaseFileProvider DatabaseFileProvider => MicroThreadLocalDatabaseFileProvider.Value;

        /// <summary>
        /// Merges the given dictionary of build output objects into the shared group. Objects merged here will be integrated to every database.
        /// </summary>
        /// <param name="outputObjects">The dictionary containing the <see cref="OutputObject"/> to merge into the shared group.</param>
        public static void AddToSharedGroup(IReadOnlyDictionary<ObjectUrl, OutputObject> outputObjects)
        {
            lock (SharedOutputObjects)
            {
                foreach (var outputObject in outputObjects)
                    SharedOutputObjects[outputObject.Key] = outputObject.Value;
            }
        }

        /// <summary>
        /// Gets a <see cref="MicroThreadLocalDatabaseFileProvider"/> containing only objects from the shared group.
        /// The shared group is a group of objects registered via <see cref="AddToSharedGroup"/> and shared amongst all databases.
        /// </summary>
        /// <returns>A <see cref="MicroThreadLocalDatabaseFileProvider"/> that can provide objects from the common group.</returns>
        public static DatabaseFileProvider GetSharedDatabase()
        {
            return CreateDatabase(CreateTransaction(null));
        }
        
        /// <summary>
        /// Creates and mounts a database containing the given output object groups and the shared group in the microthread-local
        /// <see cref="MicroThreadLocalDatabaseFileProvider"/>.
        /// </summary>
        /// <param name="outputObjectsGroups">A collection of dictionaries representing a group of output object.</param>
        public static void MountDatabase(IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> outputObjectsGroups)
        {
            MountDatabase(CreateTransaction(outputObjectsGroups));
        }

        /// <summary>
        /// Creates and mounts a database containing output objects from the shared group in the microthread-local <see cref="MicroThreadLocalDatabaseFileProvider"/>.
        /// </summary>
        public static void MountCommonDatabase()
        {
            MicroThreadLocalDatabaseFileProvider.Value = CreateDatabase(CreateTransaction(null));
        }

        /// <summary>
        /// Unmounts the currently mounted microthread-local database.
        /// </summary>
        public static void UnmountDatabase()
        {
            MicroThreadLocalDatabaseFileProvider.ClearValue();
        }

        public static IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups(IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> transactionOutputObjectsGroups)
        {
            if (transactionOutputObjectsGroups != null)
            {
                foreach (var outputObjects in transactionOutputObjectsGroups)
                    yield return outputObjects;
            }
            yield return SharedOutputObjects;
        }

        internal static void MountDatabase(BuildTransaction transaction)
        {
            MicroThreadLocalDatabaseFileProvider.Value = CreateDatabase(transaction); 
        }

        internal static BuildTransaction CreateTransaction(IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> transactionOutputObjectsGroups)
        {
            return new BuildTransaction(Builder.ObjectDatabase.ContentIndexMap, GetOutputObjectsGroups(transactionOutputObjectsGroups));
        }

        private static DatabaseFileProvider CreateDatabase(BuildTransaction transaction)
        {
            return new DatabaseFileProvider(new BuildTransaction.DatabaseContentIndexMap(transaction), Builder.ObjectDatabase);
        }

        private class MicroThreadLocalProviderService : IDatabaseFileProviderService
        {
            public DatabaseFileProvider FileProvider => MicroThreadLocalDatabaseFileProvider.Value;
        }
    }
}
