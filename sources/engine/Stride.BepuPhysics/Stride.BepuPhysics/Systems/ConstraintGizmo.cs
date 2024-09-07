// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.BepuPhysics.Systems;

[GizmoComponent(typeof(ConstraintComponentBase), false)]
public class ConstraintGizmo : IEntityGizmo
{
    static Dictionary<GraphicsDevice, ShapeCache> _shapeCaches = new();
    static Dictionary<GraphicsDevice, SharedData> _sharedDatas = new();

    protected ShapeCache Shapes;

    SharedData? _sharedData;

    private bool _selected, _enabled;
    private readonly ConstraintComponentBase _component;
    private List<ModelComponent> _models = new();
    private IServiceRegistry _services = null!;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
    private Scene _editorScene = null!;

    public bool IsEnabled
    {
        get { return _enabled; }
        set
        {
            if (value && _sharedData is null)
                PrepareForFirstDraw();

            _enabled = value;
            if (_sharedData is not null && value == false)
            {
                foreach (var component in _models)
                {
                    component.Enabled = false;
                    _sharedData.Pool.Enqueue(component);
                }

                _models.Clear();
            }
        }
    }

    public float SizeFactor { get; set; }

    public bool IsSelected
    {
        get { return _selected; }
        set
        {
            _selected = value;
            if (_selected && _sharedData is null)
                PrepareForFirstDraw();

            if (_sharedData is not null)
            {
                foreach (var model in _models)
                {
                    model.Materials[0] = _selected ? _sharedData.MaterialOnSelect : _sharedData.Material;
                    model.Enabled = _selected || _enabled; // We need to account for both when the gizmo is selected and when it is force-enabled in the gizmo settings
                }
            }
        }
    }

    public ConstraintGizmo(ConstraintComponentBase component)
    {
        _component = component;
        Shapes = null!;
    }

    public void Dispose()
    {
        if (_sharedData is not null)
        {
            foreach (var component in _models)
                _sharedData.Pool.Enqueue(component);

            _models.Clear();
        }
    }

