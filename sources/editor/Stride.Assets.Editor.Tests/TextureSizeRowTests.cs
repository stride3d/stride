// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Textures;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The absolute size of a texture is a row of its own, computed from the Width and Height members: it follows them.
/// </summary>
public sealed class TextureSizeRowTests
{
    [Fact]
    public async Task TheAbsoluteSizeFollowsAnUndoAndARedoOfTheSize()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true,
            ("Texture", new TextureAsset { Source = new UFile("texture.png"), IsSizeInPercentage = false, Width = 100, Height = 50 }));
        var texture = (TextureAsset)fixture.Asset("Texture").Asset;
        var root = await fixture.PropertiesOf(fixture.Asset("Texture"));
        var absoluteWidth = Row(root, "AbsoluteWidth");
        var absoluteHeight = Row(root, "AbsoluteHeight");
        var notified = new List<string>();
        absoluteWidth.PropertyChanged += Record("width");
        absoluteHeight.PropertyChanged += Record("height");
        var undoRedo = fixture.Session.UndoRedoService;

        // The row writes an int to the float member: the undo and the redo write it back
        absoluteWidth.NodeValue = 64;
        Assert.Equal(64f, texture.Width);
        Assert.Contains("width", notified);
        AssertKeepsItsRange(absoluteWidth);

        notified.Clear();
        undoRedo.Undo();
        Assert.Equal(100f, texture.Width);
        Assert.Equal(["width"], notified);
        Assert.Equal(100, Convert.ToInt32(absoluteWidth.NodeValue));
        AssertKeepsItsRange(absoluteWidth);

        notified.Clear();
        undoRedo.Redo();
        Assert.Equal(64f, texture.Width);
        Assert.Equal(["width"], notified);
        Assert.Equal(64, Convert.ToInt32(absoluteWidth.NodeValue));
        AssertKeepsItsRange(absoluteWidth);

        // The height alike, through its own row
        absoluteHeight.NodeValue = 32;
        notified.Clear();
        undoRedo.Undo();
        Assert.Equal(50f, texture.Height);
        Assert.Equal(["height"], notified);
        AssertKeepsItsRange(absoluteHeight);

        PropertyChangedEventHandler Record(string name) => (_, e) =>
        {
            if (e.PropertyName == nameof(NodeViewModel.NodeValue))
                notified.Add(name);
        };
    }

    private static NodeViewModel Row(NodeViewModel root, string name)
    {
        return root.Children.DepthFirst(x => x.Children).Single(x => x.Name == name);
    }

    private static void AssertKeepsItsRange(NodeViewModel row)
    {
        // Set by the updater on the row when it builds the size: a refresh of the row alone would lose them
        var presenter = Assert.Single(row.NodePresenters);
        Assert.True(presenter.AttachedProperties.TryGetValue(NumericData.MinimumKey, out _));
        Assert.True(presenter.AttachedProperties.TryGetValue(NumericData.DecimalPlacesKey, out _));
    }
}
