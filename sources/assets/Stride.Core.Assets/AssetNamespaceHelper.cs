// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Assets;

/// <summary>
/// The prefix arithmetic behind asset namespaces: a qualified URL is /Namespace/Path, its
/// unqualified form drops the /Namespace root. Shared by the container/asset accessors and the
/// reference serializers so the math lives in one place.
/// </summary>
internal static class AssetNamespaceHelper
{
    /// <summary>Prepends /assetNamespace to an unqualified URL. No-op when the namespace is empty or the URL is already qualified (rooted).</summary>
    public static string Qualify(string url, string? assetNamespace)
    {
        if (string.IsNullOrEmpty(assetNamespace) || url.StartsWith('/'))
            return url;
        return "/" + assetNamespace + "/" + url;
    }

    /// <summary>Drops the /assetNamespace root from a qualified URL. No-op when the namespace is empty or the URL is not under it.</summary>
    public static string Unqualify(string url, string? assetNamespace)
    {
        if (string.IsNullOrEmpty(assetNamespace))
            return url;
        var root = "/" + assetNamespace + "/";
        return url.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? url.Substring(root.Length) : url;
    }
}
