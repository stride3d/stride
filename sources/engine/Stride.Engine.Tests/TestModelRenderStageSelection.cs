// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Rendering.Shadows;
using Xunit;

namespace Stride.Engine.Tests;

public class TestModelRenderStageSelection
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddingMeshesKeepsExistingStagesAndSelectsNewMeshes(bool attachFeatureLate)
    {
        using var system = new RenderSystem();
        using var visibility = new VisibilityGroup(system);
        var opaque = new RenderStage("Opaque", "Main");
        var transparent = new RenderStage("Transparent", "Main");
        var shadow = new RenderStage("Shadow", "Shadow");
        system.RenderStages.Add(opaque);
        system.RenderStages.Add(transparent);
        system.RenderStages.Add(shadow);

        var feature = new MeshRenderFeature();
        feature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
        {
            RenderGroup = RenderGroupMask.Group3,
            OpaqueRenderStage = opaque,
            TransparentRenderStage = transparent,
            EffectName = "Main",
        });
        feature.RenderStageSelectors.Add(new ShadowMapRenderStageSelector
        {
            RenderGroup = RenderGroupMask.Group3,
            ShadowMapRenderStage = shadow,
            EffectName = "Shadow",
        });
        if (!attachFeatureLate)
            system.RenderFeatures.Add(feature);

        var processor = new ModelRenderProcessor { VisibilityGroup = visibility };
        var solid = new Material { Passes = { new MaterialPass() } };
        var glass = new Material { Passes = { new MaterialPass { HasTransparency = true } } };
        var first = AddModel(solid);
        processor.Draw(null);
        if (attachFeatureLate)
            system.RenderFeatures.Add(feature);

        // Collect through the normal visibility path after a new model or changed configuration.
        visibility.TryCollect(new RenderView());
        var retained = processor.RenderModels[first].Meshes[0];
        AssertStages(retained, false, true);
        var originalStages = retained.ActiveRenderStages;

        var second = AddModel(glass);
        second.IsShadowCaster = false;
        processor.Draw(null);
        visibility.TryCollect(new RenderView());
        var added = processor.RenderModels[second].Meshes[0];
        AssertStages(added, true, false);
        Assert.Same(originalStages, retained.ActiveRenderStages);

        second.Materials[0] = solid;
        second.IsShadowCaster = true;
        processor.Draw(null);
        visibility.TryCollect(new RenderView());
        AssertStages(added, false, true);

        second.Materials[0] = glass;
        processor.Draw(null);
        visibility.TryCollect(new RenderView());
        AssertStages(added, true, true);

        second.IsShadowCaster = false;
        processor.Draw(null);
        visibility.TryCollect(new RenderView());
        AssertStages(added, true, false);

        second.Model = null;
        processor.Draw(null);
        Assert.DoesNotContain(added, visibility.RenderObjects);
        second.Model = first.Model;
        processor.Draw(null);
        visibility.TryCollect(new RenderView());
        AssertStages(processor.RenderModels[second].Meshes[0], true, false);
        AssertStages(retained, false, true);

        ModelComponent AddModel(Material material)
        {
            var model = new Model { Meshes = { new Mesh() }, Materials = { material } };
            var component = new ModelComponent(model) { RenderGroup = RenderGroup.Group3 };
            var entity = new Entity { component };
            processor.ProcessEntityComponent(entity, component, false);
            return component;
        }

        void AssertStages(RenderMesh mesh, bool isTransparent, bool castsShadow)
        {
            Assert.Equal(!isTransparent, mesh.ActiveRenderStages[opaque.Index].Active);
            Assert.Equal(isTransparent, mesh.ActiveRenderStages[transparent.Index].Active);
            Assert.Equal(castsShadow, mesh.ActiveRenderStages[shadow.Index].Active);
        }
    }
}
