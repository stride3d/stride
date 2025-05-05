// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Yaml;
using Stride.Engine;

namespace Stride.Assets.Editor.Components.CopyPasteProcessors;

internal sealed class EntityComponentCopyProcessor : ICopyProcessor
{
    /// <inheritdoc/>
    public bool Accept(Type dataType)
    {
        return dataType == typeof(TransformComponent) || dataType == typeof(EntityComponentCollection);
    }
        
    /// <inheritdoc/>
    public bool Process(ref object data, AttachedYamlAssetMetadata metadata)
    {
        switch (data)
        {
            case TransformComponent transform:
                PatchTransformComponent(transform);
                return true;

            case EntityComponentCollection collection:
            {
                var processed = false;
                foreach (var t in collection.OfType<TransformComponent>())
                {
                    PatchTransformComponent(t);
                    processed = true;
                }
                return processed;
            }

            default:
                return false;
        }

        static void PatchTransformComponent(TransformComponent transform)
        {
            // We don't want to copy the children of a transform component
            transform.Children.Clear();
        }
    }
}
