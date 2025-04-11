// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.CodeEditorSupport;

/// <summary>
/// Represents the type of <see cref="IDEInfo"/> class.
/// </summary>
public enum IDEType
{
    /// <summary>
    /// Represents the Visual Studio IDE
    /// </summary>
    VisualStudio,

    /// <summary>
    /// Represents the JetBrains Rider IDE
    /// </summary>
    Rider,

    /// <summary>
    /// Represents the Visual Studio Code editor, or it's fork - VS Codium
    /// </summary>
    VSCode
}
