// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Lights;

namespace Stride.Assets.Entities
{
    public abstract class SceneBaseFactory : AssetFactory<SceneAsset>
    {
        public const string SkyboxEntityName = "Skybox";
        public const string CameraEntityName = "Camera";
        public const string SunEntityName = "Directional light";

        protected static SceneAsset CreateBase(float skyIntensity, float sunIntensity)
        {
            // Create the skybox
            var skyboxEntity = new Entity(SkyboxEntityName)
            {
                new BackgroundComponent { Intensity = skyIntensity },
            };
            skyboxEntity.Transform.Position = new Vector3(0.0f, 2.0f, -2.0f);

            // Create default camera
            var cameraEntity = new Entity(CameraEntityName) { new CameraComponent { Projection = CameraProjectionMode.Perspective } };
            cameraEntity.Transform.Position = new Vector3(2.6f, 0.6f, -1.0f);
            cameraEntity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(0)) * Quaternion.RotationY(MathUtil.DegreesToRadians(112.0f));

            // Create default light (with shadows)
            var lightEntity = new Entity(SunEntityName) { new LightComponent
            {
                Intensity = sunIntensity,
                Type = new LightDirectional
                {
                    Shadow =
                    {
                        Enabled = true,
                        Size = LightShadowMapSize.Large,
                        Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                    }
                }
            } };
            lightEntity.Transform.Position = new Vector3(0, 2.0f, 0);
            lightEntity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30.0f)) * Quaternion.RotationY(MathUtil.DegreesToRadians(-180.0f));

            var sceneAsset = new SceneAsset();

            sceneAsset.Hierarchy.Parts.Add(new EntityDesign(cameraEntity));
            sceneAsset.Hierarchy.RootParts.Add(cameraEntity);

            sceneAsset.Hierarchy.Parts.Add(new EntityDesign(lightEntity));
            sceneAsset.Hierarchy.RootParts.Add(lightEntity);

            sceneAsset.Hierarchy.Parts.Add(new EntityDesign(skyboxEntity));
            sceneAsset.Hierarchy.RootParts.Add(skyboxEntity);

            return sceneAsset;
        }
    }

    public class SceneLDRFactory : SceneBaseFactory
    {
        private const string AmbientEntityName = "Ambient light";
        private const float SkyIntensity = 1.0f;
        private const float AmbientIntensity = 0.1f;
        private const float SuntIntensity = 1.0f;

        public static SceneAsset Create()
        {
            var sceneAsset = CreateBase(SkyIntensity, SuntIntensity);

            // Add an ambient light
            var ambientLight = new Entity(AmbientEntityName)
                {
                    new LightComponent
                    {
                        Intensity = AmbientIntensity,
                        Type = new LightAmbient { Color = new ColorRgbProvider(Color.FromBgra(0xA5C9F0)) }
                    }
                };
            ambientLight.Transform.Position = new Vector3(-2.0f, 2.0f, 0.0f);

            sceneAsset.Hierarchy.Parts.Add(new EntityDesign(ambientLight));
            sceneAsset.Hierarchy.RootParts.Add(ambientLight);

            return sceneAsset;
        }

        public override SceneAsset New()
        {
            return Create();
        }
    }

    public class SceneHDRFactory : SceneBaseFactory
    {
        private const float SkyIntensity = 1.0f;
        private const float SunIntensity = 20.0f;

        public static SceneAsset Create()
        {
            var sceneAsset = CreateBase(SkyIntensity, SunIntensity);

            // Add a sky light to the scene
            var skyboxEntity = sceneAsset.Hierarchy.Parts.Select(x => x.Value.Entity).Single(x => x.Name == SkyboxEntityName);
            skyboxEntity.Add(new LightComponent
            {
                Intensity = 1.0f,
                Type = new LightSkybox(),
            });

            return sceneAsset;
        }

        public override SceneAsset New()
        {
            return Create();
        }
    }
}
