// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Rendering;
using Stride.Editor.Build;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Perspective camera", "Camera")]
    public class PerspectiveCameraEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(30, "Camera");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var slotId = default(SceneCameraSlotId);
            var gameSettings = parent.Asset.ServiceProvider.Get<GameSettingsProviderService>().CurrentGameSettings;
            if (gameSettings != null)
            {
                var assetFinder = (IAssetFinder)parent.Asset.Session;
                var compositor = assetFinder.FindAssetFromProxyObject(gameSettings.GraphicsCompositor)?.Asset as GraphicsCompositorAsset;
                slotId = compositor?.Cameras.FirstOrDefault()?.ToSlotId() ?? default(SceneCameraSlotId);
            }
            var name = ComputeNewName(parent, "Camera");
            var component = new CameraComponent
            {
                Projection = CameraProjectionMode.Perspective,
                Slot = slotId,
            };
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "Orthographic camera", "Camera")]
    public class OrthographicCameraEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Camera");
            var component = new CameraComponent
            {
                Projection = CameraProjectionMode.Orthographic,
            };
            return CreateEntityWithComponent(name, component);
        }
    }
}
