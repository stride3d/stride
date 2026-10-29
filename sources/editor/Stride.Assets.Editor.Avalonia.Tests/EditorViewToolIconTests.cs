// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Stride.Assets.Editor.Avalonia.Converters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The icons of the tools in the toolbar of the scene view: path data, as in the WPF scene editor.
/// </summary>
public sealed class EditorViewToolIconTests
{
    [AvaloniaFact]
    public void TheIconsOfTheEditorToolsAreValidPathData()
    {
        new StrideEditorPlugin().InitializePlugin(new LoggerResult());
        var tools = StrideEditorPlugin.EditorViewToolTypes.Where(x => x.Assembly == typeof(StrideEditorPlugin).Assembly).Select(x => (IEditorViewTool)Activator.CreateInstance(x)!).ToList();
        Assert.Equal(4, tools.Count);

        Assert.All(tools, tool =>
        {
            var geometry = Assert.IsAssignableFrom<Geometry>(Convert(tool.IconPath));
            Assert.True(geometry.Bounds.Width > 0 && geometry.Bounds.Height > 0, $"The icon of {tool.Name} is empty.");
        });
    }

    [AvaloniaFact]
    public void AnIconThatIsNotPathDataGivesNoGeometry()
    {
        Assert.Null(Convert("not path data"));
        Assert.Null(Convert(""));
        Assert.Null(Convert(null));
    }

    private static object? Convert(string? pathData) => new PathDataToGeometry().Convert(pathData, typeof(Geometry), null, CultureInfo.InvariantCulture);
}
