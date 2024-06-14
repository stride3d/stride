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
    private IGame _game = null!;
    private SceneSystem _sceneSystem = null!;
    private ShapeCacheSystem _shapeCacheSystem = null!;
    private VisibilityGroup _visibilityGroup = null!;
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
            if (_sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>() is { } proc)
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
        if (_sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>() is not null)
            Visible = component.Visible;
        else if (component.Visible)
            _latent = true;

        component._processor = this;
    }

    protected override void OnSystemAdd()
    {
        SinglePassWireframeRenderFeature wireframeRenderFeature;

        ServicesHelper.LoadBepuServices(Services, out _, out _shapeCacheSystem, out _);
        _game = Services.GetSafeServiceAs<IGame>();
        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();

        if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault() is { } wireframeFeature)
        {
            wireframeRenderFeature = wireframeFeature;
        }
        else
        {
            wireframeRenderFeature = new();
            _sceneSystem.GraphicsCompositor.RenderFeatures.Add(wireframeRenderFeature);
        }

        _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
    }

    protected override void OnSystemRemove()
    {
        Clear();
    }

    public override void Draw(RenderContext context)
    {
        if (_latent)
        {
            Visible = true;
            if (Visible)
                _latent = false;
        }

        base.Draw(context);

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

    static int Vector3ToRGBA(Vector3 rgb, byte a = 255)
    {
        //Clamp to [0;1]
        rgb.X = Math.Clamp(rgb.X, 0f, 1f);
        rgb.Y = Math.Clamp(rgb.Y, 0f, 1f);
        rgb.Z = Math.Clamp(rgb.Z, 0f, 1f);

        // Scale RGB values to the range [0, 255]
        int r = (int)(rgb.X * 255);
        int g = (int)(rgb.Y * 255);
        int b = (int)(rgb.Z * 255);

        // Combine values into an int32 RGBA format
        //int rgba = (r << 24) | (g << 16) | (b << 8) | 255;
        int rgba = (a << 24) | (b << 16) | (g << 8) | r;

        return rgba;
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
                _visibilityGroup.RenderObjects.Remove(wireframe);
            }
        }
        _wireFrameRenderObject.Clear();
    }

    private Color GetCurrentColor(CollidableComponent collidable)
    {
        var color = new Vector3(0, 0, 0);
        byte a = 255;

        if (collidable.Collider is MeshCollider)
        {
            //color += new Vector3(0, 0, 0);
        }
        else if (collidable.Collider is CompoundCollider cc)
        {
            if (cc.IsBig)
                color += new Vector3(1f, 0, 0);
            else
                color += new Vector3(0.5f, 0, 0);
        }
        else if (collidable.Collider is EmptyCollider)
        {
            a = 128;
        }

        if (collidable is Body2DComponent)
        {
            color += new Vector3(0, 1, 0);
        }
        else
        {
            //color += new Vector3(0, 0, 0);
        }

        if (collidable is BodyComponent bodyC)
        {
            color += new Vector3(0, 0, 1);

            if (!bodyC.Awake)
                color /= 3f;
        }
        else if (collidable is StaticComponent staticC)
        {
            //color += new Vector3(0, 0, 0);
            color /= 9f;
        }

        return Color.FromRgba(Vector3ToRGBA(color, a)); //R : Mesh, G : 2D, B : body. Total : awake => 100% else 33% | Static => 11%
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
