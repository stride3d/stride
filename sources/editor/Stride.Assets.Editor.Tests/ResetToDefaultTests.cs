// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Presentation.Quantum.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Colors;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// A property of the grid goes back to its default value: the value a new instance of its owner has, or the one its
/// <c>[DefaultValue]</c> declares. The grid says which properties differ from it, and one revert is one undo step.
/// </summary>
public sealed class ResetToDefaultTests
{
    [Fact]
    public async Task AValueMemberDiffersFromItsDefaultAndGoesBackToIt()
    {
        var texture = new TextureAsset { Width = 50, IsCompressed = false };
        await using var fixture = await OpenAsync(("Texture", texture));
        texture = (TextureAsset)fixture.Asset("Texture").Asset;
        var root = await fixture.PropertiesOf(fixture.Asset("Texture"));
        var steps = CountSteps(fixture);

        var width = Find(root, nameof(TextureAsset.Width));
        var height = Find(root, nameof(TextureAsset.Height));
        Assert.True(Differs(width));
        Assert.False(Differs(height));
        Assert.True(Differs(Find(root, nameof(TextureAsset.IsCompressed))));

        Revert(width);
        Assert.Equal(100.0f, texture.Width);
        Assert.False(Differs(width));
        Assert.Equal(1, steps());
        // A property at its default has nothing to revert
        Assert.False(width.GetCommand(ResetToDefaultCommand.CommandName)!.CanExecute(null));

        fixture.Session.UndoRedoService.Undo();
        Assert.Equal(50.0f, texture.Width);
        Assert.True(Differs(width));
    }

    [Fact]
    public async Task AnEnumOfANestedObjectGoesBackToItsDeclaredDefault()
    {
        var texture = new TextureAsset { Type = new ColorTextureType { Alpha = AlphaFormat.Interpolated, ColorKeyColor = Color.Red } };
        await using var fixture = await OpenAsync(("Texture", texture));
        texture = (TextureAsset)fixture.Asset("Texture").Asset;
        var root = await fixture.PropertiesOf(fixture.Asset("Texture"));

        // The object itself has the type a new texture has: its members tell what differs
        var type = Find(root, nameof(TextureAsset.Type));
        Assert.False(Differs(type));
        var alpha = Find(type, nameof(ColorTextureType.Alpha));
        Assert.True(Differs(alpha));

        Revert(alpha);
        var colorType = Assert.IsType<ColorTextureType>(texture.Type);
        Assert.Equal(AlphaFormat.Auto, colorType.Alpha);
        // The other member keeps its value
        Assert.Equal(Color.Red, colorType.ColorKeyColor);
        Assert.True(Differs(Find(type, nameof(ColorTextureType.ColorKeyColor))));
    }

    [Fact]
    public async Task ANestedObjectOfAnotherTypeGoesBackToANewObjectOfTheDefaultType()
    {
        var texture = new TextureAsset { Type = new GrayscaleTextureType() };
        await using var fixture = await OpenAsync(("Texture", texture));
        texture = (TextureAsset)fixture.Asset("Texture").Asset;
        var root = await fixture.PropertiesOf(fixture.Asset("Texture"));
        var steps = CountSteps(fixture);

        var type = Find(root, nameof(TextureAsset.Type));
        Assert.True(Differs(type));

        Revert(type);
        Assert.IsType<ColorTextureType>(texture.Type);
        Assert.False(Differs(Find(root, nameof(TextureAsset.Type))));
        Assert.Equal(1, steps());

        fixture.Session.UndoRedoService.Undo();
        Assert.IsType<GrayscaleTextureType>(texture.Type);
    }

