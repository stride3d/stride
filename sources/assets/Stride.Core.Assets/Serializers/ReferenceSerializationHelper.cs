// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers;

internal static class ReferenceSerializationHelper
{
    /// <summary>
    /// Formats an "id:url" reference scalar. URLs under the saving package's own namespace
    /// (<see cref="AssetObjectSerializerBackend.AssetNamespaceKey"/>) are written bare; the id
    /// restores the canonical rooted form at load.
    /// </summary>
    public static string FormatReference(ref ObjectContext objectContext, AssetId id, string? location)
    {
        // A broken reference (empty id) could not restore the rooted form at load, so keep its spelling
        if (location is not null
            && id != AssetId.Empty
            && objectContext.SerializerContext.Properties.TryGetValue(AssetObjectSerializerBackend.AssetNamespaceKey, out var assetNamespace))
        {
            location = AssetNamespaceHelper.Unqualify(location, assetNamespace);
        }
        return $"{id}:{location}";
    }
}
