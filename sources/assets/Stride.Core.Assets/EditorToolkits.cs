// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// The UI toolkits an <see cref="PackageKind.Editor"/> package can be written for (<see cref="Package.Toolkit"/>,
/// StrideEditorToolkit in the project).
/// </summary>
public static class EditorToolkits
{
    /// <summary>
    /// The WPF editor (Game Studio).
    /// </summary>
    public const string Wpf = "Wpf";

    /// <summary>
    /// The Avalonia editor.
    /// </summary>
    public const string Avalonia = "Avalonia";
}
