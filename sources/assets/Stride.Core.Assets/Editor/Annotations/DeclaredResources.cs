// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Reads the embedded resources named by <see cref="TypeImageAttribute"/> and <see cref="StaticThumbnailAttribute"/>.
/// </summary>
public static class DeclaredResources
{
    /// <summary>
    /// Reads a resource embedded in <paramref name="assembly"/>, named by its full manifest resource name or the
    /// trailing part of it; null when there is no such resource.
    /// </summary>
    public static byte[]? Read(Assembly assembly, string resourceName)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        var name = resourceNames.FirstOrDefault(x => x == resourceName)
            ?? resourceNames.FirstOrDefault(x => x.EndsWith("." + resourceName, StringComparison.Ordinal));
        if (name is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
