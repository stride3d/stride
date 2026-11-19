// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The kind of selection the session's property grid shows: the asset controls of its header (tags, open in editor) act on the
/// selected assets, so they apply only while the grid shows assets.
/// </summary>
public sealed class PropertyGridSelectionTests
{
    [Fact]
    public async Task OnlyAnEmptySelectionOrASelectionOfAssetsIsAnAssetSelection()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var properties = fixture.Session.AssetViewProperties;
            var changes = 0;
            properties.PropertyChanged += (_, e) => changes += e.PropertyName == nameof(properties.IsAssetSelection) ? 1 : 0;
            var box = EntityHierarchyEditorTests.FindEntity(editor, "Box");
            var pluginSelection = new PluginSelection(fixture.Session.AssetNodeContainer.GetOrCreateNode(new PluginNode()));

            await ShowAsync(fixture, [editor.Asset]);
            Assert.True(properties.IsAssetSelection);

            await ShowAsync(fixture, [box]);
            Assert.False(properties.IsAssetSelection);

            await ShowAsync(fixture, [editor.Asset]);
            Assert.True(properties.IsAssetSelection);

            await ShowAsync(fixture, [pluginSelection]);
            Assert.False(properties.IsAssetSelection);

            await ShowAsync(fixture, [editor.Asset, pluginSelection]);
            Assert.False(properties.IsAssetSelection);

            await ShowAsync(fixture, []);
            Assert.True(properties.IsAssetSelection);

            Assert.True(changes >= 6, $"{changes} change notifications");
        }
        finally
        {
            fixture.Session.Dispatcher.Invoke(editor.Destroy);
        }
    }

    private static Task ShowAsync(Helpers.AssetsSessionFixture fixture, IPropertyProviderViewModel[] selection)
    {
        return fixture.Session.Dispatcher.InvokeTask(() => fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync(selection));
    }

    /// <summary>
    /// What a plugin shows in the property grid, a node of a graph for example: neither an asset nor a part of one.
    /// </summary>
    private sealed class PluginNode
    {
        public string Title { get; set; } = "Start";
    }

    private sealed class PluginSelection(IObjectNode rootNode) : IPropertyProviderViewModel
    {
        public bool CanProvidePropertiesViewModel => true;

        public IObjectNode GetRootNode() => rootNode;

        public bool ShouldConstructMember(IMemberNode member) => true;

        public bool ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }
}
