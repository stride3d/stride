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
        private IGame? _game = null;
        private SceneSystem _sceneSystem;
        private BepuShapeCacheSystem _bepuShapeCacheSystem;
        private InputManager _input;
        private SinglePassWireframeRenderFeature _wireframeRenderFeature;
        private VisibilityGroup _visibilityGroup;
        private List<WireFrameRenderObject> _wireFrameRenderObject = new();

        private bool _alwaysOn = false;

        public DebugRenderProcessor()
        {
            Order = 10200;
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

        public override void Update(GameTime time)
        {
            if (_alwaysOn || _input.IsKeyDown(Keys.F10))
                UpdateRender();
            if (_input.IsKeyDown(Keys.F11))
                Clear();

            base.Update(time);
        }

        private void UpdateRender()
        {
            Clear();
            #warning update debug shape matrices and subscribe to append and remove of containers from the processor
            if (_sceneSystem.SceneInstance.GetProcessor<ContainerProcessor>() is { } proc)
            {
                var shapeAndOffsets = new List<(BodyShapeData data, BodyShapeTransform transform)>();
                for (var containers = proc.ComponentDatas; containers.MoveNext();)
                {
                    var containerCompo = containers.Current.Key;

                    var color = Color.Black;

                    if (containerCompo is IContainerWithColliders)
                    {
                        color = Color.Red;
                    }
                    else if (containerCompo is IContainerWithMesh)
                    {
                        color = Color.Blue;
                    }

                    shapeAndOffsets.Clear();
                    _bepuShapeCacheSystem.AppendCachedShapesFor(containerCompo, shapeAndOffsets);

                    foreach (var shapeAndOffset in shapeAndOffsets)
                    {
                        var local = shapeAndOffset;

                        Matrix.Transformation(ref local.transform.Scale, ref local.transform.RotationLocal, ref local.transform.PositionLocal, out var containerMatrix);

                        containerCompo.Entity.Transform.UpdateWorldMatrix();
                        containerMatrix *= Matrix.RotationQuaternion(containerCompo.Entity.Transform.GetWorldRot());
                        containerMatrix *= Matrix.Translation(containerCompo.Entity.Transform.GetWorldPos());

                        var wfro = WireFrameRenderObject.New(_game.GraphicsDevice, shapeAndOffset.data.Indices, shapeAndOffset.data.Vertices);
                        wfro.Color = color;
                        wfro.WorldMatrix = containerMatrix;
                        _wireFrameRenderObject.Add(wfro);
                        _visibilityGroup.RenderObjects.Add(wfro);
                    }
                }
            }
        }

        private void Clear()
        {
            for (int i = _wireFrameRenderObject.Count - 1; i >= 0; i--)
            {
                var renderObject = _wireFrameRenderObject[i];
                renderObject.Dispose();
                _visibilityGroup.RenderObjects.Remove(renderObject);
                _wireFrameRenderObject.RemoveAt(i);
            }
        }
    }
}
