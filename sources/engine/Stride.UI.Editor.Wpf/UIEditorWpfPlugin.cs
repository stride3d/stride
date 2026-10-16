// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using Stride.Core.Diagnostics;
using Stride.Editor;

namespace Stride.UI.Editor.Views;

/// <summary>
/// The Game Studio's entry point into this package: the UI page and library editor views declared in this assembly
/// register through it, and it adds the UI property templates (thickness, strip definition).
/// </summary>
public sealed class UIEditorWpfPlugin : StrideAssetsPlugin
{
    /// <inheritdoc />
    protected override void Initialize(ILogger logger)
    {
        var templates = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.UI.Editor.Wpf;component/Views/UIPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
        RegisterResourceDictionary(templates);
    }
}
