// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core;

namespace Xenko.Editor.EditorGame.ContentLoader
{
    // TODO: Unexpose this interface from view models. Expose only GetRuntimeObject for services
    public interface IEditorContentLoader : IDisposable
    {
        LoaderReferenceManager Manager { get; }

        /// <summary>
        /// Raised when an asset start to be compiled and loaded.
        /// </summary>
        event EventHandler<ContentLoadEventArgs> AssetLoading;

        /// <summary>
        /// Raised when an asset has been loaded.
        /// </summary>
        event EventHandler<ContentLoadEventArgs> AssetLoaded;

        /// <summary>
        /// Builds and reloads if necessary the asset corresponding to the given id.
        /// </summary>
        /// <param name="assetId">The id of the asset to build and reload.</param>
        void BuildAndReloadAsset(AssetId assetId);

        /// <summary>
        /// Unloads the asset corresponding to the given id. This must be called from the game thread.
        /// </summary>
        /// <param name="id">The id of the asset to unload.</param>
        /// <returns>A task that completes when the asset has been unloaded.</returns>
        Task UnloadAsset(AssetId id);

        /// <summary>
        /// Locks synchronously (using a <see cref="Monitor"/>) the database until the returned <see cref="IDisposable"/> object is disposed.
        /// This method must be called out of a micro-thread.
        /// </summary>
        /// <returns>A task that completes when the lock is acquired.</returns>
        Task<ISyncLockable> ReserveDatabaseSyncLock();

        /// <summary>
        /// Locks asynchronously the database until the returned <see cref="IDisposable"/> object is disposed.
        /// This method must be called from a micro-thread.
        /// </summary>
        /// <returns>A task that completes when the lock is acquired.</returns>
        Task<IDisposable> LockDatabaseAsynchronously();

        /// <summary>
        /// Retrieves the run-time object corresponding to the given asset item.
        /// </summary>
        /// <typeparam name="T">The expected type of the run-time object.</typeparam>
        /// <param name="assetItem">The asset corresponding to the run-time object to retrieve.</param>
        /// <returns>The run-time object corresponding to the given asset item if it exists, null otherwise.</returns>
        T GetRuntimeObject<T>(AssetItem assetItem) where T : class;
    }
}
