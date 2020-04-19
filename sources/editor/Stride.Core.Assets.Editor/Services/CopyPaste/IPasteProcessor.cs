// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Interface for a paste processor used by the <see cref="ICopyPasteService"/>.
    /// </summary>
    public interface IPasteProcessor
    {
        /// <summary>
        /// Gets whether this processor is able to process the data.
        /// </summary>
        /// <param name="targetRootType"></param>
        /// <param name="targetMemberType"></param>
        /// <param name="pastedDataType"></param>
        /// <returns><c>true</c> if this processor is able to process the data; otherwise, <c>false</c>.</returns>
        bool Accept([NotNull] Type targetRootType, [NotNull] Type targetMemberType, [NotNull] Type pastedDataType);

        /// <summary>
        /// Processes the data for the target <paramref name="targetRootObject"/>.
        /// </summary>
        /// <param name="graphContainer">The <see cref="AssetPropertyGraphContainer"/> instance to use to manipulate Quantum graph.</param>
        /// <param name="targetRootObject"></param>
        /// <param name="targetMemberType"></param>
        /// <param name="data">The pasted data.</param>
        /// <param name="isRootDataObjectReference">Indicate if the root data is an object reference.</param>
        /// <param name="sourceId">The identifier of the source object, if it has one.</param>
        /// <param name="overrides">A collection of overrides hat were attached to the copied data before serialization.</param>
        /// <param name="basePath">
        ///     The base path of the current <paramref name="data"/> in the original serialized data.
        ///     Only metadata prefixed by this base patn are supposed to affect the current <paramref name="data"/>.
        /// </param>
        /// <returns><c>true</c> if the process was successful; otherwise <c>false</c>.</returns>
        /// <remarks>
        ///     When this method returns <c>false</c>, the <see cref="ICopyPasteService"/> fallbacks to the next processor.
        ///     In case the process was not successful but this processor determines that no other processing should be performed, this method should
        ///     clear the data (by resetting <paramref name="data"/>) and return <c>true</c> instead.
        /// </remarks>
        bool ProcessDeserializedData(AssetPropertyGraphContainer graphContainer, [NotNull] object targetRootObject, [NotNull] Type targetMemberType, [NotNull] ref object data, bool isRootDataObjectReference, AssetId? sourceId, YamlAssetMetadata<OverrideType> overrides, YamlAssetPath basePath);

        /// <summary>
        /// Pastes the data to the target content at the given index.
        /// </summary>
        /// <param name="pasteResultItem">An item from the result of the paste operation. (see <see cref="ICopyPasteService.DeserializeCopiedData"/></param>
        /// <param name="propertyGraph"></param>
        /// <param name="nodeAccessor">An acessor to the target content where the data will be pasted.</param>
        /// <param name="propertyContainer">A property container to pass specific information to a processor.</param>
        /// <returns>A tuple representing the result of the operation, containing a message and a boolean.</returns>
        [NotNull]
        Task Paste([NotNull] IPasteItem pasteResultItem, AssetPropertyGraph propertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer propertyContainer);
    }
}
