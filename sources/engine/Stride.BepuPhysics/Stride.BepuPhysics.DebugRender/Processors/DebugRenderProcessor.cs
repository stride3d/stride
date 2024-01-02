using BepuPhysics;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.DebugRender.Components;
using Stride.BepuPhysics.DebugRender.Effects;
using Stride.BepuPhysics.DebugRender.Effects.RenderFeatures;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.DebugRender.Processors
{
    public class DebugRenderProcessor : EntityProcessor<DebugRenderComponent>
    {
        private BepuConfiguration _bepuConfiguration = new();
        private IGame? _game = null;
        private SinglePassWireframeRenderFeature? _wireframeRenderFeature;
        private List<WireFrameRenderObject> _wireFrameRenderObject = new();

        public DebugRenderProcessor()
        {
            Order = 10000;
        }
        protected override void OnSystemAdd()
        {
            var configService = Services.GetService<IGameSettingsService>();
            _bepuConfiguration = configService.Settings.Configurations.Get<BepuConfiguration>();
            _game = Services.GetService<IGame>();
            _wireframeRenderFeature = _game.GameSystems.OfType<SceneSystem>().First().GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault();//We should add the RenderFeature if missing
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DebugRenderComponent component, [NotNull] DebugRenderComponent data)
        {
           
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DebugRenderComponent component, [NotNull] DebugRenderComponent data)
        {

        }

        //internal void UpdateDebugRender()
        //{
        //    if (_visibilityGroup == null)
        //        return;

        //    Vector3 location;
        //    Quaternion rotation;

        //    if (_isStatic)
        //    {
        //        var a = ((StaticContainerComponent)_containerComponent).GetPhysicStatic();
        //        if (a == null)
        //            return;
        //        location = a.Value.Pose.Position.ToStrideVector();
        //        rotation = a.Value.Pose.Orientation.ToStrideQuaternion();

        //    }
        //    else
        //    {
        //        var a = ((BodyContainerComponent)_containerComponent).GetPhysicBody();
        //        if (a == null)
        //            return;
        //        location = a.Value.Pose.Position.ToStrideVector();
        //        rotation = a.Value.Pose.Orientation.ToStrideQuaternion();
        //    }

        //    //var containerMatrix = Matrix.AffineTransformation(1f, Quaternion.Identity, new Vector3(0,10,0));
        //    var containerMatrix = Matrix.AffineTransformation(1f, rotation, location);

        //    for (int i = 0; i < _wireFrameRenderObject.Count; i++)
        //    {
        //        _wireFrameRenderObject[i].WorldMatrix = containerMatrix;
        //    }
        //}
        //private void RebuildDebugRender()
        //{
        //    if (_visibilityGroup == null)
        //        return;

        //    var shapes = _containerComponent.GetShapeData();

        //    if (_wireFrameRenderObject.Count != shapes.Count)
        //    {
        //        DestroyDebugRender();

        //        for (int i = 0; i < shapes.Count; i++)
        //        {
        //            _wireFrameRenderObject.Add(new());
        //            _visibilityGroup.RenderObjects.Add(_wireFrameRenderObject[i]);
        //        }
        //    }

        //    for (int i = 0; i < _wireFrameRenderObject.Count; i++)
        //    {
        //        var vertextData = new VertexPositionNormalTexture[shapes[i].Points.Count];

        //        for (int ii = 0; ii < shapes[i].Points.Count; ii++)
        //        {
        //            vertextData[ii] = new(shapes[i].Points[ii], shapes[i].Normals[ii], Vector2.Zero);
        //        }

        //        _wireFrameRenderObject[i].Prepare(_game.GraphicsDevice, shapes[i].Indices.ToArray(), vertextData);
        //        _wireFrameRenderObject[i].Color = Color.Red;
        //        _wireFrameRenderObject[i].RenderGroup = RenderGroup.Group1;
        //    }
        //    UpdateDebugRender();
        //}
        //private void DestroyDebugRender()
        //{
        //    if (_visibilityGroup == null)
        //        return;

        //    for (int i = 0; i < _wireFrameRenderObject.Count; i++)
        //    {
        //        _visibilityGroup.RenderObjects.Remove(_wireFrameRenderObject[i]);
        //        _wireFrameRenderObject[i].VertexBuffer.Dispose();
        //        _wireFrameRenderObject[i].IndiceBuffer.Dispose();
        //    }
        //    _wireFrameRenderObject.Clear();
        //}

        public override void Update(GameTime time)
        {
           
        }
    }
}
