// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public interface IAddAssetPolicy
    {
        /// <summary>
        /// Checks whether this policy deals with this type of asset.
        /// </summary>
        /// <param name="type">The type of asset.</param>
        /// <returns><c>true</c> if this policy deals with this type of asset; otherwise, <c>false</c>.</returns>
        bool Accept([NotNull] Type type);

        /// <summary>
        /// Checks whether the provided <paramref name="asset"/> can be added or inserted, and sets a message about the reason the operation
        /// is accepted of refused.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="asset">The asset.</param>
        /// <param name="modifier">The modifier keys currently active.</param>
        /// <param name="index">The index at which the insertion occurs.</param>
        /// <param name="message">A message explaining the reason the operation is accepted or refused.</param>
        /// <param name="messageArgs">Optional arguments for the message.</param>
        /// <returns><c>true</c> if the provided <paramref name="asset"/> can be added or inserted; otherwise, <c>false</c>.</returns>
        /// <remarks>This method is not called if <see cref="Accept"/> returns <c>false</c> for the corresponding asset type.</remarks>
        bool CanAddOrInsert([NotNull] EntityHierarchyItemViewModel parent, [NotNull] AssetViewModel asset, AddChildModifiers modifier, int index, [NotNull] out string message, [NotNull] params object[] messageArgs);
    }

    public interface ICreateComponentPolicy : IAddAssetPolicy
    {
        /// <summary>
        /// Creates an <see cref="EntityComponent"/> corresponding to the provided <paramref name="asset"/> that will be added to the scene or prefab.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="asset">The asset.</param>
        /// <returns>The component to add or insert to the scene; or <c>null</c> if it could not be created.</returns>
        /// <remarks>This method is not called if <see cref="IAddAssetPolicy.CanAddOrInsert"/> returns <c>false</c> for the provided <paramref name="asset"/>.</remarks>
        [CanBeNull]
        EntityComponent CreateComponentFromAsset([NotNull] EntityHierarchyItemViewModel parent, [NotNull] AssetViewModel asset);
    }

    public interface ICreateEntitiesPolicy : IAddAssetPolicy
    {
        /// <remarks>This method is not called if <see cref="IAddAssetPolicy.CanAddOrInsert"/> returns <c>false</c> for the provided <paramref name="asset"/>.</remarks>
        [CanBeNull]
        AssetCompositeHierarchyData<EntityDesign, Entity> CreateEntitiesFromAsset([NotNull] EntityHierarchyItemViewModel parent, [NotNull] AssetViewModel asset);
    }

    public interface ICustomPolicy : IAddAssetPolicy
    {
        /// <remarks>This method is not called if <see cref="IAddAssetPolicy.CanAddOrInsert"/> returns <c>false</c> for the provided <paramref name="assets"/>.</remarks>
        void ApplyPolicy([NotNull] EntityHierarchyItemViewModel parent, [ItemNotNull, NotNull] IEnumerable<AssetViewModel> assets, int index, Vector3 position);
    }
}
