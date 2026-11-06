// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Graphics;

namespace Stride.Editor.Resources;

/// <summary>
/// Reads the resources embedded in an assembly.
/// </summary>
internal static class EmbeddedResourceReader
{
    /// <summary>
    /// The assembly searched when none is given: this one.
    /// </summary>
    public static readonly Assembly DefaultSource = typeof(EmbeddedResourceReader).Assembly;

    /// <summary>
    /// Decodes an embedded image, by its manifest resource name or by a name that ends with the given one (a short name without
    /// the namespace prefix).
    /// </summary>
    /// <returns>The image, or <c>null</c> when the resource does not exist.</returns>
    public static Image? TryGetImage(string name, Assembly? source = null)
    {
        using var stream = TryGetStream(name, source);
        return stream != null ? Image.Load(stream) : null;
    }

    /// <summary>
    /// Opens an embedded resource, by its manifest resource name or by a name that ends with the given one.
    /// </summary>
    /// <returns>The stream, or <c>null</c> when the resource does not exist.</returns>
    public static Stream? TryGetStream(string name, Assembly? source = null)
    {
        source ??= DefaultSource;
        var stream = source.GetManifestResourceStream(name);
        if (stream != null)
            return stream;

        var fullName = source.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("." + name, StringComparison.Ordinal) || x.EndsWith("." + name + ".png", StringComparison.Ordinal));
        return fullName != null ? source.GetManifestResourceStream(fullName) : null;
    }
}
