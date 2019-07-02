// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="CameraComponent"/>.
    /// </summary>
    public class CameraProcessor : EntityProcessor<CameraComponent>
    {
        private GraphicsCompositor currentCompositor;
        private bool cameraSlotsDirty = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraProcessor"/> class.
        /// </summary>
        public CameraProcessor()
        {
            Order = -10;
        }

        public override void Draw(RenderContext context)
        {
            var graphicsCompositor = Services.GetService<SceneSystem>()?.GraphicsCompositor;

            // Monitor changes in the camera slots of the current compositor
            if (graphicsCompositor != currentCompositor)
            {
                if (currentCompositor != null)
                {
                    currentCompositor.Cameras.CollectionChanged -= OnCameraSlotsChanged;
                }
                currentCompositor = graphicsCompositor;
                if (currentCompositor != null)
                {
                    currentCompositor.Cameras.CollectionChanged += OnCameraSlotsChanged;
                }
                cameraSlotsDirty = true;
            }

            // The compositor, or at least the list of slots, has changed. Let's detach everything
            if (cameraSlotsDirty)
            {
                cameraSlotsDirty = false;
                if (currentCompositor != null)
                {
                    // If we have a current compositor, let's clear all camera that are attached to it.
                    for (var i = 0; i < currentCompositor.Cameras.Count; ++i)
                    {
                        var cameraSlot = currentCompositor.Cameras[i];
                        if (cameraSlot.Camera != null)
                        {
                            cameraSlot.Camera.Slot.AttachedCompositor = null;
                            cameraSlot.Camera = null;
                        }
                    }
                }
                // Let's also check on all cameras if they are still attached to a compositor, then let's detach them.
                foreach (var matchingCamera in ComponentDatas)
                {
                    var camera = matchingCamera.Value;
                    if (camera.Slot.AttachedCompositor != null)
                    {
                        DetachCameraFromSlot(camera);
                    }
                }
            }

            // First pass, handle proper detach when Enabled changed
            foreach (var matchingCamera in ComponentDatas)
            {
                var camera = matchingCamera.Value;
                if (graphicsCompositor != null)
                {
                    if (camera.Enabled && camera.Slot.AttachedCompositor == null)
                    {
                        // Either the slot has been changed and need to be re-attached, or the camera has just been enabled.
                        // Make sure this camera is detached from all slots, we'll re-attach it in the second pass.
                        DetachCameraFromAllSlots(camera, graphicsCompositor);
                    }
                    else if (!camera.Enabled && camera.Slot.AttachedCompositor == graphicsCompositor)
                    {
                        // The camera has been disabled and need to be detached.
                        DetachCameraFromSlot(camera);
                    }
                }
            }

            // Second pass, handle proper attach
            foreach (var matchingCamera in ComponentDatas)
            {
                var camera = matchingCamera.Value;

                if (graphicsCompositor != null)
                {
                    if (camera.Enabled && camera.Slot.AttachedCompositor == null)
                    {
                        // Attach to the new slot
                        AttachCameraToSlot(camera);
                    }
                }

                // In case the camera has a custom aspect ratio, we can update it here
                // otherwise it is screen-dependent and we can only update it in the CameraComponentRenderer.
                if (camera.Enabled && camera.UseCustomAspectRatio)
                {
                    camera.Update();
                }
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, CameraComponent component, CameraComponent data)
        {
            if (component.Slot.AttachedCompositor != null)
                DetachCameraFromSlot(component);
        }

        private void OnCameraSlotsChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            cameraSlotsDirty = true;
        }

        private void AttachCameraToSlot(CameraComponent camera)
        {
            if (!camera.Enabled) throw new InvalidOperationException($"The camera [{camera.Entity.Name}] is disabled and can't be attached");
            if (camera.Slot.AttachedCompositor != null) throw new InvalidOperationException($"The camera [{camera.Entity.Name}] is already attached");

            var graphicsCompositor = Services.GetService<SceneSystem>()?.GraphicsCompositor;
            if (graphicsCompositor != null)
            {
                for (var i = 0; i < graphicsCompositor.Cameras.Count; ++i)
                {
                    var slot = graphicsCompositor.Cameras[i];
                    if (slot.Id == camera.Slot.Id)
                    {
                        if (slot.Camera != null)
                            throw new InvalidOperationException($"Unable to attach camera [{camera.Entity.Name}] to the graphics compositor. Another camera, [{slot.Camera.Entity.Name}], is enabled and already attached to this slot.");

                        slot.Camera = camera;
                        camera.Slot.AttachedCompositor = graphicsCompositor;
                        break;
                    }
                }
            }
        }

        private static void DetachCameraFromSlot(CameraComponent camera)
        {
            if (camera.Slot.AttachedCompositor == null)
                throw new InvalidOperationException($"The camera [{camera.Entity.Name}] isn't attached");

            for (var i = 0; i < camera.Slot.AttachedCompositor.Cameras.Count; ++i)
            {
                var slot = camera.Slot.AttachedCompositor.Cameras[i];
                if (slot.Id == camera.Slot.Id)
                {
                    if (slot.Camera != camera)
                        throw new InvalidOperationException($"Can'to detach camera [{camera.Entity.Name}] from the graphics compositor. Another camera, {slot.Camera.Entity.Name}, is attached to this slot.");

                    slot.Camera = null;
                    break;
                }
            }
            camera.Slot.AttachedCompositor = null;
        }

        private static void DetachCameraFromAllSlots(CameraComponent camera, GraphicsCompositor graphicsCompositor)
        {
            for (var i = 0; i < graphicsCompositor.Cameras.Count; ++i)
            {
                var slot = graphicsCompositor.Cameras[i];
                if (slot.Camera == camera)
                {
                    slot.Camera = null;
                }
            }
            camera.Slot.AttachedCompositor = null;
        }
    }
}
