// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public abstract class AddAssetPolicyBase<TAsset, TAssetViewModel> : IAddAssetPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        /// <inheritdoc />
        public bool Accept(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return typeof(TAsset) == type;
        }

        /// <inheritdoc />
        public bool CanAddOrInsert(EntityHierarchyItemViewModel parent, AssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs)
        {
            return CanAddOrInsert(parent, (TAssetViewModel)asset, modifier, index, out message, messageArgs);
        }

        /// <summary>
        /// Similar to <see cref="CanAddOrInsert(EntityHierarchyItemViewModel,AssetViewModel,AddChildModifiers,int,out string,object[])"/> but with the proper type for <paramref name="asset"/>.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="asset">The asset.</param>
        /// <param name="modifier">The modifier keys currently active.</param>
        /// <param name="index">The index at which the insertion occurs.</param>
        /// <param name="message">A message explaining the reason the operation is accepted or refused.</param>
        /// <param name="messageArgs">Optional arguments for the message.</param>
        /// <returns><c>true</c> if the provided <paramref name="asset"/> can be added or inserted; otherwise, <c>false</c>.</returns>
        /// <seealso cref="CanAddOrInsert(EntityHierarchyItemViewModel,AssetViewModel,AddChildModifiers,int,out string,object[])"/>
        protected abstract bool CanAddOrInsert([NotNull] EntityHierarchyItemViewModel parent, [NotNull] TAssetViewModel asset, AddChildModifiers modifier, int index, [NotNull] out string message, [NotNull] params object[] messageArgs);
    }

    public abstract class CreateComponentPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICreateComponentPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, TAssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs)
        {
            message = string.Format("Add the selection into {0}", messageArgs);
            return true;
        }

        /// <inheritdoc />
        public EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset)
        {
            return CreateComponentFromAsset(parent, (TAssetViewModel)asset);
        }

        /// <summary>
        /// Similar to <see cref="CreateComponentFromAsset(EntityHierarchyItemViewModel,AssetViewModel)"/> but with the proper type for <paramref name="asset"/>.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="asset">The asset.</param>
        /// <returns>The component to add or insert to the scene; or <c>null</c> if it could not be created.</returns>
        /// <seealso cref="CreateComponentFromAsset(EntityHierarchyItemViewModel,AssetViewModel)"/>
        [CanBeNull]
        protected abstract EntityComponent CreateComponentFromAsset([NotNull] EntityHierarchyItemViewModel parent, [NotNull] TAssetViewModel asset);
    }

    public abstract class CreateEntitiesPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICreateEntitiesPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        /// <inheritdoc />
        public AssetCompositeHierarchyData<EntityDesign, Entity> CreateEntitiesFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset)
        {
            return CreateEntitiesFromAsset(parent, (TAssetViewModel)asset);
        }

        /// <summary>
        /// Similar to <see cref="CreateEntitiesFromAsset(EntityHierarchyItemViewModel,AssetViewModel)"/> but with the proper type for <paramref name="asset"/>.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="asset">The asset.</param>
        /// <returns></returns>
        /// <seealso cref="CreateEntitiesFromAsset(EntityHierarchyItemViewModel,AssetViewModel)"/>
        [CanBeNull]
        protected abstract AssetCompositeHierarchyData<EntityDesign, Entity> CreateEntitiesFromAsset([NotNull] EntityHierarchyItemViewModel parent, [NotNull] TAssetViewModel asset);
    }

    public abstract class CustomPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICustomPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        /// <inheritdoc />
        public void ApplyPolicy(EntityHierarchyItemViewModel parent, IEnumerable<AssetViewModel> assets, int index, Vector3 position)
        {
            ApplyPolicy(parent, assets.Cast<TAssetViewModel>().ToList(), index, position);
        }

        /// <summary>
        /// Similar to <see cref="ApplyPolicy(EntityHierarchyItemViewModel,IEnumerable{AssetViewModel},int,Vector3)"/> but with the proper type for <paramref name="assets"/>.
        /// </summary>
        /// <param name="parent">The parent item where the asset will be added or inserted.</param>
        /// <param name="assets">A collection of assets.</param>
        /// <param name="index">The index at which insertion should begin.</param>
        /// <param name="position"></param>
        /// <seealso cref="ApplyPolicy(EntityHierarchyItemViewModel,IEnumerable{AssetViewModel},int,Vector3)"/>
        protected abstract void ApplyPolicy([NotNull] EntityHierarchyItemViewModel parent, [ItemNotNull, NotNull] IReadOnlyCollection<TAssetViewModel> assets, int index, Vector3 position);
    }
}
