// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Engine.Tests
{
    /// <summary>
    /// Tests for <see cref="TransformComponent"/>.
    /// </summary>
    [TestFixture]
    public class TestCameraProcessor
    {
        private CustomEntityManager entityManager;
        private GraphicsCompositor graphicsCompositor;
        private SceneSystem sceneSystem;

        [SetUp]
        public void Init()
        {
            var services = new ServiceRegistry();

            // Create entity manager and camera
            entityManager = new CustomEntityManager(services);

            // Create graphics compositor
            graphicsCompositor = new GraphicsCompositor();
            sceneSystem = new SceneSystem(services) { GraphicsCompositor = graphicsCompositor };
            services.AddService(sceneSystem);
        }

        private CameraComponent AddCamera(bool enabled, SceneCameraSlotId slot)
        {
            var camera = new CameraComponent { Enabled = enabled, Slot = slot };
            entityManager.Add(new Entity { camera });
            return camera;
        }

        [Test]
        public void EnableDisable()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(null);

            // Check if attached
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);

            // Disable camera
            camera.Enabled = false;
            cameraProcessor.Draw(null);

            // Check if detached
            Assert.IsNull(camera.Slot.AttachedCompositor);
            Assert.IsNull(graphicsCompositor.Cameras[0].Camera);

            // Enable again
            camera.Enabled = true;
            cameraProcessor.Draw(null);

            // Check if attached
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);
        }

        [Test]
        public void MoveSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(null);

            // Check if attached to slot 0
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);
            Assert.IsNull(graphicsCompositor.Cameras[1].Camera);

            // Disable camera
            camera.Slot = graphicsCompositor.Cameras[1].ToSlotId();
            cameraProcessor.Draw(null);

            // Check if attached to slot 1
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[1].Camera);
            Assert.IsNull(graphicsCompositor.Cameras[0].Camera);
        }

        [Test]
        public void MissingSlot()
        {
            var sceneCameraSlot = new SceneCameraSlot();

            var camera = AddCamera(true, sceneCameraSlot.ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(null);

            // Check if detached
            Assert.IsNull(camera.Slot.AttachedCompositor);

            graphicsCompositor.Cameras.Add(sceneCameraSlot);
            cameraProcessor.Draw(null);

            // Check if attached
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);

            // Remove and add new slot with same GUID
            graphicsCompositor.Cameras.Clear();
            cameraProcessor.Draw(null);
            graphicsCompositor.Cameras.Add(new SceneCameraSlot { Id = sceneCameraSlot.Id });
            cameraProcessor.Draw(null);

            // Check if attached
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);
        }

        [Test]
        public void MultipleCameraEnabledSameSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            Assert.Throws(typeof(InvalidOperationException), () =>
            {
                var camera1 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());
                var camera2 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

                // Run camera processor
                var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
                cameraProcessor.Draw(null);
            });
        }

        [Test]
        public void MultipleCameraOneEnabledSameSlot()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera1 = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());
            var camera2 = AddCamera(false, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(null);
        }

        [Test]
        public void ChangeGraphicsCompositor()
        {
            graphicsCompositor.Cameras.Add(new SceneCameraSlot());

            var camera = AddCamera(true, graphicsCompositor.Cameras[0].ToSlotId());

            // Run camera processor
            var cameraProcessor = entityManager.Processors.OfType<CameraProcessor>().Single();
            cameraProcessor.Draw(null);

            // Check if attached to slot 0
            Assert.AreEqual(graphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, graphicsCompositor.Cameras[0].Camera);

            // Change graphics compositor
            var newGraphicsCompositor = new GraphicsCompositor();
            sceneSystem.GraphicsCompositor = newGraphicsCompositor;

            cameraProcessor.Draw(null);

            // Check if detached
            Assert.IsNull(camera.Slot.AttachedCompositor);
            Assert.IsNull(graphicsCompositor.Cameras[0].Camera);

            // Add slot to new graphics compositor and check if attached
            newGraphicsCompositor.Cameras.Add(new SceneCameraSlot { Id = camera.Slot.Id });

            cameraProcessor.Draw(null);

            // Check if attached to slot 0
            Assert.AreEqual(newGraphicsCompositor, camera.Slot.AttachedCompositor);
            Assert.AreEqual(camera, newGraphicsCompositor.Cameras[0].Camera);
        }
    }
}
