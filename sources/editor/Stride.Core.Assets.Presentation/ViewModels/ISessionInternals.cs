// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Presentation.ViewModels
{
    /// <summary>
    /// The plumbing of the session that the session objects and the editor layers call to keep it up to date. It is kept out of
    /// <see cref="ISessionViewModel"/> so that plugins don't depend on it.
    /// </summary>
    /// <remarks>
    /// The editor's session implements both interfaces; <see cref="SessionInternalsExtensions.Internals"/> gives this part of it.
    /// </remarks>
    internal interface ISessionInternals
    {
        /// <summary>
        /// Gets whether the session is currently in a special context to fix up assets.
        /// </summary>
        bool IsInFixupAssetContext { get; }

        /// <summary>
        /// Updates whether the session has changes that are not saved, once dirty flags changed (after a change, an undo or a redo).
        /// </summary>
        void UpdateHasUnsavedChanges();

        /// <summary>
        /// Checks that the asset view models of each package match the assets of the package.
        /// </summary>
        void CheckConsistency();

        /// <summary>
        /// Validates every <see cref="Asset.Replaces"/> declaration of the session.
        /// </summary>
        /// <param name="changedAsset">An asset whose messages are cleared when it no longer declares a replacement.</param>
        void CheckAssetReplacements(AssetViewModel changedAsset = null);

        /// <summary>
        /// Refreshes the root asset state of the selection.
        /// </summary>
        void RefreshRootAssetSelection();

        /// <summary>
        /// Registers an asset so it can be found using the <see cref="ISessionViewModel.GetAssetById"/> method. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to register.</param>
        void RegisterAsset(AssetViewModel asset);

        /// <summary>
        /// Unregisters an asset previously registered with <see cref="RegisterAsset"/>. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to unregister.</param>
        void UnregisterAsset(AssetViewModel asset);

        /// <summary>
        /// Records whether the file of a deleted project asset was deleted outside Game Studio, so that a save does not delete a file that
        /// came back. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="filePath">The full path of the file of the asset.</param>
        /// <param name="deletedOutside">Whether the file was deleted outside Game Studio.</param>
        void SetFileDeletedOutside(UFile filePath, bool deletedOutside);

        /// <summary>
        /// Loads what a dependency that was just added brings to the projects.
        /// </summary>
        Task LoadAddedDependencies(LoggerResult loggerResult);
    }

    internal static class SessionInternalsExtensions
    {
        /// <summary>
        /// Gets the plumbing of the given session.
        /// </summary>
        public static ISessionInternals Internals(this ISessionViewModel session) => (ISessionInternals)session;
    }
}
