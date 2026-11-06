// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Services;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;
using Stride.Particles.Components;

namespace Stride.Assets.Editor.Tests.CurveEditor;

/// <summary>
/// The "Edit curve" command of the property grid, shared by both editors: it is on the curve properties of a particle system,
/// opens the curve through the session's <see cref="ICurveEditorService"/> under a short path, and the key frames stay hidden from
/// the property grid (the curve editor edits them).
/// </summary>
public sealed class EditCurveCommandTests
{
    [Fact]
    public async Task TheCurvePropertiesOfAnUpdaterOpenInTheCurveEditorUnderTheirPath()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var service = new RecordingCurveEditorService();
        test.Fixture.ServiceProvider.RegisterService(service);
        try
        {
            var emitter = await PropertiesOfTheEmitter(test);
            var colorCurve = CurveProperty(emitter, "Updaters", 1, "SamplerMain");
            var sizeCurve = CurveProperty(emitter, "Updaters", 0, "SamplerMain");

            colorCurve.GetCommand(EditCurveCommand.CommandName)!.Execute(null);
            sizeCurve.GetCommand(EditCurveCommand.CommandName)!.Execute(null);

            Assert.Equal(2, service.Opened.Count);
            Assert.Same(test.ColorCurve, service.Opened[0].Curve);
            Assert.Equal("Color Animation > Main > Curve", service.Opened[0].Name);
            Assert.Same(test.SizeCurve, service.Opened[1].Curve);
            Assert.Equal("Size Animation > Main > Curve", service.Opened[1].Name);
        }
        finally
        {
            test.Fixture.ServiceProvider.UnregisterService(service);
        }
    }

    [Fact]
    public async Task ACurveOfTheShapeBuilderIsNamedAfterItsProperties()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var service = new RecordingCurveEditorService();
        test.Fixture.ServiceProvider.RegisterService(service);
        try
        {
            var emitter = await PropertiesOfTheEmitter(test);
            var rotation = emitter.GetChild("ShapeBuilder")!.GetChild("SamplerRotation")!.GetChild("Curve")!;

            rotation.GetCommand(EditCurveCommand.CommandName)!.Execute(null);

            var (curve, name) = Assert.Single(service.Opened);
            Assert.Same(test.RotationCurve, curve);
            Assert.Equal("Shape > Additive Rotation Animation > Curve", name);
        }
        finally
        {
            test.Fixture.ServiceProvider.UnregisterService(service);
        }
    }

    [Fact]
    public async Task TheKeyFramesOfACurveStayHiddenFromThePropertyGrid()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();

        var emitter = await PropertiesOfTheEmitter(test);
        var curve = CurveProperty(emitter, "Updaters", 0, "SamplerMain");

        var keyFrames = curve.GetChild("KeyFrames");
        Assert.True(keyFrames is null || !keyFrames.IsVisible);
        // Only curve properties have the command
        Assert.Null(curve.Parent!.GetCommand(EditCurveCommand.CommandName));
    }

    [Fact]
    public async Task OpeningACurveAddsNoEntryToTheUndoHistory()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var service = new RecordingCurveEditorService();
        test.Fixture.ServiceProvider.RegisterService(service);
        try
        {
            var emitter = await PropertiesOfTheEmitter(test);
            var history = test.UndoRedo.RetrieveAllTransactions().ToList();

            // The command runs in a transaction ("Execute EditCurve"), as every node command, but the transaction stack drops an
            // empty transaction
            CurveProperty(emitter, "Updaters", 0, "SamplerMain").GetCommand(EditCurveCommand.CommandName)!.Execute(null);

            Assert.Single(service.Opened);
            Assert.Equal(history, test.UndoRedo.RetrieveAllTransactions());
        }
        finally
        {
            test.Fixture.ServiceProvider.UnregisterService(service);
        }
    }

    [Fact]
    public async Task WithoutACurveEditorTheCommandLeavesTheCurveAsItIs()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var emitter = await PropertiesOfTheEmitter(test);
        var curve = CurveProperty(emitter, "Updaters", 0, "SamplerMain");
        var keys = test.SizeKeys();

        curve.GetCommand(EditCurveCommand.CommandName)!.Execute(null);

        Assert.Equal(keys, test.SizeKeys());
    }

    private static async Task<NodeViewModel> PropertiesOfTheEmitter(CurveEditorTestScene test)
    {
        var entity = EntityHierarchyEditorTests.FindEntity(test.SceneEditor, "Particles");
        var graph = await test.Fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([entity]);
        var components = graph!.RootNode.GetChild(nameof(Entity.Components))!;
        var component = components.Children.Single(x => x.NodeValue is ParticleSystemComponent);
        var emitters = component.GetChild(nameof(ParticleSystemComponent.ParticleSystem))!.GetChild("Emitters")!;
        return emitters.Children.Single();
    }

    private static NodeViewModel CurveProperty(NodeViewModel emitter, string collection, int index, string sampler)
    {
        var item = emitter.GetChild(collection)!.Children.ElementAt(index);
        return item.GetChild(sampler)!.GetChild("Curve")!;
    }
}
