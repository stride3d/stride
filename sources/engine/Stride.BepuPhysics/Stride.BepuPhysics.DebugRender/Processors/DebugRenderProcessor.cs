using BepuPhysics;
using Stride.BepuPhysics._2D.Components.Containers;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.DebugRender.Components;
using Stride.BepuPhysics.DebugRender.Effects;
using Stride.BepuPhysics.DebugRender.Effects.RenderFeatures;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

namespace Stride.BepuPhysics.DebugRender.Processors
{
    public class DebugRenderProcessor : EntityProcessor<DebugRenderComponent>
    {
        public SynchronizationMode Mode { get; set; } = SynchronizationMode.Physics; // Setting it to Physics by default to show when there is a large discrepancy between the entity and physics

        private IGame _game = null!;
        private SceneSystem _sceneSystem = null!;
        private BepuShapeCacheSystem _bepuShapeCacheSystem = null!;
        private InputManager _input = null!;
        private SinglePassWireframeRenderFeature _wireframeRenderFeature = null!;
        private VisibilityGroup _visibilityGroup = null!;
        private Dictionary<ContainerComponent, WireFrameRenderObject[]> _wireFrameRenderObject = new();

        private bool _alwaysOn = false;
        private bool _isOn = false;

        public DebugRenderProcessor()
        {
            Order = BepuOrderHelper.ORDER_OF_DEBUG_P;
        }

        protected override void OnSystemAdd()
        {
            BepuServicesHelper.LoadBepuServices(Services);
            _game = Services.GetService<IGame>();
            _sceneSystem = Services.GetService<SceneSystem>();
            _bepuShapeCacheSystem = Services.GetService<BepuShapeCacheSystem>();
            _input = Services.GetService<InputManager>();

            if (_sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault() is { } wireframeFeature)
            {
                _wireframeRenderFeature = wireframeFeature;
            }
            else
            {
                _wireframeRenderFeature = new();
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(_wireframeRenderFeature);
            }

            _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DebugRenderComponent component, [NotNull] DebugRenderComponent data)
        {
            _alwaysOn = component.AlwaysRender;
            component.SetFunc = (e) => _alwaysOn = e;
            base.OnEntityComponentAdding(entity, component, data);
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            bool shouldBeOn = _alwaysOn || _input.IsKeyDown(Keys.F10);
            if (_isOn != shouldBeOn) // Changed state
            {
                if (_sceneSystem.SceneInstance.GetProcessor<ContainerProcessor>() is { } proc)
                {
                    if (shouldBeOn)
                    {
                        _isOn = true;
                        proc.OnPostAdd += StartTrackingContainer;
                        proc.OnPreRemove += ClearTrackingForContainer;
                        StartTracking(proc);
                    }
                    else
                    {
                        _isOn = false;
                        proc.OnPostAdd -= StartTrackingContainer;
                        proc.OnPreRemove -= ClearTrackingForContainer;
                        Clear();
                    }
                }
                else
                {
                    _isOn = false;
                    Clear();
                }
            }

            if (_isOn) // Update gizmos transform
            {
                foreach (var kvp in _wireFrameRenderObject)
                {
                    var container = kvp.Key;
                    Matrix matrix;
                    switch (Mode)
                    {
                        case SynchronizationMode.Physics:
                            RigidPose pose;
                            if (container.ContainerData?.StaticReference is { } sRef)
                                pose = sRef.Pose;
                            else if (container.ContainerData?.BodyReference is {} bRef)
                                pose = bRef.Pose;
                            else
                                continue;

                            var worldPosition = pose.Position.ToStrideVector();
                            var worldRotation = pose.Orientation.ToStrideQuaternion();
                            var scale = Vector3.One;
                            worldPosition -= Vector3.Transform(container.CenterOfMass, worldRotation);
                            Matrix.Transformation(ref scale, ref worldRotation, ref worldPosition, out matrix);
                            break;
                        case SynchronizationMode.Entity:
                            // We don't need to call UpdateWorldMatrix before reading WorldMatrix as we're running after the TransformProcessor operated,
                            // and we don't expect or care if other processors affect the transform afterwards
                            container.Entity.Transform.WorldMatrix.Decompose(out _, out matrix, out var translation);
                            matrix.TranslationVector = translation;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(Mode));
                    }

                    foreach (var wireframe in kvp.Value)
                    {
                        wireframe.WorldMatrix = wireframe.ContainerBaseMatrix * matrix;
                        wireframe.Color = GetCurrentColor(container);
                    }
                }
            }

