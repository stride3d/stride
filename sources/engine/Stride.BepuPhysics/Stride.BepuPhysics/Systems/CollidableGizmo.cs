// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Systems;

[GizmoComponent(typeof(CollidableComponent), false)]
public sealed class CollidableGizmo : IEntityGizmo
{
    private bool _selected, _enabled;
    private CollidableComponent _component;
    private object? _cache;
    private List<(ModelComponent model, Matrix baseMatrix)>? _models;
    private Material? _material, _materialOnSelect;
    private IServiceRegistry _services = null!;
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
    private Scene _editorScene = null!;
    private bool _latent;

    public bool IsEnabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            if (value && _models is null)
                PrepareModels();

            _enabled = value;
            if (_models is not null)
            {
                foreach ((ModelComponent model, _) in _models)
                {
                    model.Enabled = _enabled;
                }
            }
        }
    }

    public float SizeFactor { get; set; }

    public bool IsSelected
    {
        get
        {
            return _selected;
        }
        set
        {
            _selected = value;
            if (_selected && _models is null)
                PrepareModels();

            if (_models is not null)
            {
                foreach ((ModelComponent model, _) in _models)
                {
                    model.Materials[0] = _selected ? _materialOnSelect : _material;
                    model.Enabled = _selected || _enabled; // We need to account for both when the gizmo is selected and when it is force-enabled in the gizmo settings
                }
            }
        }
    }

    public CollidableGizmo(CollidableComponent component)
    {
        _component = component;
    }

    public bool HandlesComponentId(OpaqueComponentId pickedComponentId, out Entity? selection)
    {
        if (_models is null)
        {
            selection = null;
            return false;
        }

        foreach ((ModelComponent component, _) in _models)
        {
            if (pickedComponentId.Match(component))
            {
                selection = _component.Entity;
                return true;
            }
        }
        selection = null;
        return false;
    }

    public void Initialize(IServiceRegistry services, Scene editorScene)
    {
        _services = services;
        _editorScene = editorScene;
    }

    private void PrepareModels()
    {
        var bepuShapeCacheSys = _services.GetOrCreate<ShapeCacheSystem>();
        var graphicsDevice = _services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

        if (_component.Collider is MeshCollider meshCollider && (meshCollider.Model.Meshes.Count == 0 || meshCollider.Model == null!/*May be null in editor*/))
        {
            // It looks like meshes take some time before being filled in by the editor ... ?
            // Schedule it for later
            _latent = true;
            return;
        }

        var output = new List<BasicMeshBuffers>();
        Span<ShapeTransform> transforms = stackalloc ShapeTransform[_component.Collider.Transforms];
        _component.Collider.AppendModel(output, bepuShapeCacheSys, out _cache);
        _component.Collider.GetLocalTransforms(_component, transforms);

        _models = new();
        for (int i = 0; i < transforms.Length; i++)
        {
            var meshBuffer = output[i];
            if (meshBuffer.Vertices.Length == 0)
                continue;

            #warning we should cache those buffers through the cache system ... for meshes collider we could just get the actual mesh buffer
            var vertexBuffer = Buffer.Vertex.New(graphicsDevice, meshBuffer.Vertices);
            var indexBuffer = Buffer.Index.New(graphicsDevice, meshBuffer.Indices);
            var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, meshBuffer.Vertices[0].GetLayout(), vertexBuffer.ElementCount);
            var indexBufferBinding = new IndexBufferBinding(indexBuffer, true, indexBuffer.ElementCount);

            _material ??= Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(new Color4(0.25f,0.75f,0.25f,0.05f).ToColorSpace(graphicsDevice.ColorSpace)))
                    {
                        UseAlpha = true
                    },
                    Transparency = new MaterialTransparencyBlendFeature()
                },
            });
            _materialOnSelect ??= Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(new Color4(0.25f,0.75f,0.25f,0.5f).ToColorSpace(graphicsDevice.ColorSpace)))
                    {
                        UseAlpha = true
                    },
                    Transparency = new MaterialTransparencyBlendFeature()
                },
            });

            var model = new ModelComponent
            {
                Model = new Model
                {
                    (_selected ? _materialOnSelect : _material),
                    new Mesh
                    {
                        Draw = new MeshDraw
                        {
                            StartLocation = 0,
                            PrimitiveType = PrimitiveType.TriangleList,
                            VertexBuffers = new[] { vertexBufferBinding },
                            IndexBuffer = indexBufferBinding,
                            DrawCount = indexBuffer.ElementCount,
                        }
                    }
                },
                RenderGroup = IEntityGizmo.PickingRenderGroup,
                Enabled = _selected || _enabled
            };

            var entity = new Entity($"{nameof(CollidableGizmo)} for {_component.Entity.Name}"){ model };
            Matrix.Transformation(ref transforms[i].Scale, ref transforms[i].RotationLocal, ref transforms[i].PositionLocal, out var matrix);
            entity.Transform.UseTRS = false;
            entity.Scene = _editorScene;

            vertexBuffer.DisposeBy(entity);
            indexBuffer.DisposeBy(entity);
            _models.Add((model, matrix));
        }

        Update(); // Ensure positions are up-to-date
        _component.OnFeaturesUpdated += OnFeaturesUpdated;
    }

    void OnFeaturesUpdated(CollidableComponent _)
    {
        Dispose();
        _latent = true;
    }

    public void Dispose()
    {
        if (_models is null)
            return;

        _component.OnFeaturesUpdated = (_component.OnFeaturesUpdated - OnFeaturesUpdated)!;
        foreach ((ModelComponent comp, _) in _models)
        {
            comp.Entity.Scene = null;
            comp.Entity.Dispose();
        }
    }

    public void Update()
    {
        if (_latent)
        {
            _latent = false;
            PrepareModels();
        }

        if (_models is null)
            return;

        Matrix matrix;
        _component.Entity.Transform.WorldMatrix.Decompose(out _, out matrix, out var translation);
        matrix.TranslationVector = translation;

        foreach ((ModelComponent comp, Matrix baseMatrix) in _models)
        {
            comp.Entity.Transform.LocalMatrix = baseMatrix * matrix;
        }
    }
}
