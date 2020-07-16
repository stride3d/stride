// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Skyboxes;
using Stride.Engine;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;
using Stride.Rendering.Voxels.VoxelGI;
using Stride.Rendering.Voxels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Directional light", "Light")]
    public class DirectionalLightEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(20, "Light");
        }

        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Directional light");
            var component = new LightComponent();
            var entity = await CreateEntityWithComponent(name, component);
            entity.Transform.Position = new Vector3(0, 2.0f, 0);
            entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-70));
            return entity;
        }
    }

    [Display(20, "Point light", "Light")]
    public class PointLightEntityFactory : EntityFactory
    {
        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Point light");
            var component = new LightComponent { Type = new LightPoint() };
            var entity = await CreateEntityWithComponent(name, component);
            entity.Transform.Position = new Vector3(0, 2.0f, 0);
            return entity;
        }
    }

    [Display(30, "Spot light", "Light")]
    public class SpotLightEntityFactory : EntityFactory
    {
        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Spot light");
            var component = new LightComponent { Type = new LightSpot() };
            var entity = await CreateEntityWithComponent(name, component);
            entity.Transform.Position = new Vector3(0, 2.0f, 0);
            entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-90));
            return entity;
        }
    }

    [Display(40, "Ambient light", "Light")]
    public class AmbientLightEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Ambient light");
            var component = new LightComponent { Type = new LightAmbient() };
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(50, "Skybox light", "Light")]
    public class SkyboxLightEntityFactory : EntityFactory
    {
        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var skybox = await PickupAsset(parent.Editor.Session, new[] { typeof(SkyboxAsset) });
            if (skybox == null)
                return null;
            var skyboxAsset = (SkyboxAsset)skybox.Asset;

            var name = ComputeNewName(parent, "Skybox light");
            var lightComponent = new LightComponent { Type = new LightSkybox { Skybox = ContentReferenceHelper.CreateReference<Skybox>(skybox) } };
            var skyboxComponent = new BackgroundComponent { Texture = skyboxAsset.CubeMap };
            return await CreateEntityWithComponent(name, lightComponent, skyboxComponent);
        }
    }

    [Display(60, "Voxel light", "Light")]
    public class VoxelLightEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Voxel light");
            var component = new LightComponent { Type = new LightVoxel() };
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(65, "Voxel volume", "Light")]
    public class VoxelVolumeEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Voxel volume");
            var component = new VoxelVolumeComponent { Attributes = { new VoxelAttributeEmissionOpacity() } };
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(70, "Light probe", "Light")]
    public class LightProbeEntityFactory : EntityFactory
    {
        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Light probe");
            var lightProbeComponent = new LightProbeComponent();
            return await CreateEntityWithComponent(name, lightProbeComponent);
        }
    }
}
