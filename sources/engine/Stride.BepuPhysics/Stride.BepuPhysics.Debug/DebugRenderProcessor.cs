// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
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
    public bool ShowCollisions { get; set; } = true;

    private bool _latent;
    private bool _visible;
    private IGame _game = null!;
    private SceneSystem _sceneSystem = null!;
    private ShapeCacheSystem _shapeCacheSystem = null!;
    private VisibilityGroup _visibilityGroup = null!;
    private readonly Dictionary<CollidableComponent, (WireFrameRenderObject[] Wireframes, object? cache)> _wireFrameRenderObject = new();

    // Collision gizmo pooling:
    private readonly List<CollisionGizmo> _collisionGizmos = new();
    private int _collisionGizmosUsedThisFrame;

    private sealed class CollisionGizmo
    {
        public WireFrameRenderObject Point = null!;
        public WireFrameRenderObject Shaft = null!;
        public WireFrameRenderObject Tip = null!;

        public void SetVisible(bool visible, VisibilityGroup visibilityGroup)
        {
            if (visible && !Point.Enabled)
            {
                visibilityGroup.RenderObjects.Add(Point);
                visibilityGroup.RenderObjects.Add(Shaft);
                visibilityGroup.RenderObjects.Add(Tip);
            }
            else if (!visible && Point.Enabled)
            {
                visibilityGroup.RenderObjects.Remove(Point);
                visibilityGroup.RenderObjects.Remove(Shaft);
                visibilityGroup.RenderObjects.Remove(Tip);
            }
            Point.Enabled = visible;
            Shaft.Enabled = visible;
            Tip.Enabled = visible;
        }

        public void Dispose()
        {
            Point.Dispose();
            Shaft.Dispose();
            Tip.Dispose();
        }
    }

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
                {
                    proc.OnPostAdd += StartTrackingCollidable;
                    proc.OnPreRemove += ClearTrackingForCollidable;
                    StartTracking(proc);
                }
                else
                {
                    proc.OnPostAdd -= StartTrackingCollidable;
                    proc.OnPreRemove -= ClearTrackingForCollidable;
                    Clear();
                }
            }
            else
            {
                _visible = false;
                Clear();
            }
        }
    }

    protected override void OnEntityComponentAdding(Entity entity, DebugRenderComponent component, DebugRenderComponent data)
    {
        base.OnEntityComponentAdding(entity, component, data);
        if (_sceneSystem?.SceneInstance?.GetProcessor<CollidableProcessor>() is not null && _visibilityGroup is not null)
            Visible = component.Visible;
        else if (component.Visible)
            _latent = true;
        Mode = component.Mode;
        component._processor = this;
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
    }

    public override void Draw(RenderContext context)
    {
        if (_visibilityGroup is null)
        {
            if (_sceneSystem.SceneInstance.VisibilityGroups.Count == 0)
                return;

            _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
            if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault() is null)
            {
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(new SinglePassWireframeRenderFeature());
            }
        }

        if (_latent)
        {
            Visible = true;
            if (Visible)
                _latent = false;
        }

        base.Draw(context);

        // Collect sims that have at least one tracked collidable.
        var simulations = new List<BepuSimulation>();
        foreach (var (collidable, (wireframes, _)) in _wireFrameRenderObject)
        {
            if (collidable.Simulation != null && !simulations.Contains(collidable.Simulation))
                simulations.Add(collidable.Simulation);

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

        // Collisions rendering (pooled)
        if (!ShowCollisions)
        {
            // Ensure physics doesn't waste time collecting.
            for (int s = 0; s < simulations.Count; s++)
                simulations[s].ContactEvents.DebugCollectAllContacts = false;

            HideAllCollisionGizmos();
            return;
        }

        _collisionGizmosUsedThisFrame = 0;

        for (int s = 0; s < simulations.Count; s++)
        {
            var sim = simulations[s];
            sim.ContactEvents.DebugCollectAllContacts = true;

            var points = sim.ContactEvents.DebugPoints;
            if (points.Length == 0)
                continue;

            EnsureCollisionGizmoCapacity(_collisionGizmosUsedThisFrame + points.Length);

            for (int i = 0; i < points.Length; i++)
            {
                var p = points[i];

                // Visual parameters
                float pointRadius = 0.05f;
                float normalLength = 0.35f;
                float shaftRadius = 0.02f;
                float tipRadius = 0.035f;

                var n = p.WorldNormal;
                if (n.LengthSquared() < 1e-6f)
                    continue;

                n.Normalize();

                var start = p.WorldPoint;
                var end = start + n * normalLength;
                var mid = (start + end) * 0.5f;

                // Align cylinder Y axis to normal
                var rot = Quaternion.BetweenDirections(Vector3.UnitY, n);

                var gizmo = _collisionGizmos[_collisionGizmosUsedThisFrame++];

                // 1) contact point sphere
                {
                    gizmo.Point.Color = Color.Red;

                    var scale = new Vector3(pointRadius);
                    Matrix.Scaling(ref scale, out gizmo.Point.CollidableBaseMatrix);

                    Matrix world;
                    Matrix.Translation(ref start, out world);

                    gizmo.Point.WorldMatrix = gizmo.Point.CollidableBaseMatrix * world;
                }

                // 2) normal shaft (cylinder)
                {
                    gizmo.Shaft.Color = Color.Yellow;

                    var shaftScale = new Vector3(shaftRadius, normalLength, shaftRadius);

                    // WorldMatrix directly
                    Matrix.Transformation(ref shaftScale, ref rot, ref mid, out gizmo.Shaft.WorldMatrix);
                    gizmo.Shaft.CollidableBaseMatrix = gizmo.Shaft.WorldMatrix;
                }

                // 3) normal tip (sphere)
                {
                    gizmo.Tip.Color = Color.Yellow;

                    var tipScale = new Vector3(tipRadius);
                    Matrix.Transformation(ref tipScale, ref rot, ref end, out gizmo.Tip.WorldMatrix);
                    gizmo.Tip.CollidableBaseMatrix = gizmo.Tip.WorldMatrix;
                }

                gizmo.SetVisible(true, _visibilityGroup);
            }
        }

        // Hide unused
        for (int i = _collisionGizmosUsedThisFrame; i < _collisionGizmos.Count; i++)
            _collisionGizmos[i].SetVisible(false, _visibilityGroup);
    }

    private void StartTracking(CollidableProcessor proc)
    {
        var shapeAndOffsets = new List<BasicMeshBuffers>();
        for (var collidables = proc.ComponentDataEnumerator; collidables.MoveNext();)
            StartTrackingCollidable(collidables.Current.Key, shapeAndOffsets);
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
        // We have to store the cache alongside it to ensure it doesn't get discarded for future calls to GetModelCache with the same model
        _wireFrameRenderObject.Add(collidable, (wireframes, cache));
    }

    private void CollidableUpdate(CollidableComponent collidable)
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
        foreach (var (collidable, (wireframes, _)) in _wireFrameRenderObject)
        {
            collidable.OnFeaturesUpdated -= CollidableUpdate;
            foreach (var wireframe in wireframes)
            {
                wireframe.Dispose();
                if (_visibilityGroup is not null)
                    _visibilityGroup.RenderObjects.Remove(wireframe);
            }
        }
        _wireFrameRenderObject.Clear();

        ClearCollisionGizmos();
    }

    // ------------------------------------------------------------------------
    // Collision gizmo pool helpers
    // ------------------------------------------------------------------------
    private void HideAllCollisionGizmos()
    {
        _collisionGizmosUsedThisFrame = 0;
        for (int i = 0; i < _collisionGizmos.Count; i++)
            _collisionGizmos[i].SetVisible(false, _visibilityGroup);
    }

    private void EnsureCollisionGizmoCapacity(int required)
    {
        while (_collisionGizmos.Count < required)
            _collisionGizmos.Add(CreateCollisionGizmo());
    }

    private CollisionGizmo CreateCollisionGizmo()
    {
        var gizmo = new CollisionGizmo
        {
            Point = WireFrameRenderObject.New(
                _game.GraphicsDevice,
                _shapeCacheSystem._sphereShapeData.Indices,
                _shapeCacheSystem._sphereShapeData.Vertices),

            Shaft = WireFrameRenderObject.New(
                _game.GraphicsDevice,
                _shapeCacheSystem._cylinderShapeData.Indices,
                _shapeCacheSystem._cylinderShapeData.Vertices),

            Tip = WireFrameRenderObject.New(
                _game.GraphicsDevice,
                _shapeCacheSystem._sphereShapeData.Indices,
                _shapeCacheSystem._sphereShapeData.Vertices),
        };

        gizmo.Point.Color = Color.Red;
        gizmo.Shaft.Color = Color.Yellow;
        gizmo.Tip.Color = Color.Yellow;

        // not the best code, but it works for now
        _visibilityGroup.RenderObjects.Add(gizmo.Point);
        _visibilityGroup.RenderObjects.Add(gizmo.Shaft);
        _visibilityGroup.RenderObjects.Add(gizmo.Tip);
        gizmo.SetVisible(false, _visibilityGroup);
        return gizmo;
    }

    private void ClearCollisionGizmos()
    {
        if (_collisionGizmos.Count == 0)
            return;

        if (_visibilityGroup is not null)
        {
            for (int i = 0; i < _collisionGizmos.Count; i++)
            {
                var g = _collisionGizmos[i];
                _visibilityGroup.RenderObjects.Remove(g.Point);
                _visibilityGroup.RenderObjects.Remove(g.Shaft);
                _visibilityGroup.RenderObjects.Remove(g.Tip);
                g.Dispose();
            }
        }
        else
        {
            for (int i = 0; i < _collisionGizmos.Count; i++)
                _collisionGizmos[i].Dispose();
        }

        _collisionGizmos.Clear();
        _collisionGizmosUsedThisFrame = 0;
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
