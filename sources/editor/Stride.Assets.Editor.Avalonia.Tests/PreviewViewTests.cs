// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class PreviewViewTests
{
    [Fact]
    public void TheViewPluginRegistersThePreviewViews()
    {
        var views = new Dictionary<Type, Type>();

        new StrideEditorViewPlugin().RegisterAssetPreviewViewTypes(views);

        Assert.Equal(typeof(TexturePreviewView), views[typeof(TexturePreview)]);
        Assert.Equal(typeof(AnimationPreviewView), views[typeof(AnimationPreview)]);
        Assert.Equal(typeof(MaterialPreviewView), views[typeof(MaterialPreview)]);
        Assert.Equal(typeof(ScenePreviewView), views[typeof(ScenePreview)]);
        Assert.Equal(typeof(SkyboxPreviewView), views[typeof(SkyboxPreview)]);
        Assert.Equal(typeof(SpriteSheetPreviewView), views[typeof(SpriteSheetPreview)]);
        Assert.Equal(typeof(EntityPreviewView), views[typeof(EntityPreview)]);
        // The model-like previews share the model view
        Assert.Equal(typeof(ModelPreviewView), views[typeof(ModelPreview)]);
        Assert.Equal(typeof(ModelPreviewView), views[typeof(PrefabPreview)]);
        Assert.Equal(typeof(ModelPreviewView), views[typeof(PrefabModelPreview)]);
        Assert.Equal(typeof(ModelPreviewView), views[typeof(ProceduralModelPreview)]);
        // Both font previews share the font view
        Assert.Equal(typeof(SpriteFontPreviewView), views[typeof(SpriteFontPreview)]);
        Assert.Equal(typeof(SpriteFontPreviewView), views[typeof(PrecompiledSpriteFontPreview)]);
        // Any sprite batch preview without a view of its own gets the generic one
        Assert.Equal(typeof(SpritePreviewView), views[typeof(PreviewFromSpriteBatch<>)]);
    }

    [Fact]
    public void TheDefaultPreviewViewIsTheStrideOne()
    {
        Assert.Equal(typeof(StridePreviewView), AssetPreview.DefaultViewType);
    }

    [AvaloniaTheory]
    [InlineData(typeof(TexturePreviewView))]
    [InlineData(typeof(ModelPreviewView))]
    [InlineData(typeof(SpritePreviewView))]
    [InlineData(typeof(AnimationPreviewView))]
    public void ThePreviewViewsShareTheStrideTemplate(Type viewType)
    {
        var view = Assert.IsAssignableFrom<StridePreviewView>(Activator.CreateInstance(viewType));
        var window = new Window { Content = view };

        window.Show();

        Assert.Single(view.GetVisualDescendants().OfType<ContentPresenter>(), x => x.Name == StridePreviewView.PartStrideView);
    }

    [AvaloniaFact]
    public void TheScenePreviewViewShowsAMessageInPlaceOfTheGameView()
    {
        var view = new ScenePreviewView();
        var window = new Window { Content = view };

        window.Show();

        Assert.DoesNotContain(view.GetVisualDescendants().OfType<ContentPresenter>(), x => x.Name == StridePreviewView.PartStrideView);
        var message = Assert.Single(view.GetVisualDescendants().OfType<TextBlock>());
        Assert.Equal("Scenes can't be previewed. To see the scene, open it in the scene editor.", message.Text);
    }
}
