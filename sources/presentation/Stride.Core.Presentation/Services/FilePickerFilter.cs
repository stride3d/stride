// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Services;

public struct FilePickerFilter
{
    public FilePickerFilter(string? name)
    {
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Filter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// List of extensions in GLOB format. I.e. "*.png" or "*.*".
    /// </summary>
    public IReadOnlyList<string>? Patterns { get; set; }
}
