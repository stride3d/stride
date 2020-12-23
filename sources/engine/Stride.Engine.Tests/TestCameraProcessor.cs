// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Tests for <see cref="TransformComponent"/>.
    /// </summary>
    public class TestCameraProcessor
    {
        private CustomEntityManager entityManager;
        private GraphicsCompositor graphicsCompositor;
        private RenderContext context;

        public TestCameraProcessor()
        {
            var services = new ServiceRegistry();

            // Create entity manager and camera
            entityManager = new CustomEntityManager(services);

            // Create graphics compositor
            graphicsCompositor = new GraphicsCompositor();
            var graphicsDevice = GraphicsDevice.New(DeviceCreationFlags.Debug);
            services.AddService<IGraphicsDeviceService>(new GraphicsDeviceServiceLocal(graphicsDevice));
            services.AddService(new EffectSystem(services));
            services.AddService(new GraphicsContext(graphicsDevice));
            context = RenderContext.GetShared(services);
            context.PushTagAndRestore(GraphicsCompositor.Current, graphicsCompositor);
        }

        private CameraComponent AddCamera(bool enabled, SceneCameraSlotId slot)
        {
            var camera = new CameraComponent { Enabled = enabled, Slot = slot };
            entityManager.Add(new Entity { camera });
            return camera;
        }

        [Fact]
        public void EnableDisable()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(context);

            // Check if attached
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);

            // Disable camera
            camera.Enabled = false;
            cameraProcessor.Draw(context);

            // Check if detached
            Assert.Null(camera.Slot.AttachedCompositor);
            Assert.Null(graphicsCompositor.Cameras[0].Camera);

            // Enable again
            camera.Enabled = true;
            cameraProcessor.Draw(context);

            // Check if attached
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);
        }

        [Fact]
        public void MoveSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(context);

            // Check if attached to slot 0
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);
            Assert.Null(graphicsCompositor.Cameras[1].Camera);

            // Disable camera
            camera.Slot = graphicsCompositor.Cameras[1].ToSlotId();
            cameraProcessor.Draw(context);

            // Check if attached to slot 1
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[1].Camera);
            Assert.Null(graphicsCompositor.Cameras[0].Camera);
        }

        [Fact]
        public void MissingSlot()
        {
            var sceneCameraSlot = new SceneCameraSlot();

            var camera = AddCamera(true, sceneCameraSlot.ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(context);

            // Check if detached
            Assert.Null(camera.Slot.AttachedCompositor);

            graphicsCompositor.Cameras.Add(sceneCameraSlot);
            cameraProcessor.Draw(context);

            // Check if attached
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);

            // Remove and add new slot with same GUID
            graphicsCompositor.Cameras.Clear();
            cameraProcessor.Draw(context);
            graphicsCompositor.Cameras.Add(new SceneCameraSlot { Id = sceneCameraSlot.Id });
            cameraProcessor.Draw(context);

            // Check if attached
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);
        }

        [Fact]
        public void MultipleCameraEnabledSameSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            Assert.Throws<InvalidOperationException>(() =>
            {
                var camera1 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());
                var camera2 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

                // Run camera processor
                var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
                cameraProcessor.Draw(context);
            });
        }

        [Fact]
        public void MultipleCameraOneEnabledSameSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera1 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());
            var camera2 = AddCamera(false, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(context);
        }

        [Fact]
        public void ChangeGraphicsCompositor()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(context);

            // Check if attached to slot 0
            Assert.Equal(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, graphicsCompositor.Cameras[0].Camera);

            // Change graphics compositor
            var newGraphicsCompositor = new GraphicsCompositor();
            context.PushTagAndRestore(GraphicsCompositor.Current, newGraphicsCompositor);

            cameraProcessor.Draw(context);

            // Check if detached
            Assert.Null(camera.Slot.AttachedCompositor);
            Assert.Null(graphicsCompositor.Cameras[0].Camera);

            // Add slot to new graphics compositor and check if attached
            newGraphicsCompositor.Cameras.Add(new SceneCameraSlot { Id = camera.Slot.Id });

            cameraProcessor.Draw(context);

            // Check if attached to slot 0
            Assert.Equal(newGraphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.Equal(camera, newGraphicsCompositor.Cameras[0].Camera);
        }
    }
}