            if (_input.IsKeyPressed(Keys.F11))
                _alwaysOn = !_alwaysOn;
        }

        private void StartTracking(ContainerProcessor proc)
        {
            var shapeAndOffsets = new List<(BodyShapeData data, BodyShapeTransform transform)>();
            for (var containers = proc.ComponentDataEnumerator; containers.MoveNext();)
            {
                StartTrackingContainer(containers.Current.Key, shapeAndOffsets);
            }
        }

        private void StartTrackingContainer(ContainerComponent container) => StartTrackingContainer(container, new());

        private void StartTrackingContainer(ContainerComponent container, List<(BodyShapeData data, BodyShapeTransform transform)> shapeAndOffsets)
        {
            shapeAndOffsets.Clear();
            _bepuShapeCacheSystem.AppendCachedShapesFor(container, shapeAndOffsets);

            WireFrameRenderObject[] wireframes = new WireFrameRenderObject[shapeAndOffsets.Count];
            for (int i = 0; i < shapeAndOffsets.Count; i++)
            {
                var shapeAndOffset = shapeAndOffsets[i];
                var local = shapeAndOffset;

                var wireframe = WireFrameRenderObject.New(_game.GraphicsDevice, shapeAndOffset.data.Indices, shapeAndOffset.data.Vertices);
                wireframe.Color = GetCurrentColor(container);
                Matrix.Transformation(ref local.transform.Scale, ref local.transform.RotationLocal, ref local.transform.PositionLocal, out wireframe.ContainerBaseMatrix);
                wireframes[i] = wireframe;
                _visibilityGroup.RenderObjects.Add(wireframe);
            }
            _wireFrameRenderObject.Add(container, wireframes);
        }
        static int Vector3ToRGBA(Vector3 rgb)
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
            int rgba = (255 << 24) | (b << 16) | (g << 8) | r;

            return rgba;
        }
        private void ClearTrackingForContainer(ContainerComponent container)
        {
            if (_wireFrameRenderObject.Remove(container, out var wfros))
            {
                foreach (var wireframe in wfros)
                {
                    wireframe.Dispose();
                    _visibilityGroup.RenderObjects.Remove(wireframe);
                }
            }
        }

        private void Clear()
        {
            foreach (var kvp in _wireFrameRenderObject)
            {
                foreach (var wireframe in kvp.Value)
                {
                    wireframe.Dispose();
                    _visibilityGroup.RenderObjects.Remove(wireframe);
                }
            }
            _wireFrameRenderObject.Clear();
        }

        public enum SynchronizationMode
        {
            /// <summary> Read from the physics engine, ignore any changes made to the entity </summary>
            /// <remarks> Ensures that users can see when their entities/shapes are not synchronized with physics </remarks>
            Physics,
            /// <summary> Read from the entity, showing any changes that affected it after physics </summary>
            Entity
        }

        private Color GetCurrentColor(ContainerComponent container)
        {
            var color = new Vector3(0, 0, 0);

#warning replace with I2DContainer
            if (container is _2DBodyContainerComponent)
            {
                color += new Vector3(0, 0, 1);
            }
            else
            {
                color += new Vector3(0, 0, 0);
            }

            if (container is IContainerWithColliders)
            {
                color += new Vector3(1, 0, 0);
            }
            else if (container is IContainerWithMesh)
            {
                color += new Vector3(0, 1, 0);
            }

            if (container is IBodyContainer bodyC && !bodyC.Awake)
            {
                color /= new Vector3(4);
            }
            return Color.FromRgba(Vector3ToRGBA(color));
        }

    }
}
