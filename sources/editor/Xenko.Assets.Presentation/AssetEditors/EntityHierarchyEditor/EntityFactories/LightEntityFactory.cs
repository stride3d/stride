// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Skyboxes;
using Xenko.Engine;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Skyboxes;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
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

    [Display(60, "Light probe", "Light")]
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