    public bool HandlesComponentId(OpaqueComponentId pickedComponentId, out Entity? selection)
    {
        if (_sharedData is null)
        {
            selection = null;
            return false;
        }

        foreach (var model in _models)
        {
            if (pickedComponentId.Match(model))
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

    void PrepareForFirstDraw()
    {
        var graphicsDevice = _services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

        if (_shapeCaches.TryGetValue(graphicsDevice, out var shapes) == false)
        {
            _sharedDatas[graphicsDevice] = new(graphicsDevice);
            shapes = new(graphicsDevice);
            _shapeCaches.Add(graphicsDevice, shapes);
        }

        _sharedData = _sharedDatas[graphicsDevice];
        Shapes = shapes;

        Update(); // Ensure everything is up-to-date
    }

    void Draw(Mesh shape, Vector3 position, Quaternion rotation, Vector3? scale = null)
    {
        if (_sharedData is null)
            return;

        var scale2 = scale ?? new Vector3(SizeFactor);

        if (_sharedData.Pool.TryDequeue(out var comp))
        {
            comp.Model.Meshes[0] = shape;
            comp.Materials[0] = _selected ? _sharedData.MaterialOnSelect : _sharedData.Material;
        }
        else
        {
            var e = new Entity(nameof(ConstraintGizmo))
            {
                (comp = new()
                {
                    Model = new()
                    {
                        _selected ? _sharedData.MaterialOnSelect : _sharedData.Material,
                        shape,
                    },
                    RenderGroup = IEntityGizmo.PickingRenderGroup,
                    Enabled = _selected || _enabled
                })
            };
        }

        comp.Entity.Scene = _editorScene;
        comp.Entity.Transform.Position = position;
        comp.Entity.Transform.Rotation = rotation;
        comp.Entity.Transform.Scale = scale2;
        comp.Entity.Transform.UpdateWorldMatrix();
        comp.Enabled = true;

        _models.Add(comp);
    }

    float GetDefaultArrowRadius() => 0.15f * SizeFactor;

    void DrawArrow(Vector3 pivot, Vector3 dir, float? length = null, float? radius = null)
    {
        float radius2 = radius ?? GetDefaultArrowRadius();
        float length2 = length ?? SizeFactor;

        dir = Vector3.Normalize(dir);
        var rotation = Quaternion.BetweenDirections(Vector3.UnitY, dir);
        var cylinderSize = new Vector3(radius2 * 0.75f, length2 - radius2, radius2 * 0.5f);
        Draw(Shapes.Cylinder, pivot + dir * cylinderSize.Y * 0.5f, rotation, cylinderSize);
        Draw(Shapes.Cone, pivot + dir * (cylinderSize.Y + radius2 * 0.5f), rotation, new(radius2));
    }

    public void Update()
    {
        if (_sharedData is null || _enabled == false)
            return;

        foreach (var component in _models)
        {
            component.Enabled = false;
            _sharedData.Pool.Enqueue(component);
        }

        _models.Clear();

        foreach (var component in _component.Bodies)
        {
            if (component == null!)
                return;
        }

        switch (_component)
        {
            case AngularSwivelHingeConstraintComponent ash:
                ExtractWorld(ash.A!, out var aPos, out var aRot);
                ExtractWorld(ash.B!, out var bPos, out var bRot);
                DrawArrow(aPos, aRot * ash.LocalSwivelAxisA);
                DrawArrow(bPos, bRot * ash.LocalHingeAxisB);
                break;
            case BallSocketConstraintComponent bs:
                ExtractWorld(bs.A!, out aPos, out aRot);
                ExtractWorld(bs.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * bs.LocalOffsetA, bs.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * bs.LocalOffsetB, bs.LocalOffsetB.Length());
                break;
            case BallSocketMotorConstraintComponent bsm:
                ExtractWorld(bsm.A!, out aPos, out aRot);
                ExtractWorld(bsm.B!, out bPos, out bRot);
                var target = bPos + bRot * bsm.LocalOffsetB;
                var deltaA = target - aPos;
                var deltaB = target - bPos;
                DrawArrow(aPos, aRot * deltaA, deltaA.Length());
                DrawArrow(bPos, bRot * deltaB, deltaB.Length());
                break;
            case BallSocketServoConstraintComponent bss:
                ExtractWorld(bss.A!, out aPos, out aRot);
                ExtractWorld(bss.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * bss.LocalOffsetA, bss.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * bss.LocalOffsetB, bss.LocalOffsetB.Length());
                break;
            case CenterDistanceConstraintComponent cd:
                ExtractWorld(cd.A!, out aPos, out aRot);
                ExtractWorld(cd.B!, out bPos, out bRot);
                var delta = bPos - aPos;
                DrawArrow(aPos, delta, cd.TargetDistance);
                break;
            case CenterDistanceLimitConstraintComponent cdl:
                ExtractWorld(cdl.A!, out aPos, out aRot);
                ExtractWorld(cdl.B!, out bPos, out bRot);
                var dir = Vector3.Normalize(bPos - aPos);
                DrawArrow(aPos, dir, cdl.MinimumDistance);
                Draw(Shapes.Cone, aPos + dir * cdl.MaximumDistance, Quaternion.BetweenDirections(Vector3.UnitY, -dir), new(GetDefaultArrowRadius()));
                break;
            case DistanceLimitConstraintComponent dl:
                ExtractWorld(dl.A!, out aPos, out aRot);
                ExtractWorld(dl.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * dl.LocalOffsetA, dl.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * dl.LocalOffsetB, dl.LocalOffsetB.Length());
                var endA = aPos + aRot * dl.LocalOffsetA;
                var endB = bPos + bRot * dl.LocalOffsetB;
                dir = Vector3.Normalize(endB - endA);
                DrawArrow(endA, dir, dl.MinimumDistance);
                Draw(Shapes.Cone, endA + dir * dl.MaximumDistance, Quaternion.BetweenDirections(Vector3.UnitY, -dir), new(GetDefaultArrowRadius()));
                break;
            case DistanceServoConstraintComponent ds:
                ExtractWorld(ds.A!, out aPos, out aRot);
                ExtractWorld(ds.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * ds.LocalOffsetA, ds.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * ds.LocalOffsetB, ds.LocalOffsetB.Length());
                endA = aPos + aRot * ds.LocalOffsetA;
                endB = bPos + bRot * ds.LocalOffsetB;
                DrawArrow(endA, endB - endA, ds.TargetDistance);
                break;
            case HingeConstraintComponent h:
                ExtractWorld(h.A!, out aPos, out aRot);
                ExtractWorld(h.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * h.LocalOffsetA, h.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * h.LocalOffsetB, h.LocalOffsetB.Length());
                Draw(Shapes.Disc, aPos + aRot * h.LocalOffsetA, Quaternion.BetweenDirections(Vector3.UnitY, aRot * h.LocalHingeAxisA));
                Draw(Shapes.Disc, bPos + bRot * h.LocalOffsetB, Quaternion.BetweenDirections(Vector3.UnitY, bRot * h.LocalHingeAxisB));
                break;
            case LinearAxisServoConstraintComponent las:
                ExtractWorld(las.A!, out aPos, out aRot);
                ExtractWorld(las.B!, out bPos, out bRot);
                DrawArrow(aPos, aRot * las.LocalOffsetA, las.LocalOffsetA.Length());
                DrawArrow(bPos, bRot * las.LocalOffsetB, las.LocalOffsetB.Length());

                var worldPlaneNormal = aRot * las.LocalPlaneNormal;
                var anchorA = aPos + aRot * las.LocalOffsetA;
                var anchorB = bPos + bRot * las.LocalOffsetB;
                var planeOffset = Vector3.Dot(anchorB - anchorA, worldPlaneNormal);
                var closestPointOnPlane = anchorB - planeOffset * worldPlaneNormal;
                var targetOffset = las.TargetOffset;
                if (targetOffset < 0)
                {
                    targetOffset = -targetOffset;
                    planeOffset = -planeOffset;
                    worldPlaneNormal = -worldPlaneNormal;
                }

                var targetPoint = closestPointOnPlane + worldPlaneNormal * targetOffset;
                if (planeOffset > las.TargetOffset)
                {
                    var lA = targetPoint - closestPointOnPlane;
                    var lB = anchorB - targetPoint;
                    DrawArrow(closestPointOnPlane, lA, lA.Length());
                    DrawArrow(targetPoint, lB, lB.Length());
                }
                else
                {
                    var lA = anchorB - closestPointOnPlane;
                    var lB = targetPoint - anchorB;
                    DrawArrow(closestPointOnPlane, lA, lA.Length());
                    DrawArrow(anchorB, lB, lB.Length());
                }

                break;
            case OneBodyLinearServoConstraintComponent obls:
                ExtractWorld(obls.A!, out aPos, out aRot);
                DrawArrow(aPos, aRot * obls.LocalOffset, obls.LocalOffset.Length());
                DrawArrow(aPos + aRot * obls.LocalOffset, obls.Target, obls.Target.Length());
                break;
            case PointOnLineServoConstraintComponent pols:
                ExtractWorld(pols.A!, out aPos, out aRot);
                ExtractWorld(pols.B!, out bPos, out bRot);

                var worldOffsetA = aRot * pols.LocalOffsetA;
                var worldDirection = aRot * pols.LocalDirection;
                var worldOffsetB = bRot * pols.LocalOffsetB;

                anchorA = aPos + worldOffsetA;
                anchorB = bPos + worldOffsetB;
                var closestPointOnLine = Vector3.Dot(anchorB - anchorA, worldDirection) * worldDirection + anchorA;

                DrawArrow(aPos, anchorA - aPos, (anchorA - aPos).Length());
                DrawArrow(anchorA, closestPointOnLine - anchorA, (closestPointOnLine - anchorA).Length());
                DrawArrow(closestPointOnLine, anchorB - closestPointOnLine, (anchorB - closestPointOnLine).Length());
                DrawArrow(anchorB, bPos - anchorB, (bPos - anchorB).Length());
                break;
            case SwivelHingeConstraintComponent sh:
                ExtractWorld(sh.A!, out aPos, out aRot);
                ExtractWorld(sh.B!, out bPos, out bRot);

                var offsetA = aRot * sh.LocalOffsetA;
                var swivelAxis = aRot * sh.LocalSwivelAxisA;
                var offsetB = bRot * sh.LocalOffsetB;
                var hingeAxis = bRot * sh.LocalHingeAxisB;
                var jointAnchorA = aPos + offsetA;
                var jointAnchorB = bPos + offsetB;
                Draw(Shapes.Disc, jointAnchorA, Quaternion.BetweenDirections(Vector3.UnitY, swivelAxis));
                Draw(Shapes.Disc, jointAnchorB, Quaternion.BetweenDirections(Vector3.UnitY, hingeAxis));

                DrawArrow(aPos, offsetA);
                DrawArrow(bPos, offsetB);

                DrawArrow(jointAnchorA, (jointAnchorB - jointAnchorA), (jointAnchorB - jointAnchorA).Length());
                break;
            case WeldConstraintComponent w:
                ExtractWorld(w.A!, out aPos, out aRot);
                ExtractWorld(w.B!, out bPos, out bRot);
                var worldOffset = aRot * w.LocalOffset;
                var bTarget = aPos + worldOffset;
                DrawArrow(aPos, worldOffset, w.LocalOffset.Length());
                DrawArrow(bTarget, bPos - bTarget, (bPos - bTarget).Length());
                break;
            default:
                if (_component.Bodies.Length != 2)
                    break;

                var a = _component.Bodies[0]!.Entity.Transform.WorldMatrix.TranslationVector;
                var b = _component.Bodies[1]!.Entity.Transform.WorldMatrix.TranslationVector;
                var mid = (a + b) * 0.5f;
                delta = b - a;
                var length = delta.Length();
                dir = Vector3.Normalize(delta);
                var rot = Quaternion.BetweenDirections(Vector3.UnitZ, dir);
                Draw(Shapes.Box, mid, rot, new(0.1f * SizeFactor, 0.1f * SizeFactor, length));
                break;
        }

        static void ExtractWorld(BodyComponent comp, out Vector3 pos, out Quaternion rot)
        {
            comp.Entity.Transform.GetWorldTransformation(out pos, out rot, out _);
        }
    }


    protected class ShapeCache
    {
        public readonly Mesh Cone, Cylinder, Box, Disc, Sphere;

        public ShapeCache(GraphicsDevice graphicsDevice)
        {
            Cone = new(){ Draw = GeometricPrimitive.Cone.New(graphicsDevice).ToMeshDraw() };
            Cylinder = new(){ Draw = GeometricPrimitive.Cylinder.New(graphicsDevice).ToMeshDraw() };
            Box = new(){ Draw = GeometricPrimitive.Cube.New(graphicsDevice).ToMeshDraw() };
            Disc = new(){ Draw = GeometricPrimitive.Disc.New(graphicsDevice).ToMeshDraw() };
            Sphere = new(){ Draw = GeometricPrimitive.Sphere.New(graphicsDevice).ToMeshDraw() };
        }
    }

    class SharedData
    {
        public readonly Material Material, MaterialOnSelect;
        public readonly Queue<ModelComponent> Pool = new();

        public SharedData(GraphicsDevice graphicsDevice)
        {
            Material ??= Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(new Color4(0.75f,0.75f,0.25f).ToColorSpace(graphicsDevice.ColorSpace)))
                    {
                        UseAlpha = true
                    },
                    Transparency = new MaterialTransparencyBlendFeature()
                },
            });
            MaterialOnSelect ??= Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(new Color4(0.75f,0.75f,0.25f).ToColorSpace(graphicsDevice.ColorSpace)))
                    {
                        UseAlpha = true
                    },
                    Transparency = new MaterialTransparencyBlendFeature()
                },
            });
        }
    }
}
