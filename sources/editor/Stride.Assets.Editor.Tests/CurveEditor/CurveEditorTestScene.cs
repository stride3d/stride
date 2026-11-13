// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Animations;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Presentation.Services;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Updaters;

namespace Stride.Assets.Editor.Tests.CurveEditor;

/// <summary>
/// A scene with a "Particles" entity whose particle system has one emitter with a "Size Animation" updater (a float curve with
/// keys at 0, 0.5 and 1) and a "Color Animation" updater (a color curve with two keys), and a quad shape builder with a rotation
/// curve; its scene editor; and a curve editor on the session, drawn on a 600x400 area.
/// </summary>
internal sealed class CurveEditorTestScene : IAsyncDisposable
{
    public const float Width = 600;
    public const float Height = 400;

    private CurveEditorTestScene(AssetsSessionFixture fixture, SceneEditorViewModel sceneEditor, ParticleSystemComponent component)
    {
        Fixture = fixture;
        SceneEditor = sceneEditor;
        Component = component;
        Editor = new CurveEditorViewModel(fixture.ServiceProvider, fixture.Session);
    }

    public AssetsSessionFixture Fixture { get; }

    public SceneEditorViewModel SceneEditor { get; }

    public CurveEditorViewModel Editor { get; }

    /// <summary>
    /// The particle system component, on the asset side.
    /// </summary>
    public ParticleSystemComponent Component { get; }

    public ParticleEmitter Emitter => Component.ParticleSystem.Emitters[0];

    public ComputeAnimationCurveFloat SizeCurve => (ComputeAnimationCurveFloat)Emitter.Updaters.OfType<UpdaterSizeOverTime>().Single().SamplerMain.Curve;

    public ComputeAnimationCurveColor4 ColorCurve => (ComputeAnimationCurveColor4)Emitter.Updaters.OfType<UpdaterColorOverTime>().Single().SamplerMain.Curve;

    public ComputeAnimationCurveQuaternion RotationCurve => (ComputeAnimationCurveQuaternion)((ShapeBuilderQuad)Emitter.ShapeBuilder).SamplerRotation.Curve;

    public IUndoRedoService UndoRedo => Fixture.Session.UndoRedoService;

    public static async Task<CurveEditorTestScene> OpenAsync()
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, new NullDispatcherService(), ("MainScene", new SceneAsset()));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var sceneEditor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);

        var size = new ComputeAnimationCurveFloat();
        size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 0, Value = 1 });
        size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 0.5f, Value = 3 });
        size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 1, Value = 2 });
        var color = new ComputeAnimationCurveColor4();
        color.KeyFrames.Add(new AnimationKeyFrame<Color4> { Key = 0, Value = Color4.White });
        color.KeyFrames.Add(new AnimationKeyFrame<Color4> { Key = 1, Value = new Color4(1, 0, 0, 0) });
        var rotation = new ComputeAnimationCurveQuaternion();
        rotation.KeyFrames.Add(new AnimationKeyFrame<Quaternion> { Key = 0, Value = Quaternion.Identity });
        rotation.KeyFrames.Add(new AnimationKeyFrame<Quaternion> { Key = 1, Value = Quaternion.RotationZ(MathUtil.PiOverTwo) });

        var emitter = new ParticleEmitter { ShapeBuilder = new ShapeBuilderQuad { SamplerRotation = new ComputeCurveSamplerQuaternion { Curve = rotation } } };
        emitter.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = new ComputeCurveSamplerFloat { Curve = size } });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = new ComputeCurveSamplerColor4 { Curve = color } });
        var component = new ParticleSystemComponent();
        component.ParticleSystem.Emitters.Add(emitter);
        var entity = new Entity("Particles") { component };
        var design = new EntityDesign(entity);
        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
        }

        var assetSideComponent = EntityHierarchyEditorTests.FindEntity(sceneEditor, "Particles").AssetSideEntity.Get<ParticleSystemComponent>()!;
        return new CurveEditorTestScene(fixture, sceneEditor, assetSideComponent);
    }

    /// <summary>
    /// Opens the size curve as the WPF editor does ("Edit curve"): adds it and fits the view to it, then draws it.
    /// </summary>
    public IEditableCurveViewModel OpenSizeCurve()
    {
        Assert.True(Editor.AddCurve((object)SizeCurve, "Size"));
        Editor.Focus();
        Draw();
        return Assert.IsAssignableFrom<IEditableCurveViewModel>(Editor.SelectedCurve);
    }

    /// <summary>
    /// Updates and draws the curve editor, as its view does on each invalidation.
    /// </summary>
    public RecordingDrawingContext Draw()
    {
        var context = new RecordingDrawingContext();
        IDrawingModel model = Editor;
        model.Update(true);
        model.Render(context, Width, Height);
        return context;
    }

    /// <summary>
    /// The position in the drawing of the given key and value of the selected curve.
    /// </summary>
    public DrawingPoint ScreenPoint(float key, float value) => Editor.SelectedCurve!.TransformPoint(new Vector2(key, value));

    public List<string?> RecordUndoNames()
    {
        var undoRedo = UndoRedo;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        return names;
    }

    /// <summary>
    /// The keys and values of the size curve, in the order of its key frames.
    /// </summary>
    public (float Key, float Value)[] SizeKeys() => SizeCurve.KeyFrames.Select(x => (x.Key, x.Value)).ToArray();

    public async ValueTask DisposeAsync()
    {
        Editor.Destroy();
        SceneEditor.Destroy();
        await Fixture.DisposeAsync();
    }
}

/// <summary>
/// A curve editor service that records the curves it is asked to open.
/// </summary>
internal sealed class RecordingCurveEditorService : ICurveEditorService
{
    public List<(object Curve, string Name)> Opened { get; } = [];

    public int CloseCount { get; private set; }

    public void Open(object curve, string name) => Opened.Add((curve, name));

    public void Close() => CloseCount++;
}
