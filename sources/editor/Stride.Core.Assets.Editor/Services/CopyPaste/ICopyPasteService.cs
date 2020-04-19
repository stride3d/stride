// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// An interface for a service implementing copy and paste.
    /// </summary>
    public interface ICopyPasteService
    {
        IReadOnlyCollection<IAssetPostPasteProcessor> PostProcessors { get; }

        /// <summary>
        /// Gets the <see cref="AssetPropertyGraphContainer"/> used to manipulate Quantum graphs of objects.
        /// </summary>
        AssetPropertyGraphContainer PropertyGraphContainer { get; }

        /// <summary>
        /// Copies a serialized version of an asset or part of an asset.
        /// </summary>
        /// <param name="propertyGraph">The property graph of the asset from which the copy is done.</param>
        /// <param name="sourceId"></param>
        /// <param name="value"></param>
        /// <param name="isObjectReference"></param>
        /// <returns>A string containing the serialized version of the copied data.</returns>
        [CanBeNull]
        string CopyFromAsset([CanBeNull] AssetPropertyGraph propertyGraph, AssetId? sourceId, object value, bool isObjectReference);

        /// <summary>
        /// Copies a serialized version of assets or part of assets.
        /// </summary>
        /// <param name="items">A collection of value tuple items which elements are:
        /// <list type="number">
        ///     <item>The property graph of the asset from which the copy is done</item>
        ///     <item>The id of the source asset, if relevant</item>
        ///     <item>The value to copy</item>
        ///     <item><c>true</c> if the root of the copied object is a reference to another object; otherwise, <c>false</c></item>
        /// </list></param>
        /// <param name="itemType">The type of the copied values.</param>
        /// <returns>A string containing the serialized version of the copied data.</returns>
        [CanBeNull]
        string CopyFromAssets([NotNull] IReadOnlyList<(AssetPropertyGraph propertyGraph, AssetId? sourceId, object value, bool isObjectReference)> items, [NotNull] Type itemType);

        /// <summary>
        /// Copies an object containing multiple assets. Each asset must have an associated <see cref="AssetPropertyGraph"/> registered in the <see cref="PropertyGraphContainer"/>.
        /// </summary>
        /// <param name="container">The container object to copy, referencing multiple assets.</param>
        /// <returns>A string containing the serialized version of the copied data.</returns>
        [CanBeNull]
        string CopyMultipleAssets(object container);

        /// <summary>
        /// Checks whether the given serialized <paramref name="text"/> can be pasted for an object of <paramref name="targetRootType"/>.
        /// </summary>
        /// <param name="text">The serialized data.</param>
        /// <param name="targetRootType"></param>
        /// <param name="targetMemberType"></param>
        /// <param name="expectedTypes"></param>
        /// <returns><c>true</c> if the given serialized data can be paste </returns>
        bool CanPaste(string text, [NotNull] Type targetRootType, Type targetMemberType, params Type[] expectedTypes);

        [NotNull]
        IPasteResult DeserializeCopiedData(string text, [NotNull] object targetObject, [NotNull] Type targetMemberType);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> to the list of available copy processors.
        /// </summary>
        /// <param name="processor"></param>
        void RegisterProcessor([NotNull] ICopyProcessor processor);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> to the list of available paste processors.
        /// </summary>
        /// <param name="processor"></param>
        void RegisterProcessor([NotNull] IPasteProcessor processor);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> to the list of available post-paste processors.
        /// </summary>
        /// <param name="processor"></param>
        void RegisterProcessor([NotNull] IAssetPostPasteProcessor processor);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> from the list of available copy processors.
        /// </summary>
        /// <param name="processor"></param>
        void UnregisterProcessor([NotNull] ICopyProcessor processor);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> from the list of available paste processors.
        /// </summary>
        /// <param name="processor"></param>
        void UnregisterProcessor([NotNull] IPasteProcessor processor);

        /// <summary>
        /// Adds the provided <paramref name="processor"/> from the list of available post-paste processors.
        /// </summary>
        /// <param name="processor"></param>
        void UnregisterProcessor([NotNull] IAssetPostPasteProcessor processor);
    }
}
