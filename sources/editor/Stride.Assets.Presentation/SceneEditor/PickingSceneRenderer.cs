// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.SceneEditor
{
    internal struct PickingObjectInfo
    {
        private const float MaxMaterialIndex = 1024;
        private const float MaxInstancingId = 1024;

        public float ModelComponentId;
        public float MeshMaterialIndex;

        public PickingObjectInfo(int componentId, int meshIndex, int materialIndex)
        {
            ModelComponentId = componentId;
            MeshMaterialIndex = meshIndex + (Math.Min(materialIndex, MaxMaterialIndex - 1) / MaxMaterialIndex); // Pack to: MeshIndex.MaterialIndex
        }

        public EntityPickingResult GetResult(Dictionary<int, Entity> idToEntity)
        {
            var mcFraction = ModelComponentId - Math.Floor(ModelComponentId);
            var mcIntegral = ModelComponentId - mcFraction;

            var mmiFraction = MeshMaterialIndex - Math.Floor(MeshMaterialIndex);
            var mmiIntegral = MeshMaterialIndex - mmiFraction;

            var result = new EntityPickingResult
            {
                ComponentId = (int)Math.Round(mcIntegral),
                InstanceId = (int)Math.Round(mcFraction * MaxInstancingId),
                MeshNodeIndex = (int)Math.Round(mmiIntegral),
                MaterialIndex = (int)Math.Round(mmiFraction * MaxMaterialIndex),
            };
            idToEntity.TryGetValue(result.ComponentId, out result.Entity);
            return result;
        }
    }

    public sealed class PickingSceneRenderer : SceneRendererBase
    {
        private const int PickingTargetSize = 512;

        private PickingObjectInfo pickingResult;
        private readonly Dictionary<int, Entity> idToEntity = new Dictionary<int, Entity>();
        private Texture pickingTexture;

        [DataMemberIgnore]
        public RenderStage PickingRenderStage { get; set; }

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            // Fill RenderStage formats
            PickingRenderStage.Output = new RenderOutputDescription(PixelFormat.R32G32_Float, PixelFormat.D32_Float);
            PickingRenderStage.Output.ScissorTestEnable = true;

            context.RenderView.RenderStages.Add(PickingRenderStage);
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            if (pickingTexture == null)
            {
                // TODO: Release resources!
                pickingTexture = Texture.New2D(drawContext.GraphicsDevice, 1, 1, PickingRenderStage.Output.RenderTargetFormat0, TextureFlags.None, 1, GraphicsResourceUsage.Staging);
            }
            var inputManager = context.Services.GetSafeServiceAs<InputManager>();

            // Skip rendering if mouse position is the same
            var mousePosition = inputManager.MousePosition;

            // TODO: Use RenderFrame
            var pickingRenderTarget = PushScopedResource(context.Allocator.GetTemporaryTexture2D(PickingTargetSize, PickingTargetSize, PickingRenderStage.Output.RenderTargetFormat0));
            var pickingDepthStencil = PushScopedResource(context.Allocator.GetTemporaryTexture2D(PickingTargetSize, PickingTargetSize, PickingRenderStage.Output.DepthStencilFormat, TextureFlags.DepthStencil));

            var renderTargetSize = new Vector2(pickingRenderTarget.Width, pickingRenderTarget.Height);
            var positionInTexture = Vector2.Modulate(renderTargetSize, mousePosition);
            int x = Math.Max(0, Math.Min((int)renderTargetSize.X - 2, (int)positionInTexture.X));
            int y = Math.Max(0, Math.Min((int)renderTargetSize.Y - 2, (int)positionInTexture.Y));

            // Render the picking stage using the current view
            using (drawContext.PushRenderTargetsAndRestore())
            {
                drawContext.CommandList.Clear(pickingRenderTarget, Color.Transparent);
                drawContext.CommandList.Clear(pickingDepthStencil, DepthStencilClearOptions.DepthBuffer);

                drawContext.CommandList.SetRenderTargetAndViewport(pickingDepthStencil, pickingRenderTarget);
                drawContext.CommandList.SetScissorRectangle(new Rectangle(x, y, 1, 1));
                context.RenderSystem.Draw(drawContext, context.RenderView, PickingRenderStage);
                drawContext.CommandList.SetScissorRectangle(new Rectangle());
            }

            // Copy results to 1x1 target
            drawContext.CommandList.CopyRegion(pickingRenderTarget, 0, new ResourceRegion(x, y, 0, x + 1, y + 1, 1), pickingTexture, 0);

            // Get data
            var data = new PickingObjectInfo[1];
            pickingTexture.GetData(drawContext.CommandList, data);
            pickingResult = data[0];
        }

        /// <summary>
        /// Cache identifier of all components of the specified <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity which components to cache.</param>
        /// <param name="isRecursive"><c>true</c> if the components of child entities should also be cached, recursively; otherwise, <c>false</c>.</param>
        public void CacheEntity([NotNull] Entity entity, bool isRecursive)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            foreach (var component in entity.Components)
            {
                idToEntity[RuntimeIdHelper.ToRuntimeId(component)] = component.Entity;
            }

            if (!isRecursive)
                return;

            foreach (var component in entity.GetChildren().BreadthFirst(x => x.GetChildren()).SelectMany(e => e.Components))
            {
                idToEntity[RuntimeIdHelper.ToRuntimeId(component)] = component.Entity;
            }
        }

        /// <summary>
        /// Cache identifier of the component specified />. 
        /// </summary>
        /// <param name="component">The component to cache</param>
        public void CacheEntityComponent([NotNull] EntityComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            idToEntity[RuntimeIdHelper.ToRuntimeId(component)] = component.Entity;
        }

        /// <summary>
        /// Uncache identifier of all components of the specified <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity which components to uncache.</param>
        /// <param name="isRecursive"><c>true</c> if the components of child entities should also be uncached recursively; otherwise, <c>false</c>.</param>
        public void UncacheEntity([NotNull] Entity entity, bool isRecursive)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            foreach (var component in entity.Components)
            {
                idToEntity.Remove(RuntimeIdHelper.ToRuntimeId(component));
            }

            if (!isRecursive)
                return;

            foreach (var component in entity.GetChildren().BreadthFirst(x => x.GetChildren()).SelectMany(e => e.Components))
            {
                idToEntity.Remove(RuntimeIdHelper.ToRuntimeId(component));
            }
        }

        /// <summary>
        /// Uncache identifier of the component specified />. 
        /// </summary>
        /// <param name="component">The component to uncache</param>
        public void UncacheEntityComponent([NotNull] EntityComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            idToEntity.Remove(RuntimeIdHelper.ToRuntimeId(component));
        }

        /// <summary>
        /// Cache identifier of all components of all entities of the specified <paramref name="scene"/>.
        /// </summary>
        /// <param name="scene">The scene which entity components to cache.</param>
        /// <param name="isRecursive"><c>true</c> if the components of all entities in child scenes should also be cached, recursively; otherwise, <c>false</c>.</param>
        public void CacheScene([NotNull] Scene scene, bool isRecursive)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            foreach (var entity in scene.Entities)
            {
                CacheEntity(entity, true);
            }

            if (!isRecursive)
                return;

            foreach (var entity in scene.Children.BreadthFirst(s => s.Children).SelectMany(s => s.Entities))
            {
                CacheEntity(entity, true);
            }
        }

        /// <summary>
        /// Uncache identifier of all components of all entities of the specified <paramref name="scene"/>.
        /// </summary>
        /// <param name="scene">The scene which entity components to uncache.</param>
        /// <param name="isRecursive"><c>true</c> if the components of all entities in child scenes should also be uncached, recursively; otherwise, <c>false</c>.</param>
        public void UncacheScene([NotNull] Scene scene, bool isRecursive)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            foreach (var entity in scene.Entities)
            {
                UncacheEntity(entity, true);
            }

            if (!isRecursive)
                return;

            foreach (var entity in scene.Children.BreadthFirst(s => s.Children).SelectMany(s => s.Entities))
            {
                UncacheEntity(entity, true);
            }
        }

        /// <summary>
        /// Gets the entity at the provided screen position
        /// </summary>
        /// <returns></returns>
        public EntityPickingResult Pick()
        {
            return pickingResult.GetResult(idToEntity);
        }
    }
}
