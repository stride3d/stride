// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Debug.Effects;
using Stride.BepuPhysics.Debug.Effects.RenderFeatures;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

namespace Stride.BepuPhysics.Debug;

public class DebugRenderProcessor : EntityProcessor<DebugRenderComponent>
{
    public SynchronizationMode Mode { get; set; } = SynchronizationMode.Physics; // Setting it to Physics by default to show when there is a large discrepancy between the entity and physics

    private bool _latent;
    private bool _visible;
    private bool _trackingShapes;
    private DebugRenderComponent? _component;
    private IGame _game = null!;
    private SceneSystem _sceneSystem = null!;
    private ShapeCacheSystem _shapeCacheSystem = null!;
    private VisibilityGroup _visibilityGroup = null!;
    private BepuConfiguration? _bepuConfiguration;
    private readonly LineRenderObject _contactLines = new();
    private readonly Dictionary<BepuSimulation, ContactRecorder> _contactRecorders = new();
    private readonly Dictionary<CollidableComponent, (WireFrameRenderObject[] Wireframes, object? cache)> _wireFrameRenderObject = new();

    public DebugRenderProcessor()
    {
        Order = SystemsOrderHelper.ORDER_OF_DEBUG_P;
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>() is { } proc && _visibilityGroup is not null)
            {
                if (_visible == value)
                    return;

                _visible = value;
                if (_visible)
                    _visibilityGroup.RenderObjects.Add(_contactLines);
                else
                    Clear();
                UpdateShapeTracking(proc);
            }
            else
            {
                _visible = false;
                _trackingShapes = false; // No processor left to unsubscribe from
                Clear();
            }
        }
    }

    private bool ShowShapes => _component?.ShowShapes ?? true;

    /// <summary> Wireframes are only built and updated while they are shown </summary>
    private void UpdateShapeTracking(CollidableProcessor proc)
    {
        var track = _visible && ShowShapes;
        if (track == _trackingShapes)
            return;

        _trackingShapes = track;
        if (track)
        {
            proc.OnPostAdd += StartTrackingCollidable;
            proc.OnPreRemove += ClearTrackingForCollidable;
            StartTracking(proc);
        }
        else
        {
            proc.OnPostAdd -= StartTrackingCollidable;
            proc.OnPreRemove -= ClearTrackingForCollidable;
            ClearShapes();
        }
    }

    protected override void OnEntityComponentAdding(Entity entity, DebugRenderComponent component, DebugRenderComponent data)
    {
        base.OnEntityComponentAdding(entity, component, data);
        if (_sceneSystem?.SceneInstance?.GetProcessor<CollidableProcessor>() is not null && _visibilityGroup is not null)
            Visible = component.Visible;
        else if (component.Visible)
            _latent = true;

        _component = component;
        component._processor = this;
    }

    protected override void OnEntityComponentRemoved(Entity entity, DebugRenderComponent component, DebugRenderComponent data)
    {
        base.OnEntityComponentRemoved(entity, component, data);
        component._processor = null;
        if (_component != component)
            return;

        // Another component takes over, with none left nothing could toggle the debug render off anymore
        _component = null;
        foreach (var other in ComponentDatas.Keys)
        {
            if (other != component)
            {
                _component = other;
                break;
            }
        }

        if (_component is null)
        {
            _latent = false;
            Visible = false;
        }
    }

    protected override void OnSystemAdd()
    {
        _shapeCacheSystem = Services.GetOrCreate<ShapeCacheSystem>();
        _game = Services.GetSafeServiceAs<IGame>();
        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
    }

    protected override void OnSystemRemove()
    {
        Clear();
        _contactLines.Dispose();
    }

    public override void Draw(RenderContext context)
    {
        if (_visibilityGroup is null)
        {
            if (_sceneSystem.SceneInstance.VisibilityGroups.Count == 0)
                return;

            _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
            if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault() is not { } wireframeFeature)
            {
                wireframeFeature = new SinglePassWireframeRenderFeature();
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(wireframeFeature);
            }
            AddOverlayStageSelector(wireframeFeature, "StrideSinglePassWireframeShader");
            if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<LineRenderFeature>().FirstOrDefault() is not { } lineFeature)
            {
                lineFeature = new LineRenderFeature();
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(lineFeature);
            }
            AddOverlayStageSelector(lineFeature, "StrideDebugLineShader");
        }

        if (_latent)
        {
            Visible = true;
            if (Visible)
                _latent = false;
        }

        base.Draw(context);

        if (_visible && _sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>() is { } collidables)
            UpdateShapeTracking(collidables);

        foreach (var (collidable, (wireframes, cache)) in _wireFrameRenderObject)
        {
            Matrix matrix;
            switch (Mode)
            {
                case SynchronizationMode.Physics:
                    if (collidable.Pose is { } pose)
                    {
                        var worldPosition = pose.Position.ToStride();
                        var worldRotation = pose.Orientation.ToStride();
                        var scale = Vector3.One;
                        worldPosition -= Vector3.Transform(collidable.CenterOfMass, worldRotation);
                        Matrix.Transformation(ref scale, ref worldRotation, ref worldPosition, out matrix);
                    }
                    else
                    {
                        continue;
                    }
                    break;
                case SynchronizationMode.Entity:
                    // We don't need to call UpdateWorldMatrix before reading WorldMatrix as we're running after the TransformProcessor operated,
                    // and we don't expect or care if other processors affect the transform afterwards
                    collidable.Entity.Transform.WorldMatrix.Decompose(out _, out matrix, out var translation);
                    matrix.TranslationVector = translation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode));
            }

            foreach (var wireframe in wireframes)
            {
                wireframe.WorldMatrix = wireframe.CollidableBaseMatrix * matrix;
                wireframe.Color = GetCurrentColor(collidable);
            }
        }

        _contactLines.Clear();
        // GetService, not GetOrCreate: showing contacts must not add a Bepu configuration to the game settings
        _bepuConfiguration ??= Services.GetService<BepuConfiguration>();
        if (_visible && _component is { } options && (options.ShowContactPoints || options.ShowContactNormals) && _bepuConfiguration is not null)
        {
            var pointColor = options.ShowContactPoints ? Color.Red : (Color?)null;
            var normalColor = options.ShowContactNormals ? Color.Yellow : (Color?)null;
            foreach (var simulation in _bepuConfiguration.BepuSimulations)
            {
                if (!_contactRecorders.TryGetValue(simulation, out var recorder))
                    _contactRecorders.Add(simulation, recorder = new ContactRecorder(simulation.Simulation));
                recorder.AddLines(_contactLines, options.ContactSize, pointColor, normalColor);
            }
        }
        else
        {
            StopRecordingContacts();
        }
    }

    /// <summary> Draws the feature in the main view's transparent stage, or its opaque one; never in a shadow map or G-buffer stage </summary>
    private void AddOverlayStageSelector(RootRenderFeature feature, string effectName)
    {
        if (feature.RenderStageSelectors.Count > 0)
            return;

        var mainStages = _sceneSystem.GraphicsCompositor.RenderStages.Where(s => s.EffectSlotName == "Main").ToList();
        if ((mainStages.FirstOrDefault(s => s.Name == "Transparent") ?? mainStages.FirstOrDefault()) is { } stage)
            feature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector { RenderStage = stage, EffectName = effectName });
    }

    private void StopRecordingContacts()
    {
        foreach (var recorder in _contactRecorders.Values)
            recorder.Dispose();
        _contactRecorders.Clear();
    }

    private void StartTracking(CollidableProcessor proc)
    {
        var shapeAndOffsets = new List<BasicMeshBuffers>();
        for (var collidables = proc.ComponentDataEnumerator; collidables.MoveNext();)
        {
            StartTrackingCollidable(collidables.Current.Key, shapeAndOffsets);
        }
    }

    private void StartTrackingCollidable(CollidableComponent collidable) => StartTrackingCollidable(collidable, new());

    private void StartTrackingCollidable(CollidableComponent collidable, List<BasicMeshBuffers> shapeData)
    {
        shapeData.Clear();

        collidable.OnFeaturesUpdated += CollidableUpdate;

        collidable.Collider.AppendModel(shapeData, _shapeCacheSystem, out var cache);

        Span<ShapeTransform> transforms = stackalloc ShapeTransform[collidable.Collider.Transforms];
        collidable.Collider.GetLocalTransforms(collidable, transforms);

        WireFrameRenderObject[] wireframes = new WireFrameRenderObject[transforms.Length];
        for (int i = 0; i < shapeData.Count; i++)
        {
            var data = shapeData[i];

            var wireframe = WireFrameRenderObject.New(_game.GraphicsDevice, data.Indices, data.Vertices);
            wireframe.Color = GetCurrentColor(collidable);
            Matrix.Transformation(ref transforms[i].Scale, ref transforms[i].RotationLocal, ref transforms[i].PositionLocal, out wireframe.CollidableBaseMatrix);
            wireframes[i] = wireframe;
            _visibilityGroup.RenderObjects.Add(wireframe);
        }
        _wireFrameRenderObject.Add(collidable, (wireframes, cache)); // We have to store the cache alongside it to ensure it doesn't get discarded for future calls to GetModelCache with the same model
    }

    void CollidableUpdate(CollidableComponent collidable)
    {
        ClearTrackingForCollidable(collidable);
        StartTrackingCollidable(collidable);
    }

    private void ClearTrackingForCollidable(CollidableComponent collidable)
    {
        collidable.OnFeaturesUpdated -= CollidableUpdate;

        if (_wireFrameRenderObject.Remove(collidable, out var wfros))
        {
            foreach (var wireframe in wfros.Wireframes)
            {
                wireframe.Dispose();
                _visibilityGroup.RenderObjects.Remove(wireframe);
            }
        }
    }

    private void Clear()
    {
        ClearShapes();
        StopRecordingContacts();
        _contactLines.Clear();
        _visibilityGroup?.RenderObjects.Remove(_contactLines);
    }

    private void ClearShapes()
    {
        foreach (var (collidable, (wireframes, _)) in _wireFrameRenderObject)
        {
            collidable.OnFeaturesUpdated -= CollidableUpdate;
            foreach (var wireframe in wireframes)
            {
                wireframe.Dispose();
                _visibilityGroup.RenderObjects.Remove(wireframe);
            }
        }
        _wireFrameRenderObject.Clear();
    }

    private Color GetCurrentColor(CollidableComponent collidable)
    {
        ColorHSV hsv;
        hsv.A = 1f;
        if (collidable.Collider is MeshCollider)
        {
            hsv.H = 0.333f * 360f;
        }
        else if (collidable.Collider is CompoundCollider cc)
        {
            if (cc.IsBig)
                hsv.H = 0.6f * 360f;
            else
                hsv.H = 0.7f * 360f;
        }
        else
        {
            hsv.H = 0f;
        }

        if (collidable is BodyComponent bodyC)
        {
            hsv.S = bodyC.Awake ? 1f : 0.66f;
            hsv.V = collidable.GetType() == typeof(BodyComponent) ? 1f : 0.5f;
        }
        else
        {
            hsv.S = 0.33f;
            hsv.V = 0.5f;
        }

        return (Color)hsv.ToColor();
    }

    public enum SynchronizationMode
    {
        /// <summary> Read from the physics engine, ignore any changes made to the entity </summary>
        /// <remarks> Ensures that users can see when their entities/shapes are not synchronized with physics </remarks>
        Physics,
        /// <summary> Read from the entity, showing any changes that affected it after physics </summary>
        Entity
    }
}
