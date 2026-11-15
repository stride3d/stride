// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Serialization;

/// <summary>
/// Declares, in a runtime assembly, that asset files with <see cref="Extension"/> compile to <see cref="ContentType"/>.
/// The asset URL constants generator types its constants from it.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AssetFileExtensionAttribute : Attribute
{
    public AssetFileExtensionAttribute(string extension, Type contentType)
    {
        Extension = extension;
        ContentType = contentType;
    }

    /// <summary>
    /// The asset file extension, with its leading dot.
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// The type the compiled content of such an asset loads as.
    /// </summary>
    public Type ContentType { get; }
}
