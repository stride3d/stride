// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers;

internal static class ReferenceSerializationHelper
{
    /// <summary>
    /// Formats an "id:url" reference. A URL that sits under the saving package's own namespace
    /// (<see cref="AssetObjectSerializerBackend.AssetNamespaceKey"/>) is written bare, i.e. without
    /// its /Namespace/ prefix; loading adds the prefix back (see <see cref="RestoreLocation"/>).
    /// </summary>
    public static string FormatReference(ref ObjectContext objectContext, AssetId id, string? location)
    {
        // A broken reference (empty id) can't be rooted again at load, so leave its URL as-is
        if (location is not null
            && id != AssetId.Empty
            && objectContext.SerializerContext.Properties.TryGetValue(AssetObjectSerializerBackend.AssetNamespaceKey, out var assetNamespace))
        {
            location = AssetNamespaceHelper.Unqualify(location, assetNamespace);
        }
        return $"{id}:{location}";
    }

    /// <summary>
    /// Adds the /Namespace/ prefix back to a reference URL at load - the mirror of
    /// <see cref="FormatReference"/>. A URL saved bare under the loading package's namespace
    /// (<see cref="AssetObjectSerializerBackend.AssetNamespaceKey"/>) gets its prefix restored. This
    /// runs on every reference as it is read, so it works wherever the reference is nested (in structs,
    /// collections or custom types). URLs that are already rooted (references to another package) are
    /// left unchanged.
    /// </summary>
    public static string RestoreLocation(ref ObjectContext objectContext, string location)
    {
        if (objectContext.SerializerContext.Properties.TryGetValue(AssetObjectSerializerBackend.AssetNamespaceKey, out var assetNamespace))
        {
            location = AssetNamespaceHelper.Qualify(location, assetNamespace);
        }
        return location;
    }
}
