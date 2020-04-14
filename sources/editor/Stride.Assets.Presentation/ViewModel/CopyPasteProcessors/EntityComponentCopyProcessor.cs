// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Yaml;
using Xenko.Core.Annotations;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.ViewModel.CopyPasteProcessors
{
    public class EntityComponentCopyProcessor : ICopyProcessor
    {
        /// <inheritdoc />
        public bool Accept(Type dataType)
        {
            return dataType == typeof(TransformComponent) || dataType == typeof(EntityComponentCollection);
        }

        /// <inheritdoc />
        public bool Process(ref object data, AttachedYamlAssetMetadata metadata)
        {
            if (data is TransformComponent transform)
            {
                PatchTransformComponent(transform);
                return true;
            }

            if (data is EntityComponentCollection collection)
            {
                var processed = false;
                foreach (var t in collection.OfType<TransformComponent>())
                {
                    PatchTransformComponent(t);
                    processed = true;
                }
                return processed;
            }

            return false;
        }

        private static void PatchTransformComponent([NotNull] TransformComponent transform)
        {
            // We don't want to copy the children of a transform component
            transform.Children.Clear();
        }
    }
}
