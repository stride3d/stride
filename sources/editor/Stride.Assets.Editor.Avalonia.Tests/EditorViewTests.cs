// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class EditorViewTests
{
    [AvaloniaFact]
    public void TheViewPluginRegistersTheEditorViews()
    {
        var views = PluginRegistrations.Of(new StrideEditorViewPlugin(), typeof(IAssetEditorView)).AssetEditorViewTypes;

        Assert.Equal(typeof(SpriteSheetEditorView), views[typeof(SpriteSheetEditorViewModel)]);
        Assert.Equal(typeof(GraphicsCompositorEditorView), views[typeof(GraphicsCompositorEditorViewModel)]);
        Assert.Equal(typeof(ScriptSourceFileEditorView), views[typeof(ScriptSourceFileEditorViewModel)]);
        Assert.Equal(typeof(VisualScriptEditorView), views[typeof(VisualScriptEditorViewModel)]);
        Assert.Equal(typeof(EntityHierarchyEditorView), views[typeof(EntityHierarchyEditorViewModel)]);
        Assert.All(views.Values, x => Assert.True(typeof(IAssetEditorView).IsAssignableFrom(x)));
    }

    [AvaloniaTheory]
    [InlineData(typeof(SpriteSheetEditorView))]
    [InlineData(typeof(GraphicsCompositorEditorView))]
    [InlineData(typeof(ScriptSourceFileEditorView))]
    [InlineData(typeof(VisualScriptEditorView))]
    [InlineData(typeof(EntityHierarchyEditorView))]
    public void TheEditorViewsLoadTheirMarkup(Type viewType)
    {
        var view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(viewType));
        var window = new Window { Content = view };

        window.Show();

        Assert.NotNull(view.Content);
    }

    [AvaloniaFact]
    public void TheSelectorBuildsNothingWithoutASession()
    {
        var selector = new EditorViewSelector();

        Assert.Null(selector.Session);
        Assert.Null(selector.Build(null));
        Assert.Null(selector.Build(new object()));
        Assert.False(selector.Match(null));
        Assert.False(selector.Match("not an editor"));
    }
}