    [Fact]
    public async Task AnAssetReferenceGoesBackToNoReference()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, "Skeleton") };
        await using var fixture = await OpenAsync(("Skeleton", skeleton), ("Model", model));
        model = (ModelAsset)fixture.Asset("Model").Asset;
        var root = await fixture.PropertiesOf(fixture.Asset("Model"));

        var reference = Find(root, nameof(ModelAsset.Skeleton));
        Assert.True(Differs(reference));

        Revert(reference);
        Assert.Null(model.Skeleton);
        Assert.False(Differs(reference));
    }

    [Fact]
    public async Task ACollectionAndTheNameHaveNoDefault()
    {
        var model = new ModelAsset();
        model.Materials.Add(new ModelMaterial { Name = "Material" });
        await using var fixture = await OpenAsync(("Model", model));
        var root = await fixture.PropertiesOf(fixture.Asset("Model"));

        var materials = Find(root, nameof(ModelAsset.Materials));
        Assert.Null(materials.GetAssociatedData(DefaultValueData.DiffersFromDefault));
        Assert.Null(materials.GetCommand(ResetToDefaultCommand.CommandName));
        // The items of the collection are objects whose members have defaults; the name of an object has none
        var material = materials.Children.Single();
        Assert.Null(Find(material, nameof(ModelMaterial.Name)).GetCommand(ResetToDefaultCommand.CommandName));
    }

    [Fact]
    public async Task APropertyWithABaseGoesBackToItsBaseValueOnly()
    {
        var baseTexture = new TextureAsset { Width = 50 };
        var derived = (TextureAsset)baseTexture.CreateDerivedAsset("Base");
        derived.Height = 20;
        await using var fixture = await OpenAsync(("Base", baseTexture), ("Derived", derived));
        var root = await fixture.PropertiesOf(fixture.Asset("Derived"));

        var width = Find(root, nameof(TextureAsset.Width));
        var height = Find(root, nameof(TextureAsset.Height));
        Assert.True(((AssetNodeViewModel)width).HasBase);
        Assert.Null(width.GetAssociatedData(DefaultValueData.DiffersFromDefault));
        Assert.Null(height.GetAssociatedData(DefaultValueData.DiffersFromDefault));
        Assert.Null(height.GetCommand(ResetToDefaultCommand.CommandName));
        Assert.NotNull(height.GetCommand(ResetOverrideCommand.CommandName));

        // The base asset has none either: its derived assets don't make it a derived asset
        var baseRoot = await fixture.PropertiesOf(fixture.Asset("Base"));
        Assert.True(Differs(Find(baseRoot, nameof(TextureAsset.Width))));
    }

    [Fact]
    public async Task AMemberWithADeclaredDefaultAndTheInlinedColorOfALight()
    {
        await using var fixture = await OpenSceneWithAsync(
            new Entity("Light") { new LightComponent { Intensity = 3, Type = new LightPoint { Color = new ColorRgbProvider(Color.Red) } } },
            new Entity("Model") { new ModelComponent { IsShadowCaster = false } });
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var light = await PropertiesOfAsync(fixture, EntityHierarchyEditorTests.FindEntity(editor, "Light"));
            var lightComponent = ComponentOf<LightComponent>(light);
            var intensity = Find(lightComponent, nameof(LightComponent.Intensity));
            Assert.True(Differs(intensity));
            Revert(intensity);
            Assert.Equal(1.0f, (float)intensity.NodeValue!);

            // The light type differs from the type of a new light (directional), so the type reverts as a whole
            var type = Find(lightComponent, nameof(LightComponent.Type));
            Assert.True(Differs(type));

            // The color of a point light shows its color inline: the row of the color differs when the inlined color does
            var color = Find(type, nameof(LightPoint.Color));
            Assert.True(Differs(color));
            Revert(color);
            var pointLight = Assert.IsType<LightPoint>(type.NodeValue);
            Assert.Equal(new Color3(1, 1, 1), Assert.IsType<ColorRgbProvider>(pointLight.Color).Value);
            Assert.False(Differs(Find(type, nameof(LightPoint.Color))));

            // [DefaultValue(true)]
            var model = await PropertiesOfAsync(fixture, EntityHierarchyEditorTests.FindEntity(editor, "Model"));
            var shadowCaster = Find(ComponentOf<ModelComponent>(model), nameof(ModelComponent.IsShadowCaster));
            Assert.True(Differs(shadowCaster));
            Revert(shadowCaster);
            Assert.Equal(true, shadowCaster.NodeValue);
        }
        finally
        {
            fixture.Session.Dispatcher.Invoke(editor.Destroy);
        }
    }

    [Fact]
    public async Task RevertingACombinedPropertyRevertsItOnEachObjectInOneStep()
    {
        await using var fixture = await OpenSceneWithAsync(
            new Entity("A") { new LightComponent { Intensity = 3 } },
            new Entity("B") { new LightComponent { Intensity = 1 } },
            new Entity("C") { new LightComponent { Intensity = 5 } });
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var entities = new[] { "A", "B", "C" }.Select(x => EntityHierarchyEditorTests.FindEntity(editor, x)).ToList();
            var root = await PropertiesOfAsync(fixture, [.. entities]);
            var steps = CountSteps(fixture);
            var intensity = Find(ComponentOf<LightComponent>(root), nameof(LightComponent.Intensity));
            // One of them differs
            Assert.True(Differs(intensity));

            Revert(intensity);
            Assert.All(entities, x => Assert.Equal(1.0f, x.AssetSideEntity.Get<LightComponent>()!.Intensity));
            Assert.Equal(1, steps());

            fixture.Session.UndoRedoService.Undo();
            Assert.Equal([3.0f, 1.0f, 5.0f], entities.Select(x => x.AssetSideEntity.Get<LightComponent>()!.Intensity));
        }
        finally
        {
            fixture.Session.Dispatcher.Invoke(editor.Destroy);
        }
    }

    [Fact]
    public async Task ACombinedPropertyEditedThenRevertedGoesBackOnEachObject()
    {
        await using var fixture = await OpenSceneWithAsync(
            new Entity("A") { new ModelComponent() },
            new Entity("B") { new ModelComponent() });
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var entities = new[] { "A", "B" }.Select(x => EntityHierarchyEditorTests.FindEntity(editor, x)).ToList();
            var root = await PropertiesOfAsync(fixture, [.. entities]);
            var castShadows = Find(ComponentOf<ModelComponent>(root), nameof(ModelComponent.IsShadowCaster));
            Assert.False(Differs(castShadows));

            castShadows.NodeValue = false;
            Assert.All(entities, x => Assert.False(x.AssetSideEntity.Get<ModelComponent>()!.IsShadowCaster));
            castShadows = Find(ComponentOf<ModelComponent>(root), nameof(ModelComponent.IsShadowCaster));
            Assert.True(Differs(castShadows));

            Revert(castShadows);
            Assert.All(entities, x => Assert.True(x.AssetSideEntity.Get<ModelComponent>()!.IsShadowCaster));
        }
        finally
        {
            fixture.Session.Dispatcher.Invoke(editor.Destroy);
        }
    }

    [Fact]
    public async Task WithoutTheUpdaterAndTheCommandTheGridShowsNoDefault()
    {
        // The editors that show the revert register them: the other grids (and the editors that don't) are unchanged
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Texture", new TextureAsset { Width = 50 }));
        var root = await fixture.PropertiesOf(fixture.Asset("Texture"));
        var width = Find(root, nameof(TextureAsset.Width));
        Assert.Null(width.GetAssociatedData(DefaultValueData.DiffersFromDefault));
        Assert.Null(width.GetCommand(ResetToDefaultCommand.CommandName));
    }

    internal static async DispatcherTask<AssetsSessionFixture> OpenAsync(params (string Location, Asset Asset)[] assets)
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, assets);
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        Register(fixture);
        return fixture;
    }

    internal static void Register(AssetsSessionFixture fixture)
    {
        fixture.Session.AssetViewProperties.RegisterNodePresenterCommand(new ResetToDefaultCommand());
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new DefaultValueNodeUpdater());
    }

    /// <summary>
    /// A scene with the given root entities, open in a session with the revert registered.
    /// </summary>
    internal static async DispatcherTask<AssetsSessionFixture> OpenSceneWithAsync(params Entity[] entities)
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, new ExclusiveDispatcherService(), ("MainScene", new SceneAsset()));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            for (var i = 0; i < entities.Length; i++)
            {
                var design = new EntityDesign(entities[i]);
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, i);
            }
        }
        Register(fixture);
        return fixture;
    }

    internal static async Task<NodeViewModel> PropertiesOfAsync(AssetsSessionFixture fixture, params IPropertyProviderViewModel[] selection)
    {
        var graph = await fixture.Session.Dispatcher.InvokeTask(() => fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync(selection));
        return graph?.RootNode ?? throw new InvalidOperationException("No properties.");
    }

    internal static NodeViewModel ComponentOf<T>(NodeViewModel entity)
    {
        return Find(entity, nameof(Entity.Components)).Children.Single(x => x.NodeValue is T);
    }

    /// <summary>
    /// The property of the given name under <paramref name="node"/>, wherever a category puts it.
    /// </summary>
    internal static NodeViewModel Find(NodeViewModel node, string name)
    {
        return node.Children.BreadthFirst(x => x.Children).FirstOrDefault(x => x.Name == name || x.Name == name + "_")
               ?? throw new InvalidOperationException($"No property {name} under {node.Name}: " + string.Join(", ", node.Children.BreadthFirst(x => x.Children).Select(x => x.Name)));
    }

    internal static bool Differs(NodeViewModel node)
    {
        return node.GetAssociatedData(DefaultValueData.DiffersFromDefault) is true;
    }

    internal static void Revert(NodeViewModel node)
    {
        var command = node.GetCommand(ResetToDefaultCommand.CommandName) ?? throw new InvalidOperationException($"{node.Name} has no revert.");
        Assert.True(command.CanExecute(null));
        command.Execute(null);
    }

    private static Func<int> CountSteps(AssetsSessionFixture fixture)
    {
        var steps = 0;
        fixture.Session.UndoRedoService.Done += (_, _) => steps++;
        return () => steps;
    }
}
