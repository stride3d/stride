// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors
{
    public abstract class PasteProcessorBase : IPasteProcessor
    {
        /// <inheritdoc />
        public abstract bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType);

        /// <inheritdoc />
        public abstract bool ProcessDeserializedData(AssetPropertyGraphContainer graphContainer, object targetRootObject, Type targetMemberType, ref object data, bool isRootDataObjectReference, AssetId? sourceId, YamlAssetMetadata<OverrideType> overrides, YamlAssetPath basePath);

        /// <inheritdoc />
        public virtual Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph propertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer propertyContainer)
        {
            // default implementation does nothing
            return Task.CompletedTask;
        }
    }
}
